using Ghost.Core;

namespace Ghost.Graphics.RHI;

public interface IGraphicsEngine : IDisposable
{
    IRenderDevice Device
    {
        get;
    }

    IShaderCompiler ShaderCompiler
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
    /// Creates a new instance of a renderer for drawing graphical content.
    /// </summary>
    /// <returns>An object that implements the IRenderer interface, which can be used to render graphics.</returns>
    IRenderer CreateRenderer();

    /// <summary>
    /// Removes the specified renderer from the collection of active renderers.
    /// </summary>
    /// <param name="renderer">The renderer instance to remove.</param>
    void RemoveRenderer(IRenderer renderer);

    /// <summary>
    /// Removes all registered renderers from the collection.
    /// </summary>
    /// <remarks>Call this method to reset the renderer collection to an empty state. After calling this
    ///     method, no renderers will be available until new ones are added.</remarks>
    void ClearRenderers();

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
    /// Creates a swap chain for presentation
    /// </summary>
    /// <param name="desc">Swap chain description</param>
    /// <returns>A new swap chain instance</returns>
    ISwapChain CreateSwapChain(SwapChainDesc desc);

    /// <summary>
    /// Renders the current frame.
    /// </summary>
    /// <param name="commandAllocator">Command allocator to use for rendering</param>
    /// <returns>Result of the rendering operation</returns>
    Result RenderFrame(ICommandAllocator commandAllocator);
}
