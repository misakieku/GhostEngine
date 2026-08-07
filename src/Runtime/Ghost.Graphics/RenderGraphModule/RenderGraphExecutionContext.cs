using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Provides the runtime services and frame allocators used to execute a compiled render graph.
/// </summary>
/// <remarks>
/// The context is render-thread-only. It contains runtime ownership services and is never retained by the compilation cache.
/// </remarks>
public readonly struct RenderGraphExecutionContext
{
    /// <summary>
    /// Gets the graphics engine that owns pooled command buffers.
    /// </summary>
    public IGraphicsEngine GraphicsEngine
    {
        get;
    }

    /// <summary>
    /// Gets the frame scheduler that receives ended command buffers.
    /// </summary>
    public IFrameScheduler FrameScheduler
    {
        get;
    }

    /// <summary>
    /// Gets the current frame's Graphics command allocator.
    /// </summary>
    public ICommandAllocator GraphicsCommandAllocator
    {
        get;
    }

    /// <summary>
    /// Gets the current frame's Compute command allocator.
    /// </summary>
    public ICommandAllocator ComputeCommandAllocator
    {
        get;
    }

    /// <summary>
    /// Creates a render-graph execution context for the current frame.
    /// </summary>
    public RenderGraphExecutionContext(
        IGraphicsEngine graphicsEngine,
        IFrameScheduler frameScheduler,
        ICommandAllocator graphicsCommandAllocator,
        ICommandAllocator computeCommandAllocator)
    {
        ArgumentNullException.ThrowIfNull(graphicsEngine);
        ArgumentNullException.ThrowIfNull(frameScheduler);
        ArgumentNullException.ThrowIfNull(graphicsCommandAllocator);
        ArgumentNullException.ThrowIfNull(computeCommandAllocator);

        GraphicsEngine = graphicsEngine;
        FrameScheduler = frameScheduler;
        GraphicsCommandAllocator = graphicsCommandAllocator;
        ComputeCommandAllocator = computeCommandAllocator;
    }
}
