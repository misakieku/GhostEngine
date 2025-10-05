namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Context passed to render pass execution functions
/// </summary>
public readonly struct RenderPassContext
{
    internal readonly int PassIndex;
    // TODO: Add command list and other rendering context when available

    internal RenderPassContext(int passIndex)
    {
        PassIndex = passIndex;
    }
}

/// <summary>
/// Builder for configuring render pass resource dependencies
/// </summary>
public sealed class RenderPassBuilder
{
    private readonly RenderGraph _renderGraph;
    private readonly int _passIndex;
    private readonly List<ResourceAccess> _resourceAccesses;

    internal RenderPassBuilder(RenderGraph renderGraph, int passIndex)
    {
        _renderGraph = renderGraph;
        _passIndex = passIndex;
        _resourceAccesses = new List<ResourceAccess>();
    }

    internal IReadOnlyList<ResourceAccess> ResourceAccesses => _resourceAccesses;

    /// <summary>
    /// Declare a texture read dependency
    /// </summary>
    public RGTextureHandle ReadTexture(RGTextureHandle handle)
    {
        if (!handle.IsValid)
            throw new ArgumentException("Invalid texture handle", nameof(handle));

        _resourceAccesses.Add(new ResourceAccess(handle._resourceId, ResourceAccessType.Read, _passIndex));
        _renderGraph.UpdateResourceLifetime(handle._resourceId, _passIndex);
        return handle;
    }

    /// <summary>
    /// Declare a texture write dependency
    /// </summary>
    public RGTextureHandle WriteTexture(RGTextureHandle handle)
    {
        if (!handle.IsValid)
            throw new ArgumentException("Invalid texture handle", nameof(handle));

        _resourceAccesses.Add(new ResourceAccess(handle._resourceId, ResourceAccessType.Write, _passIndex));
        _renderGraph.UpdateResourceLifetime(handle._resourceId, _passIndex);
        return handle;
    }

    /// <summary>
    /// Declare a texture read-write dependency
    /// </summary>
    public RGTextureHandle ReadWriteTexture(RGTextureHandle handle)
    {
        if (!handle.IsValid)
            throw new ArgumentException("Invalid texture handle", nameof(handle));

        _resourceAccesses.Add(new ResourceAccess(handle._resourceId, ResourceAccessType.ReadWrite, _passIndex));
        _renderGraph.UpdateResourceLifetime(handle._resourceId, _passIndex);
        return handle;
    }

    /// <summary>
    /// Declare a buffer read dependency
    /// </summary>
    public RGBufferHandle ReadBuffer(RGBufferHandle handle)
    {
        if (!handle.IsValid)
            throw new ArgumentException("Invalid buffer handle", nameof(handle));

        _resourceAccesses.Add(new ResourceAccess(handle._resourceId, ResourceAccessType.Read, _passIndex));
        _renderGraph.UpdateResourceLifetime(handle._resourceId, _passIndex);
        return handle;
    }

    /// <summary>
    /// Declare a buffer write dependency
    /// </summary>
    public RGBufferHandle WriteBuffer(RGBufferHandle handle)
    {
        if (!handle.IsValid)
            throw new ArgumentException("Invalid buffer handle", nameof(handle));

        _resourceAccesses.Add(new ResourceAccess(handle._resourceId, ResourceAccessType.Write, _passIndex));
        _renderGraph.UpdateResourceLifetime(handle._resourceId, _passIndex);
        return handle;
    }

    /// <summary>
    /// Declare a buffer read-write dependency
    /// </summary>
    public RGBufferHandle ReadWriteBuffer(RGBufferHandle handle)
    {
        if (!handle.IsValid)
            throw new ArgumentException("Invalid buffer handle", nameof(handle));

        _resourceAccesses.Add(new ResourceAccess(handle._resourceId, ResourceAccessType.ReadWrite, _passIndex));
        _renderGraph.UpdateResourceLifetime(handle._resourceId, _passIndex);
        return handle;
    }

    /// <summary>
    /// Create a new transient texture within this pass
    /// </summary>
    public RGTextureHandle CreateTexture(string name, TextureDescription description)
    {
        return _renderGraph.CreateTexture(name, ResourceLifetime.Transient, description);
    }

    /// <summary>
    /// Create a new transient buffer within this pass
    /// </summary>
    public RGBufferHandle CreateBuffer(string name, BufferDescription description)
    {
        return _renderGraph.CreateBuffer(name, ResourceLifetime.Transient, description);
    }
}
