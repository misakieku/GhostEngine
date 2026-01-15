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
            var newPool = new ObjectPool<T>(() => new T(), null);
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
        // FIX: ObjectPool<T>.Rent() has a critical bug that it will put the newly created object into the pool directly and give out the same instance again.
        // This will cause multiple renters to get the same instance.
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
/// Represents a resource in the render graph (texture or buffer).
/// </summary>
internal sealed class RenderGraphResource
{
    public RenderGraphResourceType type;
    public int index;
    public TextureDescriptor textureDescriptor;
    public BufferDescriptor bufferDescriptor;
    public bool isImported;
    public int firstUsePass = -1;
    public int lastUsePass = -1;
    public int producerPass = -1;
    public List<int> consumerPasses = new(4);
    public int refCount;

    public void Reset()
    {
        type = RenderGraphResourceType.Texture;
        index = -1;
        textureDescriptor = default;
        bufferDescriptor = default;
        isImported = false;
        firstUsePass = -1;
        lastUsePass = -1;
        producerPass = -1;
        consumerPasses.Clear();
        refCount = 0;
    }
}

/// <summary>
/// Registry for managing all resources in the render graph.
/// Uses pooling to minimize allocations after the first frame.
/// Uses a single unified list for both textures and buffers with global indexing.
/// </summary>
internal sealed class RenderGraphResourceRegistry
{
    private readonly List<RenderGraphResource> _resources = new(64);
    private readonly RenderGraphObjectPool _pool = new();

    public int ResourceCount => _resources.Count;
    public int TextureResourceCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _resources.Count; i++)
            {
                if (_resources[i].type == RenderGraphResourceType.Texture)
                    count++;
            }
            return count;
        }
    }
    public int BufferResourceCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _resources.Count; i++)
            {
                if (_resources[i].type == RenderGraphResourceType.Buffer)
                    count++;
            }
            return count;
        }
    }

    public void BeginFrame()
    {
        // Return all resources to pool
        for (var i = 0; i < _resources.Count; i++)
        {
            _pool.Return(_resources[i]);
        }
        _resources.Clear();
    }

    public Identifier<RGTexture> ImportTexture(TextureDescriptor descriptor)
    {
        var resource = _pool.Rent<RenderGraphResource>();
        resource.type = RenderGraphResourceType.Texture;
        resource.index = _resources.Count;
        resource.textureDescriptor = descriptor;
        resource.isImported = true;

        _resources.Add(resource);

        return new Identifier<RGTexture>(resource.index);
    }

    public Identifier<RGTexture> CreateTexture(TextureDescriptor descriptor)
    {
        var resource = _pool.Rent<RenderGraphResource>();
        resource.type = RenderGraphResourceType.Texture;
        resource.index = _resources.Count;
        resource.textureDescriptor = descriptor;
        resource.isImported = false;

        _resources.Add(resource);

        return new Identifier<RGTexture>(resource.index);
    }
    
    public Identifier<RGBuffer> ImportBuffer(BufferDescriptor descriptor)
    {
        var resource = _pool.Rent<RenderGraphResource>();
        resource.type = RenderGraphResourceType.Buffer;
        resource.index = _resources.Count;
        resource.bufferDescriptor = descriptor;
        resource.isImported = true;

        _resources.Add(resource);

        return new Identifier<RGBuffer>(resource.index);
    }

    public Identifier<RGBuffer> CreateBuffer(BufferDescriptor descriptor)
    {
        var resource = _pool.Rent<RenderGraphResource>();
        resource.type = RenderGraphResourceType.Buffer;
        resource.index = _resources.Count;
        resource.bufferDescriptor = descriptor;
        resource.isImported = false;

        _resources.Add(resource);

        return new Identifier<RGBuffer>(resource.index);
    }

    public RenderGraphResource GetResource(Identifier<RGResource> resource)
    {
        return _resources[resource.Value];
    }
    
    public RenderGraphResource GetResource(Identifier<RGTexture> texture)
    {
        return _resources[texture.Value];
    }
    
    public RenderGraphResource GetResource(Identifier<RGBuffer> buffer)
    {
        return _resources[buffer.Value];
    }
    
    /// <summary>
    /// Gets resource by global index. Use this when iterating over all resources.
    /// </summary>
    public RenderGraphResource GetResourceByIndex(int index)
    {
        return _resources[index];
    }

    public void SetProducer(Identifier<RGResource> resourceID, int passIndex)
    {
        var resource = GetResource(resourceID);
        resource.producerPass = passIndex;
        if (resource.firstUsePass < 0)
        {
            resource.firstUsePass = passIndex;
        }
    }

    public void AddConsumer(Identifier<RGResource> resourceID, int passIndex)
    {
        var resource = GetResource(resourceID);
        resource.consumerPasses.Add(passIndex);
        resource.lastUsePass = passIndex;
        if (resource.firstUsePass < 0)
        {
            resource.firstUsePass = passIndex;
        }
    }
}
