using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline
{
    private struct TileLightCullingPassData
    {
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGBuffer> tileLightList;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public uint tilesX;
        public uint tilesY;
    }

    private Identifier<RGBuffer> AddTileLightCullingPass(RenderGraph rg, Identifier<RGTexture> depthTexture, uint2 renderSize)
    {
        var tilesX = (renderSize.x + 15u) / 16u;
        var tilesY = (renderSize.y + 15u) / 16u;
        var bufferSize = (nuint)(tilesX * tilesY * PipelineConstants.DWORDS_PER_TILE * 4u);

        var tileBufferDesc = new BufferDesc
        {
            Size = bufferSize,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource | BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default
        };

        var tileLightList = rg.CreateBuffer(in tileBufferDesc, "TileLightList");

        using var builder = rg.AddComputeRenderPass<TileLightCullingPassData>("TileLightCulling");
        builder.AllowPassCulling(false);
        builder.UseTexture(depthTexture, AccessFlags.Read);
        builder.UseBuffer(tileLightList, AccessFlags.Write);

        builder.SetPassData(new TileLightCullingPassData
        {
            depthTexture = depthTexture,
            tileLightList = tileLightList,
            shader = _lightingPipelineResource.tileLightCullingShader,
            renderSize = renderSize,
            tilesX = tilesX,
            tilesY = tilesY
        });

        builder.SetRenderFunc<TileLightCullingPassData>(static (ref readonly passData, computeCtx) =>
        {
            var depthSrv = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.depthTexture).AsResource(), BindlessAccess.ShaderResource);
            var tileLightListUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.tileLightList).AsResource(), BindlessAccess.UnorderedAccess);

            var props = new InternalTileLightCullingShaderProperties
            {
                depthTextureIndex = depthSrv,
                tileLightListUav = tileLightListUav,
                renderWidth = passData.renderSize.x,
                renderHeight = passData.renderSize.y,
                tilesX = passData.tilesX,
                tilesY = passData.tilesY
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(passData.tilesX, passData.tilesY, 1);
        });

        return tileLightList;
    }

    private struct DebugTileLightHeatmapPassData
    {
        public Identifier<RGBuffer> tileLightList;
        public Identifier<RGTexture> depthTexture;
        public Handle<Shader> shader;
        public uint2 renderSize;
        public uint tilesX;
    }

    private void AddDebugTileLightHeatmapPass(RenderGraph rg, Identifier<RGBuffer> tileLightList, Identifier<RGTexture> depthTexture, Identifier<RGTexture> colorTarget, uint2 renderSize)
    {
        var tilesX = (renderSize.x + 15u) / 16u;

        using var builder = rg.AddRasterRenderPass<DebugTileLightHeatmapPassData>("DebugTileLightHeatmap");
        builder.SetColorAttachment(colorTarget, 0, AccessFlags.WriteAll);
        builder.UseBuffer(tileLightList, AccessFlags.Read);
        builder.UseTexture(depthTexture, AccessFlags.Read);

        builder.SetPassData(new DebugTileLightHeatmapPassData
        {
            tileLightList = tileLightList,
            depthTexture = depthTexture,
            shader = _lightingPipelineResource.debugTileLightHeatmapShader,
            renderSize = renderSize,
            tilesX = tilesX
        });

        builder.SetRenderFunc<DebugTileLightHeatmapPassData>(static (ref readonly passData, renderCtx) =>
        {
            if (!renderCtx.TrySetActiveShaderPass(passData.shader, PassSemantic.Forward))
            {
                return;
            }

            var tileLightListSrv = renderCtx.GetActualBindlessIndex(passData.tileLightList);
            var depthSrv = renderCtx.GetActualBindlessIndex(passData.depthTexture);

            var props = new HiddenDebugTileLightHeatmapShaderProperties
            {
                tileLightListBufferIndex = tileLightListSrv,
                depthTextureIndex = depthSrv,
                renderWidth = passData.renderSize.x,
                renderHeight = passData.renderSize.y,
                tilesX = passData.tilesX
            };

            renderCtx.SetUserDataWithProperties(props, target: DataTarget.Graphics);
            renderCtx.DispatchMesh(1, 1, 1);
        });
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InternalDeferredLightingShaderProperties
    {
        public uint gbuffer0Srv;
        public uint gbuffer1Srv;
        public uint gbuffer2Srv;
        public uint gbuffer3Srv;
        public uint depthTextureIndex;
        public uint tileLightListBufferIndex;
        public uint tileShadingModelMaskBufferIndex;
        public uint litColorUav;
        public uint renderWidth;
        public uint renderHeight;
        public uint tilesPerRow;
        public uint shadingModelId;
    }

    private struct DeferredLightingPassData
    {
        public Identifier<RGTexture> gbuffer0;
        public Identifier<RGTexture> gbuffer1;
        public Identifier<RGTexture> gbuffer2;
        public Identifier<RGTexture> gbuffer3;
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGBuffer> tileLightList;
        public Identifier<RGTexture> litColorTarget;
        public ShaderVariantRegistry variantRegistry;
        public uint tilesPerRow;
        public uint tilesY;
        public uint2 renderSize;
    }

    private Identifier<RGTexture> AddDeferredLightingPass(RenderGraph rg, in GBufferResources gbuffer, Identifier<RGTexture> depthTexture, Identifier<RGBuffer> tileLightList, uint2 renderSize)
    {
        var tilesX = (renderSize.x + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var tilesY = (renderSize.y + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;

        var litColorDesc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R16G16B16A16_Float,
            usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);

        using var builder = rg.AddComputeRenderPass<DeferredLightingPassData>("DeferredLighting");

        var litColorTarget = builder.CreateTexture(in litColorDesc, "HDRColorBuffer");

        builder.UseTexture(gbuffer.GBuffer0, AccessFlags.Read);
        builder.UseTexture(gbuffer.GBuffer1, AccessFlags.Read);
        builder.UseTexture(gbuffer.GBuffer2, AccessFlags.Read);
        builder.UseTexture(gbuffer.GBuffer3, AccessFlags.Read);
        builder.UseTexture(depthTexture, AccessFlags.Read);
        builder.UseBuffer(tileLightList, AccessFlags.Read);
        builder.UseTexture(litColorTarget, AccessFlags.Write);

        builder.SetPassData(new DeferredLightingPassData
        {
            gbuffer0 = gbuffer.GBuffer0,
            gbuffer1 = gbuffer.GBuffer1,
            gbuffer2 = gbuffer.GBuffer2,
            gbuffer3 = gbuffer.GBuffer3,
            depthTexture = depthTexture,
            tileLightList = tileLightList,
            litColorTarget = litColorTarget,
            variantRegistry = _assetManager.ShaderVariants,
            tilesPerRow = tilesX,
            tilesY = tilesY,
            renderSize = renderSize
        });

        builder.SetRenderFunc<DeferredLightingPassData>(static (ref readonly passData, computeCtx) =>
        {
            var gb0Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer0, BindlessAccess.ShaderResource);
            var gb1Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer1, BindlessAccess.ShaderResource);
            var gb2Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer2, BindlessAccess.ShaderResource);
            var gb3Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer3, BindlessAccess.ShaderResource);
            var depthSrv = computeCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource);
            var tileLightListSrv = computeCtx.GetActualBindlessIndex(passData.tileLightList, BindlessAccess.ShaderResource);
            var litColorUav = computeCtx.GetActualBindlessIndex(passData.litColorTarget, BindlessAccess.UnorderedAccess);

            var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.DeferredLighting);
            var dispatchedShadingModels = 0u;

            for (var i = 0; i < dispatchVariants.Length; i++)
            {
                ref readonly var variant = ref dispatchVariants[i];
                ref readonly var variantRecord = ref passData.variantRegistry.GetVariant(new ShaderVariantIndex(variant.DenseIndex));
                var shadingModelId = variantRecord.ShadingModelId;

                var maskBit = 1u << (int)(shadingModelId & 31u);
                if ((dispatchedShadingModels & maskBit) != 0)
                {
                    continue;
                }

                if (!passData.variantRegistry.IsBytecodeReady(variant.DenseIndex) ||
                    !computeCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.DeferredLighting))
                {
                    continue;
                }

                dispatchedShadingModels |= maskBit;

                var props = new InternalDeferredLightingShaderProperties
                {
                    gbuffer0Srv = gb0Srv,
                    gbuffer1Srv = gb1Srv,
                    gbuffer2Srv = gb2Srv,
                    gbuffer3Srv = gb3Srv,
                    depthTextureIndex = depthSrv,
                    tileLightListBufferIndex = tileLightListSrv,
                    tileShadingModelMaskBufferIndex = uint.MaxValue,
                    litColorUav = litColorUav,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y,
                    tilesPerRow = passData.tilesPerRow,
                    shadingModelId = shadingModelId
                };

                computeCtx.SetUserDataWithProperties(in props, target: DataTarget.Compute);
                computeCtx.DispatchCompute(passData.tilesPerRow, passData.tilesY, 1);
            }
        });

        return litColorTarget;
    }

    private unsafe void UploadLights(ResourceContext ctx, GhostRenderPayload payload, out uint punctualLightsSrv, out uint punctualLightCount, out uint directionalLightSrv)
    {
        punctualLightsSrv = uint.MaxValue;
        punctualLightCount = (uint)payload.PunctualLights.Length;
        directionalLightSrv = uint.MaxValue;

        if (punctualLightCount > 0)
        {
            var lights = payload.PunctualLights;
            var bufferSize = punctualLightCount * (nuint)sizeof(GPUPunctualLight);
            var desc = new BufferDesc
            {
                Size = bufferSize,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload
            };

            var lightBuffer = ctx.ResourceManager.CreateTransientBuffer(in desc, "PunctualLightsBuffer");
            var pData = (GPUPunctualLight*)ctx.ResourceDatabase.MapResource(lightBuffer.AsResource(), 0, null);
            fixed (GPUPunctualLight* pSrc = lights)
            {
                Buffer.MemoryCopy(pSrc, pData, bufferSize, bufferSize);
            }
            ctx.ResourceDatabase.UnmapResource(lightBuffer.AsResource(), 0, null);

            punctualLightsSrv = ctx.ResourceDatabase.GetBindlessIndex(lightBuffer.AsResource());
        }

        if (payload.HasDirectionalLight)
        {
            var dirSize = (nuint)sizeof(GPUDirectionalLight);
            var desc = new BufferDesc
            {
                Size = dirSize,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload
            };

            var dirBuffer = ctx.ResourceManager.CreateTransientBuffer(in desc, "DirectionalLightBuffer");
            var pData = (GPUDirectionalLight*)ctx.ResourceDatabase.MapResource(dirBuffer.AsResource(), 0, null);
            *pData = payload.CurrentSunLight;
            ctx.ResourceDatabase.UnmapResource(dirBuffer.AsResource(), 0, null);

            directionalLightSrv = ctx.ResourceDatabase.GetBindlessIndex(dirBuffer.AsResource());
        }
    }
}