using Ghost.Core;
using System.Diagnostics;

namespace Ghost.RenderGraph.Concept;

[Flags]
public enum AccessFlags
{
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
    ReadWrite = Read | Write,
}

public interface IRenderGraphBuilder : IDisposable
{
    /// <summary>
    /// Enables or disables pass culling for the current context.
    /// </summary>
    /// <param name="value">A value indicating whether pass culling is allowed.</param>
    void AllowPassCulling(bool value);

    /// <summary>
    /// Creates a new texture resource based on the specified descriptor.
    /// </summary>
    /// <param name="descriptor">A structure that defines the properties and configuration of the texture to create.</param>
    /// <returns>An identifier for the newly created texture resource.</returns>
    Identifier<RGTexture> CreateTexture(in TextureDescriptor descriptor);

    /// <summary>
    /// Registers the specified texture for use in the current render graph pass with the given access mode.
    /// </summary>
    /// <param name="texture">The identifier of the texture to be used in the render graph pass.</param>
    /// <param name="accessMode">The access mode specifying how the texture will be read or written during the pass.</param>
    /// <returns>An identifier for the texture.</returns>
    Identifier<RGTexture> UseTexture(Identifier<RGTexture> texture, AccessFlags accessMode);
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
    /// <param name="buffer">An identifier for the buffer to be used for random access. Must reference a valid buffer resource.</param>
    /// <returns>An identifier for the buffer.</returns>
    Identifier<RGBuffer> UseRandomAccessBuffer(Identifier<RGBuffer> buffer);

    /// <summary>
    /// Sets the color attachment at the specified index to the given texture.
    /// </summary>
    /// <param name="texture">The identifier of the texture to use as the color attachment.</param>
    /// <param name="index">The zero-based index of the color attachment to set.</param>
    void SetColorAttachment(Identifier<RGTexture> texture, int index);

    /// <summary>
    /// Sets the depth attachment for the current render pass using the specified texture.
    /// </summary>
    /// <param name="texture">The identifier of the texture to use as the depth attachment. Cannot be null.</param>
    void SetDepthAttachment(Identifier<RGTexture> texture);

    /// <summary>
    /// Sets the function used to render a pass with the specified pass data and render context.
    /// </summary>
    /// <typeparam name="TPassData">The type of data associated with the render pass.</typeparam>
    /// <param name="renderFunc">The delegate that defines the rendering logic for the pass.</param>
    void SetRenderFunc<TPassData>(Action<TPassData, RasterRenderContext> renderFunc)
        where TPassData : class, new();
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
    void SetRenderFunc<TPassData>(Action<TPassData, ComputeRenderContext> renderFunc)
        where TPassData : class, new();
}

internal class RenderGraphBuilder : IRasterRenderGraphBuilder, IComputeRenderGraphBuilder
{
    private RenderGraph _graph = null!;
    private RenderGraphPassBase _pass = null!;
    private RenderGraphResourceRegistry _resources = null!;
    private bool _disposed;

    internal void Init(RenderGraph graph, RenderGraphPassBase pass, RenderGraphResourceRegistry resources)
    {
        _graph = graph;
        _pass = pass;
        _resources = resources;
        _disposed = false;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private Identifier<RGResource> UseResource(Identifier<RGResource> resource, AccessFlags accessFlags)
    {
        if (accessFlags.HasFlag(AccessFlags.Read))
        {
            _pass.resourceReads.Add(resource);
            _resources.AddConsumer(resource, _pass.index);
        }

        if (accessFlags.HasFlag(AccessFlags.Write))
        {
            _pass.resourceWrites.Add(resource);
            _resources.SetProducer(resource, _pass.index);
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

    public Identifier<RGTexture> CreateTexture(in TextureDescriptor descriptor)
    {
        ThrowIfDisposed();

        var handle = _resources.CreateTexture(descriptor);
        _pass.resourceCreates.Add(handle.AsResource());
        _resources.SetProducer(handle.AsResource(), _pass.index);
        return handle;
    }

    public Identifier<RGTexture> UseTexture(Identifier<RGTexture> texture, AccessFlags flags)
    {
        ThrowIfDisposed();

        return UseResource(texture.AsResource(), flags).AsTexture();
    }

    public Identifier<RGTexture> UseRandomAccessTexture(Identifier<RGTexture> texture)
    {
        ThrowIfDisposed();

        var resource = texture.AsResource();
        UseResource(resource, AccessFlags.ReadWrite);
        _pass.randomAccess.Add(resource);
        return texture;
    }

    public Identifier<RGBuffer> UseRandomAccessBuffer(Identifier<RGBuffer> buffer)
    {
        ThrowIfDisposed();

        var resource = buffer.AsResource();
        UseResource(resource, AccessFlags.ReadWrite);
        _pass.randomAccess.Add(resource);
        return buffer;
    }

    public void SetColorAttachment(Identifier<RGTexture> texture, int index)
    {
        ThrowIfDisposed();

        Debug.Assert(index >= 0 && index < _pass.colorAccess.Length, "Color attachment index out of range.");

        var id = UseTexture(texture, AccessFlags.Write);
        if (_pass.colorAccess[index].id == id || _pass.colorAccess[index].id.IsInvalid)
        {
            _pass.maxColorIndex = Math.Max(_pass.maxColorIndex, index);
            _pass.colorAccess[index] = new TextureAccess(id, AccessFlags.Write);
        }
        else
        {
            throw new InvalidOperationException($"Color attachment at index {index} is already set to a different texture.");
        }
    }

    public void SetDepthAttachment(Identifier<RGTexture> texture)
    {
        ThrowIfDisposed();

        var id = UseTexture(texture, AccessFlags.Write);
        if (_pass.depthAccess.id == id || _pass.depthAccess.id.IsInvalid)
        {
            _pass.depthAccess = new TextureAccess(id, AccessFlags.Write);
        }
        else
        {
            throw new InvalidOperationException("Depth attachment is already set to a different texture.");
        }
    }

    public void SetRenderFunc<TPassData>(Action<TPassData, RasterRenderContext> renderFunc)
        where TPassData : class, new()
    {
        ((RasterRenderGraphPass<TPassData>)_pass).renderFunc = renderFunc;
    }

    public void SetRenderFunc<TPassData>(Action<TPassData, ComputeRenderContext> renderFunc)
        where TPassData : class, new()
    {
        ((ComputeRenderGraphPass<TPassData>)_pass).renderFunc = renderFunc;
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

        _graph = null!;
        _pass = null!;
        _resources = null!;

        _disposed = true;
    }
}
