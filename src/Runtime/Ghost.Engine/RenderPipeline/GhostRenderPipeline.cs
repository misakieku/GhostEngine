using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Engine.Streaming;
using Ghost.Engine.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;

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

    private bool _disposed;
    private int _lastRenderRequestCount = -1;
    private uint _lastInstanceCount = uint.MaxValue;

    public GPUScene GPUScene => _gpuScene;
    public GPUViewManager GPUViewManager => _gpuViewManager;
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

        _gpuSceneResource.Resolve();
        _meshPipelineResource.Resolve();
        _materialPipelineResource.Resolve();
        _lightingPipelineResource.Resolve();

        InitializeCulling(renderEngine, assetManager);
        InitializeVisibility(renderEngine, assetManager);
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

    public void RecordPrelude(RenderContext ctx, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        ctx.ResourceManager.UploadMaterials(ctx);
        ctx.ResourceManager.UploadMaterialPaletteData(ctx);

        // Update GPU scene instance buffer once per frame prelude
        UpdateGPUScene(ctx, ghostPayload);
    }

    public Result ExecuteGraph(ResourceContext ctx, int frameIndex, IRenderPayload payload, in RenderGraphExecutionContext executionContext)
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

        UploadLights(ctx, ghostPayload,
            out var punctualLightsSrv, out var punctualLightCount,
            out var directionalLightSrv, out var directionalLightCount, out var primaryDirectionalLightIndex);

        var frameBuffer = RenderPipelineUtility.CreateFrameBuffer(ctx, _gpuScene.SceneBufferSrvIndex, punctualLightsSrv, punctualLightCount, directionalLightSrv, directionalLightCount, primaryDirectionalLightIndex);

        for (var requestIndex = 0; requestIndex < ghostPayload.RenderRequests.Length; requestIndex++)
        {
            ref readonly var request = ref ghostPayload.RenderRequests[requestIndex];
            using var renderView = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);
            var viewState = new ViewState(renderView.ScreenSize.x, renderView.ScreenSize.y, renderView.ScreenSize.x, renderView.ScreenSize.y);

            // Upload ViewData (Reversed-Z: near=1.0, far=0.0)
            RenderPipelineUtility.GetVPMatricesReversedZ(in request, renderView.ScreenSize, out var viewMatrix, out var projMatrix);

            var viewProjMatrix = math.mul(projMatrix, viewMatrix);
            var frustum = Frustum.Create(viewProjMatrix, request.view.localToWorld.c3.xyz, request.view.localToWorld.c2.xyz, request.view.nearClipPlane, request.view.farClipPlane);

            ExecutePerViewShadowSetup(ctx, ghostPayload, in frustum,
                out var shadowViewsBufferSrv, out var shadowViewCount, out var shadowIndicesBufferSrv);

            var viewContext = _gpuViewManager.GetView(request.viewId);
            viewContext.EnsureResources(renderView.ScreenSize, _settings.ShadowAtlasResolution);

            ExecutePerViewShadowSetup(ctx, ghostPayload, viewContext.ShadowAllocator, in frustum,
                out var shadowViewsBufferSrv, out var shadowViewCount, out var shadowIndicesBufferSrv);

            if (viewContext.prevViewProjMatrix.Equals(float4x4.zero))
            {
                viewContext.prevViewProjMatrix = viewProjMatrix;
            }

            var viewBuffer = RenderPipelineUtility.CreateViewDataBuffer(ctx, request, renderView, viewMatrix, projMatrix, viewProjMatrix, frustum, ref viewContext.prevViewProjMatrix);

            viewContext.RenderGraph.Reset();
            viewContext.RenderGraph.SetFrameData(frameBuffer);
            viewContext.RenderGraph.SetViewData(viewBuffer);

            var colorTarget = viewContext.RenderGraph.ImportTexture(renderView.ColorTexture, ResourceBarrierData.Present, ResourceBarrierData.Present);
            var hzb = viewContext.RenderGraph.ImportTexture(viewContext.HzbTexture, ResourceBarrierData.Common, ResourceBarrierData.Common);

            AddCullingAndVbufferPasses(viewContext, ghostPayload.InstanceCount, hzb, _gpuScene.SceneBufferSrvIndex,
                out var currentDepth, out var currentVisBuffer,
                out var visibleMeshlets0, out var visibleMeshlets1);

            AddTileClassificationPass(viewContext.RenderGraph, currentVisBuffer, visibleMeshlets0, visibleMeshlets1, viewContext.RenderSize,
                out var tileListBuffer, out var tileOffsetsBuffer, out var indirectArgsBuffer);

            var gbuffer = AddDeferredTexturingPass(viewContext.RenderGraph, currentVisBuffer, visibleMeshlets0, visibleMeshlets1, tileListBuffer, tileOffsetsBuffer, indirectArgsBuffer, viewContext.RenderSize);
            var tileLightList = AddTileLightCullingPass(viewContext.RenderGraph, currentDepth, viewContext.RenderSize);

            var shadowAtlas = AddPunctualShadowAtlasPass(viewContext.RenderGraph, ghostPayload.InstanceCount, shadowViewsBufferSrv, shadowViewCount);

            if (_settings.DebugMode == RenderPipelineDebugMode.TileLightHeatmap)
            {
                AddDebugTileLightHeatmapPass(viewContext.RenderGraph, tileLightList, currentDepth, colorTarget, viewContext.RenderSize);
            }
            else
            {
                var litColor = AddDeferredLightingPass(viewContext.RenderGraph, gbuffer, currentDepth, tileLightList, shadowAtlas, shadowViewsBufferSrv, shadowIndicesBufferSrv, viewContext.RenderSize);
                viewContext.RenderGraph.AddBlitPass(litColor, colorTarget, _meshPipelineResource.blitShader, true);
            }

            // TODO: Dynamic resolution.
            var viewPort = new ViewportState(renderView.ScreenSize.x, renderView.ScreenSize.y, renderView.ScreenSize.x, renderView.ScreenSize.y);
            var result = viewContext.RenderGraph.CompileAndExecute(executionContext, viewPort, RGFlags.Default);
            if (result.IsFailure)
            {
                return Result.Failure($"Render graph execution failed: {result.Error}");
            }

            // The graph has now actually consumed its backing memory, so subsequent dispatches must not
            // re-specify D3D12_SET_WORK_GRAPH_FLAG_INITIALIZE. Latching here (rather than at pass-build
            // time) keeps the flag honest if this frame's graph was compiled but failed to execute.
            _meshPipelineResource.cullWorkGraphProgram?.MarkInitialized();
            if (shadowAtlas.IsValid)
            {
                _meshPipelineResource.shadowCullWorkGraphProgram?.MarkInitialized();
            }
        }

        return Result.Success();
    }

    private void AddCullingAndVbufferPasses(GPUViewContext viewContext, uint instanceCount, Identifier<RGTexture> hzb, uint sceneBuffer,
        out Identifier<RGTexture> currentDepth, out Identifier<RGTexture> currentVisBuffer, out Identifier<RGBuffer> visibleMeshlets0, out Identifier<RGBuffer> visibleMeshlets1)
    {
        currentVisBuffer = Identifier<RGTexture>.Invalid;
        currentDepth = Identifier<RGTexture>.Invalid;

        // Pass 1: Early-Z Hierarchical Meshlet Culling

        AddMeshletCullPass1(viewContext.RenderGraph, hzb, viewContext.HzbMipCount, viewContext.RenderSize, viewContext.HzbSize, instanceCount,
            out var unbinnedMeshlets0, out var occludedMeshlets, out var counterBuffer);

        AddPrepareIndirectArgsPass(viewContext.RenderGraph, counterBuffer, 0, out var indirectArg0, out var binOffsets0, out var binScatterCounters0);
        visibleMeshlets0 = AddScatterMeshletsPass(viewContext.RenderGraph, unbinnedMeshlets0, binScatterCounters0, counterBuffer, 0);
        AddVisibilityBufferPass(viewContext.RenderGraph, visibleMeshlets0, binOffsets0, indirectArg0, 0, sceneBuffer, viewContext.RenderSize, ref currentVisBuffer);
        AddBuildHZBPasses(viewContext.RenderGraph, currentVisBuffer, hzb, viewContext.HzbMipCount, viewContext.HzbSize, viewContext.RenderSize);

        // Pass 2: Late-Z Meshlet Culling

        var unbinnedMeshlets1 = AddMeshletCullPass2(viewContext.RenderGraph, occludedMeshlets, counterBuffer, indirectArg0, hzb,
            viewContext.HzbMipCount, viewContext.RenderSize, viewContext.HzbSize);

        AddPrepareIndirectArgsPass(viewContext.RenderGraph, counterBuffer, 1, out var indirectArg1, out var binOffsets1, out var binScatterCounters1);
        visibleMeshlets1 = AddScatterMeshletsPass(viewContext.RenderGraph, unbinnedMeshlets1, binScatterCounters1, counterBuffer, 1);
        AddVisibilityBufferPass(viewContext.RenderGraph, visibleMeshlets1, binOffsets1, indirectArg1, 1, sceneBuffer, viewContext.RenderSize, ref currentVisBuffer);

        // Export stable depth ONCE at the end of geometry passes
        AddExportVisibilityDepthPass(viewContext.RenderGraph, currentVisBuffer, viewContext.RenderSize, ref currentDepth);
        AddBuildHZBPasses(viewContext.RenderGraph, currentVisBuffer, hzb, viewContext.HzbMipCount, viewContext.HzbSize, viewContext.RenderSize);
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
        DisposeDeferredTexturing();

        _gpuSceneResource.Dispose();
        _meshPipelineResource.Dispose();
        _materialPipelineResource.Dispose();
        _lightingPipelineResource.Dispose();

        _gpuScene.Dispose();
        _gpuViewManager.Dispose();
    }
}
