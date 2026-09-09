using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;

namespace Ghost.Graphics;

internal struct ResourceStreamingContext
{
    public required IFrameScheduler FrameScheduler
    {
        get; init;
    }

    public required IGraphicsEngine GraphicsEngine
    {
        get; init;
    }

    public required ICommandAllocator CopyCommandAllocator
    {
        get; init;
    }

    public ICommandBuffer CopyCommandBuffer
    {
        get; set;
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

    public ShaderLibrary ShaderLibrary
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
    void ProcessPendingShaderCommits(ResourceStreamingContext context);
    void ProcessPendingUploads(ResourceStreamingContext context);
}
