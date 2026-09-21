using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private struct ClearVisibilityBufferPassData
    {
        public Identifier<RGBuffer> visBuffer;
        public Handle<ComputeShader> shader;
        public uint totalPixels;
    }

    private struct VisibilityPassData
    {
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> visBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ulong indirectArgsOffset;
        public uint passIndex;
        public uint sceneBuffer;
        public uint2 screenSize;
    }

    private struct ExportVisibilityDepthPassData
    {
        public Identifier<RGBuffer> visBuffer;
        public Identifier<RGTexture> depthTexture;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
    }

    private void AddClearVisibilityBufferPass(RenderGraph rg, Identifier<RGBuffer> visBuffer, uint2 screenSize)
    {
        if (!_cullingResource.clearVisibilityBufferShader.IsValid)
        {
            return;
        }

        using var builder = rg.AddComputeRenderPass<ClearVisibilityBufferPassData>("ClearVisibilityBuffer");
        builder.UseBuffer(visBuffer, AccessFlags.Write);

        var totalPixels = screenSize.x * screenSize.y;
        builder.SetPassData(new ClearVisibilityBufferPassData
        {
            visBuffer = visBuffer,
            shader = _cullingResource.clearVisibilityBufferShader,
            totalPixels = totalPixels
        });

        builder.SetRenderFunc<ClearVisibilityBufferPassData>(static (ref readonly passData, computeCtx) =>
        {
            var visBufferUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var props = new InternalClearVisibilityBufferShaderProperties
            {
                visBufferIndex = visBufferUav,
                totalPixels = passData.totalPixels
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            var threadGroups = Math.Max(1u, (passData.totalPixels + 63) / 64);
            computeCtx.DispatchCompute(threadGroups, 1, 1);
        });
    }

    private void AddVisibilityBufferPass(RenderGraph rg, in CameraCullingBuffers buffers, uint cullPassIndex, uint sceneBuffer, uint2 screenSize, ref Identifier<RGBuffer> existingVisBuffer)
    {
        if (s_dispatchMeshCommandSignature == null)
        {
            return;
        }

        var isPass1 = cullPassIndex == 0;
        var passName = isPass1 ? "Visibility_Pass1_EarlyZ" : "Visibility_Pass2_LateZ";

        if (existingVisBuffer.IsInvalid)
        {
            var vbufferSize = (ulong)screenSize.x * screenSize.y * 8UL;
            var vbufferDesc = new BufferDesc
            {
                Size = (uint)vbufferSize,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            };
            existingVisBuffer = rg.CreateBuffer(in vbufferDesc, "VisibilityBuffer");
            AddClearVisibilityBufferPass(rg, existingVisBuffer, screenSize);
        }

        using var builder = rg.AddUnsafeRenderPass<VisibilityPassData>(passName);

        var visibleBuffer = isPass1 ? buffers.visibleMeshletsPass1 : buffers.visibleMeshletsPass2;
        builder.UseBuffer(visibleBuffer, AccessFlags.Read);
        builder.UseRandomAccessBuffer(existingVisBuffer);
        builder.UseBuffer(buffers.indirectArgsBuffer, AccessFlags.Read);

        builder.SetPassData(new VisibilityPassData
        {
            visibleMeshlets = visibleBuffer,
            visBuffer = existingVisBuffer,
            indirectArgsBuffer = buffers.indirectArgsBuffer,
            indirectArgsOffset = isPass1 ? CullConstants.INDIRECT_OFFSET_PASS1_VISIBLE : CullConstants.INDIRECT_OFFSET_PASS2_VISIBLE,
            passIndex = cullPassIndex,
            sceneBuffer = sceneBuffer,
            screenSize = screenSize
        });

        builder.SetRenderFunc<VisibilityPassData>(static (ref readonly passData, unsafeCtx) =>
        {
            unsafeCtx.SetViewport(new ViewportDesc
            {
                X = 0,
                Y = 0,
                Width = passData.screenSize.x,
                Height = passData.screenSize.y,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            });
            unsafeCtx.SetScissorRect(new ScissorRectDesc
            {
                Left = 0,
                Top = 0,
                Right = passData.screenSize.x,
                Bottom = passData.screenSize.y
            });

            var actualIndirectBuf = unsafeCtx.GetActualBuffer(passData.indirectArgsBuffer);
            var visibleBufferIndex = unsafeCtx.ResourceDatabase.GetBindlessIndex(unsafeCtx.GetActualBuffer(passData.visibleMeshlets).AsResource(), BindlessAccess.ShaderResource);
            var visBufferUav = unsafeCtx.ResourceDatabase.GetBindlessIndex(unsafeCtx.GetActualBuffer(passData.visBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            if (s_shaderVariants == null)
            {
                return;
            }

            var dispatchVariants = s_shaderVariants.GetDispatchVariants(PassSemantic.Visibility);
            for (var i = 0; i < dispatchVariants.Length; i++)
            {
                ref readonly var variant = ref dispatchVariants[i];
                if (variant.Shader.IsValid &&
                    unsafeCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.Visibility))
                {
                    unsafeCtx.SetUserData(
                        userData0: visibleBufferIndex,
                        userData1: visBufferUav,
                        userData2: (uint)variant.DenseIndex,
                        userData3: passData.passIndex,
                        target: DataTarget.Graphics);

                    unsafeCtx.ExecuteIndirect(s_dispatchMeshCommandSignature, 1, actualIndirectBuf, passData.indirectArgsOffset);
                }
            }
        });
    }

    private void AddExportVisibilityDepthPass(RenderGraph rg, Identifier<RGBuffer> visBuffer, uint2 screenSize, ref Identifier<RGTexture> existingDepth)
    {
        if (!_cullingResource.exportVisibilityDepthShader.IsValid)
        {
            return;
        }

        if (existingDepth.IsInvalid)
        {
            var depthDesc = RGTextureDesc.Relative(
                1.0f,
                TextureFormat.R32_Float,
                usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
                clearAtFirstUse: false);
            existingDepth = rg.CreateTexture(in depthDesc, "SceneDepthBuffer");
        }

        using var builder = rg.AddComputeRenderPass<ExportVisibilityDepthPassData>("ExportVisibilityDepth");
        builder.UseBuffer(visBuffer, AccessFlags.Read);
        builder.UseTexture(existingDepth, AccessFlags.Write);

        builder.SetPassData(new ExportVisibilityDepthPassData
        {
            visBuffer = visBuffer,
            depthTexture = existingDepth,
            shader = _cullingResource.exportVisibilityDepthShader,
            renderSize = screenSize
        });

        builder.SetRenderFunc<ExportVisibilityDepthPassData>(static (ref readonly passData, computeCtx) =>
        {
            var visBufferIndex = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visBuffer).AsResource(), BindlessAccess.ShaderResource);
            var depthUavIndex = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.depthTexture).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalExportVisibilityDepthShaderProperties
            {
                visBufferIndex = visBufferIndex,
                depthTextureUav = depthUavIndex,
                renderWidth = passData.renderSize.x,
                renderHeight = passData.renderSize.y
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);

            var threadGroupsX = Math.Max(1u, (passData.renderSize.x + 15) / 16);
            var threadGroupsY = Math.Max(1u, (passData.renderSize.y + 15) / 16);
            computeCtx.DispatchCompute(threadGroupsX, threadGroupsY, 1);
        });
    }
}
