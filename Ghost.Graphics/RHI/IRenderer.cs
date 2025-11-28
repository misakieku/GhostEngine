using Ghost.Core;
using Misaki.HighPerformance.Mathematics;
using Ghost.Graphics.Core;

namespace Ghost.Graphics.RHI;

/// <summary>
/// High-level renderer interface that uses RHI abstractions
/// </summary>
public interface IRenderer : IDisposable
{
    public uint2 Size
    {
        get;
    }

    /// <summary>
    /// Sets the render Target for this renderer
    /// </summary>
    /// <param name="renderTarget">Render Target to render into</param>
    public void SetRenderTarget(Handle<Texture> renderTarget);

    /// <summary>
    /// Sets the swap chain for this renderer
    /// </summary>
    /// <param name="swapChain">Swap chain for presentation</param>
    public void SetSwapChain(ISwapChain? swapChain);

    /// <summary>
    /// Executes any pending resize operations
    /// </summary>
    public void ExecutePendingResize();

    /// <summary>
    /// Renders a frame
    /// </summary>
    public void Render();

    /// <summary>
    /// Requests a resize operation
    /// </summary>
    /// <param name="newSize">New size</param>
    public void RequestResize(uint2 newSize);

    /// <summary>
    /// Waits for the GPU to complete all work
    /// </summary>
    public void WaitIdle();
}
