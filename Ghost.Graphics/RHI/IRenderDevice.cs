namespace Ghost.Graphics.RHI;

/// <summary>
/// D3D12-native render device interface for creating graphics resources
/// </summary>
public interface IRenderDevice : IDisposable
{
    /// <summary>
    /// Graphics command queue for rendering operations
    /// </summary>
    ICommandQueue GraphicsQueue
    {
        get;
    }

    /// <summary>
    /// Compute command queue for compute shader operations
    /// </summary>
    ICommandQueue ComputeQueue
    {
        get;
    }

    /// <summary>
    /// Copy command queue for data transfer operations
    /// </summary>
    ICommandQueue CopyQueue
    {
        get;
    }

    /// <summary>
    /// Gets the descriptor allocator for managing descriptors
    /// </summary>
    IDescriptorAllocator DescriptorAllocator
    {
        get;
    }

    /// <summary>
    /// Creates a command buffer for recording rendering commands
    /// </summary>
    /// <param name="type">Type of command buffer to create</param>
    /// <returns>A new command buffer instance</returns>
    ICommandBuffer CreateCommandBuffer(CommandBufferType type = CommandBufferType.Graphics);

    /// <summary>
    /// Creates a swap chain for presentation
    /// </summary>
    /// <param name="desc">Swap chain description</param>
    /// <returns>A new swap chain instance</returns>
    ISwapChain CreateSwapChain(SwapChainDesc desc);
}

/// <summary>
/// Command buffer types matching D3D12 command list types
/// </summary>
public enum CommandBufferType
{
    Graphics,
    Compute,
    Copy
}
