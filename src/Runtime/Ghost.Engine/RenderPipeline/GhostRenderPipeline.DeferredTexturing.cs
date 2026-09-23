using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private ICommandSignature _deferredTexturingCommandSignature = null!;

    [StructLayout(LayoutKind.Sequential)]
    public struct InternalDeferredTexturingShaderProperties
    {
        public uint visBufferIndex;
        public uint visibleMeshletsPass1;
        public uint visibleMeshletsPass2;
        public uint variantTileListIndex;
        public uint maxTilesPerVariant;
        public uint tilesPerRow;
        public uint renderWidth;
        public uint renderHeight;
        public uint gbuffer0Uav;
        public uint gbuffer1Uav;
        public uint gbuffer2Uav;
        public uint gbuffer3Uav;
        public uint variantIndex;
    }

    public readonly struct GBufferResources
    {
        public Identifier<RGTexture> GBuffer0
        {
            get; init;
        }
        public Identifier<RGTexture> GBuffer1
        {
            get; init;
        }
        public Identifier<RGTexture> GBuffer2
        {
            get; init;
        }
        public Identifier<RGTexture> GBuffer3
        {
            get; init;
        }
    }

    public struct DeferredTexturingPassData
    {
        public Identifier<RGBuffer> visBuffer;
        public Identifier<RGBuffer> visibleMeshletsPass1;
        public Identifier<RGBuffer> visibleMeshletsPass2;
        public Identifier<RGBuffer> variantTileList;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Identifier<RGTexture> gbuffer0;
        public Identifier<RGTexture> gbuffer1;
        public Identifier<RGTexture> gbuffer2;
        public Identifier<RGTexture> gbuffer3;
        public ICommandSignature commandSignature;
        public ShaderVariantRegistry variantRegistry;
        public uint tilesPerRow;
        public uint maxTilesPerVariant;
        public uint2 renderSize;
    }

    private void InitializeDeferredTexturing(RenderEngine renderEngine)
    {
        _deferredTexturingCommandSignature = renderEngine.GraphicsEngine.CreateCommandSignature(new CommandSignatureDesc
        {
            Stride = INDIRECT_ARGS_STRIDE,
            Arguments = new IndirectArgumentDesc[]
            {
                new() { Type = IndirectArgumentType.Dispatch }
            }
        }, default);
    }

    private GBufferResources AddDeferredTexturingPass(RenderGraph rg, Identifier<RGBuffer> visBuffer, Identifier<RGBuffer> visibleMeshlets0, Identifier<RGBuffer> visibleMeshlets1, Identifier<RGBuffer> tileListBuffer, Identifier<RGBuffer> indirectArgsBuffer, uint2 screenSize)
    {
        var tilesX = (screenSize.x + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var tilesY = (screenSize.y + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var totalTiles = Math.Max(1u, tilesX * tilesY);

        using var builder = rg.AddComputeRenderPass<DeferredTexturingPassData>("DeferredTexturing");

        var gbuffer0Desc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R8G8B8A8_UNorm,
            usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        var gbuffer0 = builder.CreateTexture(in gbuffer0Desc, "GBuffer0_AlbedoFlags");

        var gbuffer1Desc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R8G8B8A8_UNorm,
            usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        var gbuffer1 = builder.CreateTexture(in gbuffer1Desc, "GBuffer1_NormalRoughMetal");

        var gbuffer2Desc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R16G16B16A16_Float,
            usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        var gbuffer2 = builder.CreateTexture(in gbuffer2Desc, "GBuffer2_MotionAO");

        var gbuffer3Desc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R16G16B16A16_Float,
            usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
        var gbuffer3 = builder.CreateTexture(in gbuffer3Desc, "GBuffer3_Emissive");

        builder.UseBuffer(visBuffer, AccessFlags.Read);
        builder.UseBuffer(visibleMeshlets0, AccessFlags.Read);
        builder.UseBuffer(visibleMeshlets1, AccessFlags.Read);
        builder.UseBuffer(tileListBuffer, AccessFlags.Read);
        builder.UseBuffer(indirectArgsBuffer, AccessFlags.Read);

        builder.UseTexture(gbuffer0, AccessFlags.Write);
        builder.UseTexture(gbuffer1, AccessFlags.Write);
        builder.UseTexture(gbuffer2, AccessFlags.Write);
        builder.UseTexture(gbuffer3, AccessFlags.Write);

        builder.SetPassData(new DeferredTexturingPassData
        {
            visBuffer = visBuffer,
            visibleMeshletsPass1 = visibleMeshlets0,
            visibleMeshletsPass2 = visibleMeshlets1,
            variantTileList = tileListBuffer,
            indirectArgsBuffer = indirectArgsBuffer,
            gbuffer0 = gbuffer0,
            gbuffer1 = gbuffer1,
            gbuffer2 = gbuffer2,
            gbuffer3 = gbuffer3,
            commandSignature = _deferredTexturingCommandSignature,
            variantRegistry = _assetManager.ShaderVariants,
            tilesPerRow = tilesX,
            maxTilesPerVariant = totalTiles,
            renderSize = screenSize
        });

        builder.SetRenderFunc<DeferredTexturingPassData>(static (ref readonly passData, computeCtx) =>
        {
            var visSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visBuffer).AsResource(), BindlessAccess.ShaderResource);
            var meshlets0Srv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass1).AsResource(), BindlessAccess.ShaderResource);
            var meshlets1Srv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshletsPass2).AsResource(), BindlessAccess.ShaderResource);
            var tileListSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.variantTileList).AsResource(), BindlessAccess.ShaderResource);
            var actualIndirectArgs = computeCtx.GetActualBuffer(passData.indirectArgsBuffer);

            var gb0Uav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.gbuffer0).AsResource(), BindlessAccess.UnorderedAccess);
            var gb1Uav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.gbuffer1).AsResource(), BindlessAccess.UnorderedAccess);
            var gb2Uav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.gbuffer2).AsResource(), BindlessAccess.UnorderedAccess);
            var gb3Uav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.gbuffer3).AsResource(), BindlessAccess.UnorderedAccess);

            var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.DeferredTexturing);
            for (var i = 0; i < dispatchVariants.Length; i++)
            {
                ref readonly var variant = ref dispatchVariants[i];
                if (!passData.variantRegistry.IsBytecodeReady(variant.DenseIndex) ||
                    !computeCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.DeferredTexturing))
                {
                    continue;
                }

                var v = (uint)variant.DenseIndex;
                var props = new InternalDeferredTexturingShaderProperties
                {
                    visBufferIndex = visSrv,
                    visibleMeshletsPass1 = meshlets0Srv,
                    visibleMeshletsPass2 = meshlets1Srv,
                    variantTileListIndex = tileListSrv,
                    maxTilesPerVariant = passData.maxTilesPerVariant,
                    tilesPerRow = passData.tilesPerRow,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y,
                    gbuffer0Uav = gb0Uav,
                    gbuffer1Uav = gb1Uav,
                    gbuffer2Uav = gb2Uav,
                    gbuffer3Uav = gb3Uav,
                    variantIndex = v
                };

                computeCtx.SetUserDataWithProperties(in props, target: DataTarget.Compute);
                computeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectArgs, (ulong)v * INDIRECT_ARGS_STRIDE);
            }
        });

        return new GBufferResources
        {
            GBuffer0 = gbuffer0,
            GBuffer1 = gbuffer1,
            GBuffer2 = gbuffer2,
            GBuffer3 = gbuffer3
        };
    }

    private void DisposeDeferredTexturing()
    {
        _deferredTexturingCommandSignature?.Dispose();
        _deferredTexturingCommandSignature = null!;
    }
}
