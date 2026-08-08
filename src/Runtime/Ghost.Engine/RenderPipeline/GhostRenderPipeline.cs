using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline : IRenderPipeline
{
    private readonly RenderEngine _renderEngine;

    private readonly RenderGraph _renderGraph;
    private readonly GPUScene _gpuScene;

    public GPUScene GPUScene => _gpuScene;

    public GhostRenderPipeline(RenderEngine renderEngine)
    {
        _renderEngine = renderEngine;

        _renderGraph = new RenderGraph(
            renderEngine.GraphicsEngine.ResourceDatabase,
            renderEngine.GraphicsEngine.ResourceAllocator,
            renderEngine.GraphicsEngine.PipelineLibrary,
            renderEngine.ResourceManager,
            renderEngine.ShaderLibrary);
        _gpuScene = new GPUScene(renderEngine.GraphicsEngine.ResourceAllocator, renderEngine.GraphicsEngine.ResourceDatabase, 102_400u); // 102.4k objects should be enough for now
    }

    public void RecordPrelude(RenderContext ctx, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        foreach (ref readonly var request in ghostPayload.RenderRequests)
        {
            try
            {
                using var viewData = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);
                RenderPipelineUtility.GetVPMatrices(in request, viewData.ScreenSize, out var view, out var projection);

                UpdateGPUScene(ctx, ghostPayload);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }

    public RGExecution ExecuteGraph(RenderContext ctx, int frameIndex, IRenderPayload payload,
        in RenderGraphExecutionContext executionContext)
    {
        var ghostPayload = (GhostRenderPayload)payload;
        var viewState = DeriveViewState(ghostPayload);
        var result = _renderGraph.CompileAndExecute(executionContext, viewState);
        if (result.IsFailure)
        {
            Logger.Error($"Render graph execution failed: {result.Error}");
            return default;
        }

        return result.Value;
    }

    /// <summary>
    /// Derives a <see cref="ViewState"/> from the first available render request.
    /// </summary>
    /// <remarks>
    /// Phase 6 placeholder: the graph has no passes yet so ViewState only affects relative texture
    /// sizing. Phase 7 will derive exact dimensions from the active swap-chain back buffer.
    /// </remarks>
    private static ViewState DeriveViewState(GhostRenderPayload payload)
    {
        var requests = payload.RenderRequests;
        if (requests.Length > 0)
        {
            return new ViewState(1920, 1080, 1920, 1080);
        }

        return default;
    }

    public void Dispose()
    {
        _renderGraph.Dispose();
        _gpuScene.Dispose();
    }
}
