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

            var viewContext = _gpuViewManager.GetView(request.viewId);
            viewContext.EnsureResources(viewPort.Size, _settings.ShadowAtlasResolution);

            ExecutePerViewShadowSetup(ctx, viewContext.ShadowAllocator, in frustum, viewPort.Size.y, lights, request.view.localToWorld.c3.xyz,
                out var shadowViewsBufferSrv, out var shadowViewCount, out var shadowIndicesBufferSrv);

            if (viewContext.prevViewProjMatrix.Equals(float4x4.zero))
            {
                viewContext.prevViewProjMatrix = viewProjMatrix;
                viewContext.prevCameraPosition = request.view.localToWorld.c3.xyz;
            }

            var viewBuffer = RenderPipelineUtility.CreateViewDataBuffer(ctx, request, renderView, viewMatrix, projMatrix, viewProjMatrix, frustum, ref viewContext.prevViewProjMatrix, ref viewContext.prevCameraPosition);

            var renderGraph = viewContext.RenderGraph;

            renderGraph.Reset();
            renderGraph.SetFrameData(frameBuffer);
            renderGraph.SetViewData(viewBuffer);

            var colorTarget = renderGraph.ImportTexture(renderView.ColorTexture, ResourceBarrierData.Present, ResourceBarrierData.Present);
            var historyHzb = renderGraph.ImportTexture(viewContext.HzbTexture, ResourceBarrierData.Common, ResourceBarrierData.Common);

            AddCullingAndVbufferPasses(viewContext, ghostPayload.InstanceCount, historyHzb, _gpuScene.SceneBufferSrvIndex,
                out var currentDepth, out var currentVisBuffer,
                out var visibleMeshlets0, out var visibleMeshlets1);

            AddTileClassificationPass(renderGraph, currentVisBuffer, visibleMeshlets0, visibleMeshlets1, viewContext.RenderSize,
                out var tileListBuffer, out var tileOffsetsBuffer,
                out var indirectArgsBuffer, out var tileShadingModelMaskBuffer);

            var gbuffer = AddDeferredTexturingPass(renderGraph, currentVisBuffer, visibleMeshlets0, visibleMeshlets1, tileListBuffer, tileOffsetsBuffer, indirectArgsBuffer, viewContext.RenderSize);
            var tileLightList = AddTileLightCullingPass(renderGraph, currentDepth, viewContext.RenderSize);
            var shadowAtlas = AddPunctualShadowAtlasPass(renderGraph, ghostPayload.InstanceCount, shadowViewsBufferSrv, shadowViewCount);
            var persistentViewStateBuffer = renderGraph.ImportBuffer(_gpuViewManager.PersistentViewStateBuffer, ResourceBarrierData.Common, ResourceBarrierData.Common);
            var skyContext = PrepareSky(renderGraph, viewContext, in request, ghostPayload, persistentViewStateBuffer);

            var litColor = AddDeferredLightingPass(
                renderGraph, gbuffer, currentDepth, tileLightList, shadowAtlas, shadowViewsBufferSrv, shadowIndicesBufferSrv, tileShadingModelMaskBuffer,
                in skyContext, request.viewId, viewContext.RenderSize);

            AddSkyPass(renderGraph, in skyContext, litColor, currentDepth);

            renderGraph.AddBlitPass(litColor, colorTarget, _meshPipelineResource.blitShader, true);

            var result = renderGraph.CompileAndExecute(executionContext, viewPort, RGFlags.GenerateDump);
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

    private void AddCullingAndVbufferPasses(GPUViewContext viewContext, uint instanceCount, Identifier<RGTexture> historyHzb, uint sceneBuffer,
        out Identifier<RGTexture> currentDepth, out Identifier<RGTexture> currentVisBuffer, out Identifier<RGBuffer> visibleMeshlets0, out Identifier<RGBuffer> visibleMeshlets1)
    {
        currentVisBuffer = Identifier<RGTexture>.Invalid;
        currentDepth = Identifier<RGTexture>.Invalid;
        var currentHzb = Identifier<RGTexture>.Invalid;

        // Pass 1: Early-Z Hierarchical Meshlet Culling

        AddMeshletCullPass1(viewContext.RenderGraph, historyHzb, viewContext.HzbMipCount, viewContext.RenderSize, viewContext.HzbSize, instanceCount,
            out var unbinnedMeshlets0, out var occludedMeshlets, out var counterBuffer);

        AddPrepareIndirectArgsPass(viewContext.RenderGraph, counterBuffer, 0, out var indirectArg0, out var binOffsets0, out var binScatterCounters0);
        visibleMeshlets0 = AddScatterMeshletsPass(viewContext.RenderGraph, unbinnedMeshlets0, binScatterCounters0, counterBuffer, 0);
        AddVisibilityBufferPass(viewContext.RenderGraph, visibleMeshlets0, binOffsets0, indirectArg0, 0, sceneBuffer, viewContext.RenderSize, ref currentVisBuffer);
        AddBuildHZBPasses(viewContext.RenderGraph, currentVisBuffer, ref currentHzb, viewContext.HzbMipCount, viewContext.HzbSize, viewContext.RenderSize, viewContext.HzbTexture);

        // Pass 2: Late-Z Meshlet Culling

        var unbinnedMeshlets1 = AddMeshletCullPass2(viewContext.RenderGraph, occludedMeshlets, counterBuffer, indirectArg0, currentHzb,
            viewContext.HzbMipCount, viewContext.RenderSize, viewContext.HzbSize);

        AddPrepareIndirectArgsPass(viewContext.RenderGraph, counterBuffer, 1, out var indirectArg1, out var binOffsets1, out var binScatterCounters1);
        visibleMeshlets1 = AddScatterMeshletsPass(viewContext.RenderGraph, unbinnedMeshlets1, binScatterCounters1, counterBuffer, 1);
        AddVisibilityBufferPass(viewContext.RenderGraph, visibleMeshlets1, binOffsets1, indirectArg1, 1, sceneBuffer, viewContext.RenderSize, ref currentVisBuffer);

        // Export stable depth ONCE at the end of geometry passes
        AddExportVisibilityDepthPass(viewContext.RenderGraph, currentVisBuffer, viewContext.RenderSize, ref currentDepth);
        AddBuildHZBPasses(viewContext.RenderGraph, currentVisBuffer, ref currentHzb, viewContext.HzbMipCount, viewContext.HzbSize, viewContext.RenderSize);
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
    }
}
