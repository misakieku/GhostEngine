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
    /// Renders the current frame.
    /// </summary>
    void RenderFrame(ICommandBuffer commandBuffer);
}
