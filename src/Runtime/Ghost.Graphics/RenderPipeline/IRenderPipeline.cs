using Ghost.Graphics.Core;

namespace Ghost.Graphics.RenderPipeline;

public interface IRenderPayload : IDisposable;

public interface IRenderPipelineSettings
{
    void CreatePipeline(RenderSystem renderSystem, out IRenderPipeline renderPipeline, out IRenderPayload renderPayload);
}

public interface IRenderPipeline : IDisposable
{
    void Render(RenderContext ctx, int frameIndex, IRenderPayload payload);
}
