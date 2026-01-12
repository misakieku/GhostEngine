using Ghost.Core;
using Misaki.HighPerformance.Buffer;

namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Object pool for reusing allocated objects across frames.
/// This is key to minimizing GC allocations after the first frame.
/// </summary>
internal sealed class RenderGraphObjectPool
{
    private static readonly List<SharedObjectPoolBase> s_allocatedPools = new();

    private class SharedObjectPoolBase
    {
        public SharedObjectPoolBase() { }
        public virtual void Clear() { }
    }

    private class SharedObjectPool<T> : SharedObjectPoolBase where T : class, new()
    {
        private static readonly ObjectPool<T> s_pool = AllocatePool();

        private static ObjectPool<T> AllocatePool()
        {
            var newPool = new ObjectPool<T>(() => new T());
            // Storing instance to clear the static pool of the same type if needed
            s_allocatedPools.Add(new SharedObjectPool<T>());
            return newPool;
        }

        /// <summary>
        /// Clear the pool using SharedObjectPool instance.
        /// </summary>
        /// <returns></returns>
        public override void Clear()
        {
            s_pool.Reset();
        }

        /// <summary>
        /// Rent a new instance from the pool.
        /// </summary>
        /// <returns></returns>
        public static T Rent() => s_pool.Rent();

        /// <summary>
        /// Return an object to the pool.
        /// </summary>
        /// <param name="toRelease">instance to release.</param>
        public static void Return(T toRelease) => s_pool.Return(toRelease);
    }

    public T Rent<T>()
        where T : class, new()
    {
        return SharedObjectPool<T>.Rent();
    }

    public void Return<T>(T obj)
        where T : class, new()
    {
        SharedObjectPool<T>.Return(obj);
    }

    public void Clear()
    {
        for (var i = 0; i < s_allocatedPools.Count; i++)
        {
            s_allocatedPools[i].Clear();
        }
    }
}

/// <summary>
/// Represents a texture resource in the render graph.
/// </summary>
internal sealed class RenderGraphResource
{
    public RenderGraphResourceType type;
    public int Index;
    public TextureDescriptor Descriptor;
    public bool IsImported;
    public int FirstUsePass = -1;
    public int LastUsePass = -1;
    public int ProducerPass = -1;
    public List<int> ConsumerPasses = new(4);
    public int RefCount;

    public void Reset()
    {
        Index = -1;
        Descriptor = default;
        IsImported = false;
        FirstUsePass = -1;
        LastUsePass = -1;
        ProducerPass = -1;
        ConsumerPasses.Clear();
        RefCount = 0;
    }
}

/// <summary>
/// Registry for managing all resources in the render graph.
/// Uses pooling to minimize allocations after the first frame.
/// </summary>
internal sealed class RenderGraphResourceRegistry
{
    private readonly List<RenderGraphResource> _resources = new(64);
    private readonly RenderGraphObjectPool _pool = new();
    private int _textureResourceCount;

    public int TextureResourceCount => _textureResourceCount;

    public void BeginFrame()
    {
        // Don't clear the lists, just reset the count
        // This avoids reallocating the backing arrays
        _textureResourceCount = 0;
    }

    public Identifier<RGTexture> ImportTexture(TextureDescriptor descriptor)
    {
        var resource = GetOrCreateTextureResource();
        resource.Index = _textureResourceCount - 1;
        resource.Descriptor = descriptor;
        resource.IsImported = true;

        return new Identifier<RGTexture>(resource.Index);
    }

    public Identifier<RGTexture> CreateTexture(TextureDescriptor descriptor)
    {
        var resource = GetOrCreateTextureResource();
        resource.Index = _textureResourceCount - 1;
        resource.Descriptor = descriptor;
        resource.IsImported = false;

        return new Identifier<RGTexture>(resource.Index);
    }

    public RenderGraphResource GetResource(Identifier<RGResource> resource)
    {
        if (resource.Value < 0 || resource.Value >= _textureResourceCount)
            throw new ArgumentException($"Invalid texture handle: {resource}");

        return _resources[resource.Value];
    }

    public RenderGraphResource GetTextureResourceByIndex(int index)
    {
        if (index < 0 || index >= _textureResourceCount)
            throw new ArgumentException($"Invalid texture index: {index}");

        return _resources[index];
    }

    public void SetProducer(Identifier<RGResource> resourceID, int passIndex)
    {
        var resource = GetResource(resourceID);
        resource.ProducerPass = passIndex;
        if (resource.FirstUsePass < 0)
        {
            resource.FirstUsePass = passIndex;
        }
    }

    public void AddConsumer(Identifier<RGResource> resourceID, int passIndex)
    {
        var resource = GetResource(resourceID);
        resource.ConsumerPasses.Add(passIndex);
        resource.LastUsePass = passIndex;
        if (resource.FirstUsePass < 0)
        {
            resource.FirstUsePass = passIndex;
        }
    }

    private RenderGraphResource GetOrCreateTextureResource()
    {
        RenderGraphResource resource;
        if (_textureResourceCount < _resources.Count)
        {
            // Reuse existing slot
            resource = _resources[_textureResourceCount];
            resource.Reset();
        }
        else
        {
            // Need to grow the list
            resource = _pool.Rent<RenderGraphResource>();
            resource.Reset();
            _resources.Add(resource);
        }

        _textureResourceCount++;
        return resource;
    }

    public void Clear()
    {
        for (var i = 0; i < _resources.Count; i++)
        {
            _pool.Return(_resources[i]);
        }
        _resources.Clear();
        _textureResourceCount = 0;
    }
}
