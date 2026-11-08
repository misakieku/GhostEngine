using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Engine.Volumes;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.RenderPipeline;

internal unsafe partial class GhostRenderPipeline : IRenderPipeline
{
    [GenerateHLSL(PackingRules.Exact, "EngineResources/Shaders/Generated/GhostRenderPipeline.hlsl")]
    private struct PipelineConstants
    {
        public const uint DWORDS_PER_TILE = 32u;
        public const uint MAX_LIGHTS_PER_TILE = 63u;
    }

    private readonly RenderEngine _renderEngine;
    private readonly AssetManager _assetManager;
    private readonly JobScheduler _jobScheduler;
    private readonly GhostRenderPipelineSettings _settings;

    private readonly GPUSceneResource _gpuSceneResource;
    private readonly MeshPipelineResource _meshPipelineResource;
    private readonly MaterialPipelineResource _materialPipelineResource;
    private readonly LightingPipelineResource _lightingPipelineResource;

    private readonly GPUScene _gpuScene;
    private readonly GPUViewManager _gpuViewManager;
    private readonly RayTracingManager _rayTracingManager;

    private static readonly PipelineKeyword s_kwDirectLightingFPTL = PipelineKeywordRegistry.GetOrRegister("_DIRECTLIGHTING_FPTL");
    private static readonly PipelineKeyword s_kwDirectLightingStochastic = PipelineKeywordRegistry.GetOrRegister("_DIRECTLIGHTING_STOCHASTIC");
    private static readonly PipelineKeyword s_kwShadowPCF = PipelineKeywordRegistry.GetOrRegister("_SHADOW_PCF");
    private static readonly PipelineKeyword s_kwShadowRayTraced = PipelineKeywordRegistry.GetOrRegister("_SHADOW_RAYTRACED");

    private bool _disposed;
    private int _lastRenderRequestCount = -1;
    private uint _lastInstanceCount = uint.MaxValue;

    public GPUScene GPUScene => _gpuScene;
    public GPUViewManager GPUViewManager => _gpuViewManager;

    public GhostRenderPipeline(RenderEngine renderEngine, AssetManager assetManager, JobScheduler jobScheduler, GhostRenderPipelineSettings settings)
    {
        ValidateRequiredGPUFeatures(renderEngine.GraphicsEngine.Device);

        _renderEngine = renderEngine;
        _assetManager = assetManager;
        _jobScheduler = jobScheduler;
        _settings = settings;

        _gpuSceneResource = new GPUSceneResource(assetManager);
        _meshPipelineResource = new MeshPipelineResource(assetManager);
        _materialPipelineResource = new MaterialPipelineResource(assetManager);
        _lightingPipelineResource = new LightingPipelineResource(assetManager);

        _gpuScene = new GPUScene(renderEngine.GraphicsEngine.ResourceAllocator, renderEngine.GraphicsEngine.ResourceDatabase, settings.MaxVisibleMeshletsOnScreen / 64);
        _gpuViewManager = new GPUViewManager(renderEngine.GraphicsEngine.ResourceAllocator, renderEngine.GraphicsEngine.ResourceDatabase, renderEngine.GraphicsEngine.PipelineLibrary, renderEngine.ResourceManager, renderEngine.ShaderLibrary);
        _rayTracingManager = new RayTracingManager(renderEngine.GraphicsEngine.Device, renderEngine.GraphicsEngine.ResourceAllocator, renderEngine.GraphicsEngine.ResourceDatabase, renderEngine.ResourceManager, jobScheduler);

        _gpuSceneResource.Resolve();
        _meshPipelineResource.Resolve();
        _materialPipelineResource.Resolve();
        _lightingPipelineResource.Resolve();

        InitializeCulling(renderEngine, assetManager);
        InitializeVisibility(renderEngine, assetManager);
        InitializeClassification(renderEngine, assetManager);
        InitializeDeferredTexturing(renderEngine);
    }

    private void ValidateRequiredGPUFeatures(IRenderDevice renderDevice)
    {
        var supported = renderDevice.DeviceFeture.SupportedFeatures;
        if (!supported.HasFlag(FeatureSupport.MeshShaders))
        {
            throw new PlatformNotSupportedException("Mesh shaders are not supported on this device. GhostRenderPipeline requires mesh shader support.");
        }

        if (!supported.HasFlag(FeatureSupport.BindlessResources))
        {
            throw new PlatformNotSupportedException("Bindless resources are not supported on this device. GhostRenderPipeline requires bindless resource support.");
        }

        if (!supported.HasFlag(FeatureSupport.Int64Atomics))
        {
            throw new PlatformNotSupportedException("Int64 atomics are not supported on this device. GhostRenderPipeline requires Int64 atomics support.");
        }

        if (!supported.HasFlag(FeatureSupport.Int64AtomicsOnTypedResources))
        {
            throw new PlatformNotSupportedException("Int64 atomics on typed resources are not supported on this device. GhostRenderPipeline requires Int64 atomics on typed resources support.");
        }

        if (!supported.HasFlag(FeatureSupport.WorkGraphs))
        {
            throw new PlatformNotSupportedException("Work graphs are not supported on this device. GhostRenderPipeline requires work graphs support.");
        }

        if (!supported.HasFlag(FeatureSupport.RayTracing) && _settings.EnableRayTracing)
        {
            Logger.Warning("Ray tracing is not supported on this device but is enabled in the render pipeline settings. Ray tracing features will be disabled.");
            _settings.EnableRayTracing = false;
        }
    }

    public IRenderPayload CreatePayload()
    {
        return new GhostRenderPayload(this);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsRTEnabled(in ShadowSettings shadowSettings)
    {
        if (!_settings.EnableRayTracing)
        {
            return false;
        }

        return _settings.DirectLightingMode == DirectLightingMode.Stochastic ||
            shadowSettings.shadowMode == ShadowMode.RayTraced;
    }

    public void RecordPrelude(scoped in RenderContext ctx, ICommandBuffer commandBuffer, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        ctx.ResourceManager.UploadMaterials(ctx, commandBuffer);
        ctx.ResourceManager.UploadMaterialPaletteData(ctx, commandBuffer);

        // Update GPU scene instance buffer once per frame prelude
        UpdateGPUScene(ctx, commandBuffer, ghostPayload);

        // Build any newly added mesh proxy BLASes
        _rayTracingManager.BuildPendingBlas(commandBuffer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ModifyVolumeSettings(ref ShadowSettings shadowSettings)
    {
        if (!_settings.EnableRayTracing)
        {
            if (shadowSettings.shadowMode == ShadowMode.RayTraced)
            {
                shadowSettings.shadowMode = ShadowMode.ShadowMap;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private KeywordMask128 BuildViewKeywords(in ShadowSettings shadowSettings)
    {
        var viewKeywords = KeywordMask128.Empty;

        switch (_settings.DirectLightingMode)
        {
            case DirectLightingMode.FPTL:
                viewKeywords = viewKeywords.Set(s_kwDirectLightingFPTL.Id);
                break;
            case DirectLightingMode.Stochastic:
                viewKeywords = viewKeywords.Set(s_kwDirectLightingStochastic.Id);
                break;
        }

        switch (shadowSettings.shadowMode)
        {
            case ShadowMode.ShadowMap:
                viewKeywords = viewKeywords.Set(s_kwShadowPCF.Id);
                break;
            case ShadowMode.RayTraced:
                viewKeywords = viewKeywords.Set(s_kwShadowRayTraced.Id);
                break;
        }

        return viewKeywords;
    }

    private Identifier<RGBuffer> AddBuildTLASPass(RenderGraph rg, in RenderRequest request, in Frustum frustum, GhostRenderPayload ghostPayload)
    {
        var activeInstanceCount = Math.Min(ghostPayload.InstanceCount, _gpuScene.InstanceCount);
        if (activeInstanceCount == 0)
        {
            return Identifier<RGBuffer>.Invalid;
        }

        var inputs = new AccelerationStructureBuildInputs
        {
            type = AccelerationStructureType.TopLevel,
            flags = AccelerationStructureBuildFlags.PreferFastTrace,
            numDescs = activeInstanceCount,
            instanceDescs = 0
        };
        var prebuildInfo = _renderEngine.GraphicsEngine.Device.GetAccelerationStructurePrebuildInfo(in inputs);
        var alignedResultSize = RayTracingManager.Align256(prebuildInfo.resultDataMaxSizeInBytes);
        var alignedScratchSize = RayTracingManager.Align256(prebuildInfo.scratchDataSizeInBytes);

        using var builder = rg.AddComputeRenderPass<BuildTLASPassData>("Build TLAS");
        builder.EnableAsyncCompute(true);

        var instanceDescBuffer = builder.CreateBuffer(new BufferDesc
        {
            Size = activeInstanceCount * (ulong)sizeof(RaytracingInstanceDesc),
            Stride = (uint)sizeof(RaytracingInstanceDesc),
            Usage = BufferUsage.ShaderResource,
            HeapType = HeapType.Upload
        }, "TLAS_InstanceDescBuffer");

        var scratchBuffer = builder.CreateBuffer(new BufferDesc
        {
            Size = alignedScratchSize,
            Stride = 1,
            Usage = BufferUsage.AccelerationStructure | BufferUsage.UnorderedAccess,
            HeapType = HeapType.Default
        }, "TLAS_ScratchBuffer");

        var tlasBuffer = builder.CreateBuffer(new BufferDesc
        {
            Size = alignedResultSize,
            Stride = 1,
            Usage = BufferUsage.AccelerationStructure | BufferUsage.ShaderResource,
            HeapType = HeapType.Default
        }, "Scene_TLAS");

        builder.UseBuffer(instanceDescBuffer, AccessFlags.Read);
        builder.UseBuffer(scratchBuffer, AccessFlags.Write);
        builder.UseBuffer(tlasBuffer, AccessFlags.Write);

        var rtSettings = ghostPayload.GetViewVolumeData<RayTracingSettings>(request.viewId);
        builder.SetPassData(new BuildTLASPassData
        {
            rayTracingManager = _rayTracingManager,
            settings = new TLASBuildSettings
            {
                frustum = frustum,
                cameraPosition = request.view.localToWorld.c3.xyz,
                cameraRotation = request.view.localToWorld.GetRotation(),
                cullMode = rtSettings.cullMode.GetValueOrDefault(),
                sphereCullDistance = rtSettings.sphereCullDistance.GetValueOrDefault(),
                solidAngleThreshold = rtSettings.solidAngleThreshold.GetValueOrDefault()
            },
            activeInstanceCount = activeInstanceCount,
            instanceDescBuffer = instanceDescBuffer,
            scratchBuffer = scratchBuffer,
            tlasBuffer = tlasBuffer
        });

        builder.SetRenderFunc<BuildTLASPassData>(static (ref readonly data, computeCtx) =>
        {
            data.rayTracingManager.BuildTLAS(in data.settings, computeCtx, data.instanceDescBuffer, data.scratchBuffer, data.tlasBuffer, data.activeInstanceCount);
        });

        return tlasBuffer;
    }

    private Identifier<RGTexture> AddCullingAndVbufferPasses(RenderGraph rg, HzbViewResource hzbHistory, uint2 renderSize, uint instanceCount, Identifier<RGTexture> hzbTexture, uint sceneBuffer)
    {
        var currentVisBuffer = Identifier<RGTexture>.Invalid;

        // Pass 1: Early-Z Hierarchical Meshlet Culling

        AddMeshletCullPass1(rg, hzbTexture, hzbHistory.HzbMipCount, renderSize, hzbHistory.HzbSize, instanceCount,
            out var unbinnedMeshlets0, out var occludedMeshlets, out var counterBuffer);

        AddPrepareIndirectArgsPass(rg, counterBuffer, 0, out var indirectArg0, out var binOffsets0, out var binScatterCounters0);
        var visibleMeshlets0 = AddScatterMeshletsPass(rg, unbinnedMeshlets0, binScatterCounters0, counterBuffer, 0);
        AddVisibilityBufferPass(rg, visibleMeshlets0, binOffsets0, indirectArg0, 0, sceneBuffer, renderSize, ref currentVisBuffer);
        AddSoftwareRasterizerPass(rg, visibleMeshlets0, binOffsets0, indirectArg0, currentVisBuffer, renderSize);
        AddBuildHZBPasses(rg, currentVisBuffer, hzbTexture, hzbHistory.HzbMipCount, hzbHistory.HzbSize, renderSize);

        // Pass 2: Late-Z Meshlet Culling

        var unbinnedMeshlets1 = AddMeshletCullPass2(rg, occludedMeshlets, counterBuffer, indirectArg0, hzbTexture,
            hzbHistory.HzbMipCount, renderSize, hzbHistory.HzbSize);

        AddPrepareIndirectArgsPass(rg, counterBuffer, 1, out var indirectArg1, out var binOffsets1, out var binScatterCounters1);
        var visibleMeshlets1 = AddScatterMeshletsPass(rg, unbinnedMeshlets1, binScatterCounters1, counterBuffer, 1);
        AddVisibilityBufferPass(rg, visibleMeshlets1, binOffsets1, indirectArg1, 1, sceneBuffer, renderSize, ref currentVisBuffer);

        // Export stable depth ONCE at the end of geometry passes
        var depthtexture = AddExportVisibilityDepthPass(rg, currentVisBuffer, renderSize);
        AddBuildHZBPasses(rg, currentVisBuffer, hzbTexture, hzbHistory.HzbMipCount, hzbHistory.HzbSize, renderSize);

        var visData = new GeometryVisibilityData
        {
            visBuffer = currentVisBuffer,
            visibleMeshlets0 = visibleMeshlets0,
            visibleMeshlets1 = visibleMeshlets1
        };

        rg.Blackboard.Add(ref visData);

        return depthtexture;
    }

    public Result ExecuteGraph(scoped in ResourceContext ctx, int frameIndex, ulong frameNumber, IRenderPayload payload, in RenderGraphExecutionContext executionContext)
    {
        var ghostPayload = (GhostRenderPayload)payload;
        if (_lastRenderRequestCount != ghostPayload.RenderRequests.Length || _lastInstanceCount != ghostPayload.InstanceCount)
        {
            _lastRenderRequestCount = ghostPayload.RenderRequests.Length;
            _lastInstanceCount = ghostPayload.InstanceCount;
        }

        if (ghostPayload.RenderRequests.Length == 0)
        {
            return Result.Success();
        }

        using var lights = UploadLights(ctx, ghostPayload,
            out var punctualLightsSrv, out var punctualLightCount,
            out var directionalLightSrv, out var directionalLightCount, out var primaryDirectionalLightIndex);

        var frameBuffer = RenderPipelineUtility.CreateFrameBuffer(ctx, _gpuScene.SceneBufferSrvIndex,
            punctualLightsSrv, punctualLightCount, directionalLightSrv, directionalLightCount, primaryDirectionalLightIndex,
            _assetManager.MeshletStreamingManager.PagePoolSrvIndex, _assetManager.MeshletStreamingManager.PageTableSrvIndex);

        for (var requestIndex = 0; requestIndex < ghostPayload.RenderRequests.Length; requestIndex++)
        {
            ref readonly var request = ref ghostPayload.RenderRequests[requestIndex];
            using var renderView = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);

            // TODO: Dynamic resolution. For now, we just use the render view's screen size as the viewport size.
            var viewPort = new ViewportState(renderView.ScreenSize.x, renderView.ScreenSize.y, renderView.ScreenSize.x, renderView.ScreenSize.y);

            // Upload ViewData (Reversed-Z: near=1.0, far=0.0)
            RenderPipelineUtility.GetVPMatricesReversedZ(in request, viewPort.Size, out var viewMatrix, out var projMatrix);

            var viewProjMatrix = math.mul(projMatrix, viewMatrix);
            var frustum = Frustum.Create(viewProjMatrix, float3.zero, request.view.localToWorld.c2.xyz, request.view.nearClipPlane, request.view.farClipPlane);

            var shadowSettings = ghostPayload.GetViewVolumeData<ShadowSettings>(request.viewId);
            ModifyVolumeSettings(ref shadowSettings);

            var viewKeywords = BuildViewKeywords(shadowSettings);
            var viewContext = _gpuViewManager.GetView(request.viewId);
            viewContext.Initialize(viewPort.Size);

            if (viewContext.prevViewProjMatrix.Equals(float4x4.zero))
            {
                viewContext.prevViewProjMatrix = viewProjMatrix;
                viewContext.prevCameraPosition = request.view.localToWorld.c3.xyz;
            }

            var viewBuffer = RenderPipelineUtility.CreateViewDataBuffer(ctx, request, renderView, viewMatrix, projMatrix, viewProjMatrix, frustum,
                ref viewContext.prevViewProjMatrix, ref viewContext.prevCameraPosition);

            var renderGraph = viewContext.RenderGraph;

            renderGraph.BeginRecord(viewKeywords);
            renderGraph.SetFrameData(frameBuffer);
            renderGraph.SetViewData(viewBuffer);

            _meshPipelineResource.EnsureWorkGraphProgram(_renderEngine, _assetManager, viewKeywords);

            var colorTarget = renderGraph.ImportTexture(renderView.ColorTexture, ResourceBarrierData.Present, ResourceBarrierData.Present);
            var hzbHistory = viewContext.GetOrCreateResource<HzbViewResource, HzbViewResource.Desc>(new HzbViewResource.Desc
            {
                renderSize = viewPort.ActualSize
            });

            var historyHzb = renderGraph.ImportTexture(hzbHistory.Texture, ResourceBarrierData.Common, ResourceBarrierData.Common);

            var tlasBuffer = Identifier<RGBuffer>.Invalid;
            if (IsRTEnabled(shadowSettings))
            {
                tlasBuffer = AddBuildTLASPass(renderGraph, in request, in frustum, ghostPayload);
            }

            // Geometry & Visibility passes
            var depthTexture = AddCullingAndVbufferPasses(renderGraph, hzbHistory, viewPort.Size, ghostPayload.InstanceCount, historyHzb, _gpuScene.SceneBufferSrvIndex);

            // Tile Classification & Deferred Texturing
            AddTileClassificationPass(renderGraph, viewPort.Size);
            var gbuffer = AddDeferredTexturingPass(renderGraph, viewPort.Size);
            var tileLightList = AddTileLightCullingPass(renderGraph, depthTexture, viewPort.Size);
            var gbufferData = new GBufferPassData
            {
                gbuffer = gbuffer,
                tileLightList = tileLightList
            };

            renderGraph.Blackboard.Add(ref gbufferData);

            // Shadow Passes
            var shadowAtlas = Identifier<RGTexture>.Invalid;
            var shadowViewsBufferSrv = RHIUtility.INVALID_BINDLESS_INDEX;
            var shadowIndicesBufferSrv = RHIUtility.INVALID_BINDLESS_INDEX;

            if (_settings.DirectLightingMode == DirectLightingMode.Stochastic)
            {
                var desc = new RisViewResource.Desc
                {
                    renderSize = viewPort.ActualSize
                };

                var risHistory = viewContext.GetOrCreateResource<RisViewResource, RisViewResource.Desc>(in desc);
                AddRisPasses(renderGraph, risHistory, depthTexture, tlasBuffer, viewPort.Size, (uint)frameNumber, request.viewId);
            }
            else // FPTL
            {
                ExecutePerViewShadowSetup(ctx, in frustum, viewPort.Size.y, lights, request.view.localToWorld.c3.xyz,
                    out shadowViewsBufferSrv, out var shadowViewCount, out shadowIndicesBufferSrv);

                if (shadowSettings.shadowMode == ShadowMode.ShadowMap)
                {
                    shadowAtlas = AddPunctualShadowAtlasPass(renderGraph, ghostPayload.InstanceCount, shadowViewsBufferSrv, shadowViewCount);
                }
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
                    var setupResult = ComputeDirectionalCascadeViews(ctx, in dirRequest, in request, in projMatrix, in shadowSettings, viewPort.Size.y);
                    directionalShadowViewsBufferSrv = setupResult.directionalShadowViewsBufferSrv;
                    cascadeCount = setupResult.cascadeCount;
                    cascadeSplits = setupResult.cascadeSplits;
                    cascadeBlends = setupResult.cascadeBlends;
                    maxShadowDistance = setupResult.maxShadowDistance;
                    shadowFadeRange = setupResult.shadowFadeRange;

                    var occluderMapDesc = RGTextureDesc.Absolute(
                        64,
                        64,
                        TextureFormat.R32_Typeless,
                        clearColor: default,
                        clearAtFirstUse: false,
                        discardAtLastUse: true,
                        dimension: TextureDimension.Texture2DArray,
                        mipLevels: 7,
                        slice: setupResult.cascadeCount,
                        usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
                        typelessViewFormat: new TypelessFormatDesc { Srv = TextureFormat.R32_Float, Uav = TextureFormat.R32_UInt });

                    var occluderMap = renderGraph.CreateTexture(in occluderMapDesc, "CascadeOccluderMap");
                    AddGenerateShadowOccludersPass(renderGraph, depthTexture, occluderMap, directionalShadowViewsBufferSrv, viewPort.Size, cascadeCount, cascadeSplits);
                    AddBuildShadowOccluderHZBPass(renderGraph, occluderMap, cascadeCount);

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

            // Sky & Lighting
            var persistentViewStateBuffer = renderGraph.ImportBuffer(_gpuViewManager.PersistentViewStateBuffer, ResourceBarrierData.Common, ResourceBarrierData.Common);
            var skyContext = PrepareSky(renderGraph, viewContext, in request, ghostPayload, persistentViewStateBuffer);
            var litColor = AddDeferredLightingPass(renderGraph, in skyContext, historyHzb, hzbHistory.HzbMipCount, hzbHistory.HzbSize,
                    depthTexture, tlasBuffer, request.viewId, viewPort.Size);

            AddSkyPass(renderGraph, in skyContext, litColor, depthTexture);

            // Presentation
            renderGraph.AddBlitPass(litColor, colorTarget, _meshPipelineResource.blitShader, true);

            var result = renderGraph.CompileAndExecute(executionContext, viewPort, RGFlags.Default);
            if (result.IsFailure)
            {
                return Result.Failure($"Render graph execution failed: {result.Error}");
            }
        }

        _gpuViewManager.EndFrame();

        return Result.Success();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        DisposeCulling();
        DisposeVisibility();
        DisposeClassification();
        DisposeDeferredTexturing();

        _gpuSceneResource.Dispose();
        _meshPipelineResource.Dispose();
        _materialPipelineResource.Dispose();
        _lightingPipelineResource.Dispose();

        _gpuScene.Dispose();
        _gpuViewManager.Dispose();
        _rayTracingManager.Dispose();
    }
}
