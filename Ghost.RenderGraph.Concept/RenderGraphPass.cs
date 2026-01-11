namespace Ghost.RenderGraph.Concept;

/// <summary>
/// Represents different types of render passes.
/// </summary>
public enum RenderPassType : byte
{
    Raster,
    Compute
}

/// <summary>
/// Base class for render passes.
/// Uses pooling to avoid allocations after the first frame.
/// </summary>
internal abstract class RenderGraphPassBase
{
    public string Name = string.Empty;
    public int Index;
    public RenderPassType Type;
    public bool AllowCulling = true;
    public bool AsyncCompute;
    
    // Resource dependencies
    public readonly List<RenderGraphTextureHandle> TextureReads = new(8);
    public readonly List<RenderGraphTextureHandle> TextureWrites = new(4);
    public readonly List<RenderGraphTextureHandle> TextureCreates = new(4);
    
    // Execution state
    public bool Culled;
    public bool HasSideEffects;

    public abstract void Execute(RenderContext context);
    public abstract void Clear();

    public virtual void Reset()
    {
        Name = string.Empty;
        Index = -1;
        Type = RenderPassType.Raster;
        AllowCulling = true;
        AsyncCompute = false;
        TextureReads.Clear();
        TextureWrites.Clear();
        TextureCreates.Clear();
        Culled = false;
        HasSideEffects = false;
    }
}

/// <summary>
/// Typed render pass with user data.
/// </summary>
internal sealed class RenderGraphPass<TPassData> : RenderGraphPassBase
    where TPassData : class, new()
{
    public TPassData? PassData;
    public Action<TPassData, RasterRenderContext>? RasterRenderFunc;
    public Action<TPassData, ComputeRenderContext>? ComputeRenderFunc;

    public override void Execute(RenderContext context)
    {
        if (PassData == null)
            return;

        if (Type == RenderPassType.Raster && RasterRenderFunc != null)
        {
            RasterRenderFunc(PassData, context.RasterContext);
        }
        else if (Type == RenderPassType.Compute && ComputeRenderFunc != null)
        {
            ComputeRenderFunc(PassData, context.ComputeContext);
        }
    }

    public override void Clear()
    {
        PassData = null;
        RasterRenderFunc = null;
        ComputeRenderFunc = null;
    }

    public override void Reset()
    {
        base.Reset();
        Clear();
    }
}

/// <summary>
/// Builder for constructing render passes.
/// Implements IDisposable for using() pattern.
/// </summary>
public sealed class RenderGraphBuilder : IDisposable
{
    private RenderGraphPassBase? _pass;
    private RenderGraphResourceRegistry? _resources;
    private bool _disposed;

    internal void Initialize(RenderGraphPassBase pass, RenderGraphResourceRegistry resources)
    {
        _pass = pass;
        _resources = resources;
        _disposed = false;
    }

    /// <summary>
    /// Creates a new transient texture that only lives for this pass.
    /// </summary>
    public RenderGraphTextureHandle CreateTexture(TextureDescriptor descriptor)
    {
        ThrowIfDisposed();
        var handle = _resources!.CreateTexture(descriptor);
        _pass!.TextureCreates.Add(handle);
        _resources.SetProducer(handle, _pass.Index);
        return handle;
    }

    /// <summary>
    /// Marks a texture as being read by this pass.
    /// </summary>
    public RenderGraphTextureHandle ReadTexture(RenderGraphTextureHandle handle)
    {
        ThrowIfDisposed();
        _pass!.TextureReads.Add(handle);
        _resources!.AddConsumer(handle, _pass.Index);
        return handle;
    }

    /// <summary>
    /// Marks a texture as being written by this pass.
    /// </summary>
    public RenderGraphTextureHandle WriteTexture(RenderGraphTextureHandle handle)
    {
        ThrowIfDisposed();
        _pass!.TextureWrites.Add(handle);
        _resources!.SetProducer(handle, _pass.Index);
        return handle;
    }

    /// <summary>
    /// Sets up a depth buffer for this pass.
    /// </summary>
    public RenderGraphTextureHandle UseDepthBuffer(RenderGraphTextureHandle handle, bool writeAccess)
    {
        ThrowIfDisposed();
        if (writeAccess)
        {
            _pass!.TextureWrites.Add(handle);
            _resources!.SetProducer(handle, _pass.Index);
        }
        else
        {
            _pass!.TextureReads.Add(handle);
            _resources!.AddConsumer(handle, _pass.Index);
        }
        return handle;
    }

    /// <summary>
    /// Sets the render function for a raster pass.
    /// </summary>
    public void SetRenderFunc<TPassData>(Action<TPassData, RasterRenderContext> renderFunc)
        where TPassData : class, new()
    {
        ThrowIfDisposed();
        
        if (_pass is RenderGraphPass<TPassData> typedPass)
        {
            typedPass.RasterRenderFunc = renderFunc;
            typedPass.Type = RenderPassType.Raster;
        }
    }

    /// <summary>
    /// Sets the compute function for a compute pass.
    /// </summary>
    public void SetComputeFunc<TPassData>(Action<TPassData, ComputeRenderContext> computeFunc, bool asyncCompute = false)
        where TPassData : class, new()
    {
        ThrowIfDisposed();
        
        if (_pass is RenderGraphPass<TPassData> typedPass)
        {
            typedPass.ComputeRenderFunc = computeFunc;
            typedPass.Type = RenderPassType.Compute;
            typedPass.AsyncCompute = asyncCompute;
        }
    }

    /// <summary>
    /// Controls whether this pass can be culled if its outputs are unused.
    /// </summary>
    public void SetAllowCulling(bool allow)
    {
        ThrowIfDisposed();
        _pass!.AllowCulling = allow;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _pass = null;
            _resources = null;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed || _pass == null)
            throw new ObjectDisposedException(nameof(RenderGraphBuilder));
    }
}
