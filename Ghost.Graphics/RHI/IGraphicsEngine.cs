namespace Ghost.Graphics.RHI;

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

    IRenderer CreateRenderer();
    void RemoveRenderer(IRenderer renderer);
    void ClearRenderers();

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

    /// <summary>
    /// Begins a new rendering frame, preparing the graphics context for drawing operations.
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// Renders the current frame.
    /// </summary>
    void RenderFrame();

    /// <summary>
    /// Completes the current rendering frame and performs any necessary finalization steps.
    /// </summary>
    void EndFrame();
}
