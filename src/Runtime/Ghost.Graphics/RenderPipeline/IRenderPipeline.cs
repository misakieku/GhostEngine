using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RenderPipeline;

public readonly struct RenderContext
{
    public ICommandBuffer CommandBuffer { get; init; }
    public ICommandQueue GraphicsQueue { get; init; }
    public ICommandQueue ComputeQueue { get; init; }
    public ICommandQueue CopyQueue { get; init; }
}

public interface IRenderPipelineSettings
{
    IRenderPipeline CreatePipeline(RenderSystem renderSystem);
}

public interface IRenderPipeline : IDisposable
{
    void Render(RenderContext ctx, ReadOnlySpan<RenderRequest> requests);
}
