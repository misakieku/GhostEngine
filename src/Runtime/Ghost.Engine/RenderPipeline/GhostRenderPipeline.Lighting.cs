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
        public GPUPunctualLight[] lights;
        public UnsafeArray<int> shadowIndices;
        public float meshletLodErrorThreshold;
        public Frustum frustum;
        public ShadowAtlasRegionAllocator allocator;

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
            var m23 = (farClip * nearClip) / (farClip - nearClip);

            return new float4x4(
                m00, 0.0f, 0.0f, 0.0f,
                0.0f, m11, 0.0f, 0.0f,
                0.0f, 0.0f, m22, m23,
                0.0f, 0.0f, 1.0f, 0.0f);
        }

        public void Execute(int loopIndex, ref readonly JobExecutionContext ctx)
        {
            ref readonly var light = ref lights[loopIndex];

            if (!MathUtility.SphereIntersectFrustum(light.positionWS, light.range, frustum.planes))
            {
                return;
            }

            var shadowSize = (light.lightTypeAndFlags >> 4) & 0xFFFFu;
            if (shadowSize == 0)
            {
                return;
            }

            var lightType = light.lightTypeAndFlags & 0xFu;

            if (lightType == 1u) // Spot
            {
                if (!allocator.Allocate(shadowSize, shadowSize, out var region))
                {
                    return;
                }

                var cosOuter = (light.spotAngleScale > 0.0001f)
                    ? math.clamp(-light.spotAngleOffset / light.spotAngleScale, -1.0f, 1.0f)
                    : 0.7071f;
                var outerAngle = math.acos(cosOuter);
                var fov = math.clamp(outerAngle * 2.0f, 0.01f, math.radians(175.0f));

                var up = math.abs(light.directionWS.y) > 0.99f ? new float3(0.0f, 0.0f, 1.0f) : new float3(0.0f, 1.0f, 0.0f);
                var viewMat = CreateLookAtMatrix(light.positionWS, light.directionWS, up);
                var nearPlane = 0.05f;
                var farPlane = light.range;
                var projMat = CreatePerspectiveReversedZ(fov, 1.0f, nearPlane, farPlane);
                var shadowViewProj = math.mul(projMat, viewMat);
                var shadowFrustum = Frustum.Create(shadowViewProj, light.positionWS, light.directionWS, nearPlane, farPlane);

                var svData = new GPUShadowViewData
                {
                    shadowViewProj = shadowViewProj,
                    plane0 = shadowFrustum.planes[0],
                    plane1 = shadowFrustum.planes[1],
                    plane2 = shadowFrustum.planes[2],
                    plane3 = shadowFrustum.planes[3],
                    plane4 = shadowFrustum.planes[4],
                    plane5 = shadowFrustum.planes[5],
                    tileOffsetScale = region.tileOffsetScale,
                    lightPositionWS = light.positionWS,
                    lodErrorThreshold = meshletLodErrorThreshold,
                    proj11 = 1.0f / math.tan(fov * 0.5f),
                    tileSize = shadowSize,
                };

                var slot = shadowViewsWriter.AddNoResize(svData);
                shadowIndices[loopIndex] = slot;
            }
            else if (lightType == 0u) // Point
            {
                Span<ShadowAtlasRegion> faceRegions = stackalloc ShadowAtlasRegion[6];
                if (!allocator.AllocatePointLightBlock(shadowSize, faceRegions))
                {
                    return;
                }

                var nearPlane = 0.05f;
                var farPlane = light.range;
                var guardPixels = 2.0f;
                var fov = (shadowSize > 4)
                    ? 2.0f * math.atan(shadowSize / (shadowSize - 2.0f * guardPixels))
                    : math.radians(90.0f);
                var projMat = CreatePerspectiveReversedZ(fov, 1.0f, nearPlane, farPlane);
                var pointProj11 = 1.0f / math.tan(fov * 0.5f);

                Span<GPUShadowViewData> faceViews = stackalloc GPUShadowViewData[6];
                for (var f = 0; f < 6; f++)
                {
                    GetPointFaceOrientation(f, out var dir, out var up);
                    var viewMat = CreateLookAtMatrix(light.positionWS, dir, up);
                    var shadowViewProj = math.mul(projMat, viewMat);
                    var shadowFrustum = Frustum.Create(shadowViewProj, light.positionWS, dir, nearPlane, farPlane);

                    faceViews[f] = new GPUShadowViewData
                    {
                        shadowViewProj = shadowViewProj,
                        plane0 = shadowFrustum.planes[0],
                        plane1 = shadowFrustum.planes[1],
                        plane2 = shadowFrustum.planes[2],
                        plane3 = shadowFrustum.planes[3],
                        plane4 = shadowFrustum.planes[4],
                        plane5 = shadowFrustum.planes[5],
                        tileOffsetScale = faceRegions[f].tileOffsetScale,
                        lightPositionWS = light.positionWS,
                        lodErrorThreshold = meshletLodErrorThreshold,
                        proj11 = pointProj11,
                        tileSize = shadowSize
                    };
                }

                var baseSlot = shadowViewsWriter.AddRangeNoResize(faceViews, 6);
                shadowIndices[loopIndex] = baseSlot;
            }
        }
    }

    private void ExecutePerViewShadowSetup(ResourceContext ctx, GhostRenderPayload payload, ShadowAtlasRegionAllocator atlasAllocator, in Frustum viewFrustum, out uint shadowViewsBufferSrv, out uint shadowViewCount, out uint shadowIndicesBufferSrv)
    {
        shadowViewsBufferSrv = uint.MaxValue;
        shadowViewCount = 0;
        shadowIndicesBufferSrv = uint.MaxValue;

        var lightCount = payload.PunctualLights.Length;
        if (lightCount == 0)
        {
            return;
        }

        using var stackScope = AllocationManager.CreateStackScope();
        using var shadowIndices = new UnsafeArray<int>(lightCount, stackScope.AllocationHandle);
        shadowIndices.AsSpan().Fill(-1);

        using var shadowViews = new UnsafeList<GPUShadowViewData>(Math.Max(16, lightCount * 6), AllocationHandle.TempRender);

        var job = new PerViewShadowSetupJob
        {
            lights = payload.PunctualLights.ToArray(),
            frustum = viewFrustum,
            allocator = atlasAllocator,
            shadowViewsWriter = shadowViews.AsParallelWriter(),
            shadowIndices = shadowIndices,
            meshletLodErrorThreshold = _settings.MeshletLodErrorThreshold
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

    private void UploadLights(ResourceContext ctx, GhostRenderPayload payload,
        out uint punctualLightsSrv, out uint punctualLightCount,
        out uint directionalLightSrv, out uint directionalLightCount, out int primaryDirectionalLightIndex)
    {
        UploadDirectionalLights(ctx, payload, out directionalLightSrv, out directionalLightCount, out primaryDirectionalLightIndex);
        
        punctualLightsSrv = uint.MaxValue;
        punctualLightCount = (uint)payload.PunctualLights.Length;

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
            MemoryUtility.MemCpy(pData, lights.GetUnsafePtr(), bufferSize);
            ctx.ResourceDatabase.UnmapResource(lightBuffer.AsResource(), 0, null);
            punctualLightsSrv = ctx.ResourceDatabase.GetBindlessIndex(lightBuffer.AsResource());
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
        public uint shadowAtlasSrv;
        public uint shadowViewsBufferSrv;
        public uint shadowIndicesBufferSrv;
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
        public uint shadowViewsBufferSrv;
        public uint shadowIndicesBufferSrv;
        public ShaderVariantRegistry variantRegistry;
        public uint tilesPerRow;
        public uint tilesY;
        public uint2 renderSize;
    }

    private Identifier<RGTexture> AddDeferredLightingPass(
        RenderGraph rg, in GBufferResources gbuffer, Identifier<RGTexture> depthTexture, Identifier<RGBuffer> tileLightList,
        Identifier<RGTexture> shadowAtlas, uint shadowViewsBufferSrv, uint shadowIndicesBufferSrv, uint2 renderSize)
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
        if (shadowAtlas.IsValid)
        {
            builder.UseTexture(shadowAtlas, AccessFlags.Read);
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
            var shadowAtlasSrv = passData.shadowAtlas.IsValid
                ? computeCtx.GetActualBindlessIndex(passData.shadowAtlas, BindlessAccess.ShaderResource)
                : uint.MaxValue;

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
                    shadingModelId = shadingModelId,
                    shadowAtlasSrv = shadowAtlasSrv,
                    shadowViewsBufferSrv = passData.shadowViewsBufferSrv,
                    shadowIndicesBufferSrv = passData.shadowIndicesBufferSrv
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
    }

    private struct PrepareShadowIndirectArgsPassData
    {
        public Identifier<RGBuffer> counterBuffer;
        public Identifier<RGBuffer> indirectArgsBuffer;
        public Handle<ComputeShader> shader;
        public uint maxCount;
        public uint maxVariants;
    }

    private struct PunctualShadowRasterPassData
    {
        public Identifier<RGTexture> shadowAtlas;
        public Identifier<RGBuffer> visibleMeshlets;
        public Identifier<RGBuffer> indirectArgs;
        public ICommandSignature commandSignature;
        public uint shadowViewsBufferSrv;
        public ShaderVariantRegistry variantRegistry;
    }

    private Identifier<RGTexture> AddPunctualShadowAtlasPass(RenderGraph rg, uint instanceCount, uint shadowViewsBufferSrv, uint shadowViewCount, uint atlasWidth = 2048, uint atlasHeight = 2048)
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

        Identifier<RGBuffer> visibleMeshlets;
        Identifier<RGBuffer> counterBuffer;
        using (var cullBuilder = rg.AddComputeRenderPass<ShadowCullPassData>("MeshletCull_Shadow"))
        {
            // Work Graph Shadow Culling
            visibleMeshlets = cullBuilder.CreateBuffer(new BufferDesc
            {
                Size = visibleBufferSize,
                Stride = entrySize,
                Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowVisibleMeshlets");

            counterBuffer = cullBuilder.CreateBuffer(new BufferDesc
            {
                Size = CullConstants.COUNTER_BUFFER_SIZE,
                Stride = 4,
                Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowCounterBuffer");

            cullBuilder.AllowPassCulling(false);
            cullBuilder.UseBuffer(visibleMeshlets, AccessFlags.Write);
            cullBuilder.UseBuffer(counterBuffer, AccessFlags.ReadWrite);

            cullBuilder.SetPassData(new ShadowCullPassData
            {
                visibleMeshlets = visibleMeshlets,
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
            });

            cullBuilder.SetRenderFunc<ShadowCullPassData>(static (ref readonly passData, computeCtx) =>
            {
                computeCtx.ClearBuffer(passData.counterBuffer, CullConstants.COUNTER_BUFFER_SIZE);

                var visibleUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.visibleMeshlets).AsResource(), BindlessAccess.UnorderedAccess);
                var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);

                var props = new InternalShadowCullGraphShaderProperties
                {
                    visibleMeshletsUav = visibleUav,
                    counterBufferUav = counterUav,
                    maxVisibleMeshlets = passData.maxVisibleMeshlets,
                    instanceCount = passData.instanceCount,
                    shadowViewsBuffer = passData.shadowViewsBufferSrv,
                    shadowViewCount = passData.shadowViewCount,
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

        // Prepare Indirect Arguments
        Identifier<RGBuffer> indirectArgs;
        using (var prepBuilder = rg.AddComputeRenderPass<PrepareShadowIndirectArgsPassData>("PrepareShadowIndirectArgs"))
        {
            indirectArgs = prepBuilder.CreateBuffer(new BufferDesc
            {
                Size = CullConstants.INDIRECT_ARGS_BUFFER_SIZE,
                Stride = 4,
                Usage = BufferUsage.IndirectArgument | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
            }, "ShadowIndirectArgsBuffer");

            prepBuilder.UseBuffer(counterBuffer, AccessFlags.Read);
            prepBuilder.UseBuffer(indirectArgs, AccessFlags.Write);

            prepBuilder.SetPassData(new PrepareShadowIndirectArgsPassData
            {
                counterBuffer = counterBuffer,
                indirectArgsBuffer = indirectArgs,
                shader = _meshPipelineResource.prepareShadowIndirectArgsShader,
                maxCount = _settings.MaxVisibleMeshletsOnScreen,
                maxVariants = CullConstants.MAX_VARIANTS
            });

            prepBuilder.SetRenderFunc<PrepareShadowIndirectArgsPassData>(static (ref readonly passData, computeCtx) =>
            {
                var counterUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.counterBuffer).AsResource(), BindlessAccess.UnorderedAccess);
                var indirectUav = computeCtx.ResourceDatabase.GetBindlessIndex(computeCtx.GetActualBuffer(passData.indirectArgsBuffer).AsResource(), BindlessAccess.UnorderedAccess);

                var props = new InternalPrepareShadowIndirectArgsShaderProperties
                {
                    counterBuffer = counterUav,
                    indirectArgsBuffer = indirectUav,
                    maxCount = passData.maxCount,
                    maxVariants = passData.maxVariants
                };

                computeCtx.SetActiveCompute(passData.shader, 0);
                computeCtx.SetUserDataWithProperties(in props);
                computeCtx.DispatchCompute(1, 1, 1);
            });
        }

        // Raster Shadow Depth
        Identifier<RGTexture> shadowAtlas;
        using (var rasterBuilder = rg.AddRasterRenderPass<PunctualShadowRasterPassData>("PunctualShadowRaster"))
        {
            var shadowAtlasDesc = RGTextureDesc.AbsoluteDepth(
                _settings.ShadowAtlasResolution,
                _settings.ShadowAtlasResolution,
                TextureFormat.R32_Typeless,
                usage: TextureUsage.DepthStencil | TextureUsage.ShaderResource);

            shadowAtlas = rasterBuilder.CreateTexture(in shadowAtlasDesc, "PunctualShadowAtlas");

            rasterBuilder.SetDepthAttachment(shadowAtlas, AccessFlags.WriteAll);
            rasterBuilder.UseBuffer(visibleMeshlets, AccessFlags.Read);
            rasterBuilder.UseBuffer(indirectArgs, AccessFlags.Read);

            rasterBuilder.SetPassData(new PunctualShadowRasterPassData
            {
                shadowAtlas = shadowAtlas,
                visibleMeshlets = visibleMeshlets,
                indirectArgs = indirectArgs,
                commandSignature = _dispatchMeshCommandSignature,
                shadowViewsBufferSrv = shadowViewsBufferSrv,
                variantRegistry = _assetManager.ShaderVariants,
            });

            rasterBuilder.SetRenderFunc<PunctualShadowRasterPassData>(static (ref readonly passData, rasterCtx) =>
            {
                var visibleMeshletsSrv = rasterCtx.ResourceDatabase.GetBindlessIndex(rasterCtx.GetActualBuffer(passData.visibleMeshlets).AsResource(), BindlessAccess.ShaderResource);
                var actualIndirectBuf = rasterCtx.GetActualBuffer(passData.indirectArgs);

                var dispatchVariants = passData.variantRegistry.GetDispatchVariants(PassSemantic.Shadow);
                if (dispatchVariants.Length > 0)
                {
                    ref readonly var variant = ref dispatchVariants[0];
                    if (variant.Shader.IsValid && rasterCtx.TrySetActiveShaderPass(variant.Shader, PassSemantic.Shadow))
                    {
                        rasterCtx.SetUserData(visibleMeshletsSrv, passData.shadowViewsBufferSrv, 0, 0);
                        rasterCtx.ExecuteIndirect(passData.commandSignature, 1, actualIndirectBuf, 0);
                    }
                }
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