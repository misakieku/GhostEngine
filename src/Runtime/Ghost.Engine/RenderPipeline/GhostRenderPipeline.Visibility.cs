using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
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
        public Identifier<RGBuffer> binOffsetsBuffer;
        public Identifier<RGBuffer> visBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ulong indirectArgsOffset;
        public uint passIndex;
        public uint sceneBuffer;
        public uint2 screenSize;
        public ShaderVariantRegistry variantRegistry;
        public ICommandSignature commandSignature;
    }

    private struct ExportVisibilityDepthPassData
    {
        public Identifier<RGBuffer> visBuffer;
        public Identifier<RGTexture> depthTexture;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
    }

    internal ICommandSignature _dispatchMeshCommandSignature = null!;

    private void InitializeVisibility(RenderEngine renderEngine, AssetManager assetManager)
    {
        var indirectDesc = new CommandSignatureDesc
        {
            Stride = 12,
            Arguments = new IndirectArgumentDesc[]
            {
                new() { Type = IndirectArgumentType.DispatchMesh }
            }
        };
        _dispatchMeshCommandSignature = renderEngine.GraphicsEngine.CreateCommandSignature(in indirectDesc, default);
    }

    private void AddClearVisibilityBufferPass(RenderGraph rg, Identifier<RGBuffer> visBuffer, uint totalPixels)
    {
        using var builder = rg.AddComputeRenderPass<ClearVisibilityBufferPassData>("ClearVisibilityBuffer");
        builder.UseBuffer(visBuffer, AccessFlags.Write);

        builder.SetPassData(new ClearVisibilityBufferPassData
        {
            visBuffer = visBuffer,
            shader = _materialPipelineResource.clearVisibilityBufferShader,
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

    private void AddVisibilityBufferPass(RenderGraph rg, Identifier<RGBuffer> visibleMeshlets, Identifier<RGBuffer> binOffsetsBuffer, Identifier<RGBuffer> indirectArgsBuffer, uint cullPassIndex, uint sceneBuffer, uint2 screenSize, ref Identifier<RGBuffer> existingVisBuffer)
    {
        var isPass1 = cullPassIndex == 0;
        var passName = isPass1 ? "Visibility_Pass1_EarlyZ" : "Visibility_Pass2_LateZ";

        if (existingVisBuffer.IsInvalid)
        {
            var tilesX = (screenSize.x + 7u) / 8u;
            var tilesY = (screenSize.y + 7u) / 8u;
            var totalAllocatedPixels = tilesX * tilesY * 64u;
            var vbufferSize = totalAllocatedPixels * 8UL;
            var vbufferDesc = new BufferDesc
            {
                Size = (uint)vbufferSize,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            };
            existingVisBuffer = rg.CreateBuffer(in vbufferDesc, "VisibilityBuffer");
            AddClearVisibilityBufferPass(rg, existingVisBuffer, totalAllocatedPixels);
        }

        using var builder = rg.AddUnsafeRenderPass<VisibilityPassData>(passName);

        builder.UseBuffer(visibleMeshlets, AccessFlags.Read);
        builder.UseBuffer(binOffsetsBuffer, AccessFlags.Read);
        builder.UseRandomAccessBuffer(existingVisBuffer);
        builder.UseBuffer(indirectArgsBuffer, AccessFlags.Read);

        builder.SetPassData(new VisibilityPassData
        {
            visibleMeshlets = visibleMeshlets,
            binOffsetsBuffer = binOffsetsBuffer,
            visBuffer = existingVisBuffer,
            indirectArgsBuffer = indirectArgsBuffer,
            indirectArgsOffset = isPass1 ? CullConstants.INDIRECT_OFFSET_PASS1_VARIANTS : CullConstants.INDIRECT_OFFSET_PASS2_VARIANTS,
            passIndex = cullPassIndex,
            sceneBuffer = sceneBuffer,
            screenSize = screenSize,
            variantRegistry = _assetManager.ShaderVariants,
            commandSignature = _dispatchMeshCommandSignature
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
            var binOffsetsIndex = unsafeCtx.ResourceDatabase.GetBindlessIndex(unsafeCtx.GetActualBuffer(passData.binOffsetsBuffer).AsResource(), BindlessAccess.ShaderResource);
            var visBufferUav = unsafeCtx.ResourceDatabase.GetBindlessIndex(unsafeCtx.GetActualBuffer(passData.visBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.Visibility);
            for (var i = 0; i < dispatchVariants.Length; i++)
            {
                ref readonly var variant = ref dispatchVariants[i];
                if (variant.Shader.IsValid &&
                    unsafeCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.Visibility))
                {
                    var v = (uint)variant.DenseIndex;
                    unsafeCtx.SetUserData(
                        userData0: visibleBufferIndex,
                        userData1: visBufferUav,
                        userData2: binOffsetsIndex,
                        userData3: (v << 1) | (passData.passIndex & 1u),
                        target: DataTarget.Graphics);

                    ulong variantIndirectOffset = passData.indirectArgsOffset + (ulong)v * 16UL;
                    unsafeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, variantIndirectOffset);
                }
            }
        });
    }

    private void AddExportVisibilityDepthPass(RenderGraph rg, Identifier<RGBuffer> visBuffer, uint2 screenSize, ref Identifier<RGTexture> existingDepth)
    {
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
            shader = _materialPipelineResource.exportVisibilityDepthShader,
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

    private void DisposeVisibility()
    {
        _dispatchMeshCommandSignature?.Dispose();
        _dispatchMeshCommandSignature = null!;
    }
}
