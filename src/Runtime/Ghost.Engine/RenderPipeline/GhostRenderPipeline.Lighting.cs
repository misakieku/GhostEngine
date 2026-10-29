using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.ShaderProperties;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
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
        public ShadowAtlasRegionAllocator allocator;
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

            var size = new float4(allocator.AtlasSize, 1.0f / allocator.AtlasSize, shadowSize, 1.0f / shadowSize);
            var nearPlane = shadowData.nearPlane > 0.0f ? shadowData.nearPlane : 0.05f;
            var farPlane = light.range;
            var lightType = light.lightTypeAndShadowSize & 0xFu;
            var lodErrorThreshold = meshletLodErrorThreshold * (viewHeight / shadowSize) * (shadowData.lodErrorScale > 0 ? shadowData.lodErrorScale : 1.0f);

            // TODO: Expose softnessScale?
            var softness = GetNonDirectionalSoftness(1.0f, viewHeight, light.size.x);
            var basis = GetSlopeBasedBias(softness);

            if (lightType == (uint)GPULightType.Spot) // Spot
            {
                if (!allocator.Allocate(shadowSize, out var region))
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
                        normalBias = shadowData.normalBias,
                        slopeBias = basis * shadowData.depthBias,
                        worldTexelSize = 2.0f / projMat[0][0] / shadowSize * 1.4142135623731f,
                    }
                };

                var slot = shadowViewsWriter.AddNoResize(svData);
                shadowIndices[loopIndex] = slot;
            }
            else if (lightType == (uint)GPULightType.Point) // Point
            {
                Span<ShadowAtlasRegion> faceRegions = stackalloc ShadowAtlasRegion[6];
                if (!allocator.AllocatePointLightBlock(shadowSize, faceRegions))
                {
                    return;
                }

                const float guardPixels = 4.0f;
                // We bias the fov here because point light use cubemap for shadow, and we need to give it room for filtering.
                var fov = 2.0f * math.atan(shadowSize / (shadowSize - 2.0f * guardPixels)); // 1.570796f; // 90 degrees in radians
                var projMat = CreatePerspectiveReversedZ(fov, 1.0f, nearPlane, farPlane);
                var pro = new float4(projMat[0][0], projMat[1][1], projMat[2][2], projMat[3][2]);

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
                            normalBias = shadowData.normalBias,
                            slopeBias = basis * shadowData.depthBias,
                            worldTexelSize = 2.0f / projMat[0][0] / shadowSize * 1.4142135623731f,
                        }
                    };
                }

                var baseSlot = shadowViewsWriter.AddRangeNoResize(faceViews, 6);
                shadowIndices[loopIndex] = baseSlot;
            }
        }
    }

    private void ExecutePerViewShadowSetup(ResourceContext ctx, ShadowAtlasRegionAllocator atlasAllocator, in Frustum viewFrustum, float viewHeight, ReadOnlyView<PunctualLightRequest> lights, float3 cameraPosition,
        out uint shadowViewsBufferSrv, out uint shadowViewCount, out uint shadowIndicesBufferSrv)
    {
        shadowViewsBufferSrv = uint.MaxValue;
        shadowViewCount = 0;
        shadowIndicesBufferSrv = uint.MaxValue;

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
        var job = new PerViewShadowSetupJob
        {
            lights = lights,
            frustum = viewFrustum,
            allocator = atlasAllocator,
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

        punctualLightsSrv = uint.MaxValue;
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
        public float2 _pad;
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
        public SkyContext skyContext;
        public uint viewId;
        public uint shadowViewsBufferSrv;
        public uint shadowIndicesBufferSrv;
        public ShaderVariantRegistry variantRegistry;
        public uint tilesPerRow;
        public uint tilesY;
        public uint2 renderSize;
    }

    private Identifier<RGTexture> AddDeferredLightingPass(
        RenderGraph rg, in GBufferResources gbuffer, Identifier<RGTexture> depthTexture, Identifier<RGBuffer> tileLightList,
        Identifier<RGTexture> shadowAtlas, uint shadowViewsBufferSrv, uint shadowIndicesBufferSrv, Identifier<RGBuffer> tileShadingModelMaskBuffer,
        in SkyContext skyContext, uint viewId, uint2 renderSize)
    {
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

        if (shadowAtlas.IsValid)
        {
            builder.UseTexture(shadowAtlas, AccessFlags.Read);
        }

        if (skyContext.skyboxTexture.IsValid)
        {
            builder.UseTexture(skyContext.skyboxTexture, AccessFlags.Read);
        }

        if (skyContext.iblLutTexture.IsValid)
        {
            builder.UseTexture(skyContext.iblLutTexture, AccessFlags.Read);
        }

        if (skyContext.persistentViewStateBuffer.IsValid)
        {
            builder.UseBuffer(skyContext.persistentViewStateBuffer, AccessFlags.Read);
        }

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
            skyContext = skyContext,
            viewId = viewId,
            shadowViewsBufferSrv = shadowViewsBufferSrv,
            shadowIndicesBufferSrv = shadowIndicesBufferSrv,
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
            var shadowAtlasSrv = computeCtx.GetActualBindlessIndex(passData.shadowAtlas, BindlessAccess.ShaderResource);
            var tileShadingModelMaskSrv = computeCtx.GetActualBindlessIndex(passData.tileShadingModelMaskBuffer, BindlessAccess.ShaderResource);
            var skyboxSrv = computeCtx.GetActualBindlessIndex(passData.skyContext.skyboxTexture, BindlessAccess.ShaderResource);
            var iblLutSrv = computeCtx.GetActualBindlessIndex(passData.skyContext.iblLutTexture, BindlessAccess.ShaderResource);
            var persistentViewStateSrv = computeCtx.GetActualBindlessIndex(passData.skyContext.persistentViewStateBuffer, BindlessAccess.ShaderResource);

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
                    skyboxTextureSrv = skyboxSrv,
                    iblLutTextureSrv = iblLutSrv,
                    persistentViewStateBufferSrv = persistentViewStateSrv,
                    viewId = passData.viewId,
                    skyboxMipCount = passData.skyContext.skyboxMipCount,
                    skyboxTintAndIntensity = passData.skyContext.skyTintAndIntensity,
                    skyboxRotationSinCos = passData.skyContext.skyRotationSinCos,
                    _pad = float2.zero
                };

                computeCtx.SetUserDataWithProperties(in props, target: DataTarget.Compute);
                computeCtx.DispatchCompute(passData.tilesPerRow, passData.tilesY, 1);
            }
        });

        return litColorTarget;
    }

    private struct ShadowCullPassData
    {
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> counterBuffer;
        public uint instanceCount;
        public uint shadowViewCount;
        public uint shadowViewsBufferSrv;
        public uint entrypointIndex;
        public ProgramIdentifier programIdentifier;
        public ulong backingMemoryAddress;
        public ulong backingMemorySize;
        public SetWorkGraphFlags flags;
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

#if DEBUG
        var flags = SetWorkGraphFlags.Initialize;
#else
        var flags = shadowProgram.IsInitialized ? SetWorkGraphFlags.None : SetWorkGraphFlags.Initialize;
#endif

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
        using (var cullBuilder = rg.AddComputeRenderPass<ShadowCullPassData>("MeshletCull_Shadow"))
        {
            // Work Graph Shadow Culling
            unbinnedMeshlets = cullBuilder.CreateBuffer(new BufferDesc
            {
                Size = visibleBufferSize,
                Stride = entrySize,
                Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowUnbinnedMeshlets");

            counterBuffer = cullBuilder.CreateBuffer(new BufferDesc
            {
                Size = counterBufferSize,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowCounterBuffer");

            cullBuilder.AllowPassCulling(false);
            cullBuilder.UseBuffer(unbinnedMeshlets, AccessFlags.Write);
            cullBuilder.UseBuffer(counterBuffer, AccessFlags.ReadWrite);

            cullBuilder.SetPassData(new ShadowCullPassData
            {
                visibleMeshlets = unbinnedMeshlets,
                counterBuffer = counterBuffer,
                instanceCount = instanceCount,
                shadowViewCount = shadowViewCount,
                shadowViewsBufferSrv = shadowViewsBufferSrv,
                entrypointIndex = entrypointIndex,
                programIdentifier = shadowProgram.ProgramIdentifier,
                backingMemoryAddress = shadowProgram.BackingMemoryAddress,
                backingMemorySize = shadowProgram.BackingMemorySize,
                flags = flags,
                maxVisibleMeshlets = _settings.MaxVisibleMeshletsOnScreen,
                counterBufferSize = counterBufferSize,
                cullingThreshold = _settings.InstanceCullingThreshold,
            });

            cullBuilder.SetRenderFunc<ShadowCullPassData>(static (ref readonly passData, computeCtx) =>
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
                };

                var setProgramDesc = SetProgramDesc.ForWorkGraph(
                    passData.programIdentifier,
                    passData.backingMemoryAddress,
                    passData.backingMemorySize,
                    passData.flags);
                computeCtx.SetProgram(in setProgramDesc);
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
        using (var prepBuilder = rg.AddComputeRenderPass<PrepareIndirectArgsPassData>("PrepareShadowIndirectArgs"))
        {
            indirectArgs = prepBuilder.CreateBuffer(new BufferDesc
            {
                Size = CullCommon.GetIndirectArgsBufferSize(maxVariants),
                Stride = 4,
                Usage = BufferUsage.IndirectArgument | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowIndirectArgsBuffer");

            binOffsets = prepBuilder.CreateBuffer(new BufferDesc
            {
                Size = CullCommon.GetBinOffsetsBufferSize(maxVariants),
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowBinOffsetsBuffer");

            binScatterCounters = prepBuilder.CreateBuffer(new BufferDesc
            {
                Size = CullCommon.GetBinOffsetsBufferSize(maxVariants),
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowBinScatterCounters");

            prepBuilder.UseBuffer(counterBuffer, AccessFlags.Read);
            prepBuilder.UseBuffer(indirectArgs, AccessFlags.Write);
            prepBuilder.UseBuffer(binOffsets, AccessFlags.Write);
            prepBuilder.UseBuffer(binScatterCounters, AccessFlags.Write);

            prepBuilder.SetPassData(new PrepareIndirectArgsPassData
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

            prepBuilder.SetRenderFunc<PrepareIndirectArgsPassData>(static (ref readonly passData, computeCtx) =>
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
        using (var scatterBuilder = rg.AddComputeRenderPass<ScatterMeshletsPassData>("ScatterShadowMeshlets"))
        {
            binnedMeshlets = scatterBuilder.CreateBuffer(new BufferDesc
            {
                Size = visibleBufferSize,
                Stride = entrySize,
                Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowBinnedMeshlets");

            scatterBuilder.UseBuffer(unbinnedMeshlets, AccessFlags.Read);
            scatterBuilder.UseBuffer(binnedMeshlets, AccessFlags.Write);
            scatterBuilder.UseBuffer(binScatterCounters, AccessFlags.ReadWrite);
            scatterBuilder.UseBuffer(counterBuffer, AccessFlags.Read);

            scatterBuilder.SetPassData(new ScatterMeshletsPassData
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

            scatterBuilder.SetRenderFunc<ScatterMeshletsPassData>(static (ref readonly passData, computeCtx) =>
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

        // 1. Create and Clear Shadow Atlas UAV
        Identifier<RGTexture> shadowAtlas;
        using (var clearBuilder = rg.AddComputeRenderPass<ClearShadowAtlasPassData>("ClearShadowAtlas"))
        {
            var shadowAtlasDesc = RGTextureDesc.Absolute(
                _settings.ShadowAtlasResolution,
                _settings.ShadowAtlasResolution,
                TextureFormat.R32_Typeless,
                clearAtFirstUse: false,
                usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
                typelessViewFormat: new TypelessFormatDesc { Srv = TextureFormat.R32_Float, Uav = TextureFormat.R32_UInt });

            shadowAtlas = clearBuilder.CreateTexture(in shadowAtlasDesc, "PunctualShadowAtlas");

            clearBuilder.UseTexture(shadowAtlas, AccessFlags.Write);
            clearBuilder.SetPassData(new ClearShadowAtlasPassData
            {
                shadowAtlas = shadowAtlas,
                shader = _lightingPipelineResource.clearShadowAtlasShader,
                atlasResolution = _settings.ShadowAtlasResolution
            });

            clearBuilder.SetRenderFunc<ClearShadowAtlasPassData>(static (ref readonly passData, computeCtx) =>
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

        // 2. Hardware Raster Shadow Depth for Macro-triangles (Bins 0 and 2..N)
        using (var unsafeBuilder = rg.AddUnsafeRenderPass<PunctualShadowRasterPassData>("PunctualShadowRaster"))
        {
            unsafeBuilder.UseRandomAccessTexture(shadowAtlas);
            unsafeBuilder.UseBuffer(binnedMeshlets, AccessFlags.Read);
            unsafeBuilder.UseBuffer(binOffsets, AccessFlags.Read);
            unsafeBuilder.UseBuffer(indirectArgs, AccessFlags.Read);

            unsafeBuilder.SetPassData(new PunctualShadowRasterPassData
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

            unsafeBuilder.SetRenderFunc<PunctualShadowRasterPassData>(static (ref readonly passData, unsafeCtx) =>
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
        using (var swBuilder = rg.AddComputeRenderPass<SoftwareShadowRasterPassData>("RasterizeShadowSw"))
        {
            swBuilder.UseTexture(shadowAtlas, AccessFlags.ReadWrite);
            swBuilder.UseBuffer(binnedMeshlets, AccessFlags.Read);
            swBuilder.UseBuffer(binOffsets, AccessFlags.Read);
            swBuilder.UseBuffer(indirectArgs, AccessFlags.Read);

            swBuilder.SetPassData(new SoftwareShadowRasterPassData
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

            swBuilder.SetRenderFunc<SoftwareShadowRasterPassData>(static (ref readonly passData, computeCtx) =>
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
        directionalLightSrv = uint.MaxValue;
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
}
