using Ghost.Graphics.Core;

namespace Ghost.Graphics.RenderPipeline;

public interface IRenderPayload : IDisposable
{
    void Reset();
}

public interface IRenderPipelineSettings
{
    IRenderPipeline CreatePipeline(RenderSystem renderSystem);
    IRenderPayload CreatePayload(RenderSystem renderSystem);
}

public interface IRenderPipeline : IDisposable
{
    void Render(RenderContext ctx, int frameIndex, IRenderPayload payload);
}
