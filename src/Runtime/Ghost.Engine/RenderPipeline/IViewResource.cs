using Ghost.Graphics.RHI;

namespace Ghost.Engine.RenderPipeline;

public interface IViewResource : IDisposable;

public interface IViewResource<TResource, TDesc> : IViewResource
    where TResource : class, IViewResource<TResource, TDesc>
    where TDesc : struct
{
    static abstract TResource Create(IResourceAllocator allocator, IResourceDatabase database);
    void EnsureResource(in TDesc desc);
}