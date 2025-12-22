using Ghost.Core;
using Ghost.Graphics.Contracts;

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
    /// Creates a new instance of an object that implements the IRenderer interface.
    /// </summary>
    /// <returns>An object that provides rendering functionality through the IRenderer interface.</returns>
    IRenderer CreateRenderer();

    /// <summary>
    /// Removes the specified renderer from the collection of active renderers.
    /// </summary>
    /// <param name="renderer">The renderer instance to remove. Cannot be null.</param>
    void RemoveRenderer(IRenderer renderer);

    /// <summary>
    /// Removes all registered renderers from the collection.
    /// </summary>
    /// <remarks>Call this method to reset the renderer collection to an empty state. After calling this
    ///     method, no renderers will be available until new ones are added.</remarks>
    void ClearRenderers();

    /// <summary>
    /// Creates a new command allocator for the specified command buffer type.
    /// </summary>
    /// <param name="type">The type of command buffer for which to create the allocator. The default is CommandBufferType.Graphics.</param>
    /// <returns>An <see cref="ICommandAllocator"/> instance configured for the specified command buffer type.</returns>
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
