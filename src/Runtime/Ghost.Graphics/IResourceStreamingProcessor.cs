using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Graphics;

internal ref struct ResourceStreamingContext
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
