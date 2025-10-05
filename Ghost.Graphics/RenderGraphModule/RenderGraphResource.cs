using Ghost.Core;
using Ghost.Graphics.Data;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.RenderGraphModule;

/// <summary>
/// Represents different resource access types in the render graph
/// </summary>
public enum ResourceAccessType
{
    Read,
    Write,
    ReadWrite
}

/// <summary>
/// Resource lifetime within the render graph
/// </summary>
public enum ResourceLifetime
{
    /// <summary>
    /// Resource is created and destroyed within a single frame
    /// </summary>
    Transient,
    /// <summary>
    /// Resource is imported from external source (e.g., history buffers, backbuffer)
    /// </summary>
    External,
    /// <summary>
    /// Resource persists across multiple frames (exported for next frame)
    /// </summary>
    Persistent
}

/// <summary>
/// Base class for render graph resources
/// </summary>
public abstract class RenderGraphResource
{
    public int FirstPassIndex
    {
        get; set;
    }

    internal int LastPassIndex
    {
        get; set;
    }

    public int Id
    {
        get;
    }

    public string Name
    {
        get; set;
    }

    public ResourceLifetime Lifetime
    {
        get; internal set;
    }

    public bool IsImported
    {
        get; init;
    }

    public bool IsCreated
    {
        get; protected set;
    }

    protected RenderGraphResource(int id, string name, ResourceLifetime lifetime)
    {
        Id = id;
        Name = name;
        Lifetime = lifetime;
        FirstPassIndex = -1;
        LastPassIndex = -1;
    }

    internal abstract void CreateResource();
    internal abstract void ReleaseResource();
}

/// <summary>
/// Represents a texture resource in the render graph
/// </summary>
public sealed class RenderGraphTexture : RenderGraphResource
{
    internal Handle<Texture> Handle
    {
        get; set;
    }

    internal TextureDescription Description
    {
        get; private set;
    }

    public RenderGraphTexture(int id, string name, ResourceLifetime lifetime, TextureDescription description)
        : base(id, name, lifetime)
    {
        Description = description;
    }

    public RenderGraphTexture(int id, string name, Handle<Texture> handle, TextureDescription description)
        : base(id, name, ResourceLifetime.External)
    {
        Handle = handle;
        Description = description;
        IsImported = true;
        IsCreated = true;
    }

    internal override void CreateResource()
    {
        if (IsCreated || IsImported)
            return;

        var allocFlags = Lifetime == ResourceLifetime.Transient
            ? Win32.Graphics.D3D12MemoryAllocator.AllocationFlags.CanAlias
            : Win32.Graphics.D3D12MemoryAllocator.AllocationFlags.None;

        Handle = GraphicsPipeline.ResourceAllocator.CreateTexture2D(
            Description.Width,
            Description.Height,
            Description.MipLevels,
            Description.Format,
            Description.Flags,
            allocFlags,
            Description.InitialState);

        IsCreated = true;
    }

    internal override void ReleaseResource()
    {
        if (!IsCreated || IsImported)
            return;

        Handle.Dispose();
        IsCreated = false;
    }
}

/// <summary>
/// Represents a buffer resource in the render graph
/// </summary>
public sealed class RenderGraphBuffer : RenderGraphResource
{
    internal Handle<GraphicsBuffer> Handle
    {
        get; set;
    }

    internal BufferDescription Description
    {
        get; private set;
    }

    public RenderGraphBuffer(int id, string name, ResourceLifetime lifetime, BufferDescription description)
        : base(id, name, lifetime)
    {
        Description = description;
    }
    public RenderGraphBuffer(int id, string name, Handle<GraphicsBuffer> handle, BufferDescription description)
    : base(id, name, ResourceLifetime.External)
    {
        Handle = handle;
        Description = description;
        IsImported = true;
        IsCreated = true;
    }

    internal override void CreateResource()
    {
        if (IsCreated || IsImported)
            return;

        var allocFlags = Lifetime == ResourceLifetime.Transient
            ? Win32.Graphics.D3D12MemoryAllocator.AllocationFlags.CanAlias
            : Win32.Graphics.D3D12MemoryAllocator.AllocationFlags.None;

        Handle = GraphicsPipeline.ResourceAllocator.CreateBuffer(
            Description.SizeInBytes,
            Description.HeapType,
            Description.Flags,
            allocFlags,
            Description.InitialState);

        IsCreated = true;
    }

    internal override void ReleaseResource()
    {
        if (!IsCreated || IsImported)
            return;

        Handle.Dispose();
        IsCreated = false;
    }
}

/// <summary>
/// Texture description for render graph texture creation
/// </summary>
public readonly struct TextureDescription
{
    public readonly uint Width
    {
        get;
    }

    public readonly uint Height
    {
        get;
    }

    public readonly ushort MipLevels
    {
        get;
    }

    public readonly Format Format
    {
        get;
    }

    public readonly ResourceFlags Flags
    {
        get;
    }

    public readonly ResourceStates InitialState
    {
        get;
    }

    public TextureDescription(uint width, uint height, ushort mipLevels = 1,
        Format format = Format.R8G8B8A8Unorm, ResourceFlags flags = ResourceFlags.None,
        ResourceStates initialState = ResourceStates.Common)
    {
        Width = width;
        Height = height;
        MipLevels = mipLevels;
        Format = format;
        Flags = flags;
        InitialState = initialState;
    }
}

/// <summary>
/// Buffer description for render graph buffer creation
/// </summary>
public readonly struct BufferDescription
{
    public readonly uint SizeInBytes
    {
        get;
    }

    public readonly HeapType HeapType
    {
        get;
    }

    public readonly ResourceFlags Flags
    {
        get;
    }

    public readonly ResourceStates InitialState
    {
        get;
    }

    public BufferDescription(uint sizeInBytes, HeapType heapType = HeapType.Default,
        ResourceFlags flags = ResourceFlags.None, ResourceStates initialState = ResourceStates.Common)
    {
        SizeInBytes = sizeInBytes;
        HeapType = heapType;
        Flags = flags;
        InitialState = initialState;
    }
}
