using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Engine.Volumes;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

internal unsafe partial class GhostRenderPipeline
{
    private UnsafeList<PunctualLightRequest> UploadLights(ResourceContext ctx, GhostRenderPayload payload,
        out uint punctualLightsSrv, out uint punctualLightCount,
        out uint directionalLightSrv, out uint directionalLightCount, out int primaryDirectionalLightIndex)
    {
        UploadDirectionalLights(ctx, payload, out directionalLightSrv, out directionalLightCount, out primaryDirectionalLightIndex);

        punctualLightsSrv = RHIUtility.INVALID_BINDLESS_INDEX;
        punctualLightCount = (uint)payload.PunctualLights.Count;

        if (punctualLightCount > 0)
        {
            var lights = new UnsafeList<PunctualLightRequest>(payload.PunctualLights.Count, AllocationHandle.TempRender);
            while (payload.PunctualLights.TryDequeue(out var item))
            {
                lights.Add(item);
            }

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

            for (var i = 0; i < lights.Count; i++)
            {
                pData[i] = lights[i].light;
            }

            ctx.ResourceDatabase.UnmapResource(lightBuffer.AsResource(), 0, null);
            punctualLightsSrv = ctx.ResourceDatabase.GetBindlessIndex(lightBuffer.AsResource());

            return lights;
        }

        return default;
    }

    private void UploadDirectionalLights(ResourceContext ctx, GhostRenderPayload payload, out uint directionalLightSrv, out uint directionalLightCount, out int primaryDirectionalLightIndex)
    {
        directionalLightSrv = RHIUtility.INVALID_BINDLESS_INDEX;
        directionalLightCount = payload.DirectionalLightCount;
        primaryDirectionalLightIndex = payload.PrimaryDirectionalLightIndex;

        if (directionalLightCount > 0)
        {
            var dirLights = payload.DirectionalLights;
            var dirSize = directionalLightCount * (nuint)sizeof(GPUDirectionalLight);
            var desc = new BufferDesc
            {
                Size = dirSize,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload
            };

            var dirBuffer = ctx.ResourceManager.CreateTransientBuffer(in desc, "DirectionalLightBuffer");
            var pData = (GPUDirectionalLight*)ctx.ResourceDatabase.MapResource(dirBuffer.AsResource(), 0, null);
            MemoryUtility.MemCpy(pData, dirLights.GetUnsafePtr(), dirSize);
            ctx.ResourceDatabase.UnmapResource(dirBuffer.AsResource(), 0, null);

            directionalLightSrv = ctx.ResourceDatabase.GetBindlessIndex(dirBuffer.AsResource());
        }
    }

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
        // TODO: For Dynamic Resolution Scaling (DRS), size this buffer using the maximum resolution (window/backbuffer size)
        // instead of dynamic renderSize so that BufferDesc.Size remains constant and the render graph hash does not change every frame.
        // Dispatches will continue to use the dynamic viewport tilesX/tilesY.
        var tilesX = (renderSize.x + 15u) / 16u;
        var tilesY = (renderSize.y + 15u) / 16u;
        var bufferSize = (nuint)(tilesX * tilesY * PipelineConstants.DWORDS_PER_TILE * 4u);

        using var builder = rg.AddComputeRenderPass<TileLightCullingPassData>("TileLightCulling");

        var tileBufferDesc = new BufferDesc
        {
            Size = bufferSize,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource | BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default
        };

        var tileLightList = builder.CreateBuffer(in tileBufferDesc, "TileLightList");

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
            var depthSrv = computeCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource);
            var tileLightListUav = computeCtx.GetActualBindlessIndex(passData.tileLightList, BindlessAccess.UnorderedAccess);

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

    [GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl")]
    [StructLayout(LayoutKind.Sequential)]
    private struct DeferredLightingShaderProperties
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
        public uint shadowAtlasSrv;
        public uint shadowViewsBufferSrv;
        public uint shadowIndicesBufferSrv;
        public uint skyboxTextureSrv;
        public uint iblLutTextureSrv;
        public uint persistentViewStateBufferSrv;
        public uint viewId;
        public uint skyboxMipCount;
        public float4 skyboxTintAndIntensity;
        public float2 skyboxRotationSinCos;
        public uint hzbTextureSrv;
        public uint hzbMipCount;
        public float2 hzbUvFactor;
        public uint2 hzbBaseSize;
        public uint tlasIndex;
        public uint risResolvedDiffuseSrv;
        public uint risResolvedSpecularSrv;
        public uint directionalShadowAtlasSrv;
        public uint directionalShadowViewsBufferSrv;
        public uint cascadeCount;
        public float maxShadowDistance;
        public float shadowFadeRange;
        public float4 cascadeSplits;
        public float4 cascadeBlends;
        public uint enableContactShadows;
        public float contactShadowDistance;
        public float contactShadowTolerance;
    }

    [GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl")]
    [StructLayout(LayoutKind.Sequential)]
    private struct RisResolveShadingShaderProperties
    {
        public uint depthTextureIndex;
        public uint gbuffer0Srv;
        public uint gbuffer1Srv;
        public uint gbuffer2Srv;
        public uint gbuffer3Srv;
        public uint risLightSamplesIndex;
        public uint risShadowMaskIndex;
        public uint tileShadingModelMaskBufferIndex;
        public uint risResolvedDiffuseUav;
        public uint risResolvedSpecularUav;
        public uint renderWidth;
        public uint renderHeight;
        public uint tilesPerRow;
        public uint shadingModelId;
        public uint iblLutTextureSrv;
        public uint persistentViewStateBufferSrv;
        public uint viewId;
        public uint skyboxTextureSrv;
        public uint skyboxMipCount;
        public uint _pad0;
        public float4 skyboxTintAndIntensity;
        public float2 skyboxRotationSinCos;
        public uint _pad1;
        public uint _pad2;
    }

    private struct DeferredLightingPassData
    {
        public ShaderVariantRegistry variantRegistry;
        public Identifier<RGTexture> gbuffer0;
        public Identifier<RGTexture> gbuffer1;
        public Identifier<RGTexture> gbuffer2;
        public Identifier<RGTexture> gbuffer3;
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGBuffer> tileLightList;
        public Identifier<RGTexture> litColorTarget;
        public Identifier<RGTexture> shadowAtlas;
        public Identifier<RGBuffer> tileShadingModelMaskBuffer;
        public Identifier<RGTexture> hzbTexture;
        public Identifier<RGTexture> risResolvedDiffuse;
        public Identifier<RGTexture> risResolvedSpecular;
        public SkyContext skyContext;
        public uint viewId;
        public uint shadowViewsBufferSrv;
        public uint shadowIndicesBufferSrv;
        public Identifier<RGTexture> directionalShadowAtlas;
        public uint directionalShadowViewsBufferSrv;
        public uint cascadeCount;
        public float maxShadowDistance;
        public float shadowFadeRange;
        public float4 cascadeSplits;
        public float4 cascadeBlends;
        public uint tilesPerRow;
        public uint tilesY;
        public uint2 renderSize;
        public uint2 hzbSize;
        public uint hzbMipCount;
        public Identifier<RGBuffer> tlas;

        public bool enableContactShadows;
        public float contactShadowDistance;
        public float contactShadowTolerance;
    }

    private Identifier<RGTexture> AddDeferredLightingPass(RenderGraph renderGraph, GhostRenderPayload payload, in SkyContext skyContext, Identifier<RGTexture> hzbTexture, uint hzbMipCount, uint2 hzbSize,
        Identifier<RGTexture> depthTexture, Identifier<RGBuffer> tlasBuffer, uint viewId, uint2 renderSize)
    {
        ref readonly var gbufferData = ref renderGraph.Blackboard.Get<GBufferPassData>();
        ref readonly var tile = ref renderGraph.Blackboard.Get<TileClassificationData>();

        ref readonly var shadowMap = ref renderGraph.Blackboard.TryGet<ShadowMapPassData>(out var hasShadowMap);
        ref readonly var ris = ref renderGraph.Blackboard.TryGet<RisPassData>(out var hasRis);

        ref readonly var contactShadowSettings = ref payload.GetViewVolumeData<ContactShadowSettings>(viewId);

        var shadowAtlas = hasShadowMap ? shadowMap.shadowAtlas : Identifier<RGTexture>.Invalid;
        var shadowViewsBufferSrv = hasShadowMap ? shadowMap.shadowViewsBufferSrv : RHIUtility.INVALID_BINDLESS_INDEX;
        var shadowIndicesBufferSrv = hasShadowMap ? shadowMap.shadowIndicesBufferSrv : RHIUtility.INVALID_BINDLESS_INDEX;
        var directionalShadowAtlas = hasShadowMap ? shadowMap.directionalShadowAtlas : Identifier<RGTexture>.Invalid;
        var directionalShadowViewsBufferSrv = hasShadowMap ? shadowMap.directionalShadowViewsBufferSrv : RHIUtility.INVALID_BINDLESS_INDEX;
        var cascadeCount = hasShadowMap ? shadowMap.cascadeCount : 0u;
        var maxShadowDistance = hasShadowMap ? shadowMap.maxShadowDistance : 0f;
        var shadowFadeRange = hasShadowMap ? shadowMap.shadowFadeRange : 0f;
        var cascadeSplits = hasShadowMap ? shadowMap.cascadeSplits : default;
        var cascadeBlends = hasShadowMap ? shadowMap.cascadeBlends : default;

        var risResolvedDiffuse = hasRis ? ris.resolvedDiffuse : Identifier<RGTexture>.Invalid;
        var risResolvedSpecular = hasRis ? ris.resolvedSpecular : Identifier<RGTexture>.Invalid;

        ref readonly var gbuffer = ref gbufferData.gbuffer;
        var tileLightList = gbufferData.tileLightList;
        var tileShadingModelMaskBuffer = tile.tileShadingModelMaskBuffer;

        var tilesX = (renderSize.x + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;
        var tilesY = (renderSize.y + CLASSIFICATION_TILE_SIZE - 1u) / CLASSIFICATION_TILE_SIZE;

        var litColorDesc = RGTextureDesc.Relative(
            1.0f,
            TextureFormat.R16G16B16A16_Float,
            usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource | TextureUsage.RenderTarget);

        var litColorTarget = renderGraph.CreateTexture(in litColorDesc, "HDRColorBuffer");
        renderGraph.AddDiscardRenderTargetPass(litColorTarget, "ClearHDRColorBuffer");

        using var builder = renderGraph.AddComputeRenderPass<DeferredLightingPassData>("DeferredLighting");

        builder.UseTexture(gbuffer.GBuffer0, AccessFlags.Read);
        builder.UseTexture(gbuffer.GBuffer1, AccessFlags.Read);
        builder.UseTexture(gbuffer.GBuffer2, AccessFlags.Read);
        builder.UseTexture(gbuffer.GBuffer3, AccessFlags.Read);
        builder.UseTexture(depthTexture, AccessFlags.Read);
        builder.UseBuffer(tileLightList, AccessFlags.Read);
        builder.UseBuffer(tileShadingModelMaskBuffer, AccessFlags.Read);
        builder.UseTexture(hzbTexture, AccessFlags.Read);
        builder.UseBuffer(tlasBuffer, AccessFlags.Read);
        builder.UseTexture(shadowAtlas, AccessFlags.Read);
        if (directionalShadowAtlas.IsValid)
        {
            builder.UseTexture(directionalShadowAtlas, AccessFlags.Read);
        }
        builder.UseTexture(risResolvedDiffuse, AccessFlags.Read);
        builder.UseTexture(risResolvedSpecular, AccessFlags.Read);
        builder.UseTexture(skyContext.skyboxTexture, AccessFlags.Read);
        builder.UseTexture(skyContext.iblLutTexture, AccessFlags.Read);
        builder.UseBuffer(skyContext.persistentViewStateBuffer, AccessFlags.Read);
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
            shadowAtlas = shadowAtlas,
            tileShadingModelMaskBuffer = tileShadingModelMaskBuffer,
            hzbTexture = hzbTexture,
            risResolvedDiffuse = risResolvedDiffuse,
            risResolvedSpecular = risResolvedSpecular,
            skyContext = skyContext,
            viewId = viewId,
            shadowViewsBufferSrv = shadowViewsBufferSrv,
            shadowIndicesBufferSrv = shadowIndicesBufferSrv,
            directionalShadowAtlas = directionalShadowAtlas,
            directionalShadowViewsBufferSrv = directionalShadowViewsBufferSrv,
            cascadeCount = cascadeCount,
            maxShadowDistance = maxShadowDistance,
            shadowFadeRange = shadowFadeRange,
            cascadeSplits = cascadeSplits,
            cascadeBlends = cascadeBlends,
            variantRegistry = _assetManager.ShaderVariants,
            tilesPerRow = tilesX,
            tilesY = tilesY,
            renderSize = renderSize,
            hzbSize = hzbSize,
            hzbMipCount = hzbMipCount,
            tlas = tlasBuffer,

            enableContactShadows = contactShadowSettings.enabled.GetValueOrDefault(),
            contactShadowDistance = contactShadowSettings.maxDistance.GetValueOrDefault(),
            contactShadowTolerance = contactShadowSettings.tolerance.GetValueOrDefault(),
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
            var shadowAtlasSrv = computeCtx.GetActualBindlessIndex(passData.shadowAtlas, BindlessAccess.ShaderResource);
            var directionalShadowAtlasSrv = passData.directionalShadowAtlas.IsValid
                ? computeCtx.GetActualBindlessIndex(passData.directionalShadowAtlas, BindlessAccess.ShaderResource)
                : RHIUtility.INVALID_BINDLESS_INDEX;
            var tileShadingModelMaskSrv = computeCtx.GetActualBindlessIndex(passData.tileShadingModelMaskBuffer, BindlessAccess.ShaderResource);
            var skyboxSrv = computeCtx.GetActualBindlessIndex(passData.skyContext.skyboxTexture, BindlessAccess.ShaderResource);
            var iblLutSrv = computeCtx.GetActualBindlessIndex(passData.skyContext.iblLutTexture, BindlessAccess.ShaderResource);
            var persistentViewStateSrv = computeCtx.GetActualBindlessIndex(passData.skyContext.persistentViewStateBuffer, BindlessAccess.ShaderResource);
            var hzbSrv = computeCtx.GetActualBindlessIndex(passData.hzbTexture, BindlessAccess.ShaderResource);
            var risDiffSrv = computeCtx.GetActualBindlessIndex(passData.risResolvedDiffuse, BindlessAccess.ShaderResource);
            var risSpecSrv = computeCtx.GetActualBindlessIndex(passData.risResolvedSpecular, BindlessAccess.ShaderResource);
            var tlasSrv = computeCtx.GetActualBindlessIndex(passData.tlas, BindlessAccess.ShaderResource);

            var hzbUvFactor = (passData.hzbSize.x > 0 && passData.hzbSize.y > 0)
                ? new float2(passData.renderSize.x * 0.5f / passData.hzbSize.x, passData.renderSize.y * 0.5f / passData.hzbSize.y)
                : new float2(1.0f, 1.0f);

            var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.DeferredLighting);

            for (var i = 0; i < dispatchVariants.Length; i++)
            {
                ref readonly var variant = ref dispatchVariants[i];

                if (!passData.variantRegistry.IsBytecodeReady(variant.DenseIndex) ||
                    !computeCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.DeferredLighting))
                {
                    continue;
                }

                ref readonly var variantRecord = ref passData.variantRegistry.GetVariant(new ShaderVariantIndex(variant.DenseIndex));
                var shadingModelId = variantRecord.ShadingModelId;

                var props = new DeferredLightingShaderProperties
                {
                    gbuffer0Srv = gb0Srv,
                    gbuffer1Srv = gb1Srv,
                    gbuffer2Srv = gb2Srv,
                    gbuffer3Srv = gb3Srv,
                    depthTextureIndex = depthSrv,
                    tileLightListBufferIndex = tileLightListSrv,
                    tileShadingModelMaskBufferIndex = tileShadingModelMaskSrv,
                    litColorUav = litColorUav,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y,
                    tilesPerRow = passData.tilesPerRow,
                    shadingModelId = shadingModelId,
                    shadowAtlasSrv = shadowAtlasSrv,
                    shadowViewsBufferSrv = passData.shadowViewsBufferSrv,
                    shadowIndicesBufferSrv = passData.shadowIndicesBufferSrv,
                    directionalShadowAtlasSrv = directionalShadowAtlasSrv,
                    directionalShadowViewsBufferSrv = passData.directionalShadowViewsBufferSrv,
                    cascadeCount = passData.cascadeCount,
                    maxShadowDistance = passData.maxShadowDistance,
                    shadowFadeRange = passData.shadowFadeRange,
                    cascadeSplits = passData.cascadeSplits,
                    cascadeBlends = passData.cascadeBlends,
                    skyboxTextureSrv = skyboxSrv,
                    iblLutTextureSrv = iblLutSrv,
                    persistentViewStateBufferSrv = persistentViewStateSrv,
                    viewId = passData.viewId,
                    enableContactShadows = passData.enableContactShadows ? 1u : 0u,
                    contactShadowDistance = passData.contactShadowDistance,
                    contactShadowTolerance = passData.contactShadowTolerance,
                    skyboxMipCount = passData.skyContext.skyboxMipCount,
                    skyboxTintAndIntensity = passData.skyContext.skyTintAndIntensity,
                    skyboxRotationSinCos = passData.skyContext.skyRotationSinCos,
                    hzbTextureSrv = hzbSrv,
                    hzbMipCount = passData.hzbMipCount,
                    hzbUvFactor = hzbUvFactor,
                    hzbBaseSize = passData.hzbSize,
                    tlasIndex = tlasSrv,
                    risResolvedDiffuseSrv = risDiffSrv,
                    risResolvedSpecularSrv = risSpecSrv
                };

                computeCtx.SetUserDataWithProperties(in props, target: DataTarget.Compute);
                computeCtx.DispatchCompute(passData.tilesPerRow, passData.tilesY, 1);
            }
        });

        return litColorTarget;
    }

    private struct RisLightSamplingPassData
    {
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGTexture> gbuffer0;
        public Identifier<RGTexture> gbuffer1;
        public Identifier<RGBuffer> tileLightList;
        public Identifier<RGBuffer> visibleLightHashHistory;
        public Identifier<RGTexture> risLightSamples;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public uint tilesPerRow;
        public uint frameIndex;
    }

    private struct RisBuildVisibleLightHashPassData
    {
        public Identifier<RGTexture> risLightSamples;
        public Identifier<RGTexture> risShadowMask;
        public Identifier<RGBuffer> visibleLightHashUav;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public uint tilesPerRow;
    }

    private struct RisFilterVisibleLightHashPassData
    {
        public Identifier<RGBuffer> rawVisibleLightHash;
        public Identifier<RGBuffer> filteredVisibleLightHash;
        public Handle<ComputeShader> shader;
        public uint tilesPerRow;
        public uint tilesPerColumn;
    }

    private struct RisScreenContactShadowPassData
    {
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGTexture> gbuffer1;
        public Identifier<RGTexture> risLightSamples;
        public Identifier<RGTexture> risShadowMask;
        public Identifier<RGBuffer> unresolvedRaysBuffer;
        public Identifier<RGBuffer> counterBuffer;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public uint viewId;
        public uint frameIndex;
    }

    private struct PrepareRisRayTraceIndirectArgsPassData
    {
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Handle<ComputeShader> shader;
    }

    private struct RisRayTracePassData
    {
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGTexture> gbuffer1;
        public Identifier<RGTexture> risLightSamples;
        public Identifier<RGTexture> risShadowMask;
        public Identifier<RGBuffer> unresolvedRaysBuffer;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public ICommandSignature commandSignature;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public Identifier<RGBuffer> tlas;
        public uint viewId;
        public uint frameIndex;
    }

    private struct RisResolveShadingPassData
    {
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGTexture> gbuffer0;
        public Identifier<RGTexture> gbuffer1;
        public Identifier<RGTexture> gbuffer2;
        public Identifier<RGTexture> gbuffer3;
        public Identifier<RGBuffer> tileShadingModelMaskBuffer;
        public Identifier<RGTexture> risLightSamples;
        public Identifier<RGTexture> risShadowMask;
        public Identifier<RGTexture> risResolvedDiffuse;
        public Identifier<RGTexture> risResolvedSpecular;
        public ShaderVariantRegistry variantRegistry;
        public uint tilesPerRow;
        public uint tilesY;
        public uint2 renderSize;
    }

    private struct RisDenoiserTemporalPassData
    {
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGTexture> gbuffer1;
        public Identifier<RGTexture> motionVectors;
        public Identifier<RGTexture> currentDiffuse;
        public Identifier<RGTexture> currentSpecular;
        public Identifier<RGTexture> historyDiffuse;
        public Identifier<RGTexture> historySpecular;
        public Identifier<RGTexture> outputDiffuse;
        public Identifier<RGTexture> outputSpecular;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public uint historyValid;
    }

    private struct RisDenoiserSpatialPassData
    {
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGTexture> gbuffer1;
        public Identifier<RGTexture> inputDiffuse;
        public Identifier<RGTexture> inputSpecular;
        public Identifier<RGTexture> outputDiffuse;
        public Identifier<RGTexture> outputSpecular;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
    }

    private void AddRisPasses(RenderGraph rg, RisViewResource risResource, Identifier<RGTexture> depthTexture, Identifier<RGBuffer> tlasBuffer, uint2 renderSize, uint frameIndex, uint viewId)
    {
        ref readonly var gbufferData = ref rg.Blackboard.Get<GBufferPassData>();
        ref readonly var tile = ref rg.Blackboard.Get<TileClassificationData>();
        ref readonly var gbuffer = ref gbufferData.gbuffer;

        var tileLightList = gbufferData.tileLightList;
        var tileShadingModelMaskBuffer = tile.tileShadingModelMaskBuffer;

        var tilesX = (renderSize.x + 15u) / 16u;
        var tilesY = (renderSize.y + 15u) / 16u;

        // RisLightSampling (Generates 1 winner light and weight W per pixel)

        var visibleLightHashHistory = Identifier<RGBuffer>.Invalid;
        if (risResource.HasHistory && risResource.VisibleLightHashHistory.IsValid)
        {
            visibleLightHashHistory = rg.ImportBuffer(risResource.VisibleLightHashHistory, ResourceBarrierData.Common, ResourceBarrierData.Common);
        }

        Identifier<RGTexture> risLightSamples;
        using (var builder = rg.AddComputeRenderPass<RisLightSamplingPassData>("RisLightSampling"))
        {
            var samplesDesc = RGTextureDesc.Relative(1.0f, TextureFormat.R32G32_UInt, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
            risLightSamples = builder.CreateTexture(in samplesDesc, "RisLightSamples");

            builder.UseTexture(depthTexture, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer0, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer1, AccessFlags.Read);
            builder.UseBuffer(tileLightList, AccessFlags.Read);
            if (visibleLightHashHistory.IsValid)
            {
                builder.UseBuffer(visibleLightHashHistory, AccessFlags.Read);
            }
            builder.UseTexture(risLightSamples, AccessFlags.Write);

            builder.SetPassData(new RisLightSamplingPassData
            {
                depthTexture = depthTexture,
                gbuffer0 = gbuffer.GBuffer0,
                gbuffer1 = gbuffer.GBuffer1,
                tileLightList = tileLightList,
                visibleLightHashHistory = visibleLightHashHistory,
                risLightSamples = risLightSamples,
                shader = _lightingPipelineResource.risLightSamplingShader,
                renderSize = renderSize,
                tilesPerRow = tilesX,
                frameIndex = frameIndex
            });

            builder.SetRenderFunc<RisLightSamplingPassData>(static (ref readonly passData, computeCtx) =>
            {
                var depthSrv = computeCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource);
                var gb0Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer0, BindlessAccess.ShaderResource);
                var gb1Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer1, BindlessAccess.ShaderResource);
                var tileLightListSrv = computeCtx.GetActualBindlessIndex(passData.tileLightList, BindlessAccess.ShaderResource);
                var historyHashSrv = passData.visibleLightHashHistory.IsValid
                    ? computeCtx.GetActualBindlessIndex(passData.visibleLightHashHistory, BindlessAccess.ShaderResource)
                    : RHIUtility.INVALID_BINDLESS_INDEX;
                var samplesUav = computeCtx.GetActualBindlessIndex(passData.risLightSamples, BindlessAccess.UnorderedAccess);

                var props = new InternalRisLightSamplingShaderProperties
                {
                    depthTextureIndex = depthSrv,
                    gbuffer0Index = gb0Srv,
                    gbuffer1Index = gb1Srv,
                    tileLightListBufferIndex = tileLightListSrv,
                    visibleLightHashHistoryBufferIndex = historyHashSrv,
                    risLightSamplesUav = samplesUav,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y,
                    tilesPerRow = passData.tilesPerRow,
                    frameIndex = passData.frameIndex
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute((passData.renderSize.x + 15u) / 16u, (passData.renderSize.y + 15u) / 16u, 1);
            });
        }

        // RisScreenContactShadow (Evaluates screen contact shadow & compacts unresolved rays)
        Identifier<RGTexture> risShadowMask;
        Identifier<RGBuffer> unresolvedRaysBuffer;
        Identifier<RGBuffer> counterBuffer;
        Identifier<RGBuffer> indirectArgsBuffer;

        using (var builder = rg.AddComputeRenderPass<RisScreenContactShadowPassData>("RisScreenContactShadow"))
        {
            var maxPixels = renderSize.x * renderSize.y;

            risShadowMask = rg.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8_UNorm, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource), "RisShadowMask");
            unresolvedRaysBuffer = builder.CreateBuffer(new BufferDesc
            {
                Size = maxPixels * 4u,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "RisUnresolvedRays");
            counterBuffer = builder.CreateBuffer(new BufferDesc
            {
                Size = 4u,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "RisRayCounter");

            builder.UseTexture(depthTexture, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer1, AccessFlags.Read);
            builder.UseTexture(risLightSamples, AccessFlags.Read);
            builder.UseTexture(risShadowMask, AccessFlags.Write);
            builder.UseBuffer(unresolvedRaysBuffer, AccessFlags.Write);
            builder.UseBuffer(counterBuffer, AccessFlags.ReadWrite);

            builder.SetPassData(new RisScreenContactShadowPassData
            {
                depthTexture = depthTexture,
                gbuffer1 = gbuffer.GBuffer1,
                risLightSamples = risLightSamples,
                risShadowMask = risShadowMask,
                unresolvedRaysBuffer = unresolvedRaysBuffer,
                counterBuffer = counterBuffer,
                shader = _lightingPipelineResource.risScreenContactShadowShader,
                renderSize = renderSize,
                viewId = viewId,
                frameIndex = frameIndex
            });

            builder.SetRenderFunc<RisScreenContactShadowPassData>(static (ref readonly passData, computeCtx) =>
            {
                computeCtx.ClearBuffer(passData.counterBuffer, 4);

                var depthSrv = computeCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource);
                var gb1Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer1, BindlessAccess.ShaderResource);
                var samplesSrv = computeCtx.GetActualBindlessIndex(passData.risLightSamples, BindlessAccess.ShaderResource);
                var shadowUav = computeCtx.GetActualBindlessIndex(passData.risShadowMask, BindlessAccess.UnorderedAccess);
                var raysUav = computeCtx.GetActualBindlessIndex(passData.unresolvedRaysBuffer, BindlessAccess.UnorderedAccess);
                var counterUav = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.UnorderedAccess);

                var props = new InternalRisScreenContactShadowShaderProperties
                {
                    depthTextureIndex = depthSrv,
                    gbuffer1Index = gb1Srv,
                    risLightSamplesIndex = samplesSrv,
                    risShadowMaskUav = shadowUav,
                    unresolvedRaysBufferUav = raysUav,
                    counterBufferUav = counterUav,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y,
                    viewId = passData.viewId,
                    frameIndex = passData.frameIndex
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute((passData.renderSize.x + 7u) / 8u, (passData.renderSize.y + 7u) / 8u, 1);
            });
        }

        // PrepareRisRayTraceIndirectArgs (Computes 1D thread group dispatch size from ray count)
        using (var builder = rg.AddComputeRenderPass<PrepareRisRayTraceIndirectArgsPassData>("PrepareRisRayTraceIndirectArgs"))
        {
            var indirectDesc = new BufferDesc
            {
                Size = 12u,
                Stride = 4,
                Usage = BufferUsage.IndirectArgument | BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            };

            indirectArgsBuffer = builder.CreateBuffer(in indirectDesc, "RisRayTraceIndirectArgs");

            builder.UseBuffer(counterBuffer, AccessFlags.Read);
            builder.UseBuffer(indirectArgsBuffer, AccessFlags.Write);

            builder.SetPassData(new PrepareRisRayTraceIndirectArgsPassData
            {
                counterBuffer = counterBuffer,
                indirectArgsBuffer = indirectArgsBuffer,
                shader = _lightingPipelineResource.prepareRisRayTraceIndirectArgsShader
            });

            builder.SetRenderFunc<PrepareRisRayTraceIndirectArgsPassData>(static (ref readonly passData, computeCtx) =>
            {
                var counterUav = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.UnorderedAccess);
                var indirectUav = computeCtx.GetActualBindlessIndex(passData.indirectArgsBuffer, BindlessAccess.UnorderedAccess);

                var props = new InternalPrepareRisRayTraceIndirectArgsShaderProperties
                {
                    counterBufferUav = counterUav,
                    indirectArgsBufferUav = indirectUav
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute(1, 1, 1);
            });
        }

        // RisRayTrace (1D Indirect DXR 1.1 shadow ray query with 100% active lane occupancy)
        using (var builder = rg.AddComputeRenderPass<RisRayTracePassData>("RisRayTrace"))
        {
            builder.UseTexture(depthTexture, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer1, AccessFlags.Read);
            builder.UseTexture(risLightSamples, AccessFlags.Read);
            builder.UseBuffer(unresolvedRaysBuffer, AccessFlags.Read);
            builder.UseBuffer(counterBuffer, AccessFlags.Read);
            builder.UseBuffer(indirectArgsBuffer, AccessFlags.Read);
            builder.UseBuffer(tlasBuffer, AccessFlags.Read);
            builder.UseTexture(risShadowMask, AccessFlags.ReadWrite);

            builder.SetPassData(new RisRayTracePassData
            {
                depthTexture = depthTexture,
                gbuffer1 = gbuffer.GBuffer1,
                risLightSamples = risLightSamples,
                risShadowMask = risShadowMask,
                unresolvedRaysBuffer = unresolvedRaysBuffer,
                counterBuffer = counterBuffer,
                indirectArgsBuffer = indirectArgsBuffer,
                commandSignature = _dispatchCommandSignature,
                shader = _lightingPipelineResource.risRayTraceShader,
                renderSize = renderSize,
                tlas = tlasBuffer,
                viewId = viewId,
                frameIndex = frameIndex
            });

            builder.SetRenderFunc<RisRayTracePassData>(static (ref readonly passData, computeCtx) =>
            {
                var depthSrv = computeCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource);
                var gb1Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer1, BindlessAccess.ShaderResource);
                var samplesSrv = computeCtx.GetActualBindlessIndex(passData.risLightSamples, BindlessAccess.ShaderResource);
                var shadowUav = computeCtx.GetActualBindlessIndex(passData.risShadowMask, BindlessAccess.UnorderedAccess);
                var raysSrv = computeCtx.GetActualBindlessIndex(passData.unresolvedRaysBuffer, BindlessAccess.ShaderResource);
                var counterSrv = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.ShaderResource);
                var actualIndirectBuf = computeCtx.GetActualBuffer(passData.indirectArgsBuffer);
                var tlasSrv = passData.tlas.IsValid ? computeCtx.GetActualBindlessIndex(passData.tlas, BindlessAccess.ShaderResource) : RHIUtility.INVALID_BINDLESS_INDEX;

                var props = new InternalRisRayTraceShaderProperties
                {
                    depthTextureIndex = depthSrv,
                    gbuffer1Index = gb1Srv,
                    risLightSamplesIndex = samplesSrv,
                    risShadowMaskUav = shadowUav,
                    unresolvedRaysBufferSrv = raysSrv,
                    counterBufferSrv = counterSrv,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y,
                    tlasIndex = tlasSrv,
                    viewId = passData.viewId,
                    frameIndex = passData.frameIndex
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, 0);
            });
        }

        // RisBuildVisibleLightHash (Constructs per-tile 128-bit Bloom filter of visible lights)
        var hashBufferSize = tilesX * tilesY * 16u;
        var rawHashDesc = new BufferDesc
        {
            Size = hashBufferSize,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };
        var filteredHashDesc = new BufferDesc
        {
            Size = hashBufferSize,
            Stride = 4,
            Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
        };

        Identifier<RGBuffer> rawVisibleLightHash;
        using (var builder = rg.AddComputeRenderPass<RisBuildVisibleLightHashPassData>("RisBuildVisibleLightHash"))
        {
            rawVisibleLightHash = builder.CreateBuffer(in rawHashDesc, "RisVisibleLightHashRaw");
            builder.UseTexture(risLightSamples, AccessFlags.Read);
            builder.UseTexture(risShadowMask, AccessFlags.Read);
            builder.UseBuffer(rawVisibleLightHash, AccessFlags.Write);

            builder.SetPassData(new RisBuildVisibleLightHashPassData
            {
                risLightSamples = risLightSamples,
                risShadowMask = risShadowMask,
                visibleLightHashUav = rawVisibleLightHash,
                shader = _lightingPipelineResource.risBuildVisibleLightHashShader,
                renderSize = renderSize,
                tilesPerRow = tilesX
            });

            builder.SetRenderFunc<RisBuildVisibleLightHashPassData>(static (ref readonly passData, computeCtx) =>
            {
                var samplesSrv = computeCtx.GetActualBindlessIndex(passData.risLightSamples, BindlessAccess.ShaderResource);
                var shadowSrv = computeCtx.GetActualBindlessIndex(passData.risShadowMask, BindlessAccess.ShaderResource);
                var hashUav = computeCtx.GetActualBindlessIndex(passData.visibleLightHashUav, BindlessAccess.UnorderedAccess);

                var props = new InternalRisBuildVisibleLightHashShaderProperties
                {
                    risLightSamplesIndex = samplesSrv,
                    risShadowMaskIndex = shadowSrv,
                    visibleLightHashUav = hashUav,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y,
                    tilesPerRow = passData.tilesPerRow
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute((passData.renderSize.x + 15u) / 16u, (passData.renderSize.y + 15u) / 16u, 1);
            });
        }

        // RisFilterVisibleLightHash (Dilates 128-bit Bloom filter cross-tile to preserve penumbra boundaries under motion)
        Identifier<RGBuffer> filteredVisibleLightHash;
        using (var builder = rg.AddComputeRenderPass<RisFilterVisibleLightHashPassData>("RisFilterVisibleLightHash"))
        {
            filteredVisibleLightHash = builder.CreateBuffer(in filteredHashDesc, "RisVisibleLightHashFiltered");
            builder.UseBuffer(rawVisibleLightHash, AccessFlags.Read);
            builder.UseBuffer(filteredVisibleLightHash, AccessFlags.Write);

            builder.SetPassData(new RisFilterVisibleLightHashPassData
            {
                rawVisibleLightHash = rawVisibleLightHash,
                filteredVisibleLightHash = filteredVisibleLightHash,
                shader = _lightingPipelineResource.risFilterVisibleLightHashShader,
                tilesPerRow = tilesX,
                tilesPerColumn = tilesY
            });

            builder.SetRenderFunc<RisFilterVisibleLightHashPassData>(static (ref readonly passData, computeCtx) =>
            {
                var rawSrv = computeCtx.GetActualBindlessIndex(passData.rawVisibleLightHash, BindlessAccess.ShaderResource);
                var filteredUav = computeCtx.GetActualBindlessIndex(passData.filteredVisibleLightHash, BindlessAccess.UnorderedAccess);

                var props = new InternalRisFilterVisibleLightHashShaderProperties
                {
                    rawVisibleLightHashBufferIndex = rawSrv,
                    filteredVisibleLightHashUav = filteredUav,
                    tilesPerRow = passData.tilesPerRow,
                    tilesPerColumn = passData.tilesPerColumn
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute((passData.tilesPerRow + 7u) / 8u, (passData.tilesPerColumn + 7u) / 8u, 1);
            });

            if (risResource.VisibleLightHashHistory.IsValid)
            {
                builder.QueueBufferExtraction(filteredVisibleLightHash, risResource.VisibleLightHashHistory);
            }
        }

        // RisResolveShading (Evaluates diffuse irradiance & specular radiance)

        Identifier<RGTexture> resolvedDiffuse;
        Identifier<RGTexture> resolvedSpecular;

        using (var builder = rg.AddComputeRenderPass<RisResolveShadingPassData>("RisResolveShading"))
        {
            var diffuseDesc = RGTextureDesc.Relative(1.0f, TextureFormat.R16G16B16A16_Float, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
            var specularDesc = RGTextureDesc.Relative(1.0f, TextureFormat.R16G16B16A16_Float, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource);
            resolvedDiffuse = builder.CreateTexture(in diffuseDesc, "RisResolvedDiffuse");
            resolvedSpecular = builder.CreateTexture(in specularDesc, "RisResolvedSpecular");

            builder.UseTexture(depthTexture, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer0, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer1, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer2, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer3, AccessFlags.Read);
            builder.UseBuffer(tileShadingModelMaskBuffer, AccessFlags.Read);
            builder.UseTexture(risLightSamples, AccessFlags.Read);
            builder.UseTexture(risShadowMask, AccessFlags.Read);
            builder.UseTexture(resolvedDiffuse, AccessFlags.Write);
            builder.UseTexture(resolvedSpecular, AccessFlags.Write);

            builder.SetPassData(new RisResolveShadingPassData
            {
                depthTexture = depthTexture,
                gbuffer0 = gbuffer.GBuffer0,
                gbuffer1 = gbuffer.GBuffer1,
                gbuffer2 = gbuffer.GBuffer2,
                gbuffer3 = gbuffer.GBuffer3,
                tileShadingModelMaskBuffer = tileShadingModelMaskBuffer,
                risLightSamples = risLightSamples,
                risShadowMask = risShadowMask,
                risResolvedDiffuse = resolvedDiffuse,
                risResolvedSpecular = resolvedSpecular,
                variantRegistry = _assetManager.ShaderVariants,
                tilesPerRow = tilesX,
                tilesY = tilesY,
                renderSize = renderSize
            });

            builder.SetRenderFunc<RisResolveShadingPassData>(static (ref readonly passData, computeCtx) =>
            {
                var depthSrv = computeCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource);
                var gb0Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer0, BindlessAccess.ShaderResource);
                var gb1Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer1, BindlessAccess.ShaderResource);
                var gb2Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer2, BindlessAccess.ShaderResource);
                var gb3Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer3, BindlessAccess.ShaderResource);
                var maskBufferSrv = computeCtx.GetActualBindlessIndex(passData.tileShadingModelMaskBuffer, BindlessAccess.ShaderResource);
                var samplesSrv = computeCtx.GetActualBindlessIndex(passData.risLightSamples, BindlessAccess.ShaderResource);
                var shadowSrv = computeCtx.GetActualBindlessIndex(passData.risShadowMask, BindlessAccess.ShaderResource);
                var diffUav = computeCtx.GetActualBindlessIndex(passData.risResolvedDiffuse, BindlessAccess.UnorderedAccess);
                var specUav = computeCtx.GetActualBindlessIndex(passData.risResolvedSpecular, BindlessAccess.UnorderedAccess);

                var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.RisResolveShading);

                for (var i = 0; i < dispatchVariants.Length; i++)
                {
                    ref readonly var variant = ref dispatchVariants[i];

                    if (!passData.variantRegistry.IsBytecodeReady(variant.DenseIndex) ||
                        !computeCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.RisResolveShading))
                    {
                        continue;
                    }

                    ref readonly var variantRecord = ref passData.variantRegistry.GetVariant(new ShaderVariantIndex(variant.DenseIndex));
                    var shadingModelId = variantRecord.ShadingModelId;

                    var props = new RisResolveShadingShaderProperties
                    {
                        depthTextureIndex = depthSrv,
                        gbuffer0Srv = gb0Srv,
                        gbuffer1Srv = gb1Srv,
                        gbuffer2Srv = gb2Srv,
                        gbuffer3Srv = gb3Srv,
                        risLightSamplesIndex = samplesSrv,
                        risShadowMaskIndex = shadowSrv,
                        tileShadingModelMaskBufferIndex = maskBufferSrv,
                        risResolvedDiffuseUav = diffUav,
                        risResolvedSpecularUav = specUav,
                        renderWidth = passData.renderSize.x,
                        renderHeight = passData.renderSize.y,
                        tilesPerRow = passData.tilesPerRow,
                        shadingModelId = shadingModelId,
                        iblLutTextureSrv = RHIUtility.INVALID_BINDLESS_INDEX,
                        persistentViewStateBufferSrv = RHIUtility.INVALID_BINDLESS_INDEX,
                        viewId = 0,
                        skyboxTextureSrv = RHIUtility.INVALID_BINDLESS_INDEX,
                        skyboxMipCount = 0,
                        _pad0 = 0,
                        skyboxTintAndIntensity = default,
                        skyboxRotationSinCos = default,
                        _pad1 = 0,
                        _pad2 = 0
                    };

                    computeCtx.SetUserDataWithProperties(in props, target: DataTarget.Compute);
                    computeCtx.DispatchCompute(passData.tilesPerRow, passData.tilesY, 1);
                }
            });
        }

        // RisDenoiserTemporal (Reprojection, variance clamping, EMA accumulation)
        var historyDiffuse = rg.ImportTexture(risResource.DiffuseHistory, ResourceBarrierData.Common, ResourceBarrierData.Common);
        var historySpecular = rg.ImportTexture(risResource.SpecularHistory, ResourceBarrierData.Common, ResourceBarrierData.Common);

        Identifier<RGTexture> temporalDiffuse;
        Identifier<RGTexture> temporalSpecular;

        using (var builder = rg.AddComputeRenderPass<RisDenoiserTemporalPassData>("RisDenoiserTemporal"))
        {
            temporalDiffuse = builder.CreateTexture(historyDiffuse, "RisTemporalDiffuse");
            temporalSpecular = builder.CreateTexture(historySpecular, "RisTemporalSpecular");

            builder.UseTexture(depthTexture, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer1, AccessFlags.Read);
            builder.UseTexture(gbuffer.MotionVectors, AccessFlags.Read);
            builder.UseTexture(resolvedDiffuse, AccessFlags.Read);
            builder.UseTexture(resolvedSpecular, AccessFlags.Read);
            builder.UseTexture(historyDiffuse, AccessFlags.Read);
            builder.UseTexture(historySpecular, AccessFlags.Read);
            builder.UseTexture(temporalDiffuse, AccessFlags.Write);
            builder.UseTexture(temporalSpecular, AccessFlags.Write);

            builder.QueueTextureExtraction(temporalDiffuse, risResource.DiffuseHistory);
            builder.QueueTextureExtraction(temporalSpecular, risResource.SpecularHistory);

            builder.SetPassData(new RisDenoiserTemporalPassData
            {
                depthTexture = depthTexture,
                gbuffer1 = gbuffer.GBuffer1,
                motionVectors = gbuffer.MotionVectors,
                currentDiffuse = resolvedDiffuse,
                currentSpecular = resolvedSpecular,
                historyDiffuse = historyDiffuse,
                historySpecular = historySpecular,
                outputDiffuse = temporalDiffuse,
                outputSpecular = temporalSpecular,
                shader = _lightingPipelineResource.risDenoiserTemporalShader,
                renderSize = renderSize,
                historyValid = risResource.HasHistory ? 1u : 0u
            });

            builder.SetRenderFunc<RisDenoiserTemporalPassData>(static (ref readonly passData, computeCtx) =>
            {
                var depthSrv = computeCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource);
                var gb1Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer1, BindlessAccess.ShaderResource);
                var motionVecSrv = computeCtx.GetActualBindlessIndex(passData.motionVectors, BindlessAccess.ShaderResource);
                var curDiffSrv = computeCtx.GetActualBindlessIndex(passData.currentDiffuse, BindlessAccess.ShaderResource);
                var curSpecSrv = computeCtx.GetActualBindlessIndex(passData.currentSpecular, BindlessAccess.ShaderResource);
                var histDiffSrv = computeCtx.GetActualBindlessIndex(passData.historyDiffuse, BindlessAccess.ShaderResource);
                var histSpecSrv = computeCtx.GetActualBindlessIndex(passData.historySpecular, BindlessAccess.ShaderResource);
                var outDiffUav = computeCtx.GetActualBindlessIndex(passData.outputDiffuse, BindlessAccess.UnorderedAccess);
                var outSpecUav = computeCtx.GetActualBindlessIndex(passData.outputSpecular, BindlessAccess.UnorderedAccess);

                var props = new InternalRisDenoiserTemporalShaderProperties
                {
                    depthTextureIndex = depthSrv,
                    gbuffer1Index = gb1Srv,
                    motionVectorTextureIndex = motionVecSrv,
                    currentDiffuseIndex = curDiffSrv,
                    currentSpecularIndex = curSpecSrv,
                    historyDiffuseIndex = histDiffSrv,
                    historySpecularIndex = histSpecSrv,
                    outputDiffuseUav = outDiffUav,
                    outputSpecularUav = outSpecUav,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y,
                    historyValid = passData.historyValid
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute((passData.renderSize.x + 15u) / 16u, (passData.renderSize.y + 15u) / 16u, 1);
            });
        }

        // RisDenoiserSpatial (Edge-preserving cross-bilateral filter)
        Identifier<RGTexture> filteredDiffuse;
        Identifier<RGTexture> filteredSpecular;

        using (var builder = rg.AddComputeRenderPass<RisDenoiserSpatialPassData>("RisDenoiserSpatial"))
        {
            filteredDiffuse = builder.CreateTexture(resolvedDiffuse, "RisFilteredDiffuse");
            filteredSpecular = builder.CreateTexture(resolvedSpecular, "RisFilteredSpecular");

            builder.UseTexture(depthTexture, AccessFlags.Read);
            builder.UseTexture(gbuffer.GBuffer1, AccessFlags.Read);
            builder.UseTexture(temporalDiffuse, AccessFlags.Read);
            builder.UseTexture(temporalSpecular, AccessFlags.Read);
            builder.UseTexture(filteredDiffuse, AccessFlags.Write);
            builder.UseTexture(filteredSpecular, AccessFlags.Write);

            builder.SetPassData(new RisDenoiserSpatialPassData
            {
                depthTexture = depthTexture,
                gbuffer1 = gbuffer.GBuffer1,
                inputDiffuse = temporalDiffuse,
                inputSpecular = temporalSpecular,
                outputDiffuse = filteredDiffuse,
                outputSpecular = filteredSpecular,
                shader = _lightingPipelineResource.risDenoiserSpatialShader,
                renderSize = renderSize
            });

            builder.SetRenderFunc<RisDenoiserSpatialPassData>(static (ref readonly passData, computeCtx) =>
            {
                var depthSrv = computeCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource);
                var gb1Srv = computeCtx.GetActualBindlessIndex(passData.gbuffer1, BindlessAccess.ShaderResource);
                var inDiffSrv = computeCtx.GetActualBindlessIndex(passData.inputDiffuse, BindlessAccess.ShaderResource);
                var inSpecSrv = computeCtx.GetActualBindlessIndex(passData.inputSpecular, BindlessAccess.ShaderResource);
                var outDiffUav = computeCtx.GetActualBindlessIndex(passData.outputDiffuse, BindlessAccess.UnorderedAccess);
                var outSpecUav = computeCtx.GetActualBindlessIndex(passData.outputSpecular, BindlessAccess.UnorderedAccess);

                var props = new InternalRisDenoiserSpatialShaderProperties
                {
                    depthTextureIndex = depthSrv,
                    gbuffer1Index = gb1Srv,
                    inputDiffuseIndex = inDiffSrv,
                    inputSpecularIndex = inSpecSrv,
                    outputDiffuseUav = outDiffUav,
                    outputSpecularUav = outSpecUav,
                    renderWidth = passData.renderSize.x,
                    renderHeight = passData.renderSize.y
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute((passData.renderSize.x + 15u) / 16u, (passData.renderSize.y + 15u) / 16u, 1);
            });
        }

        var risData = new RisPassData
        {
            resolvedDiffuse = filteredDiffuse,
            resolvedSpecular = filteredSpecular
        };
        rg.Blackboard.Add(ref risData);
    }
}
