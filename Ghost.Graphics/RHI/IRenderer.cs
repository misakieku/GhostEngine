using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RHI;

/// <summary>
/// High-level renderer interface that uses RHI abstractions
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Sets the render target for this renderer
    /// </summary>
    /// <param name="renderTarget">Render target to render into</param>
    void SetRenderTarget(IRenderTarget? renderTarget);

    /// <summary>
    /// Sets the swap chain for this renderer
    /// </summary>
    /// <param name="swapChain">Swap chain for presentation</param>
    void SetSwapChain(ISwapChain? swapChain);

    /// <summary>
    /// Executes any pending resize operations
    /// </summary>
    void ExecutePendingResize();

    /// <summary>
    /// Renders a frame
    /// </summary>
    void Render();

    /// <summary>
    /// Requests a resize operation
    /// </summary>
    /// <param name="width">New width</param>
    /// <param name="height">New height</param>
    void RequestResize(uint width, uint height);

    /// <summary>
    /// Waits for the GPU to complete all work
    /// </summary>
    void WaitIdle();
}
