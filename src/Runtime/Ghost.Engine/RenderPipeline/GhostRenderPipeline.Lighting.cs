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
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine.RenderPipeline;

internal unsafe partial class GhostRenderPipeline
{
    private struct PerViewShadowSetupJob : IJobParallelFor
    {
        public ReadOnlyView<PunctualLightRequest> lights;
        public UnsafeArray<int> shadowIndices;
        public Frustum frustum;
        public ShadowAtlasRegionAllocator* allocator;
        public float meshletLodErrorThreshold;
        public float viewHeight;
        public float3 cameraPosition;

        public UnsafeList<GPUShadowViewData>.ParallelWriter shadowViewsWriter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetPointFaceOrientation(int face, out float3 dir, out float3 up)
        {
            switch (face)
            {
                case 0: dir = new float3(1, 0, 0); up = new float3(0, 1, 0); break;
                case 1: dir = new float3(-1, 0, 0); up = new float3(0, 1, 0); break;
                case 2: dir = new float3(0, 1, 0); up = new float3(0, 0, -1); break;
                case 3: dir = new float3(0, -1, 0); up = new float3(0, 0, 1); break;
                case 4: dir = new float3(0, 0, 1); up = new float3(0, 1, 0); break;
                default: dir = new float3(0, 0, -1); up = new float3(0, 1, 0); break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float4x4 CreatePerspectiveReversedZ(float fovRadians, float aspect, float nearClip, float farClip)
        {
            var m11 = 1.0f / math.tan(fovRadians * 0.5f);
            var m00 = m11 / aspect;
            var m22 = nearClip / (nearClip - farClip);
            var m23 = farClip * nearClip / (farClip - nearClip);

            return new float4x4(
                m00, 0.0f, 0.0f, 0.0f,
                0.0f, m11, 0.0f, 0.0f,
                0.0f, 0.0f, m22, m23,
                0.0f, 0.0f, 1.0f, 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetNonDirectionalSoftness(float softnessScale, float viewHeight, float shapeRadius)
        {
            // This derivation has been fitted with quartic regression checking against raytracing reference and with a resolution of 512
            var x = shapeRadius * softnessScale;
            var x2 = x * x;
            var softness = 0.02403461f + 3.452916f * x - 1.362672f * x2 + 0.6700115f * x2 * x + 0.2159474f * x2 * x2;
            softness /= 100.0f;

            softness *= viewHeight / 512;  // Make it resolution independent whereas the baseline is 512

            return softness;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetSlopeBasedBias(float softness)
        {
            // Shadow may select a different meshlet LOD than the main camera, so we need to increase the bias for softer shadows to avoid self-shadowing artifacts.
            const float maxBaseBias = 18.0f;
            var baseBias = 5.0f;
            return math.lerp(baseBias, maxBaseBias, math.min(1.0f, (softness * 100) / 5));
        }

        public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
        {
            ref readonly var request = ref lights[loopIndex];
            ref readonly var light = ref request.light;
            ref readonly var shadowData = ref request.shadowData;

            var lightPosRWS = light.positionWS - cameraPosition;

            if (light.shadowDimmer <= 0.0f || shadowData.fadeDistance < math.length(lightPosRWS))
            {
                return;
            }

            if (!MathUtility.SphereIntersectFrustum(lightPosRWS, light.range, frustum.planes))
            {
                return;
            }

            var shadowSize = (light.lightTypeAndShadowSize >> 4) & 0xFFFFu;
            if (shadowSize == 0)
            {
                return;
            }

            Logger.DebugAssert(shadowSize >= 32);

            var size = new float4(allocator->AtlasSize, 1.0f / allocator->AtlasSize, shadowSize, 1.0f / shadowSize);
            var nearPlane = shadowData.nearPlane > 0.0f ? shadowData.nearPlane : 0.05f;
            var farPlane = light.range;
            var lightType = light.lightTypeAndShadowSize & 0xFu;
            var lodErrorThreshold = meshletLodErrorThreshold * (viewHeight / shadowSize) * (shadowData.lodErrorScale > 0 ? shadowData.lodErrorScale : 1.0f);

            // TODO: Expose softnessScale?
            var softness = GetNonDirectionalSoftness(1.0f, viewHeight, light.size.x);
            var basis = GetSlopeBasedBias(softness);

            if (lightType == (uint)GPULightType.Spot) // Spot
            {
                if (!allocator->Allocate(shadowSize, out var region))
                {
                    return;
                }

                var cosOuter = (light.spotAngleScale > 0.0001f)
                    ? math.clamp(-light.spotAngleOffset / light.spotAngleScale, -1.0f, 1.0f)
                    : 0.7071f;
                var outerAngle = math.acos(cosOuter);
                var fov = math.clamp(outerAngle * 2.0f, 0.01f, math.radians(175.0f));

                var up = math.abs(light.directionWS.y) > 0.99f ? new float3(0.0f, 0.0f, 1.0f) : new float3(0.0f, 1.0f, 0.0f);
                var viewMat = CreateLookAtMatrix(lightPosRWS, light.directionWS, up);
                var projMat = CreatePerspectiveReversedZ(fov, 1.0f, nearPlane, farPlane);
                var shadowViewProj = math.mul(projMat, viewMat);
                var shadowFrustum = Frustum.Create(shadowViewProj, lightPosRWS, light.directionWS, nearPlane, farPlane);
                var pro = new float4(projMat[0][0], projMat[1][1], projMat[2][2], projMat[3][2]);

                var worldTexelSize = 2.0f / projMat[0][0] / shadowSize * 1.4142135623731f;

                var svData = new GPUShadowViewData
                {
                    cullingData = new GPUShadowCullingData
                    {
                        planes = shadowFrustum.planes,
                        size = size,
                        lightPosRWSAndProj11 = new float4(lightPosRWS, projMat[1][1]),
                        lodErrorThreshold = lodErrorThreshold,
                    },
                    samplingData = new GPUShadowSamplingData
                    {
                        shadowViewProj = shadowViewProj,
                        tileOffsetScale = region.tileOffsetScale,
                        shadowProjection = pro,
                        size = size,
                        normalBias = shadowData.normalBias * worldTexelSize,
                        slopeBias = basis * shadowData.depthBias * worldTexelSize,
                        worldTexelSize = worldTexelSize,
                    }
                };

                var slot = shadowViewsWriter.AddNoResize(svData);
                shadowIndices[loopIndex] = slot;
            }
            else if (lightType == (uint)GPULightType.Point) // Point
            {
                Span<ShadowAtlasRegion> faceRegions = stackalloc ShadowAtlasRegion[6];
                if (!allocator->AllocatePointLightBlock(shadowSize, faceRegions))
                {
                    return;
                }

                const float guardPixels = 4.0f;
                // We bias the fov here because point light use cubemap for shadow, and we need to give it room for filtering.
                var fov = 2.0f * math.atan(shadowSize / (shadowSize - 2.0f * guardPixels)); // 1.570796f; // 90 degrees in radians
                var projMat = CreatePerspectiveReversedZ(fov, 1.0f, nearPlane, farPlane);
                var pro = new float4(projMat[0][0], projMat[1][1], projMat[2][2], projMat[3][2]);

                var worldTexelSize = 2.0f / projMat[0][0] / shadowSize * 1.4142135623731f;

                Span<GPUShadowViewData> faceViews = stackalloc GPUShadowViewData[6];
                for (var f = 0; f < 6; f++)
                {
                    GetPointFaceOrientation(f, out var dir, out var up);
                    var viewMat = CreateLookAtMatrix(lightPosRWS, dir, up);
                    var shadowViewProj = math.mul(projMat, viewMat);
                    var shadowFrustum = Frustum.Create(shadowViewProj, lightPosRWS, dir, nearPlane, farPlane);

                    faceViews[f] = new GPUShadowViewData
                    {
                        cullingData = new GPUShadowCullingData
                        {
                            planes = shadowFrustum.planes,
                            size = size,
                            lightPosRWSAndProj11 = new float4(lightPosRWS, projMat[1][1]),
                            lodErrorThreshold = lodErrorThreshold,
                        },
                        samplingData = new GPUShadowSamplingData
                        {
                            shadowViewProj = shadowViewProj,
                            tileOffsetScale = faceRegions[f].tileOffsetScale,
                            shadowProjection = pro,
                            size = size,
                            normalBias = shadowData.normalBias * worldTexelSize,
                            slopeBias = basis * shadowData.depthBias * worldTexelSize,
                            worldTexelSize = worldTexelSize,
                        }
                    };
                }

                var baseSlot = shadowViewsWriter.AddRangeNoResize(faceViews, 6);
                shadowIndices[loopIndex] = baseSlot;
            }
        }
    }

    private void ExecutePerViewShadowSetup(ResourceContext ctx, in Frustum viewFrustum, float viewHeight, ReadOnlyView<PunctualLightRequest> lights, float3 cameraPosition,
        out uint shadowViewsBufferSrv, out uint shadowViewCount, out uint shadowIndicesBufferSrv)
    {
        shadowViewsBufferSrv = RHIUtility.INVALID_BINDLESS_INDEX;
        shadowViewCount = 0;
        shadowIndicesBufferSrv = RHIUtility.INVALID_BINDLESS_INDEX;

        var lightCount = lights.Length;
        if (lightCount == 0)
        {
            return;
        }

        using var stackScope = AllocationManager.CreateStackScope();

        // Create a temporary array to hold the lights and their shadow data for processing.
        // TODO: Anyway to avoid this copy?
        using var shadowIndices = new UnsafeArray<int>(lightCount, stackScope.AllocationHandle);
        shadowIndices.AsSpan().Fill(-1);

        using var shadowViews = new UnsafeList<GPUShadowViewData>(Math.Max(16, lightCount * 6), AllocationHandle.TempRender);
        using var atlasAllocator = new ShadowAtlasRegionAllocator(_settings.ShadowAtlasResolution, AllocationHandle.TempRender);
        var job = new PerViewShadowSetupJob
        {
            lights = lights,
            frustum = viewFrustum,
            allocator = &atlasAllocator,
            shadowViewsWriter = shadowViews.AsParallelWriter(),
            shadowIndices = shadowIndices,
            meshletLodErrorThreshold = _settings.MeshletLodErrorThreshold,
            viewHeight = viewHeight,
            cameraPosition = cameraPosition
        };

        if (lightCount <= 64)
        {
            _jobScheduler.RunParallelFor(ref job, lightCount);
        }
        else
        {
            var handle = _jobScheduler.ScheduleParallelFor(in job, lightCount, 64, JobPriority.Normal);
            _jobScheduler.Wait(handle);
        }

        shadowViewCount = (uint)shadowViews.Count;
        if (shadowViewCount > 0)
        {
            var bufSize = shadowViewCount * (nuint)sizeof(GPUShadowViewData);
            var desc = new BufferDesc
            {
                Size = bufSize,
                Stride = (uint)sizeof(GPUShadowViewData),
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload
            };

            var svBuffer = ctx.ResourceManager.CreateTransientBuffer(in desc, "ShadowViewsBuffer");
            var pData = (GPUShadowViewData*)ctx.ResourceDatabase.MapResource(svBuffer.AsResource(), 0, null);
            MemoryUtility.MemCpy(pData, shadowViews.GetUnsafePtr(), bufSize);
            ctx.ResourceDatabase.UnmapResource(svBuffer.AsResource(), 0, null);
            shadowViewsBufferSrv = ctx.ResourceDatabase.GetBindlessIndex(svBuffer.AsResource());

            var indicesBufSize = (nuint)(lightCount * sizeof(int));
            var indicesDesc = new BufferDesc
            {
                Size = indicesBufSize,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
                HeapType = HeapType.Upload
            };

            var indicesBuffer = ctx.ResourceManager.CreateTransientBuffer(in indicesDesc, "ShadowIndicesBuffer");
            var pIndicesData = (int*)ctx.ResourceDatabase.MapResource(indicesBuffer.AsResource(), 0, null);
            MemoryUtility.MemCpy(pIndicesData, shadowIndices.GetUnsafePtr(), indicesBufSize);
            ctx.ResourceDatabase.UnmapResource(indicesBuffer.AsResource(), 0, null);
            shadowIndicesBufferSrv = ctx.ResourceDatabase.GetBindlessIndex(indicesBuffer.AsResource());
        }
    }

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

    private struct TileLightCullingPassData
    {
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGBuffer> tileLightList;
        public Handle<ComputeShader> shader;
        public uint2 renderSize;
        public uint tilesX;
        public uint tilesY;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4x4 CreateLookAtMatrix(float3 eye, float3 forward, float3 up)
    {
        var zAxis = math.normalize(forward);
        var xAxis = math.normalize(math.cross(up, zAxis));
        var yAxis = math.cross(zAxis, xAxis);

        return new float4x4(
            xAxis.x, xAxis.y, xAxis.z, -math.dot(xAxis, eye),
            yAxis.x, yAxis.y, yAxis.z, -math.dot(yAxis, eye),
            zAxis.x, zAxis.y, zAxis.z, -math.dot(zAxis, eye),
            0.0f, 0.0f, 0.0f, 1.0f);
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
        public ShaderVariantRegistry variantRegistry;
        public uint tilesPerRow;
        public uint tilesY;
        public uint2 renderSize;
        public uint2 hzbSize;
        public uint hzbMipCount;
        public Identifier<RGBuffer> tlas;
    }

    private Identifier<RGTexture> AddDeferredLightingPass(RenderGraph rg, in SkyContext skyContext, Identifier<RGTexture> hzbTexture, uint hzbMipCount, uint2 hzbSize,
        Identifier<RGTexture> depthTexture, Identifier<RGBuffer> tlasBuffer, uint viewId, uint2 renderSize)
    {
        ref readonly var gbufferData = ref rg.Blackboard.Get<GBufferPassData>();
        ref readonly var tile = ref rg.Blackboard.Get<TileClassificationData>();

        ref readonly var shadowMap = ref rg.Blackboard.TryGet<ShadowMapPassData>(out var hasShadowMap);
        ref readonly var ris = ref rg.Blackboard.TryGet<RisPassData>(out var hasRis);

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

        var litColorTarget = rg.CreateTexture(in litColorDesc, "HDRColorBuffer");
        rg.AddDiscardRenderTargetPass(litColorTarget, "ClearHDRColorBuffer");

        using var builder = rg.AddComputeRenderPass<DeferredLightingPassData>("DeferredLighting");

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
            tlas = tlasBuffer
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

    private struct ShadowCullPassData
    {
        public IWorkGraphProgram cullWorkGraphProgram;
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public uint instanceCount;
        public uint shadowViewCount;
        public uint shadowViewsBufferSrv;
        public uint entrypointIndex;
        public uint maxVisibleMeshlets;
        public uint counterBufferSize;
        public float cullingThreshold;
    }

    private struct ClearShadowAtlasPassData
    {
        public Identifier<RGTexture> shadowAtlas;
        public Handle<ComputeShader> shader;
        public uint atlasResolution;
    }

    private struct SoftwareShadowRasterPassData
    {
        public Identifier<RGTexture> shadowAtlas;
        public Identifier<RGBuffer> binnedMeshlets;
        public Identifier<RGBuffer> binOffsetsBuffer;
        public Identifier<RGBuffer> indirectArgs;
        public Handle<ComputeShader> shader;
        public ICommandSignature commandSignature;
        public uint shadowViewsBufferSrv;
        public uint atlasResolution;
        public uint swIndirectOffset;
    }

    private struct PunctualShadowRasterPassData
    {
        public Identifier<RGTexture> shadowAtlas;
        public Identifier<RGBuffer> binnedMeshlets;
        public Identifier<RGBuffer> binOffsetsBuffer;
        public Identifier<RGBuffer> indirectArgs;
        public ICommandSignature commandSignature;
        public uint shadowViewsBufferSrv;
        public uint atlasResolution;
        public ShaderVariantRegistry variantRegistry;
    }

    private Identifier<RGTexture> AddPunctualShadowAtlasPass(RenderGraph rg, uint instanceCount, uint shadowViewsBufferSrv, uint shadowViewCount)
    {
        if (shadowViewCount == 0 || instanceCount == 0)
        {
            return Identifier<RGTexture>.Invalid;
        }

        _meshPipelineResource.EnsureWorkGraphProgram(_renderEngine);
        var shadowProgram = _meshPipelineResource.shadowCullWorkGraphProgram;
        if (shadowProgram == null)
        {
            return Identifier<RGTexture>.Invalid;
        }

        var entrypointIndex = shadowProgram.GetEntrypointIndex("InstanceCullNode");
        if (entrypointIndex == uint.MaxValue)
        {
            entrypointIndex = 0;
        }

        var entrySize = (uint)sizeof(UnbinnedMeshletEntry);
        var visibleBufferSize = _settings.MaxVisibleMeshletsOnScreen * entrySize;

        var maxVariants = GetMaxCullVariants();
        var counterBufferSize = CullCommon.GetCounterBufferSize(maxVariants);

        Identifier<RGBuffer> unbinnedMeshlets;
        Identifier<RGBuffer> counterBuffer;
        using (var builder = rg.AddComputeRenderPass<ShadowCullPassData>("MeshletCull_Shadow"))
        {
            // Work Graph Shadow Culling
            unbinnedMeshlets = builder.CreateBuffer(new BufferDesc
            {
                Size = visibleBufferSize,
                Stride = entrySize,
                Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowUnbinnedMeshlets");

            counterBuffer = builder.CreateBuffer(new BufferDesc
            {
                Size = counterBufferSize,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowCounterBuffer");

            builder.AllowPassCulling(false);
            builder.UseBuffer(unbinnedMeshlets, AccessFlags.Write);
            builder.UseBuffer(counterBuffer, AccessFlags.ReadWrite);

            builder.SetPassData(new ShadowCullPassData
            {
                cullWorkGraphProgram = shadowProgram,
                visibleMeshlets = unbinnedMeshlets,
                counterBuffer = counterBuffer,
                instanceCount = instanceCount,
                shadowViewCount = shadowViewCount,
                shadowViewsBufferSrv = shadowViewsBufferSrv,
                entrypointIndex = entrypointIndex,
                maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen,
                counterBufferSize = counterBufferSize,
                cullingThreshold = _settings.InstanceCullingThreshold,
            });

            builder.SetRenderFunc<ShadowCullPassData>(static (ref readonly passData, computeCtx) =>
            {
                computeCtx.ClearBuffer(passData.counterBuffer, passData.counterBufferSize);

                var visibleUav = computeCtx.GetActualBindlessIndex(passData.visibleMeshlets, BindlessAccess.UnorderedAccess);
                var counterUav = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.UnorderedAccess);

                var props = new InternalShadowCullGraphShaderProperties
                {
                    visibleMeshletsUav = visibleUav,
                    counterBufferUav = counterUav,
                    maxVisibleMeshlets = passData.maxVisibleMeshlets,
                    instanceCount = passData.instanceCount,
                    shadowViewsBuffer = passData.shadowViewsBufferSrv,
                    shadowViewCount = passData.shadowViewCount,
                    cullingThreshold = passData.cullingThreshold,
                    occluderHzbSrv = RHIUtility.INVALID_BINDLESS_INDEX,
                };

                computeCtx.SetWorkGraph(passData.cullWorkGraphProgram);
                computeCtx.SetUserDataWithProperties(in props);

                var threadGroupsX = Math.Max(1u, (passData.instanceCount + 63) / 64);
                var record = new WorkGraphDispatchGridRecord(threadGroupsX, passData.shadowViewCount, 1);
                var dispatchDesc = DispatchGraphDesc.ForCPUInput(passData.entrypointIndex, 1, &record, (ulong)sizeof(WorkGraphDispatchGridRecord));
                computeCtx.DispatchGraph(in dispatchDesc);
            });
        }

        // Prepare Indirect Arguments & Bin Offsets for Shadow
        Identifier<RGBuffer> indirectArgs;
        Identifier<RGBuffer> binOffsets;
        Identifier<RGBuffer> binScatterCounters;
        using (var builder = rg.AddComputeRenderPass<PrepareIndirectArgsPassData>("PrepareShadowIndirectArgs"))
        {
            indirectArgs = builder.CreateBuffer(new BufferDesc
            {
                Size = CullCommon.GetIndirectArgsBufferSize(maxVariants),
                Stride = 4,
                Usage = BufferUsage.IndirectArgument | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowIndirectArgsBuffer");

            binOffsets = builder.CreateBuffer(new BufferDesc
            {
                Size = CullCommon.GetBinOffsetsBufferSize(maxVariants),
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowBinOffsetsBuffer");

            binScatterCounters = builder.CreateBuffer(new BufferDesc
            {
                Size = CullCommon.GetBinOffsetsBufferSize(maxVariants),
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowBinScatterCounters");

            builder.UseBuffer(counterBuffer, AccessFlags.Read);
            builder.UseBuffer(indirectArgs, AccessFlags.Write);
            builder.UseBuffer(binOffsets, AccessFlags.Write);
            builder.UseBuffer(binScatterCounters, AccessFlags.Write);

            builder.SetPassData(new PrepareIndirectArgsPassData
            {
                counterBuffer = counterBuffer,
                indirectArgsBuffer = indirectArgs,
                binOffsetsBuffer = binOffsets,
                binScatterCounters = binScatterCounters,
                shader = _meshPipelineResource.prepareIndirectArgsShader,
                cullPassIndex = 0,
                maxCount = _settings.MaxVisibleMeshletsOnScreen,
                maxVariants = maxVariants
            });

            builder.SetRenderFunc<PrepareIndirectArgsPassData>(static (ref readonly passData, computeCtx) =>
            {
                var counterUav = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.UnorderedAccess);
                var indirectUav = computeCtx.GetActualBindlessIndex(passData.indirectArgsBuffer, BindlessAccess.UnorderedAccess);
                var binOffsetsUav = computeCtx.GetActualBindlessIndex(passData.binOffsetsBuffer, BindlessAccess.UnorderedAccess);
                var binCountersUav = computeCtx.GetActualBindlessIndex(passData.binScatterCounters, BindlessAccess.UnorderedAccess);

                var props = new InternalPrepareMeshletIndirectArgsShaderProperties
                {
                    counterBuffer = counterUav,
                    indirectArgsBuffer = indirectUav,
                    binOffsetsBuffer = binOffsetsUav,
                    binScatterCounters = binCountersUav,
                    variantCountsOffset = CullCommon.OFFSET_PASS1_VARIANT_COUNTS,
                    visibleArgsOffset = CullCommon.INDIRECT_OFFSET_PASS1_VARIANTS,
                    maxCount = passData.maxCount,
                    maxVariants = passData.maxVariants,
                    occludedCountOffset = uint.MaxValue,
                    cullArgsOffset = uint.MaxValue,
                    swArgsOffset = uint.MaxValue,
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute(1, 1, 1);
            });
        }

        // Scatter Shadow Meshlets into contiguous variant bins
        Identifier<RGBuffer> binnedMeshlets;
        using (var builder = rg.AddComputeRenderPass<ScatterMeshletsPassData>("ScatterShadowMeshlets"))
        {
            binnedMeshlets = builder.CreateBuffer(new BufferDesc
            {
                Size = visibleBufferSize,
                Stride = entrySize,
                Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowBinnedMeshlets");

            builder.UseBuffer(unbinnedMeshlets, AccessFlags.Read);
            builder.UseBuffer(binnedMeshlets, AccessFlags.Write);
            builder.UseBuffer(binScatterCounters, AccessFlags.ReadWrite);
            builder.UseBuffer(counterBuffer, AccessFlags.Read);

            builder.SetPassData(new ScatterMeshletsPassData
            {
                unbinnedBuffer = unbinnedMeshlets,
                binnedBuffer = binnedMeshlets,
                binScatterCounters = binScatterCounters,
                counterBuffer = counterBuffer,
                shader = _meshPipelineResource.scatterMeshletsShader,
                totalVisibleOffset = CullCommon.OFFSET_PASS1_VISIBLE_COUNT,
                maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen,
                isShadowPass = 1
            });

            builder.SetRenderFunc<ScatterMeshletsPassData>(static (ref readonly passData, computeCtx) =>
            {
                var unbinnedSrv = computeCtx.GetActualBindlessIndex(passData.unbinnedBuffer, BindlessAccess.ShaderResource);
                var binnedUav = computeCtx.GetActualBindlessIndex(passData.binnedBuffer, BindlessAccess.UnorderedAccess);
                var countersUav = computeCtx.GetActualBindlessIndex(passData.binScatterCounters, BindlessAccess.UnorderedAccess);
                var counterBufUav = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.UnorderedAccess);

                var props = new InternalScatterMeshletsShaderProperties
                {
                    unbinnedBuffer = unbinnedSrv,
                    binnedBuffer = binnedUav,
                    binScatterCounters = countersUav,
                    counterBuffer = counterBufUav,
                    totalVisibleOffset = passData.totalVisibleOffset,
                    maxVisibleMeshlets = passData.maxVisibleMeshlets,
                    isShadowPass = passData.isShadowPass
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);

                var threadGroups = Math.Max(1u, (passData.maxVisibleMeshlets + 63u) / 64u);
                var threadGroupsX = Math.Min(threadGroups, 65535u);
                var threadGroupsY = (threadGroups + 65534u) / 65535u;
                computeCtx.DispatchCompute(threadGroupsX, threadGroupsY, 1);
            });
        }

        // Create and Clear Shadow Atlas UAV
        Identifier<RGTexture> shadowAtlas;
        using (var builder = rg.AddComputeRenderPass<ClearShadowAtlasPassData>("ClearShadowAtlas"))
        {
            var shadowAtlasDesc = RGTextureDesc.Absolute(
                _settings.ShadowAtlasResolution,
                _settings.ShadowAtlasResolution,
                TextureFormat.R32_Typeless,
                clearAtFirstUse: false,
                usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
                typelessViewFormat: new TypelessFormatDesc { Srv = TextureFormat.R32_Float, Uav = TextureFormat.R32_UInt });

            shadowAtlas = builder.CreateTexture(in shadowAtlasDesc, "PunctualShadowAtlas");

            builder.UseTexture(shadowAtlas, AccessFlags.Write);
            builder.SetPassData(new ClearShadowAtlasPassData
            {
                shadowAtlas = shadowAtlas,
                shader = _lightingPipelineResource.clearShadowAtlasShader,
                atlasResolution = _settings.ShadowAtlasResolution
            });

            builder.SetRenderFunc<ClearShadowAtlasPassData>(static (ref readonly passData, computeCtx) =>
            {
                var shadowAtlasUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.shadowAtlas).AsResource(), BindlessAccess.UnorderedAccess);
                var props = new InternalClearShadowAtlasShaderProperties
                {
                    shadowAtlasIndex = shadowAtlasUav,
                    atlasResolution = passData.atlasResolution
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                var threadGroups = Math.Max(1u, (passData.atlasResolution + 15u) / 16u);
                computeCtx.DispatchCompute(threadGroups, threadGroups, 1);
            });
        }

        // Hardware Raster Shadow Depth for Macro-triangles (Bins 0 and 2..N)
        using (var builder = rg.AddUnsafeRenderPass<PunctualShadowRasterPassData>("PunctualShadowRaster"))
        {
            builder.UseRandomAccessTexture(shadowAtlas);
            builder.UseBuffer(binnedMeshlets, AccessFlags.Read);
            builder.UseBuffer(binOffsets, AccessFlags.Read);
            builder.UseBuffer(indirectArgs, AccessFlags.Read);

            builder.SetPassData(new PunctualShadowRasterPassData
            {
                shadowAtlas = shadowAtlas,
                binnedMeshlets = binnedMeshlets,
                binOffsetsBuffer = binOffsets,
                indirectArgs = indirectArgs,
                commandSignature = _dispatchMeshCommandSignature,
                shadowViewsBufferSrv = shadowViewsBufferSrv,
                atlasResolution = _settings.ShadowAtlasResolution,
                variantRegistry = _assetManager.ShaderVariants,
            });

            builder.SetRenderFunc<PunctualShadowRasterPassData>(static (ref readonly passData, unsafeCtx) =>
            {
                unsafeCtx.SetViewport(new ViewportDesc
                {
                    X = 0,
                    Y = 0,
                    Width = passData.atlasResolution,
                    Height = passData.atlasResolution,
                    MinDepth = 0.0f,
                    MaxDepth = 1.0f
                });
                unsafeCtx.SetScissorRect(new RectDesc
                {
                    Left = 0,
                    Top = 0,
                    Right = passData.atlasResolution,
                    Bottom = passData.atlasResolution
                });

                var binnedMeshletsSrv = unsafeCtx.GetActualBindlessIndex(passData.binnedMeshlets, BindlessAccess.ShaderResource);
                var binOffsetsSrv = unsafeCtx.GetActualBindlessIndex(passData.binOffsetsBuffer, BindlessAccess.ShaderResource);
                var shadowAtlasUav = unsafeCtx.GetActualBindlessIndex(passData.shadowAtlas, BindlessAccess.UnorderedAccess);
                var actualIndirectBuf = unsafeCtx.GetActualBuffer(passData.indirectArgs);

                var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.Shadow);
                if (dispatchVariants.Length == 0)
                {
                    return;
                }

                // Slot 0: Opaque shadow caster (HW Bin 0)
                ref readonly var opaqueVariant = ref dispatchVariants[0];
                if (opaqueVariant.Shader.IsValid && unsafeCtx.TrySetActiveShaderPass(opaqueVariant.Shader, PassSemantic.Shadow))
                {
                    unsafeCtx.SetUserData(binnedMeshletsSrv, passData.shadowViewsBufferSrv, binOffsetsSrv, shadowAtlasUav << 8);
                    unsafeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, 0);
                }

                // Slots 1..N-1: Alpha-tested / masked shadow variants (HW Bins 2..N)
                // Note: Bin 1 is SW Opaque, so alpha variants start at targetVariant = 2.
                for (var i = 1; i < dispatchVariants.Length; i++)
                {
                    ref readonly var variant = ref dispatchVariants[i];
                    if (variant.Shader.IsValid && unsafeCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.Shadow))
                    {
                        var targetVariant = (uint)(i + 1);
                        unsafeCtx.SetUserData(binnedMeshletsSrv, passData.shadowViewsBufferSrv, binOffsetsSrv, (shadowAtlasUav << 8) | (targetVariant & 0xFFu));
                        unsafeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, (ulong)targetVariant * INDIRECT_ARGS_STRIDE);
                    }
                }
            });
        }

        // Software Raster Shadow Depth for Micro-triangles (Bin 1)
        using (var builder = rg.AddComputeRenderPass<SoftwareShadowRasterPassData>("RasterizeShadowSw"))
        {
            builder.UseTexture(shadowAtlas, AccessFlags.ReadWrite);
            builder.UseBuffer(binnedMeshlets, AccessFlags.Read);
            builder.UseBuffer(binOffsets, AccessFlags.Read);
            builder.UseBuffer(indirectArgs, AccessFlags.Read);

            builder.SetPassData(new SoftwareShadowRasterPassData
            {
                shadowAtlas = shadowAtlas,
                binnedMeshlets = binnedMeshlets,
                binOffsetsBuffer = binOffsets,
                indirectArgs = indirectArgs,
                shader = _lightingPipelineResource.rasterizeShadowSwShader,
                commandSignature = _dispatchCommandSignature,
                shadowViewsBufferSrv = shadowViewsBufferSrv,
                atlasResolution = _settings.ShadowAtlasResolution,
                swIndirectOffset = CullCommon.INDIRECT_OFFSET_PASS1_VARIANTS + 16u
            });

            builder.SetRenderFunc<SoftwareShadowRasterPassData>(static (ref readonly passData, computeCtx) =>
            {
                var shadowAtlasUav = computeCtx.GetActualBindlessIndex(passData.shadowAtlas, BindlessAccess.UnorderedAccess);
                var binnedMeshletsSrv = computeCtx.GetActualBindlessIndex(passData.binnedMeshlets, BindlessAccess.ShaderResource);
                var binOffsetsSrv = computeCtx.GetActualBindlessIndex(passData.binOffsetsBuffer, BindlessAccess.ShaderResource);

                var props = new InternalRasterizeShadowSwShaderProperties
                {
                    shadowAtlasUavIndex = shadowAtlasUav,
                    binnedMeshletsBuffer = binnedMeshletsSrv,
                    binOffsetsBuffer = binOffsetsSrv,
                    shadowViewsBufferSrv = passData.shadowViewsBufferSrv,
                    atlasResolution = passData.atlasResolution,
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);

                var actualIndirectBuf = computeCtx.GetActualBuffer(passData.indirectArgs);
                computeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, passData.swIndirectOffset);
            });
        }

        return shadowAtlas;
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

    private static readonly float4[] s_directionalQuadrantOffsets =
    [
        new float4(0.0f, 0.0f, 0.5f, 0.5f),
        new float4(0.5f, 0.0f, 0.5f, 0.5f),
        new float4(0.0f, 0.5f, 0.5f, 0.5f),
        new float4(0.5f, 0.5f, 0.5f, 0.5f)
    ];

    private struct DirectionalShadowSetupResult
    {
        public uint directionalShadowViewsBufferSrv;
        public uint cascadeCount;
        public uint shadowSize;
        public uint atlasResolution;
        public float4 cascadeSplits;
        public float4 cascadeBlends;
        public float maxShadowDistance;
        public float shadowFadeRange;
    }

    private struct ClearShadowOccludersPassData
    {
        public Identifier<RGTexture> occluderMap;
        public Handle<ComputeShader> clearShader;
        public uint cascadeCount;
    }

    private struct GenerateShadowOccludersPassData
    {
        public Identifier<RGTexture> depthTexture;
        public Identifier<RGTexture> occluderMap;
        public Handle<ComputeShader> generateShader;
        public uint directionalShadowViewsBufferSrv;
        public uint2 renderSize;
        public uint cascadeCount;
        public float4 cascadeSplits;
    }

    private struct BuildShadowOccluderHZBPassData
    {
        public Identifier<RGTexture> occluderMap;
        public Handle<ComputeShader> shader;
        public uint cascadeCount;
    }

    private struct DirectionalShadowCullPassData
    {
        public IWorkGraphProgram cullWorkGraphProgram;
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGTexture> occluderMap;
        public uint instanceCount;
        public uint cascadeCount;
        public uint directionalShadowViewsBufferSrv;
        public uint entrypointIndex;
        public uint maxVisibleMeshlets;
        public uint counterBufferSize;
        public float cullingThreshold;
    }

    private static float4x4 CreateOrthographicReversedZ(float sizeX, float sizeY, float nearClip, float farClip)
    {
        var m00 = 2.0f / sizeX;
        var m11 = 2.0f / sizeY;
        var range = farClip - nearClip;
        var m22 = -1.0f / range;
        var m23 = farClip / range;

        return new float4x4(
            m00, 0.0f, 0.0f, 0.0f,
            0.0f, m11, 0.0f, 0.0f,
            0.0f, 0.0f, m22, m23,
            0.0f, 0.0f, 0.0f, 1.0f);
    }

    private DirectionalShadowSetupResult ComputeDirectionalCascadeViews(ResourceContext ctx, in DirectionalLightRequest dirRequest, in RenderRequest request, in float4x4 projMatrix, in ShadowSettings shadowSettings, float viewHeight)
    {
        var count = shadowSettings.cascadeCount.GetValueOrDefault(4);
        var cascadeCount = (uint)math.clamp(count, 1, 4);

        var maxDist = shadowSettings.maxDistance.GetValueOrDefault(400.0f);
        maxDist = Math.Min(maxDist, request.view.farClipPlane);
        var shadowFadeRange = maxDist * 0.1f;

        var defaultSplits = new float3(0.1f, 0.3f, 0.6f);
        var userSplits = shadowSettings.splits.GetValueOrDefault(defaultSplits);
        float4 cascadeSplits;
        if (cascadeCount == 1)
        {
            cascadeSplits = new float4(maxDist, maxDist, maxDist, maxDist);
        }
        else if (cascadeCount == 2)
        {
            cascadeSplits = new float4(userSplits.x * maxDist, maxDist, maxDist, maxDist);
        }
        else if (cascadeCount == 3)
        {
            cascadeSplits = new float4(userSplits.x * maxDist, userSplits.y * maxDist, maxDist, maxDist);
        }
        else
        {
            cascadeSplits = new float4(userSplits.x * maxDist, userSplits.y * maxDist, userSplits.z * maxDist, maxDist);
        }

        var cascadeBlends = shadowSettings.blends.GetValueOrDefault(new float4(0.1f, 0.2f, 0.3f, 0.4f));

        var shadowSize = dirRequest.shadowSize > 0 ? dirRequest.shadowSize : 1024u;
        var atlasResolution = shadowSize * 2u;

        var tanHalfFovY = 1.0f / Math.Max(0.0001f, projMatrix[1][1]);
        var tanHalfFovX = 1.0f / Math.Max(0.0001f, projMatrix[0][0]);
        var fovDiag = math.sqrt(tanHalfFovX * tanHalfFovX + tanHalfFovY * tanHalfFovY);

        var camDir = math.normalize(request.view.localToWorld.c2.xyz);
        var lightDir = math.normalize(dirRequest.light.directionWS);
        var lightUp = math.abs(lightDir.y) > 0.99f ? new float3(0.0f, 0.0f, 1.0f) : new float3(0.0f, 1.0f, 0.0f);
        var lightRight = math.normalize(math.cross(lightUp, lightDir));
        lightUp = math.cross(lightDir, lightRight);

        Span<GPUShadowViewData> views = stackalloc GPUShadowViewData[(int)cascadeCount];

        for (var i = 0u; i < cascadeCount; i++)
        {
            var nearDist = (i == 0) ? request.view.nearClipPlane : cascadeSplits[(int)i - 1];
            var farDist = cascadeSplits[(int)i];

            var rNear = nearDist * fovDiag;
            var rFar = farDist * fovDiag;
            var cz = (farDist + nearDist) * 0.5f + (rFar * rFar - rNear * rNear) / Math.Max(0.001f, 2.0f * (farDist - nearDist));
            if (cz > farDist)
            {
                cz = farDist;
            }
            var sphereRadius = math.sqrt(rFar * rFar + (farDist - cz) * (farDist - cz));

            var centerRWS = camDir * cz;
            var worldTexelSize = (2.0f * sphereRadius) / shadowSize;

            var cx = math.dot(centerRWS, lightRight);
            var cy = math.dot(centerRWS, lightUp);
            var snappedCx = math.floor(cx / worldTexelSize) * worldTexelSize;
            var snappedCy = math.floor(cy / worldTexelSize) * worldTexelSize;
            var snappedCenterRWS = centerRWS + lightRight * (snappedCx - cx) + lightUp * (snappedCy - cy);

            var nearExtrusion = Math.Max(200.0f, sphereRadius * 2.0f);
            var farPlane = sphereRadius * 2.0f + nearExtrusion;
            var lightPosRWS = snappedCenterRWS - lightDir * (sphereRadius + nearExtrusion);

            var viewMat = CreateLookAtMatrix(lightPosRWS, lightDir, lightUp);
            var projMat = CreateOrthographicReversedZ(sphereRadius * 2.0f, sphereRadius * 2.0f, 0.05f, farPlane);
            var shadowViewProj = math.mul(projMat, viewMat);
            var shadowFrustum = Frustum.Create(shadowViewProj, lightPosRWS, lightDir, 0.05f, farPlane);

            var size = new float4(atlasResolution, 1.0f / atlasResolution, shadowSize, 1.0f / shadowSize);
            var ndcDepthScale = Math.Abs(projMat[2][2]);

            views[(int)i] = new GPUShadowViewData
            {
                cullingData = new GPUShadowCullingData
                {
                    planes = shadowFrustum.planes,
                    size = size,
                    lightPosRWSAndProj11 = new float4(lightPosRWS, projMat[1][1]),
                    lodErrorThreshold = _settings.MeshletLodErrorThreshold * (dirRequest.shadowData.lodErrorScale > 0 ? dirRequest.shadowData.lodErrorScale : 1.0f)
                },
                samplingData = new GPUShadowSamplingData
                {
                    shadowViewProj = shadowViewProj,
                    tileOffsetScale = s_directionalQuadrantOffsets[i],
                    shadowProjection = new float4(projMat[0][0], projMat[1][1], projMat[2][2], projMat[2][3]),
                    size = size,
                    worldTexelSize = worldTexelSize,
                    normalBias = dirRequest.shadowData.normalBias * worldTexelSize,
                    slopeBias = dirRequest.shadowData.depthBias * worldTexelSize * ndcDepthScale
                }
            };
        }

        var viewsBufferSize = cascadeCount * (uint)sizeof(GPUShadowViewData);
        var bufferDesc = new BufferDesc
        {
            Size = viewsBufferSize,
            Stride = (uint)sizeof(GPUShadowViewData),
            Usage = BufferUsage.Raw | BufferUsage.ShaderResource,
            HeapType = HeapType.Upload
        };

        var shadowViewsBuffer = ctx.ResourceManager.CreateTransientBuffer(in bufferDesc, "DirectionalShadowViewsBuffer");
        var pViews = (GPUShadowViewData*)ctx.ResourceDatabase.MapResource(shadowViewsBuffer.AsResource(), 0, null);
        fixed (GPUShadowViewData* pSrc = views)
        {
            MemoryUtility.MemCpy(pViews, pSrc, viewsBufferSize);
        }
        ctx.ResourceDatabase.UnmapResource(shadowViewsBuffer.AsResource(), 0, null);

        var directionalShadowViewsBufferSrv = ctx.ResourceDatabase.GetBindlessIndex(shadowViewsBuffer.AsResource());

        return new DirectionalShadowSetupResult
        {
            directionalShadowViewsBufferSrv = directionalShadowViewsBufferSrv,
            cascadeCount = cascadeCount,
            shadowSize = shadowSize,
            atlasResolution = atlasResolution,
            cascadeSplits = cascadeSplits,
            cascadeBlends = cascadeBlends,
            maxShadowDistance = maxDist,
            shadowFadeRange = shadowFadeRange
        };
    }

    private void AddGenerateShadowOccludersPass(RenderGraph rg, Identifier<RGTexture> depthTexture, Identifier<RGTexture> occluderMap, uint directionalShadowViewsBufferSrv, uint2 renderSize, uint cascadeCount, float4 cascadeSplits)
    {
        using (var clearBuilder = rg.AddComputeRenderPass<ClearShadowOccludersPassData>("ClearShadowOccluders"))
        {
            clearBuilder.UseTexture(occluderMap, AccessFlags.Write);
            clearBuilder.SetPassData(new ClearShadowOccludersPassData
            {
                occluderMap = occluderMap,
                clearShader = _lightingPipelineResource.clearShadowOccludersShader,
                cascadeCount = cascadeCount
            });

            clearBuilder.SetRenderFunc<ClearShadowOccludersPassData>(static (ref readonly passData, computeCtx) =>
            {
                var occluderActual = computeCtx.GetActualTexture(passData.occluderMap);
                var occluderRes = occluderActual.AsResource();

                Span<uint> uavs = stackalloc uint[4];
                for (var c = 0u; c < 4u; c++)
                {
                    uavs[(int)c] = c < passData.cascadeCount
                        ? computeCtx.ResourceDatabase.GetBindlessIndex(occluderRes, BindlessAccess.UnorderedAccess, c * 7u)
                        : RHIUtility.INVALID_BINDLESS_INDEX;
                }

                var clearProps = new InternalClearShadowOccludersShaderProperties
                {
                    occluderUav0 = uavs[0],
                    occluderUav1 = uavs[1],
                    occluderUav2 = uavs[2],
                    occluderUav3 = uavs[3]
                };
                computeCtx.SetActiveCompute(passData.clearShader, 0);
                computeCtx.SetUserDataWithProperties(in clearProps);
                computeCtx.DispatchCompute(8, 8, 1);
            });
        }

        using (var builder = rg.AddComputeRenderPass<GenerateShadowOccludersPassData>("GenerateShadowOccluders"))
        {
            builder.UseTexture(depthTexture, AccessFlags.Read);
            builder.UseTexture(occluderMap, AccessFlags.ReadWrite);

            builder.SetPassData(new GenerateShadowOccludersPassData
            {
                depthTexture = depthTexture,
                occluderMap = occluderMap,
                generateShader = _lightingPipelineResource.generateShadowOccludersShader,
                directionalShadowViewsBufferSrv = directionalShadowViewsBufferSrv,
                renderSize = renderSize,
                cascadeCount = cascadeCount,
                cascadeSplits = cascadeSplits
            });

            builder.SetRenderFunc<GenerateShadowOccludersPassData>(static (ref readonly passData, computeCtx) =>
            {
                var depthSrv = computeCtx.GetActualBindlessIndex(passData.depthTexture, BindlessAccess.ShaderResource);
                var occluderActual = computeCtx.GetActualTexture(passData.occluderMap);
                var occluderRes = occluderActual.AsResource();

                Span<uint> uavs = stackalloc uint[4];
                for (var c = 0u; c < 4u; c++)
                {
                    uavs[(int)c] = c < passData.cascadeCount
                        ? computeCtx.ResourceDatabase.GetBindlessIndex(occluderRes, BindlessAccess.UnorderedAccess, c * 7u)
                        : RHIUtility.INVALID_BINDLESS_INDEX;
                }

                var genProps = new InternalGenerateShadowOccludersShaderProperties
                {
                    cameraDepthTextureIndex = depthSrv,
                    directionalShadowViewsBuffer = passData.directionalShadowViewsBufferSrv,
                    occluderUav0 = uavs[0],
                    occluderUav1 = uavs[1],
                    occluderUav2 = uavs[2],
                    occluderUav3 = uavs[3],
                    cameraWidth = passData.renderSize.x,
                    cameraHeight = passData.renderSize.y,
                    cascadeCount = passData.cascadeCount,
                    cascadeSplits = passData.cascadeSplits
                };

                computeCtx.SetActiveCompute(passData.generateShader, 0);
                computeCtx.SetUserDataWithProperties(in genProps);
                var groupsX = Math.Max(1u, (passData.renderSize.x + 15u) / 16u);
                var groupsY = Math.Max(1u, (passData.renderSize.y + 15u) / 16u);
                computeCtx.DispatchCompute(groupsX, groupsY, 1);
            });
        }
    }

    private void AddBuildShadowOccluderHZBPass(RenderGraph rg, Identifier<RGTexture> occluderMap, uint cascadeCount)
    {
        using var builder = rg.AddComputeRenderPass<BuildShadowOccluderHZBPassData>("BuildShadowOccluderHZB");
        builder.UseTexture(occluderMap, AccessFlags.ReadWrite);

        builder.SetPassData(new BuildShadowOccluderHZBPassData
        {
            occluderMap = occluderMap,
            shader = _lightingPipelineResource.buildShadowOccluderHZBShader,
            cascadeCount = cascadeCount
        });

        builder.SetRenderFunc<BuildShadowOccluderHZBPassData>(static (ref readonly passData, computeCtx) =>
        {
            var occluderActual = computeCtx.GetActualTexture(passData.occluderMap);
            var occluderRes = occluderActual.AsResource();
            var baseOccluderUav = computeCtx.ResourceDatabase.GetBindlessIndex(occluderRes, BindlessAccess.UnorderedAccess, 0);

            var props = new InternalBuildShadowOccluderHZBShaderProperties
            {
                baseOccluderUav = baseOccluderUav,
                cascadeCount = passData.cascadeCount
            };

            computeCtx.SetActiveCompute(passData.shader, 0);
            computeCtx.SetUserDataWithProperties(in props);
            computeCtx.DispatchCompute(1, 1, passData.cascadeCount);
        });
    }

    private Identifier<RGTexture> AddDirectionalShadowMapPass(RenderGraph rg, uint instanceCount, Identifier<RGTexture> occluderMap, uint directionalShadowViewsBufferSrv, uint cascadeCount, uint atlasResolution)
    {
        if (cascadeCount == 0 || instanceCount == 0)
        {
            return Identifier<RGTexture>.Invalid;
        }

        _meshPipelineResource.EnsureWorkGraphProgram(_renderEngine);
        var shadowProgram = _meshPipelineResource.directionalShadowCullWorkGraphProgram;
        if (shadowProgram == null)
        {
            return Identifier<RGTexture>.Invalid;
        }

        var entrypointIndex = shadowProgram.GetEntrypointIndex("InstanceCullNode");
        if (entrypointIndex == uint.MaxValue)
        {
            entrypointIndex = 0;
        }

        var entrySize = (uint)sizeof(UnbinnedMeshletEntry);
        var visibleBufferSize = _settings.MaxVisibleMeshletsOnScreen * entrySize;

        var maxVariants = GetMaxCullVariants();
        var counterBufferSize = CullCommon.GetCounterBufferSize(maxVariants);

        Identifier<RGBuffer> unbinnedMeshlets;
        Identifier<RGBuffer> counterBuffer;
        using (var builder = rg.AddComputeRenderPass<DirectionalShadowCullPassData>("MeshletCull_DirectionalShadow"))
        {
            unbinnedMeshlets = builder.CreateBuffer(new BufferDesc
            {
                Size = visibleBufferSize,
                Stride = entrySize,
                Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "DirectionalShadowUnbinnedMeshlets");

            counterBuffer = builder.CreateBuffer(new BufferDesc
            {
                Size = counterBufferSize,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "DirectionalShadowCounterBuffer");

            builder.AllowPassCulling(false);
            builder.UseBuffer(unbinnedMeshlets, AccessFlags.Write);
            builder.UseBuffer(counterBuffer, AccessFlags.ReadWrite);
            if (occluderMap.IsValid)
            {
                builder.UseTexture(occluderMap, AccessFlags.Read);
            }

            builder.SetPassData(new DirectionalShadowCullPassData
            {
                cullWorkGraphProgram = shadowProgram,
                visibleMeshlets = unbinnedMeshlets,
                counterBuffer = counterBuffer,
                occluderMap = occluderMap,
                instanceCount = instanceCount,
                cascadeCount = cascadeCount,
                directionalShadowViewsBufferSrv = directionalShadowViewsBufferSrv,
                entrypointIndex = entrypointIndex,
                maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen,
                counterBufferSize = counterBufferSize,
                cullingThreshold = _settings.InstanceCullingThreshold,
            });

            builder.SetRenderFunc<DirectionalShadowCullPassData>(static (ref readonly passData, computeCtx) =>
            {
                computeCtx.ClearBuffer(passData.counterBuffer, passData.counterBufferSize);

                var visibleUav = computeCtx.GetActualBindlessIndex(passData.visibleMeshlets, BindlessAccess.UnorderedAccess);
                var counterUav = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.UnorderedAccess);
                var occluderHzbSrv = passData.occluderMap.IsValid
                    ? computeCtx.GetActualBindlessIndex(passData.occluderMap, BindlessAccess.ShaderResource)
                    : RHIUtility.INVALID_BINDLESS_INDEX;

                var props = new InternalShadowCullGraphShaderProperties
                {
                    visibleMeshletsUav = visibleUav,
                    counterBufferUav = counterUav,
                    maxVisibleMeshlets = passData.maxVisibleMeshlets,
                    instanceCount = passData.instanceCount,
                    shadowViewsBuffer = passData.directionalShadowViewsBufferSrv,
                    shadowViewCount = passData.cascadeCount,
                    cullingThreshold = passData.cullingThreshold,
                    occluderHzbSrv = occluderHzbSrv,
                };

                computeCtx.SetWorkGraph(passData.cullWorkGraphProgram);
                computeCtx.SetUserDataWithProperties(in props);

                var threadGroupsX = Math.Max(1u, (passData.instanceCount + 63) / 64);
                var record = new WorkGraphDispatchGridRecord(threadGroupsX, passData.cascadeCount, 1);
                var dispatchDesc = DispatchGraphDesc.ForCPUInput(passData.entrypointIndex, 1, &record, (ulong)sizeof(WorkGraphDispatchGridRecord));
                computeCtx.DispatchGraph(in dispatchDesc);
            });
        }

        Identifier<RGBuffer> indirectArgs;
        Identifier<RGBuffer> binOffsets;
        Identifier<RGBuffer> binScatterCounters;
        using (var builder = rg.AddComputeRenderPass<PrepareIndirectArgsPassData>("PrepareDirectionalShadowIndirectArgs"))
        {
            indirectArgs = builder.CreateBuffer(new BufferDesc
            {
                Size = CullCommon.GetIndirectArgsBufferSize(maxVariants),
                Stride = 4,
                Usage = BufferUsage.IndirectArgument | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "DirectionalShadowIndirectArgsBuffer");

            binOffsets = builder.CreateBuffer(new BufferDesc
            {
                Size = CullCommon.GetBinOffsetsBufferSize(maxVariants),
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "DirectionalShadowBinOffsetsBuffer");

            binScatterCounters = builder.CreateBuffer(new BufferDesc
            {
                Size = CullCommon.GetBinOffsetsBufferSize(maxVariants),
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "DirectionalShadowBinScatterCounters");

            builder.UseBuffer(counterBuffer, AccessFlags.Read);
            builder.UseBuffer(indirectArgs, AccessFlags.Write);
            builder.UseBuffer(binOffsets, AccessFlags.Write);
            builder.UseBuffer(binScatterCounters, AccessFlags.Write);

            builder.SetPassData(new PrepareIndirectArgsPassData
            {
                counterBuffer = counterBuffer,
                indirectArgsBuffer = indirectArgs,
                binOffsetsBuffer = binOffsets,
                binScatterCounters = binScatterCounters,
                shader = _meshPipelineResource.prepareIndirectArgsShader,
                cullPassIndex = 0,
                maxCount = _settings.MaxVisibleMeshletsOnScreen,
                maxVariants = maxVariants
            });

            builder.SetRenderFunc<PrepareIndirectArgsPassData>(static (ref readonly passData, computeCtx) =>
            {
                var counterUav = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.UnorderedAccess);
                var indirectUav = computeCtx.GetActualBindlessIndex(passData.indirectArgsBuffer, BindlessAccess.UnorderedAccess);
                var binOffsetsUav = computeCtx.GetActualBindlessIndex(passData.binOffsetsBuffer, BindlessAccess.UnorderedAccess);
                var binCountersUav = computeCtx.GetActualBindlessIndex(passData.binScatterCounters, BindlessAccess.UnorderedAccess);

                var props = new InternalPrepareMeshletIndirectArgsShaderProperties
                {
                    counterBuffer = counterUav,
                    indirectArgsBuffer = indirectUav,
                    binOffsetsBuffer = binOffsetsUav,
                    binScatterCounters = binCountersUav,
                    variantCountsOffset = CullCommon.OFFSET_PASS1_VARIANT_COUNTS,
                    visibleArgsOffset = CullCommon.INDIRECT_OFFSET_PASS1_VARIANTS,
                    maxCount = passData.maxCount,
                    maxVariants = passData.maxVariants,
                    occludedCountOffset = uint.MaxValue,
                    cullArgsOffset = uint.MaxValue,
                    swArgsOffset = uint.MaxValue,
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute(1, 1, 1);
            });
        }

        Identifier<RGBuffer> binnedMeshlets;
        using (var builder = rg.AddComputeRenderPass<ScatterMeshletsPassData>("ScatterDirectionalShadowMeshlets"))
        {
            binnedMeshlets = builder.CreateBuffer(new BufferDesc
            {
                Size = visibleBufferSize,
                Stride = entrySize,
                Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "DirectionalShadowBinnedMeshlets");

            builder.UseBuffer(unbinnedMeshlets, AccessFlags.Read);
            builder.UseBuffer(binnedMeshlets, AccessFlags.Write);
            builder.UseBuffer(binScatterCounters, AccessFlags.ReadWrite);
            builder.UseBuffer(counterBuffer, AccessFlags.Read);

            builder.SetPassData(new ScatterMeshletsPassData
            {
                unbinnedBuffer = unbinnedMeshlets,
                binnedBuffer = binnedMeshlets,
                binScatterCounters = binScatterCounters,
                counterBuffer = counterBuffer,
                shader = _meshPipelineResource.scatterMeshletsShader,
                totalVisibleOffset = CullCommon.OFFSET_PASS1_VISIBLE_COUNT,
                maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen,
                isShadowPass = 1
            });

            builder.SetRenderFunc<ScatterMeshletsPassData>(static (ref readonly passData, computeCtx) =>
            {
                var unbinnedSrv = computeCtx.GetActualBindlessIndex(passData.unbinnedBuffer, BindlessAccess.ShaderResource);
                var binnedUav = computeCtx.GetActualBindlessIndex(passData.binnedBuffer, BindlessAccess.UnorderedAccess);
                var countersUav = computeCtx.GetActualBindlessIndex(passData.binScatterCounters, BindlessAccess.UnorderedAccess);
                var counterBufUav = computeCtx.GetActualBindlessIndex(passData.counterBuffer, BindlessAccess.UnorderedAccess);

                var props = new InternalScatterMeshletsShaderProperties
                {
                    unbinnedBuffer = unbinnedSrv,
                    binnedBuffer = binnedUav,
                    binScatterCounters = countersUav,
                    counterBuffer = counterBufUav,
                    totalVisibleOffset = passData.totalVisibleOffset,
                    maxVisibleMeshlets = passData.maxVisibleMeshlets,
                    isShadowPass = passData.isShadowPass
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);

                var threadGroups = Math.Max(1u, (passData.maxVisibleMeshlets + 63u) / 64u);
                var threadGroupsX = Math.Min(threadGroups, 65535u);
                var threadGroupsY = (threadGroups + 65534u) / 65535u;
                computeCtx.DispatchCompute(threadGroupsX, threadGroupsY, 1);
            });
        }

        Identifier<RGTexture> directionalAtlas;
        using (var builder = rg.AddComputeRenderPass<ClearShadowAtlasPassData>("ClearDirectionalShadowAtlas"))
        {
            var atlasDesc = RGTextureDesc.Absolute(
                atlasResolution,
                atlasResolution,
                TextureFormat.R32_Typeless,
                clearAtFirstUse: false,
                usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
                typelessViewFormat: new TypelessFormatDesc { Srv = TextureFormat.R32_Float, Uav = TextureFormat.R32_UInt });

            directionalAtlas = builder.CreateTexture(in atlasDesc, "DirectionalShadowAtlas");
            builder.UseTexture(directionalAtlas, AccessFlags.Write);

            builder.SetPassData(new ClearShadowAtlasPassData
            {
                shadowAtlas = directionalAtlas,
                shader = _lightingPipelineResource.clearShadowAtlasShader,
                atlasResolution = atlasResolution
            });

            builder.SetRenderFunc<ClearShadowAtlasPassData>(static (ref readonly passData, computeCtx) =>
            {
                var shadowAtlasUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualTexture(passData.shadowAtlas).AsResource(), BindlessAccess.UnorderedAccess);
                var props = new InternalClearShadowAtlasShaderProperties
                {
                    shadowAtlasIndex = shadowAtlasUav,
                    atlasResolution = passData.atlasResolution
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                var threadGroups = Math.Max(1u, (passData.atlasResolution + 15u) / 16u);
                computeCtx.DispatchCompute(threadGroups, threadGroups, 1);
            });
        }

        using (var builder = rg.AddUnsafeRenderPass<PunctualShadowRasterPassData>("DirectionalShadowRaster"))
        {
            builder.UseRandomAccessTexture(directionalAtlas);
            builder.UseBuffer(binnedMeshlets, AccessFlags.Read);
            builder.UseBuffer(binOffsets, AccessFlags.Read);
            builder.UseBuffer(indirectArgs, AccessFlags.Read);

            builder.SetPassData(new PunctualShadowRasterPassData
            {
                shadowAtlas = directionalAtlas,
                binnedMeshlets = binnedMeshlets,
                binOffsetsBuffer = binOffsets,
                indirectArgs = indirectArgs,
                commandSignature = _dispatchMeshCommandSignature,
                shadowViewsBufferSrv = directionalShadowViewsBufferSrv,
                atlasResolution = atlasResolution,
                variantRegistry = _assetManager.ShaderVariants,
            });

            builder.SetRenderFunc<PunctualShadowRasterPassData>(static (ref readonly passData, unsafeCtx) =>
            {
                unsafeCtx.SetViewport(new ViewportDesc
                {
                    X = 0,
                    Y = 0,
                    Width = passData.atlasResolution,
                    Height = passData.atlasResolution,
                    MinDepth = 0.0f,
                    MaxDepth = 1.0f
                });
                unsafeCtx.SetScissorRect(new RectDesc
                {
                    Left = 0,
                    Top = 0,
                    Right = passData.atlasResolution,
                    Bottom = passData.atlasResolution
                });

                var binnedMeshletsSrv = unsafeCtx.GetActualBindlessIndex(passData.binnedMeshlets, BindlessAccess.ShaderResource);
                var binOffsetsSrv = unsafeCtx.GetActualBindlessIndex(passData.binOffsetsBuffer, BindlessAccess.ShaderResource);
                var shadowAtlasUav = unsafeCtx.GetActualBindlessIndex(passData.shadowAtlas, BindlessAccess.UnorderedAccess);
                var actualIndirectBuf = unsafeCtx.GetActualBuffer(passData.indirectArgs);

                var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.Shadow);
                if (dispatchVariants.Length == 0)
                {
                    return;
                }

                ref readonly var opaqueVariant = ref dispatchVariants[0];
                if (opaqueVariant.Shader.IsValid && unsafeCtx.TrySetActiveShaderPass(opaqueVariant.Shader, PassSemantic.Shadow))
                {
                    unsafeCtx.SetUserData(binnedMeshletsSrv, passData.shadowViewsBufferSrv, binOffsetsSrv, shadowAtlasUav << 8);
                    unsafeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, 0);
                }

                for (var i = 1; i < dispatchVariants.Length; i++)
                {
                    ref readonly var variant = ref dispatchVariants[i];
                    if (variant.Shader.IsValid && unsafeCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.Shadow))
                    {
                        var targetVariant = (uint)(i + 1);
                        unsafeCtx.SetUserData(binnedMeshletsSrv, passData.shadowViewsBufferSrv, binOffsetsSrv, (shadowAtlasUav << 8) | (targetVariant & 0xFFu));
                        unsafeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, (ulong)targetVariant * INDIRECT_ARGS_STRIDE);
                    }
                }
            });
        }

        using (var builder = rg.AddComputeRenderPass<SoftwareShadowRasterPassData>("RasterizeDirectionalShadowSw"))
        {
            builder.UseTexture(directionalAtlas, AccessFlags.ReadWrite);
            builder.UseBuffer(binnedMeshlets, AccessFlags.Read);
            builder.UseBuffer(binOffsets, AccessFlags.Read);
            builder.UseBuffer(indirectArgs, AccessFlags.Read);

            builder.SetPassData(new SoftwareShadowRasterPassData
            {
                shadowAtlas = directionalAtlas,
                binnedMeshlets = binnedMeshlets,
                binOffsetsBuffer = binOffsets,
                indirectArgs = indirectArgs,
                shader = _lightingPipelineResource.rasterizeShadowSwShader,
                commandSignature = _dispatchCommandSignature,
                shadowViewsBufferSrv = directionalShadowViewsBufferSrv,
                atlasResolution = atlasResolution,
                swIndirectOffset = CullCommon.INDIRECT_OFFSET_PASS1_VARIANTS + 16u
            });

            builder.SetRenderFunc<SoftwareShadowRasterPassData>(static (ref readonly passData, computeCtx) =>
            {
                var shadowAtlasUav = computeCtx.GetActualBindlessIndex(passData.shadowAtlas, BindlessAccess.UnorderedAccess);
                var binnedMeshletsSrv = computeCtx.GetActualBindlessIndex(passData.binnedMeshlets, BindlessAccess.ShaderResource);
                var binOffsetsSrv = computeCtx.GetActualBindlessIndex(passData.binOffsetsBuffer, BindlessAccess.ShaderResource);

                var props = new InternalRasterizeShadowSwShaderProperties
                {
                    shadowAtlasUavIndex = shadowAtlasUav,
                    binnedMeshletsBuffer = binnedMeshletsSrv,
                    binOffsetsBuffer = binOffsetsSrv,
                    shadowViewsBufferSrv = passData.shadowViewsBufferSrv,
                    atlasResolution = passData.atlasResolution,
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);

                var actualIndirectBuf = computeCtx.GetActualBuffer(passData.indirectArgs);
                computeCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, passData.swIndirectOffset);
            });
        }

        return directionalAtlas;
    }
}
