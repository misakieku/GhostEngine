using Ghost.Graphics.RHI;

namespace Ghost.Graphics;

public interface IRenderLoopCommand : IDisposable
{
    void Execute(scoped in RenderContext context, ICommandBuffer commandBuffer);
}
