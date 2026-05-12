using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;

namespace Ghost.Graphics;

internal struct ResourceStreamingContext
{
    public AsyncCopyPipeline CopyPipeline
    {
        get;
    }

    public ResourceManager ResourceManager
    {
        get;
    }

    public IResourceDatabase ResourceDatabase
    {
        get;
    }

    public IResourceAllocator ResourceAllocator
    {
        get;
    }

    public ICommandBuffer CommandBuffer
    {
        get; set;
    } = null!;

    internal ResourceStreamingContext(AsyncCopyPipeline copyPipeline, ResourceManager resourceManager, IResourceDatabase resourceDatabase, IResourceAllocator resourceAllocator)
    {
        CopyPipeline = copyPipeline;
        ResourceManager = resourceManager;
        ResourceDatabase = resourceDatabase;
        ResourceAllocator = resourceAllocator;
    }
}

internal interface IResourceStreamingProcessor
{
    void ProcessPendingUploads(ResourceStreamingContext context);
}
