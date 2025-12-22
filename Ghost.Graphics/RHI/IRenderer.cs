using Ghost.Core;
using Ghost.Graphics.Core;

namespace Ghost.Graphics.RHI;

/// <summary>
/// High-level renderer interface that uses RHI abstractions
/// </summary>
public interface IRenderer : IDisposable
{
    Handle<Texture> RenderTarget
    {
        get;
    }

    /// <summary>
    /// Sets the render Target for this renderer
    /// </summary>
    /// <param name="renderTarget">Render Target to render into</param>
    public void SetRenderTarget(Handle<Texture> renderTarget);

    /// <summary>
    /// Renders a frame
    /// </summary>
    /// <param name="commandBuffer">Command buffer to record rendering commands into</param>
    /// <returns>Result of the rendering operation</returns>
    public Result Render(ICommandBuffer commandBuffer);
}
