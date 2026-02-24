using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Buffer;

namespace Ghost.Graphics.RenderGraphModule;

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
    public string name = string.Empty;

    public int index;
    public RenderGraphResourceType type;
    
    // Resource descriptors (only one is valid based on type)
    public RGTextureDesc rgTextureDesc;
    public BufferDesc bufferDesc;
    
    // Resolved dimensions (computed from rgTextureDesc + ViewState for textures)
    public uint resolvedWidth;
    public uint resolvedHeight;
    
    public bool isImported;
    public int firstUsePass = -1;
    public int lastUsePass = -1;
    public int producerPass = -1;
    public List<int> consumerPasses = new(4);
    public int refCount;

    public Handle<GPUResource> backingResource = Handle<GPUResource>.Invalid;

    public void Reset()
    {
        name = string.Empty;

        type = RenderGraphResourceType.Texture;
        index = -1;
        rgTextureDesc = default;
        bufferDesc = default;
        resolvedWidth = 0;
        resolvedHeight = 0;
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
    private readonly RenderGraphObjectPool _pool;
    private readonly List<RenderGraphResource> _resources;

    internal IReadOnlyList<RenderGraphResource> Resources => _resources;

    public RenderGraphResourceRegistry(RenderGraphObjectPool pool)
    {
        _pool = pool;
        _resources = new List<RenderGraphResource>(64);
    }

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

    public void Clear()
    {
        // Return all resources to pool
        for (var i = 0; i < _resources.Count; i++)
        {
            _pool.Return(_resources[i]);
        }

        _resources.Clear();
    }

    public Identifier<RGTexture> ImportTexture(ref readonly TextureDesc desc, Handle<Texture> texture, string name,
        Color128 clearColor, float clearDepth, byte clearStencil,
        bool clearAtFirstUse, bool discardAtLastUse)
    {
        var resource = _pool.Rent<RenderGraphResource>();
        resource.name = name;
        resource.type = RenderGraphResourceType.Texture;
        resource.index = _resources.Count;
        resource.rgTextureDesc = new RGTextureDesc
        {
            sizeMode = RGTextureSizeMode.Absolute,
            width = desc.Width,
            height = desc.Height,
            format = desc.Format,
            clearColor = clearColor,
            clearDepth = clearDepth,
            clearStencil = clearStencil,
            clearAtFirstUse = clearAtFirstUse,
            discardAtLastUse = discardAtLastUse,
            dimension = desc.Dimension,
            mipLevels = desc.MipLevels,
            slice = desc.Slice,
            usage = desc.Usage
        };
        resource.isImported = true;
        resource.backingResource = texture.AsResource();
        resource.resolvedWidth = desc.Width;
        resource.resolvedHeight = desc.Height;

        _resources.Add(resource);

        return new Identifier<RGTexture>(resource.index);
    }

    public Identifier<RGTexture> CreateTexture(ref readonly RGTextureDesc desc, string name)
    {
        var resource = _pool.Rent<RenderGraphResource>();
        resource.name = name;
        resource.type = RenderGraphResourceType.Texture;
        resource.index = _resources.Count;
        resource.rgTextureDesc = desc;
        resource.isImported = false;

        _resources.Add(resource);

        return new Identifier<RGTexture>(resource.index);
    }
    
    public Identifier<RGBuffer> ImportBuffer(ref readonly BufferDesc desc, Handle<GraphicsBuffer> buffer, string name)
    {
        var resource = _pool.Rent<RenderGraphResource>();
        resource.name = name;
        resource.type = RenderGraphResourceType.Buffer;
        resource.index = _resources.Count;
        resource.bufferDesc = desc;
        resource.isImported = true;
        resource.backingResource = buffer.AsResource();

        _resources.Add(resource);

        return new Identifier<RGBuffer>(resource.index);
    }

    public Identifier<RGBuffer> CreateBuffer(ref readonly BufferDesc desc, string name)
    {
        var resource = _pool.Rent<RenderGraphResource>();
        resource.name= name;
        resource.type = RenderGraphResourceType.Buffer;
        resource.index = _resources.Count;
        resource.bufferDesc = desc;
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
    
    /// <summary>
    /// Resolves texture sizes based on current view state.
    /// Must be called after all resources are created and before compilation.
    /// </summary>
    internal void ResolveTextureSizes(in ViewState viewState)
    {
        for (var i = 0; i < _resources.Count; i++)
        {
            var res = _resources[i];
            if (res.type != RenderGraphResourceType.Texture || res.isImported)
                continue;
            
            var desc = res.rgTextureDesc;
            if (desc.sizeMode == RGTextureSizeMode.Absolute)
            {
                res.resolvedWidth = desc.width;
                res.resolvedHeight = desc.height;
            }
            else // Relative
            {
                res.resolvedWidth = (uint)(desc.scaleX * viewState.viewportWidth);
                res.resolvedHeight = (uint)(desc.scaleY * viewState.viewportHeight);
            }
        }
    }
}
