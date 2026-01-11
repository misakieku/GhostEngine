namespace Ghost.RenderGraph.Concept;

public interface IRenderGraphBuilder
{
    RenderGraphTextureHandle ReadTexture(RenderGraphTextureHandle handle);
    RenderGraphTextureHandle WriteTexture(RenderGraphTextureHandle handle);
    RenderGraphTextureHandle UseDepthBuffer(RenderGraphTextureHandle handle, bool writeAccess);
    RenderGraphTextureHandle CreateTexture(TextureDescriptor descriptor);
    RenderGraphBufferHandle ReadBuffer(RenderGraphBufferHandle handle);
    RenderGraphBufferHandle WriteBuffer(RenderGraphBufferHandle handle);
    RenderGraphBufferHandle CreateBuffer(BufferDescriptor descriptor);
}

public ref struct RenderGraphPassBuilder<TPassData>
    where TPassData : class, new()
{
    private readonly RenderGraph _graph;
    private readonly string _passName;
    private readonly int _passIndex;
    private RenderQueueType _queueType;
    private readonly List<(RenderGraphResourceHandle handle, ResourceState state)> _resourceAccesses;
    private Action<TPassData, ICommandBuffer>? _renderFunc;
    private bool _committed;
    private bool _allowCulling;

    public TPassData PassData { get; }

    internal RenderGraphPassBuilder(RenderGraph graph, string passName, int passIndex, List<(RenderGraphResourceHandle handle, ResourceState state)> resourceAccesses)
    {
        _graph = graph;
        _passName = passName;
        _passIndex = passIndex;
        PassData = new TPassData();
        _resourceAccesses = resourceAccesses;
        _queueType = RenderQueueType.Graphics;
        _allowCulling = true;
        _committed = false;
        _renderFunc = null;
    }

    internal IReadOnlyList<(RenderGraphResourceHandle handle, ResourceState state)> ResourceAccesses => _resourceAccesses;
    internal RenderQueueType QueueType => _queueType;
    internal Action<TPassData, ICommandBuffer>? RenderFunc => _renderFunc;
    internal bool AllowCulling => _allowCulling;

    public RenderGraphTextureHandle ReadTexture(RenderGraphTextureHandle handle)
    {
        _resourceAccesses.Add((handle._handle, ResourceState.ShaderResource));
        return handle;
    }

    public RenderGraphTextureHandle WriteTexture(RenderGraphTextureHandle handle)
    {
        _resourceAccesses.Add((handle._handle, ResourceState.RenderTarget));
        return handle;
    }

    public RenderGraphTextureHandle UseDepthBuffer(RenderGraphTextureHandle handle, bool writeAccess)
    {
        _resourceAccesses.Add((handle._handle, writeAccess ? ResourceState.DepthWrite : ResourceState.DepthRead));
        return handle;
    }

    public RenderGraphTextureHandle CreateTexture(TextureDescriptor descriptor)
    {
        var handle = _graph.CreateTransientTexture(descriptor);
        return handle;
    }

    public RenderGraphBufferHandle ReadBuffer(RenderGraphBufferHandle handle)
    {
        _resourceAccesses.Add((handle._handle, ResourceState.ShaderResource));
        return handle;
    }

    public RenderGraphBufferHandle WriteBuffer(RenderGraphBufferHandle handle)
    {
        _resourceAccesses.Add((handle._handle, ResourceState.UnorderedAccess));
        return handle;
    }

    public RenderGraphBufferHandle CreateBuffer(BufferDescriptor descriptor)
    {
        var handle = _graph.CreateTransientBuffer(descriptor);
        return handle;
    }

    public void SetRenderFunc(Action<TPassData, RasterRenderContext> renderFunc)
    {
        _queueType = RenderQueueType.Graphics;
        _renderFunc = (data, cmd) => renderFunc(data, new RasterRenderContext(cmd));
    }

    public void SetComputeFunc(Action<TPassData, ComputeRenderContext> computeFunc, bool asyncCompute = false)
    {
        _queueType = asyncCompute ? RenderQueueType.AsyncCompute : RenderQueueType.Compute;
        _renderFunc = (data, cmd) => computeFunc(data, new ComputeRenderContext(cmd));
    }

    /// <summary>
    /// Controls whether this pass can be culled if it doesn't contribute to the final output.
    /// Set to false for synchronization passes, debug markers, or async compute boundaries.
    /// Default is true.
    /// </summary>
    public void SetAllowCulling(bool allowCulling)
    {
        _allowCulling = allowCulling;
    }


    public void Dispose()
    {
        // Commit the pass when disposed (at end of using block)
        if (!_committed)
        {
            _graph.CommitPass(this, _passName);
            _committed = true;
        }
    }
}
