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

    public void Render(RenderContext ctx, int frameIndex, IRenderPayload payload)
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

    public void Dispose()
    {
        _renderGraph.Dispose();
        _gpuScene.Dispose();
    }
}
