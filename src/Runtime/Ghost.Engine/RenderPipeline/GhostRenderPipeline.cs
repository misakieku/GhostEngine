using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Engine.Volumes;
using Ghost.Graphics;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline : IRenderPipeline
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

    private static readonly PipelineKeyword s_kwShadowPCF = PipelineKeywordRegistry.GetOrRegister("_SHADOW_PCF");
    private static readonly PipelineKeyword s_kwShadowRIS = PipelineKeywordRegistry.GetOrRegister("_SHADOW_RIS");

    private bool _disposed;
    private int _lastRenderRequestCount = -1;
    private uint _lastInstanceCount = uint.MaxValue;

    public GPUScene GPUScene => _gpuScene;
    public GPUViewManager GPUViewManager => _gpuViewManager;
    public RayTracingManager RayTracingManager => _rayTracingManager;
    public GhostRenderPipelineSettings Settings => _settings;

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

    private static void ValidateRequiredGPUFeatures(IRenderDevice renderDevice)
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
    }

    public IRenderPayload CreatePayload()
    {
        return new GhostRenderPayload(this);
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
    private static KeywordMask128 BuildViewKeywords(in ShadowSettings shadowSettings)
    {
        var viewKeywords = KeywordMask128.Empty;
        switch (shadowSettings.shadowMode)
        {
            case ShadowMode.ShadowMap:
                viewKeywords = viewKeywords.Set(s_kwShadowPCF.Id);
                break;
            case ShadowMode.RayTraced:
                viewKeywords = viewKeywords.Set(s_kwShadowRIS.Id);
                break;
        }

        return viewKeywords;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasRTEnable(in ShadowSettings shadowSettings)
    {
        return shadowSettings.shadowMode == ShadowMode.RayTraced;
    }

    private void AddCullingAndVbufferPasses(RenderGraph rg, HzbViewHistory hzbHistory, uint2 renderSize, uint instanceCount, Identifier<RGTexture> hzbTexture, uint sceneBuffer)
    {
        var currentVisBuffer = Identifier<RGTexture>.Invalid;
        var currentDepth = Identifier<RGTexture>.Invalid;

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
        AddExportVisibilityDepthPass(rg, currentVisBuffer, renderSize, ref currentDepth);
        AddBuildHZBPasses(rg, currentVisBuffer, hzbTexture, hzbHistory.HzbMipCount, hzbHistory.HzbSize, renderSize);

        var visData = new GeometryVisibilityData
        {
            depth = currentDepth,
            visBuffer = currentVisBuffer,
            visibleMeshlets0 = visibleMeshlets0,
            visibleMeshlets1 = visibleMeshlets1
        };

        rg.Blackboard.Add(ref visData);
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
            var viewKeywords = BuildViewKeywords(shadowSettings);

            var viewContext = _gpuViewManager.GetView(request.viewId);
            viewContext.EnsureSize(viewPort.Size);

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

            var hzbHistory = viewContext.GetOrCreateHistory(static (alloc, db) => new HzbViewHistory(alloc, db));
            hzbHistory.EnsureResources(viewContext.RenderSize);
            var historyHzb = renderGraph.ImportTexture(hzbHistory.Texture, ResourceBarrierData.Common, ResourceBarrierData.Common);

            RayTracingViewHistory? rtHistory = null;
            if (HasRTEnable(shadowSettings))
            {
                rtHistory = viewContext.GetOrCreateHistory(static (alloc, db) => new RayTracingViewHistory(alloc, db));
                var rtSettings = ghostPayload.GetViewVolumeData<RayTracingSettings>(request.viewId);
                using (var builder = renderGraph.AddUnsafeRenderPass<BuildTLASPassData>("Build TLAS"))
                {
                    builder.AllowPassCulling(false);
                    builder.SetPassData(new BuildTLASPassData
                    {
                        rayTracingManager = _rayTracingManager,
                        viewHistory = rtHistory,
                        settings = new TLASBuildSettings
                        {
                            frustum = frustum,
                            cameraPosition = request.view.localToWorld.c3.xyz,
                            cameraRotation = request.view.localToWorld.GetRotation(),
                            cullMode = rtSettings.cullMode.GetValueOrDefault(),
                            sphereCullDistance = rtSettings.sphereCullDistance.GetValueOrDefault(),
                            solidAngleThreshold = rtSettings.solidAngleThreshold.GetValueOrDefault()
                        },
                        activeInstanceCount = _gpuScene.InstanceCount
                    });

                    builder.SetRenderFunc<BuildTLASPassData>(static (ref readonly data, ctx) =>
                    {
                        data.rayTracingManager.BuildTLAS(data.viewHistory, in data.settings, ctx.GetCommandBufferUnsafe(), data.activeInstanceCount);
                    });
                }
            }
            else
            {
                if (viewContext.TryGetHistory<RayTracingViewHistory>(out var existingRtHistory))
                {
                    existingRtHistory.Reset();
                }
            }

            // Geometry & Visibility passes
            AddCullingAndVbufferPasses(renderGraph, hzbHistory, viewContext.RenderSize, ghostPayload.InstanceCount, historyHzb, _gpuScene.SceneBufferSrvIndex);

            // Tile Classification & Deferred Texturing
            AddTileClassificationPass(renderGraph, viewContext.RenderSize);
            var gbuffer = AddDeferredTexturingPass(renderGraph, viewContext.RenderSize);
            var tileLightList = AddTileLightCullingPass(renderGraph, viewContext.RenderSize);
            var gbufferData = new GBufferPassData
            {
                gbuffer = gbuffer,
                tileLightList = tileLightList
            };

            renderGraph.Blackboard.Add(ref gbufferData);

            // Shadow Passes
            if (shadowSettings.shadowMode == ShadowMode.RayTraced)
            {
                Logger.DebugAssert(rtHistory != null);

                var risHistory = viewContext.GetOrCreateHistory(static (alloc, db) => new RisViewHistory(alloc, db));
                risHistory.EnsureResources(viewContext.RenderSize);
                AddRisPasses(renderGraph, risHistory, rtHistory, viewContext.RenderSize, (uint)frameNumber, request.viewId);
            }
            else
            {
                if (viewContext.TryGetHistory<RisViewHistory>(out var risHistory))
                {
                    risHistory.Invalidate();
                }

                if (shadowSettings.shadowMode == ShadowMode.ShadowMap)
                {
                    ExecutePerViewShadowSetup(ctx, in frustum, viewPort.Size.y, lights, request.view.localToWorld.c3.xyz,
                        out var shadowViewsBufferSrv, out var shadowViewCount, out var shadowIndicesBufferSrv);

                    var shadowAtlas = AddPunctualShadowAtlasPass(renderGraph, ghostPayload.InstanceCount, shadowViewsBufferSrv, shadowViewCount);
                    var shadowMapData = new ShadowMapPassData
                    {
                        shadowAtlas = shadowAtlas,
                        shadowViewsBufferSrv = shadowViewsBufferSrv,
                        shadowIndicesBufferSrv = shadowIndicesBufferSrv
                    };

                    renderGraph.Blackboard.Add(ref shadowMapData);
                }
            }

            // Sky & Lighting
            var persistentViewStateBuffer = renderGraph.ImportBuffer(_gpuViewManager.PersistentViewStateBuffer, ResourceBarrierData.Common, ResourceBarrierData.Common);
            var skyContext = PrepareSky(renderGraph, viewContext, in request, ghostPayload, persistentViewStateBuffer);

            var litColor = AddDeferredLightingPass(
                renderGraph, in skyContext, historyHzb, hzbHistory.HzbMipCount, hzbHistory.HzbSize, request.viewId, viewContext.RenderSize, rtHistory);

            AddSkyPass(renderGraph, in skyContext, litColor);

            // Presentation
            renderGraph.AddBlitPass(litColor, colorTarget, _meshPipelineResource.blitShader, true);

            var result = renderGraph.CompileAndExecute(executionContext, viewPort, RGFlags.Default);
            if (result.IsFailure)
            {
                return Result.Failure($"Render graph execution failed: {result.Error}");
            }

            // The graph has now actually consumed its backing memory, so subsequent dispatches must not
            // re-specify D3D12_SET_WORK_GRAPH_FLAG_INITIALIZE. Latching here (rather than at pass-build
            // time) keeps the flag honest if this frame's graph was compiled but failed to execute.
            _meshPipelineResource.cullWorkGraphProgram?.MarkInitialized();
            if (shadowSettings.shadowMode == ShadowMode.ShadowMap)
            {
                _meshPipelineResource.shadowCullWorkGraphProgram?.MarkInitialized();
            }
        }

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
