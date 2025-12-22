using Ghost.Core;
using Ghost.Graphics.Contracts;

namespace Ghost.Graphics.RHI;

/// <summary>
/// High-level renderer interface that uses RHI abstractions
/// </summary>
public interface IRenderer : IDisposable
{
    IRenderTargetStrategy? RenderTargetStrategy
    {
        get; set;
    }

    /// <summary>
    /// Renders a frame
    /// </summary>
    /// <param name="commandAllocator">Command allocator to use for rendering</param>
    /// <returns>Result of the rendering operation</returns>
    Result Render(ICommandAllocator commandAllocator);
}