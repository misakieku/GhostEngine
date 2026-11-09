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

    private Identifier<RGTexture> AddCascadeOccluderMapPasses(RenderGraph rg, Identifier<RGTexture> depthTexture, uint directionalShadowViewsBufferSrv, uint2 renderSize, uint cascadeCount, float4 cascadeSplits)
    {
        var occluderMapDesc = RGTextureDesc.Absolute(
            64,
            64,
            TextureFormat.R32_Typeless,
            clearColor: default,
            clearAtFirstUse: false,
            discardAtLastUse: true,
            dimension: TextureDimension.Texture2DArray,
            mipLevels: 7,
            slice: cascadeCount,
            usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
            typelessViewFormat: new TypelessFormatDesc { Srv = TextureFormat.R32_Float, Uav = TextureFormat.R32_UInt });

        var occluderMap = rg.CreateTexture(in occluderMapDesc, "CascadeOccluderMap");
        AddGenerateShadowOccludersPass(rg, depthTexture, occluderMap, directionalShadowViewsBufferSrv, renderSize, cascadeCount, cascadeSplits);
        AddBuildShadowOccluderHZBPass(rg, occluderMap, cascadeCount);
        return occluderMap;
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

    private void AddShadowPasses(
        RenderGraph renderGraph, scoped in ResourceContext ctx, in RenderRequest request, in Frustum frustum,
        in float4x4 projMatrix, GhostRenderPayload ghostPayload, ReadOnlyView<PunctualLightRequest> lights,
        in ShadowSettings shadowSettings, Identifier<RGTexture> depthTexture, uint2 renderSize)
    {
        var shadowAtlas = Identifier<RGTexture>.Invalid;
        var shadowViewsBufferSrv = RHIUtility.INVALID_BINDLESS_INDEX;
        var shadowIndicesBufferSrv = RHIUtility.INVALID_BINDLESS_INDEX;

        ExecutePerViewShadowSetup(ctx, in frustum, renderSize.y, lights, request.view.localToWorld.c3.xyz,
            out shadowViewsBufferSrv, out var shadowViewCount, out shadowIndicesBufferSrv);

        if (shadowSettings.shadowMode == ShadowMode.ShadowMap)
        {
            shadowAtlas = AddPunctualShadowAtlasPass(renderGraph, ghostPayload.InstanceCount, shadowViewsBufferSrv, shadowViewCount);
        }

        var directionalShadowAtlas = Identifier<RGTexture>.Invalid;
        var directionalShadowViewsBufferSrv = RHIUtility.INVALID_BINDLESS_INDEX;
        var cascadeCount = 0u;
        var cascadeSplits = default(float4);
        var cascadeBlends = default(float4);
        var maxShadowDistance = 0f;
        var shadowFadeRange = 0f;

        if (ghostPayload.PrimaryDirectionalLightIndex >= 0 && shadowSettings.shadowMode == ShadowMode.ShadowMap)
        {
            ref readonly var dirRequest = ref ghostPayload.DirectionalLightRequests[ghostPayload.PrimaryDirectionalLightIndex];
            if (dirRequest.light.castShadows != 0 && dirRequest.light.shadowDimmer > 0.0f)
            {
                var setupResult = ComputeDirectionalCascadeViews(ctx, in dirRequest, in request, in projMatrix, in shadowSettings, renderSize.y);
                directionalShadowViewsBufferSrv = setupResult.directionalShadowViewsBufferSrv;
                cascadeCount = setupResult.cascadeCount;
                cascadeSplits = setupResult.cascadeSplits;
                cascadeBlends = setupResult.cascadeBlends;
                maxShadowDistance = setupResult.maxShadowDistance;
                shadowFadeRange = setupResult.shadowFadeRange;

                var occluderMap = AddCascadeOccluderMapPasses(renderGraph, depthTexture, directionalShadowViewsBufferSrv, renderSize, cascadeCount, cascadeSplits);
                directionalShadowAtlas = AddDirectionalShadowMapPass(renderGraph, ghostPayload.InstanceCount, occluderMap, directionalShadowViewsBufferSrv, cascadeCount, setupResult.atlasResolution);
            }
        }

        var shadowMapData = new ShadowMapPassData
        {
            shadowAtlas = shadowAtlas,
            shadowViewsBufferSrv = shadowViewsBufferSrv,
            shadowIndicesBufferSrv = shadowIndicesBufferSrv,
            directionalShadowAtlas = directionalShadowAtlas,
            directionalShadowViewsBufferSrv = directionalShadowViewsBufferSrv,
            cascadeCount = cascadeCount,
            cascadeSplits = cascadeSplits,
            cascadeBlends = cascadeBlends,
            maxShadowDistance = maxShadowDistance,
            shadowFadeRange = shadowFadeRange
        };

        renderGraph.Blackboard.Add(ref shadowMapData);
    }
}
