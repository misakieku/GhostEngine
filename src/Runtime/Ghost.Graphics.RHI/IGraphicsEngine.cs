using Ghost.Core;

namespace Ghost.Graphics.RHI;

public readonly struct GraphicsEngineDesc
{
    public uint FrameBufferCount
    {
        get; init;
    }
}

public interface IGraphicsEngine : IDisposable
{
    IRenderDevice Device
    {
        get;
    }

    IPipelineLibrary PipelineLibrary
    {
        get;
    }

    IResourceDatabase ResourceDatabase
    {
        get;
    }

    IResourceAllocator ResourceAllocator
    {
        get;
    }

    /// <summary>
    /// Creates a new command allocator for the specified command buffer space.
    /// </summary>
    /// <param name="type">The space of command buffer for which to create the allocator. The default is CommandBufferType.Graphics.</param>
    /// <returns>An <see cref="ICommandAllocator"/> instance configured for the specified command buffer space.</returns>
    ICommandAllocator CreateCommandAllocator(CommandBufferType type = CommandBufferType.Graphics);

    /// <summary>
    /// Creates a command buffer for recording rendering commands
    /// </summary>
    /// <param name="type">Type of command buffer to create</param>
    /// <returns>A new command buffer instance</returns>
    ICommandBuffer CreateCommandBuffer(CommandBufferType type = CommandBufferType.Graphics);

    /// <summary>
    /// Gets a command buffer from the pool for recording rendering commands.
    /// </summary>
    /// <param name="type">Type of command buffer to get from the pool</param>
    /// <returns>A command buffer instance from the pool</returns>
    ICommandBuffer GetPooledCommandBuffer(CommandBufferType type = CommandBufferType.Graphics);

    /// <summary>
    /// Returns a command buffer to the pool after use.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to return to the pool</param>
    void ReturnPooledCommandBuffer(ICommandBuffer commandBuffer);

    /// <summary>
    /// Creates a swap chain for presentation
    /// </summary>
    /// <param name="desc">Swap chain description</param>
    /// <returns>A new swap chain instance</returns>
    ISwapChain CreateSwapChain(SwapChainDesc desc);

    /// <summary>
    /// Begin the current frame.
    /// </summary>
    /// <param name="submittedFrame">Submitted frame value for synchronization</param>
    /// <returns>Result of the begin frame operation</returns>
    void BeginFrame(ulong submittedFrame);

    /// <summary>
    /// End the current frame.
    /// </summary>
    /// <param name="completedFrame">Completed frame value for synchronization</param>
    /// <returns>Result of the end frame operation</returns>
    void EndFrame(ulong completedFrame);
}
