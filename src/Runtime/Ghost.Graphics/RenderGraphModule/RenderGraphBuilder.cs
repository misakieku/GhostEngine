using Ghost.Core;
using Ghost.Graphics.RHI;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderGraphModule;

public delegate void PassRenderFunc<TPassData, TRenderContext>(ref readonly TPassData data, TRenderContext ctx)
    where TPassData : struct
    where TRenderContext : IRenderGraphContext;

[Flags]
public enum AccessFlags : byte
{
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
    Discard = 1 << 2,

    WriteAll = Write | Discard,
    ReadWrite = Read | Write,
}

[Flags]
public enum ResourceExtractionFlags : byte
{
    None = 0,
    /// <summary>
    /// Releases the old heap after extraction.
    /// </summary>
    ReleaseAfterExtract = 1 << 0,
}

public interface IRenderGraphBuilder : IDisposable
{
    /// <summary>
    /// Enables or disables pass culling for the current context.
    /// </summary>
    /// <param name="value">A value indicating whether pass culling is allowed.</param>
    void AllowPassCulling(bool value);

    /// <summary>
    /// Creates a new texture heap based on the specified desc.
    /// </summary>
    /// <param name="desc">A structure that defines the properties and configuration of the texture to create.</param>
    /// <param name="name">The name of the texture heap.</param>
    /// <returns>An identifier for the newly created texture heap.</returns>
    Identifier<RGTexture> CreateTexture(in RGTextureDesc desc, string name);

    /// <summary>
    /// Creates a new buffer heap based on the specified desc.
    /// </summary>
    /// <param name="desc">A structure that defines the properties and configuration of the buffer to create.</param>
    /// <param name="name">The name of the buffer heap.</param>
    /// <returns>An identifier for the newly created buffer heap.</returns>
    Identifier<RGBuffer> CreateBuffer(in BufferDesc desc, string name);

    /// <summary>
    /// Registers the specified texture for use in the current render graph pass with the given access mode.
    /// </summary>
    /// <param name="texture">The identifier of the texture to be used in the render graph pass.</param>
    /// <param name="accessMode">The access mode specifying how the texture will be read or written during the pass.</param>
    /// <returns>An identifier for the texture.</returns>
    Identifier<RGTexture> UseTexture(Identifier<RGTexture> texture, AccessFlags accessMode);

    /// <summary>
    /// Registers the specified buffer for use in the current render graph pass with the given access mode.
    /// </summary>
    /// <param name="buffer">The identifier of the buffer to be used in the render graph pass.</param>
    /// <param name="accessMode">The access mode specifying how the buffer will be read or written during the pass.</param>
    /// <param name="hint">Optional hint about how the buffer will be used.</param>
    /// <returns>An identifier for the buffer.</returns>
    Identifier<RGBuffer> UseBuffer(Identifier<RGBuffer> buffer, AccessFlags accessMode);

    /// <summary>
    /// Extracts the actual texture heap associated with the given identifier for use in outside of the render graph execution context.
    /// </summary>
    /// <param name="src">The identifier of the texture to be extracted.</param>
    /// <param name="dst">A handle to receive the actual GPU texture heap.</param>
    void QueryTextureExtraction(Identifier<RGTexture> src, Handle<GPUTexture> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.ReleaseAfterExtract);

    /// <summary>
    /// Extracts the actual buffer heap associated with the given identifier for use in outside of the render graph execution context.
    /// </summary>
    /// <param name="src">The identifier of the buffer to be extracted.</param>
    /// <param name="dst">A handle to receive the actual GPU buffer heap.</param>
    void QueryBufferExtraction(Identifier<RGBuffer> src, Handle<GPUBuffer> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.ReleaseAfterExtract);

    /// <summary>
    /// Set the data that will be used during rendering for this pass.
    /// </summary>
    /// <param name="passData">The pass data to set.</param>
    /// <param name="addToBlackboard">Add the pass data to blackboard so other passes can access it if true.</param>
    void SetPassData<T>(scoped in T passData, bool addToBlackboard = false)
        where T : struct;
}

public interface IRasterRenderGraphBuilder : IRenderGraphBuilder
{
    /// <summary>
    /// Binds a texture for random access operations within the current rendering pass.
    /// </summary>
    /// <param name="texture">The identifier of the texture to be used for random access.</param>
    /// <returns>An identifier for the texture.</returns>
    Identifier<RGTexture> UseRandomAccessTexture(Identifier<RGTexture> texture);
    /// <summary>
    /// Specifies that the given buffer will be used for random access operations with the specified access mode within the current context.
    /// </summary>
    /// <param name="buffer">An identifier for the buffer to be used for random access. Must reference a valid buffer heap.</param>
    /// <returns>An identifier for the buffer.</returns>
    Identifier<RGBuffer> UseRandomAccessBuffer(Identifier<RGBuffer> buffer);

    /// <summary>
    /// Sets the color attachment at the specified index to the given texture.
    /// </summary>
    /// <param name="texture">The identifier of the texture to use as the color attachment.</param>
    /// <param name="index">The zero-based index of the color attachment to set.</param>
    /// <param name="flags">Access flags. Default is Write (assumes partial update). Use WriteAll for fullscreen passes.</param>
    void SetColorAttachment(Identifier<RGTexture> texture, int index, AccessFlags flags = AccessFlags.Write);

    /// <summary>
    /// Sets the depth attachment for the current render pass using the specified texture.
    /// </summary>
    /// <param name="texture">The identifier of the texture to use as the depth attachment. Cannot be null.</param>
    /// <param name="flags">Access flags. Default is ReadWrite (assumes partial update). Use WriteAll for fullscreen passes.</param>
    void SetDepthAttachment(Identifier<RGTexture> texture, AccessFlags flags = AccessFlags.ReadWrite);

    /// <summary>
    /// Sets the function used to render a pass with the specified pass data and render context.
    /// </summary>
    /// <typeparam name="TPassData">The type of data associated with the render pass.</typeparam>
    /// <param name="renderFunc">The delegate that defines the rendering logic for the pass.</param>
    void SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IRasterRenderContext> renderFunc)
        where TPassData : unmanaged;
}

public interface IComputeRenderGraphBuilder : IRenderGraphBuilder
{
    /// <summary>
    /// Enables or disables asynchronous compute operations.
    /// </summary>
    /// <param name="value">true to enable asynchronous compute; otherwise, false.</param>
    void EnableAsyncCompute(bool value);

    /// <summary>
    /// Sets the render function to be invoked during the compute rendering process.
    /// </summary>
    /// <typeparam name="TPassData">The type of the data object passed to the render function.</typeparam>
    /// <param name="renderFunc">The delegate that defines the rendering logic to execute.</param>
    void SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IComputeRenderContext> renderFunc)
        where TPassData : struct;
}

public interface IUnsafeRenderGraphBuilder : IRenderGraphBuilder
{
    /// <summary>
    /// Binds a texture for random access operations within the current rendering pass.
    /// </summary>
    /// <param name="texture">The identifier of the texture to be used for random access.</param>
    /// <returns>An identifier for the texture.</returns>
    Identifier<RGTexture> UseRandomAccessTexture(Identifier<RGTexture> texture);
    /// <summary>
    /// Specifies that the given buffer will be used for random access operations with the specified access mode within the current context.
    /// </summary>
    /// <param name="buffer">An identifier for the buffer to be used for random access. Must reference a valid buffer heap.</param>
    /// <returns>An identifier for the buffer.</returns>
    Identifier<RGBuffer> UseRandomAccessBuffer(Identifier<RGBuffer> buffer);

    /// <summary>
    /// Sets the function used to render a pass with the specified pass data and render context.
    /// </summary>
    /// <typeparam name="TPassData">The type of data associated with the render pass.</typeparam>
    /// <param name="renderFunc">The delegate that defines the rendering logic for the pass.</param>
    void SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IUnsafeRenderContext> renderFunc)
        where TPassData : struct;
}

internal class RenderGraphBuilder : IRasterRenderGraphBuilder, IComputeRenderGraphBuilder, IUnsafeRenderGraphBuilder
{
    private readonly RenderGraphResourceRegistry _resourceRegistry;
    private readonly RenderGraphBlackboard _blackboard;

    private RenderGraphPass _pass = null!;
    private bool _disposed;

    public RenderGraphBuilder(RenderGraphResourceRegistry resourceRegistry, RenderGraphBlackboard blackboard)
    {
        _resourceRegistry = resourceRegistry;
        _blackboard = blackboard;
    }

    internal void Reset(RenderGraphPass pass)
    {
        _pass = pass;
        _disposed = false;
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private Identifier<RGResource> UseResource(Identifier<RGResource> resource, AccessFlags accessFlags, RenderGraphResourceType type)
    {
        if (accessFlags.HasFlag(AccessFlags.Read))
        {
            _pass.resourceReads[(int)type].Add(resource);
            _resourceRegistry.AddConsumer(resource, _pass.index);
        }

        if (accessFlags.HasFlag(AccessFlags.Write))
        {
            _pass.resourceWrites[(int)type].Add(resource);
            _resourceRegistry.SetProducer(resource, _pass.index);
        }

        return resource;
    }

    public void AllowPassCulling(bool value)
    {
        _pass.allowCulling = value;
    }

    public void EnableAsyncCompute(bool value)
    {
        _pass.asyncCompute = value;
    }

    public Identifier<RGTexture> CreateTexture(in RGTextureDesc desc, string name)
    {
        ThrowIfDisposed();

        var handle = _resourceRegistry.CreateTexture(in desc, name);
        _pass.resourceCreates[(int)RenderGraphResourceType.Texture].Add(handle.AsResource());
        _resourceRegistry.SetProducer(handle.AsResource(), _pass.index);
        return handle;
    }

    public Identifier<RGBuffer> CreateBuffer(in BufferDesc desc, string name)
    {
        ThrowIfDisposed();

        var handle = _resourceRegistry.CreateBuffer(in desc, name);
        _pass.resourceCreates[(int)RenderGraphResourceType.Buffer].Add(handle.AsResource());
        _resourceRegistry.SetProducer(handle.AsResource(), _pass.index);
        return handle;
    }

    public Identifier<RGTexture> UseTexture(Identifier<RGTexture> texture, AccessFlags flags)
    {
        ThrowIfDisposed();
        return UseResource(texture.AsResource(), flags, RenderGraphResourceType.Texture).AsTexture();
    }

    public Identifier<RGBuffer> UseBuffer(Identifier<RGBuffer> buffer, AccessFlags flags)
    {
        ThrowIfDisposed();
        return UseResource(buffer.AsResource(), flags, RenderGraphResourceType.Buffer).AsBuffer();
    }

    // TODO: Implement QueryTextureExtraction and QueryBufferExtraction to allow users to get the actual GPU resources for use outside of the render graph execution context.
    public void QueryTextureExtraction(Identifier<RGTexture> src, Handle<GPUTexture> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.ReleaseAfterExtract)
    {
        throw new NotImplementedException();
    }

    public void QueryBufferExtraction(Identifier<RGBuffer> src, Handle<GPUBuffer> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.ReleaseAfterExtract)
    {
        throw new NotImplementedException();
    }

    public Identifier<RGTexture> UseRandomAccessTexture(Identifier<RGTexture> texture)
    {
        ThrowIfDisposed();

        var resource = texture.AsResource();
        UseResource(resource, AccessFlags.ReadWrite, RenderGraphResourceType.Texture);
        _pass.randomAccess.Add(resource);
        return texture;
    }

    public Identifier<RGBuffer> UseRandomAccessBuffer(Identifier<RGBuffer> buffer)
    {
        ThrowIfDisposed();

        var resource = buffer.AsResource();
        UseResource(resource, AccessFlags.ReadWrite, RenderGraphResourceType.Buffer);
        _pass.randomAccess.Add(resource);
        return buffer;
    }

    public void SetColorAttachment(Identifier<RGTexture> texture, int index, AccessFlags flags = AccessFlags.Write)
    {
        ThrowIfDisposed();

        Logger.DebugAssert(index >= 0 && index < _pass.colorAccess.Length, "Color attachment index out of range.");

        var id = UseTexture(texture, flags);
        if (_pass.colorAccess[index].id == id || _pass.colorAccess[index].id.IsInvalid)
        {
            _pass.maxColorIndex = Math.Max(_pass.maxColorIndex, index);
            var usage = new ResourceBarrierData(BarrierLayout.RenderTarget, BarrierAccess.RenderTarget, BarrierSync.RenderTarget);
            _pass.colorAccess[index] = new TextureAccess(id, flags, usage);
        }
        else
        {
            throw new InvalidOperationException($"Color attachment at index {index} is already set to a different texture.");
        }
    }

    public void SetDepthAttachment(Identifier<RGTexture> texture, AccessFlags flags = AccessFlags.ReadWrite)
    {
        ThrowIfDisposed();

        var id = UseTexture(texture, flags);
        if (_pass.depthAccess.id == id || _pass.depthAccess.id.IsInvalid)
        {
            var layout = flags.HasFlag(AccessFlags.Write) ? BarrierLayout.DepthStencilWrite : BarrierLayout.DepthStencilRead;
            var access = flags.HasFlag(AccessFlags.Write) ? BarrierAccess.DepthStencilWrite : BarrierAccess.DepthStencilRead;
            var sync = BarrierSync.DepthStencil;
            var usage = new ResourceBarrierData(layout, access, sync);
            _pass.depthAccess = new TextureAccess(id, flags, usage);
        }
        else
        {
            throw new InvalidOperationException("Depth attachment is already set to a different texture.");
        }
    }

    public void SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IRasterRenderContext> renderFunc)
        where TPassData : unmanaged
    {
        ((RasterRenderGraphPass<TPassData>)_pass).renderFunc = renderFunc;
    }

    public void SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IComputeRenderContext> renderFunc)
        where TPassData : struct
    {
        ((ComputeRenderGraphPass<TPassData>)_pass).renderFunc = renderFunc;
    }

    public void SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IUnsafeRenderContext> renderFunc)
        where TPassData : struct
    {
        ((UnsafeRenderGraphPass<TPassData>)_pass).renderFunc = renderFunc;
    }

    public void SetPassData<T>(scoped in T passData, bool addToBlackboard = false)
        where T : struct
    {
        if (_pass is not RenderGraphPass<T> typedPass)
        {
            throw new ArgumentException("Pass type and pass data type missmatch.");
        }

        typedPass.SetPassData(passData);

        if (addToBlackboard)
        {
            _blackboard.Add<RenderGraphPass<T>, T>(typedPass);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (!_pass.HasRenderFunc())
        {
            throw new InvalidOperationException("RenderGraphBuilder must be disposed after setting up the render function.");
        }

        if (_pass.type == RenderPassType.Raster && _pass.colorAccess[0].id.IsInvalid && _pass.depthAccess.id.IsInvalid)
        {
            throw new InvalidOperationException("Raster render pass must have at least one color or depth attachment.");
        }

        _pass = null!;

        _disposed = true;
    }
}
