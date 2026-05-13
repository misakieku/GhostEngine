using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;

namespace Ghost.Graphics;

internal struct ResourceStreamingContext
{
    public AsyncCopyPipeline CopyPipeline
    {
        get; init;
    }

    public ResourceManager ResourceManager
    {
        get; init;
    }

    public IResourceDatabase ResourceDatabase
    {
        get; init;
    }

    public IResourceAllocator ResourceAllocator
    {
        get; init;
    }

    public ICommandBuffer CommandBuffer
    {
        get; init;
    }
}

internal interface IResourceStreamingProcessor
{
    void ProcessPendingUploads(ResourceStreamingContext context);
}
