using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RenderPipeline;

public interface IRenderPipelineSettings
{
    static abstract IRenderPipeline CreatePipeline(IRenderSystem renderSystem);
}

public interface IRenderPipeline : IDisposable
{
    void Render(RenderContext ctx, ReadOnlySpan<RenderRequest> requests);
}
