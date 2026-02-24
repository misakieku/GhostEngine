using Ghost.Core;

namespace Ghost.Graphics.RHI;

public interface IRenderOutput
{
    ViewportDesc Viewport
    {
        get; set;
    }

    RectDesc Scissor
    {
        get; set;
    }

    /// <summary>
    /// Gets a handle to the current render target texture.
    /// </summary>
    /// <returns>A handle to the texture that is currently set as the render target.</returns>
    Handle<Texture> GetRenderTarget();

    /// <summary>
    /// Begins a rendering operation using the specified command buffer. Typically this will include resource barriers,
    /// </summary>
    /// <param name="cmd">The command buffer that records rendering commands.</param>
    /// 
    void BeginRender(ICommandBuffer cmd);
    /// <summary>
    /// Finalizes the rendering process using the specified command buffer.
    /// </summary>
    /// <param name="cmd">The command buffer that contains the rendering commands to be finalized.</param>
    void EndRender(ICommandBuffer cmd);

    /// <summary>
    /// Displays the current frame to the output device or screen.
    /// </summary>
    /// <remarks>Call this method after rendering operations to present the rendered content. The exact
    /// behavior may depend on the underlying graphics implementation or device.</remarks>
    void Present();
}
