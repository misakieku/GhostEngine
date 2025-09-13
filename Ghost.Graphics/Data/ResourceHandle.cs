using System.Runtime.CompilerServices;
using Win32.Graphics.D3D12MemoryAllocator;

namespace Ghost.Graphics.Data;

public readonly struct ResourceHandle : IEquatable<ResourceHandle>
{
    private const int _INVALID_ID = -1;

    public readonly int id;
    public readonly uint generation;

    public static ResourceHandle Invalid => new(-1, 0);

    internal ResourceHandle(int id, uint generation)
    {
        this.id = id;
        this.generation = generation;
    }

    public bool IsValid => id != _INVALID_ID && generation >= 0;

    public bool Equals(ResourceHandle other)
    {
        return id == other.id && generation == other.generation;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (id * 397) ^ (int)generation;
        }
    }

    public override bool Equals(object? obj)
    {
        return obj is ResourceHandle handle && Equals(handle);
    }

    public static bool operator ==(ResourceHandle left, ResourceHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ResourceHandle left, ResourceHandle right)
    {
        return !(left == right);
    }
}

public readonly struct TextureHandle : IEquatable<TextureHandle>
{
    private readonly ResourceHandle _resourceHandle;

    public ResourceHandle ResourceHandle => _resourceHandle;
    public static TextureHandle Invalid => new(ResourceHandle.Invalid);

    internal TextureHandle(ResourceHandle resourceHandle)
    {
        _resourceHandle = resourceHandle;
    }

    public bool IsValid => _resourceHandle.IsValid;

    public bool Equals(TextureHandle other)
    {
        return _resourceHandle.Equals(other._resourceHandle);
    }

    public override bool Equals(object? obj)
    {
        return obj is TextureHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _resourceHandle.GetHashCode();
    }

    public static bool operator ==(TextureHandle left, TextureHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextureHandle left, TextureHandle right)
    {
        return !(left == right);
    }
}

public readonly struct BufferHandle : IEquatable<BufferHandle>
{
    private readonly ResourceHandle _resourceHandle;
    private readonly BindlessDescriptor _bindlessDescriptor;

    public static BufferHandle Invalid => new(ResourceHandle.Invalid);

    public ResourceHandle ResourceHandle => _resourceHandle;
    public BindlessDescriptor BindlessDescriptor => _bindlessDescriptor;

    internal BufferHandle(ResourceHandle resourceHandle)
    {
        _resourceHandle = resourceHandle;
        _bindlessDescriptor = BindlessDescriptor.Invalid;
    }

    internal BufferHandle(ResourceHandle resourceHandle, BindlessDescriptor descriptor)
    {
        _resourceHandle = resourceHandle;
        _bindlessDescriptor = descriptor;
    }

    public bool IsValid => _resourceHandle.IsValid;

    public bool Equals(BufferHandle other)
    {
        return _resourceHandle.Equals(other._resourceHandle);
    }

    public override bool Equals(object? obj)
    {
        return obj is BufferHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _resourceHandle.GetHashCode();
    }

    public static bool operator ==(BufferHandle left, BufferHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BufferHandle left, BufferHandle right)
    {
        return !(left == right);
    }
}