using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System;

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
        var dwordsPerTile = _settings.HighDensityLightTiles ? 32u : 16u;
        var bufferSize = (nuint)(tilesX * tilesY * dwordsPerTile * 4u);

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


    private unsafe void UploadLights(RenderContext ctx, GhostRenderPayload payload, out uint punctualLightsSrv, out uint punctualLightCount, out uint directionalLightSrv)
    {
        punctualLightsSrv = uint.MaxValue;
        punctualLightCount = (uint)payload.PunctualLights.Length;
        directionalLightSrv = uint.MaxValue;

        if (punctualLightCount > 0)
        {
            var lights = payload.PunctualLights;
            var bufferSize = (nuint)punctualLightCount * (nuint)sizeof(GPUPunctualLight);
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

