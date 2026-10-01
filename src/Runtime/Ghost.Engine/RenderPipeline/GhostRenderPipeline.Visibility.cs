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
        public Identifier<RGTexture> visBuffer;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
    }

    private struct VisibilityPassData
    {
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> binOffsetsBuffer;
        public Identifier<RGTexture> visBuffer;
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
        public Identifier<RGTexture> visBuffer;
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

    private void AddClearVisibilityBufferPass(RenderGraph rg, Identifier<RGTexture> visBuffer, uint2 renderSize)
    {
        using var builder = rg.AddComputeRenderPass<ClearVisibilityBufferPassData>("ClearVisibilityBuffer");
        builder.UseTexture(visBuffer, AccessFlags.Write);

        builder.SetPassData(new ClearVisibilityBufferPassData
        {
            visBuffer = visBuffer,
            shader = _materialPipelineResource.clearVisibilityBufferShader,
            renderSize = renderSize
        });

        builder.SetRenderFunc<ClearVisibilityBufferPassData>(static (ref readonly passData, computeCtx) =>
        {
            var visBufferUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.visBuffer).AsResource(), BindlessAccess.UnorderedAccess);
            var props = new InternalClearVisibilityBufferShaderProperties
            {
                visBufferIndex = visBufferUav,
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

    private void AddVisibilityBufferPass(RenderGraph rg, Identifier<RGBuffer> visibleMeshlets, Identifier<RGBuffer> binOffsetsBuffer, Identifier<RGBuffer> indirectArgsBuffer, uint cullPassIndex, uint sceneBuffer, uint2 screenSize, ref Identifier<RGTexture> existingVisBuffer)
    {
        var isPass1 = cullPassIndex == 0;
        var passName = isPass1 ? "Visibility_Pass1_EarlyZ" : "Visibility_Pass2_LateZ";

        if (existingVisBuffer.IsInvalid)
        {
            var vbufferDesc = RGTextureDesc.Relative(
                1.0f,
                TextureFormat.R32G32_UInt,
                usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
                clearAtFirstUse: false);
            existingVisBuffer = rg.CreateTexture(in vbufferDesc, "VisibilityBuffer");
            AddClearVisibilityBufferPass(rg, existingVisBuffer, screenSize);
        }

        using var builder = rg.AddUnsafeRenderPass<VisibilityPassData>(passName);

        builder.UseBuffer(visibleMeshlets, AccessFlags.Read);
        builder.UseBuffer(binOffsetsBuffer, AccessFlags.Read);
        builder.UseRandomAccessTexture(existingVisBuffer);
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
            var visBufferUav = unsafeCtx.ResourceDatabase.GetBindlessIndex(unsafeCtx.GetActualTexture(passData.visBuffer).AsResource(), BindlessAccess.UnorderedAccess);

            var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.Visibility);
            if (dispatchVariants.Length == 0)
            {
                return;
            }

            // Dispatch Bin 0 (All Opaque meshlets across all materials in ONE draw call)
            var opaqueShaderBound = false;
            for (var i = 0; i < dispatchVariants.Length; i++)
            {
                ref readonly var v = ref dispatchVariants[i];
                if (v.Shader.IsValid && unsafeCtx.TrySetActiveShaderPass(v.Shader, PassSemantic.Visibility))
                {
                    opaqueShaderBound = true;
                    break;
                }
            }

            if (opaqueShaderBound)
            {
                unsafeCtx.SetUserData(visibleBufferIndex, visBufferUav, binOffsetsIndex, (0u << 1) | (passData.passIndex & 1u));
                var bin0IndirectOffset = passData.indirectArgsOffset + 0UL * 16UL;
                unsafeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, bin0IndirectOffset);
            }

            // Dispatch Bins 1..N (Alpha-Clipped variants)
            for (var i = 0; i < dispatchVariants.Length; i++)
            {
                ref readonly var variant = ref dispatchVariants[i];
                if (variant.Shader.IsValid && unsafeCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.Visibility))
                {
                    var bin = (uint)variant.DenseIndex + 1u;
                    unsafeCtx.SetUserData(visibleBufferIndex, visBufferUav, binOffsetsIndex, (bin << 1) | (passData.passIndex & 1u));
                    var variantIndirectOffset = passData.indirectArgsOffset + (ulong)bin * 16UL;
                    unsafeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, variantIndirectOffset);
                }
            }
        });
    }

    private void AddExportVisibilityDepthPass(RenderGraph rg, Identifier<RGTexture> visBuffer, uint2 screenSize, ref Identifier<RGTexture> existingDepth)
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
        builder.UseTexture(visBuffer, AccessFlags.Read);
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
            var visBufferIndex = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.visBuffer).AsResource(), BindlessAccess.ShaderResource);
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
