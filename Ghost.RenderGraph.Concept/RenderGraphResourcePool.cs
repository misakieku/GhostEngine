namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Object pool for reusing allocated objects across frames.
/// This is key to minimizing GC allocations after the first frame.
/// </summary>
internal sealed class RenderGraphObjectPool
{
    private readonly Dictionary<Type, Stack<object>> _pools = new();

    public T Get<T>() where T : class, new()
    {
        var type = typeof(T);
        if (_pools.TryGetValue(type, out var pool) && pool.Count > 0)
        {
            return (T)pool.Pop();
        }
        return new T();
    }

    public void Release<T>(T obj) where T : class
    {
        if (obj == null) return;

        var type = typeof(T);
        if (!_pools.TryGetValue(type, out var pool))
        {
            pool = new Stack<object>(16);
            _pools[type] = pool;
        }
        pool.Push(obj);
    }

    public void Clear()
    {
        _pools.Clear();
    }
}

/// <summary>
/// Represents a texture resource in the render graph.
/// </summary>
internal sealed class TextureResource
{
    public int Index;
    public int Version;
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
        Version = 0;
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
    private readonly List<TextureResource> _textureResources = new(64);
    private readonly RenderGraphObjectPool _pool = new();
    private int _textureResourceCount;

    public int TextureResourceCount => _textureResourceCount;

    public void BeginFrame()
    {
        // Don't clear the lists, just reset the count
        // This avoids reallocating the backing arrays
        _textureResourceCount = 0;
    }

    public RenderGraphTextureHandle ImportTexture(TextureDescriptor descriptor)
    {
        var resource = GetOrCreateTextureResource();
        resource.Index = _textureResourceCount - 1;
        resource.Version = 0;
        resource.Descriptor = descriptor;
        resource.IsImported = true;

        return new RenderGraphTextureHandle(resource.Index, resource.Version, descriptor.Name);
    }

    public RenderGraphTextureHandle CreateTexture(TextureDescriptor descriptor)
    {
        var resource = GetOrCreateTextureResource();
        resource.Index = _textureResourceCount - 1;
        resource.Version = 0;
        resource.Descriptor = descriptor;
        resource.IsImported = false;

        return new RenderGraphTextureHandle(resource.Index, resource.Version, descriptor.Name);
    }

    public TextureResource GetTextureResource(RenderGraphTextureHandle handle)
    {
        if (handle.Index < 0 || handle.Index >= _textureResourceCount)
            throw new ArgumentException($"Invalid texture handle: {handle.Index}");

        return _textureResources[handle.Index];
    }

    public TextureResource GetTextureResourceByIndex(int index)
    {
        if (index < 0 || index >= _textureResourceCount)
            throw new ArgumentException($"Invalid texture index: {index}");

        return _textureResources[index];
    }

    public void SetProducer(RenderGraphTextureHandle handle, int passIndex)
    {
        var resource = GetTextureResource(handle);
        resource.ProducerPass = passIndex;
        if (resource.FirstUsePass < 0)
            resource.FirstUsePass = passIndex;
    }

    public void AddConsumer(RenderGraphTextureHandle handle, int passIndex)
    {
        var resource = GetTextureResource(handle);
        resource.ConsumerPasses.Add(passIndex);
        resource.LastUsePass = passIndex;
        if (resource.FirstUsePass < 0)
            resource.FirstUsePass = passIndex;
    }

    private TextureResource GetOrCreateTextureResource()
    {
        TextureResource resource;
        if (_textureResourceCount < _textureResources.Count)
        {
            // Reuse existing slot
            resource = _textureResources[_textureResourceCount];
            resource.Reset();
        }
        else
        {
            // Need to grow the list
            resource = _pool.Get<TextureResource>();
            resource.Reset();
            _textureResources.Add(resource);
        }

        _textureResourceCount++;
        return resource;
    }

    public void Clear()
    {
        for (int i = 0; i < _textureResources.Count; i++)
        {
            _pool.Release(_textureResources[i]);
        }
        _textureResources.Clear();
        _textureResourceCount = 0;
    }
}
