using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;

namespace Ghost.Graphics;

internal readonly struct ResourceStreamingContext
{
    public required AsyncCopyPipeline CopyPipeline
    {
        get; init;
    }

    public required ResourceManager ResourceManager
    {
        get; init;
    }

    public required IResourceDatabase ResourceDatabase
    {
        get; init;
    }

    public required IResourceAllocator ResourceAllocator
    {
        get; init;
    }

    public required ICommandBuffer GraphicsCommandBuffer
    {
        get; init;
    }
}

internal interface IResourceStreamingProcessor
{
    void ProcessPendingUploads(ResourceStreamingContext context);
}
