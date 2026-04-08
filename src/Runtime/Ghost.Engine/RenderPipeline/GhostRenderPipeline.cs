using Ghost.Graphics;
using Ghost.Graphics.Core;

namespace Ghost.Engine.RenderPipeline;

internal class GhostRenderPipeline : IRenderPipeline
{
    private readonly RenderSystem _renderSystem;

    private readonly GPUScene _gpuScene;

    public GPUScene GPUScene => _gpuScene;

    public GhostRenderPipeline(RenderSystem renderSystem)
    {
        _renderSystem = renderSystem;
        _gpuScene = new GPUScene(renderSystem.GraphicsEngine.ResourceAllocator, renderSystem.GraphicsEngine.ResourceDatabase, 102_400u); // 102.4k objects should be enough for now
    }

    public void Render(RenderContext ctx, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        var resourceManager = _renderSystem.ResourceManager;
        var resourceDatabase = _renderSystem.GraphicsEngine.ResourceDatabase;

        foreach (ref readonly var request in ghostPayload.RenderRequests)
        {
            if (!RenderPipelineUtility.GetViewAndProjectionMatrices(_renderSystem, in request, out var view, out var projection, out var screenSize))
            {
                continue;
            }
        }
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
