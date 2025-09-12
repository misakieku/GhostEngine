namespace Ghost.Graphics.RHI;

public interface IGraphicsEngine : IDisposable
{
    public IRenderDevice Device
    {
        get;
    }

    public IResourceAllocator ResourceAllocator
    {
        get;
    }

    public IRenderer CreateRenderer();

    /// <summary>
    /// Creates a command buffer for recording rendering commands
    /// </summary>
    /// <param name="type">Type of command buffer to create</param>
    /// <returns>A new command buffer instance</returns>
    public ICommandBuffer CreateCommandBuffer(CommandBufferType type = CommandBufferType.Graphics);

    /// <summary>
    /// Creates a swap chain for presentation
    /// </summary>
    /// <param name="desc">Swap chain description</param>
    /// <returns>A new swap chain instance</returns>
    public ISwapChain CreateSwapChain(SwapChainDesc desc);
}