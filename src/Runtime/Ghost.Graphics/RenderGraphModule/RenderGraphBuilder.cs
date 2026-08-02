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
    /// Releases the old resource after extraction.
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
    Identifier<RGTexture> CreateTexture(scoped in RGTextureDesc desc, string? name = null);

    /// <summary>
    /// Creates a new buffer heap based on the specified desc.
    /// </summary>
    /// <param name="desc">A structure that defines the properties and configuration of the buffer to create.</param>
    /// <param name="name">The name of the buffer heap.</param>
    /// <returns>An identifier for the newly created buffer heap.</returns>
    Identifier<RGBuffer> CreateBuffer(scoped in BufferDesc desc, string? name = null);

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
    /// <param name="src">The identifier of the render graph texture to be extracted.</param>
    /// <param name="dst">The handle to receive the actual GPU texture.</param>
    /// <param name="flags">Flags that control the extraction behavior.</param>
    void QueueTextureExtraction(Identifier<RGTexture> src, Handle<GPUTexture> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.None);

    /// <summary>
    /// Extracts the actual buffer heap associated with the given identifier for use in outside of the render graph execution context.
    /// </summary>
    /// <param name="src">The identifier of the render graph buffer to be extracted.</param>
    /// <param name="dst">The handle to receive the actual GPU buffer.</param>
    /// <param name="flags">Flags that control the extraction behavior.</param>
    void QueueBufferExtraction(Identifier<RGBuffer> src, Handle<GPUBuffer> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.None);

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
        where TPassData : struct;
}

public interface IComputeRenderGraphBuilder : IRenderGraphBuilder
{
    /// <summary>
    /// Marks the compute pass as eligible for asynchronous compute scheduling.
    /// </summary>
    /// <remarks>
    /// This is a scheduling hint, not a guarantee of compute-queue execution. Until dependency-aware queue batching is implemented, eligible passes execute on the graphics/direct queue.
    /// </remarks>
    /// <param name="value"><see langword="true"/> to request asynchronous compute eligibility; otherwise, <see langword="false"/>.</param>
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
    /// Declares that a texture will be used as a render target by the unsafe pass.
    /// </summary>
    /// <param name="texture">The texture used as a render target.</param>
    /// <param name="flags">The access performed by the pass.</param>
    /// <returns>The declared texture.</returns>
    Identifier<RGTexture> UseRenderTargetTexture(Identifier<RGTexture> texture, AccessFlags flags = AccessFlags.Write);

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
#if GHOST_SAFETY_CHECKS
    private bool _faulted;
#endif

    public RenderGraphBuilder(RenderGraphResourceRegistry resourceRegistry, RenderGraphBlackboard blackboard)
    {
        _resourceRegistry = resourceRegistry;
        _blackboard = blackboard;
    }

    internal void Reset(RenderGraphPass pass)
    {
        _pass = pass;
        _disposed = false;
#if GHOST_SAFETY_CHECKS
        _faulted = false;
#endif
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    [Conditional("GHOST_SAFETY_CHECKS")]
    private void Reject(string error)
    {
#if GHOST_SAFETY_CHECKS
        _faulted = true;
        throw new InvalidOperationException(error);
#endif
    }

    [Conditional("GHOST_SAFETY_CHECKS")]
    private void ValidateDeclaration(Identifier<RGResource> resource, PassResourceUsageClass usageClass)
    {
#if GHOST_SAFETY_CHECKS
        var error = RenderGraphValidator.ValidateDeclaration(_pass, resource, usageClass, _resourceRegistry);
        if (error is not null)
        {
            Reject(error);
        }
#endif
    }

    private void CompleteDispose()
    {
        _pass = null!;
        _disposed = true;
    }

    private Identifier<RGResource> UseResource(Identifier<RGResource> resource, AccessFlags accessFlags, RGResourceType type)
    {
        if (accessFlags.HasFlag(AccessFlags.Read) && _pass.resourceReads[(int)type].Add(resource))
        {
            _resourceRegistry.AddConsumer(resource, _pass.index);
        }

        if (accessFlags.HasFlag(AccessFlags.Write) && _pass.resourceWrites[(int)type].Add(resource))
        {
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

    public Identifier<RGTexture> CreateTexture(scoped in RGTextureDesc desc, string? name = null)
    {
        ThrowIfDisposed();

        var handle = _resourceRegistry.CreateTexture(in desc, name);
        _pass.resourceCreates[(int)RGResourceType.Texture].Add(handle.AsResource());
        _resourceRegistry.SetProducer(handle.AsResource(), _pass.index);
        return handle;
    }

    public Identifier<RGBuffer> CreateBuffer(scoped in BufferDesc desc, string? name = null)
    {
        ThrowIfDisposed();

        var handle = _resourceRegistry.CreateBuffer(in desc, name);
        _pass.resourceCreates[(int)RGResourceType.Buffer].Add(handle.AsResource());
        _resourceRegistry.SetProducer(handle.AsResource(), _pass.index);
        return handle;
    }

    public Identifier<RGTexture> UseTexture(Identifier<RGTexture> texture, AccessFlags flags)
    {
        ThrowIfDisposed();
        return UseResource(texture.AsResource(), flags, RGResourceType.Texture).AsTexture();
    }

    public Identifier<RGBuffer> UseBuffer(Identifier<RGBuffer> buffer, AccessFlags flags)
    {
        ThrowIfDisposed();
        return UseResource(buffer.AsResource(), flags, RGResourceType.Buffer).AsBuffer();
    }

    public void QueueTextureExtraction(Identifier<RGTexture> src, Handle<GPUTexture> dst, ResourceExtractionFlags flags)
    {
        ref var resource = ref _resourceRegistry.GetResource(src);
        resource.isExtracted = true;
        resource.extractionTarget = dst.AsResource();
        resource.extractionFlags = flags;

        UseResource(src.AsResource(), AccessFlags.Read, RGResourceType.Texture);
    }

    public void QueueBufferExtraction(Identifier<RGBuffer> src, Handle<GPUBuffer> dst, ResourceExtractionFlags flags)
    {
        ref var resource = ref _resourceRegistry.GetResource(src);
        resource.isExtracted = true;
        resource.extractionTarget = dst.AsResource();
        resource.extractionFlags = flags;

        UseResource(src.AsResource(), AccessFlags.Read, RGResourceType.Buffer);
    }

    public Identifier<RGTexture> UseRandomAccessTexture(Identifier<RGTexture> texture)
    {
        ThrowIfDisposed();

        var resource = texture.AsResource();
        ValidateDeclaration(resource, PassResourceUsageClass.UnorderedAccess);
        UseResource(resource, AccessFlags.ReadWrite, RGResourceType.Texture);
        _pass.randomAccess.Add(resource);
        return texture;
    }

    public Identifier<RGBuffer> UseRandomAccessBuffer(Identifier<RGBuffer> buffer)
    {
        ThrowIfDisposed();

        var resource = buffer.AsResource();
        ValidateDeclaration(resource, PassResourceUsageClass.UnorderedAccess);
        UseResource(resource, AccessFlags.ReadWrite, RGResourceType.Buffer);
        _pass.randomAccess.Add(resource);
        return buffer;
    }

    public Identifier<RGTexture> UseRenderTargetTexture(Identifier<RGTexture> texture, AccessFlags flags = AccessFlags.Write)
    {
        ThrowIfDisposed();

        var resource = texture.AsResource();
        ValidateDeclaration(resource, PassResourceUsageClass.ColorAttachment);
        UseResource(resource, flags, RGResourceType.Texture);
        _pass.renderTargetWrites.Add(resource);
        return texture;
    }

    public void SetColorAttachment(Identifier<RGTexture> texture, int index, AccessFlags flags = AccessFlags.Write)
    {
        ThrowIfDisposed();

        Logger.DebugAssert(index >= 0 && index < RHIUtility.MAX_RENDER_TARGETS, "Color attachment index out of range.");

        var existingAttachment = _pass.colorAccess[index].id;
        if (existingAttachment.IsValid && existingAttachment != texture)
        {
            Reject($"Color attachment at index {index} is already set to a different texture.");
        }

        var resource = texture.AsResource();
        ValidateDeclaration(resource, PassResourceUsageClass.ColorAttachment);
        UseResource(resource, flags, RGResourceType.Texture);
        _pass.maxColorIndex = Math.Max(_pass.maxColorIndex, index);
        var usage = new ResourceBarrierData(BarrierLayout.RenderTarget, BarrierAccess.RenderTarget, BarrierSync.RenderTarget);
        _pass.colorAccess[index] = new TextureAccess(texture, flags, usage);
    }

    public void SetDepthAttachment(Identifier<RGTexture> texture, AccessFlags flags = AccessFlags.ReadWrite)
    {
        ThrowIfDisposed();

        if (_pass.depthAccess.id.IsValid && _pass.depthAccess.id != texture)
        {
            Reject("Depth attachment is already set to a different texture.");
        }

        var layout = flags.HasFlag(AccessFlags.Write) ? BarrierLayout.DepthStencilWrite : BarrierLayout.DepthStencilRead;
        var usageClass = layout == BarrierLayout.DepthStencilWrite ? PassResourceUsageClass.DepthWrite : PassResourceUsageClass.DepthRead;
        var resource = texture.AsResource();
        ValidateDeclaration(resource, usageClass);
        UseResource(resource, flags, RGResourceType.Texture);
        var access = flags.HasFlag(AccessFlags.Write) ? BarrierAccess.DepthStencilWrite : BarrierAccess.DepthStencilRead;
        var sync = BarrierSync.DepthStencil;
        var usage = new ResourceBarrierData(layout, access, sync);
        _pass.depthAccess = new TextureAccess(texture, flags, usage);
    }

    public void SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IRasterRenderContext> renderFunc)
        where TPassData : struct
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
            throw new ArgumentException("Pass type and pass data type mismatch.");
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

#if GHOST_SAFETY_CHECKS
        if (_faulted)
        {
            CompleteDispose();
            return;
        }

        var error = RenderGraphValidator.ValidatePass(_pass, _resourceRegistry);
#endif
        CompleteDispose();
#if GHOST_SAFETY_CHECKS
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }
#endif
    }
}
