using Ghost.Core;

namespace Ghost.Graphics.RHI;

public readonly struct RenderContext
{
    public ICommandBuffer CommandBuffer { get; init; }
}

// TODO: We may don't need this anymore. We Use RenderExtractionSystem to extract render data from entities and pass them to IRenderPipeline to render.

/// <summary>
/// High-level renderer interface that uses RHI abstractions
/// </summary>
public interface IRenderer : IDisposable
{
    /// <summary>
    /// Gets or sets the render output target for this renderer.
    /// </summary>
    IRenderOutput? RenderOutput
    {
        get; set;
    }

    /// <summary>
    /// The function that performs the actual rendering operations. Skip rendering if this is null.
    /// </summary>
    Func<RenderContext, Error>? RenderFunc
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
