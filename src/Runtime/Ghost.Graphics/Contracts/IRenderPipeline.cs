using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Contracts;

public interface IRenderPipeline
{
    void Render(RenderContext ctx, ReadOnlySpan<Camera> cameras);
}
