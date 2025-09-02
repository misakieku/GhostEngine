using System.Runtime.CompilerServices;
using Win32.Graphics.D3D12MemoryAllocator;

namespace Ghost.Graphics.Data;

internal readonly struct ResourceHandle : IEquatable<ResourceHandle>, IDisposable
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Allocation GetAllocation()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Cannot get allocation from an invalid AllocationHandle.");
        }

        return GraphicsPipeline.ResourceAllocator.GetAllocation(this);
    }

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

    public void Dispose()
    {
        GraphicsPipeline.ResourceAllocator.ReleaseAllocation(this);
    }

    public static implicit operator Allocation(ResourceHandle handle)
    {
        if (!handle.IsValid)
        {
            throw new InvalidOperationException("Cannot convert an invalid AllocationHandle to Allocation.");
        }

        return handle.GetAllocation();
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

public readonly struct TextureHandle : IEquatable<TextureHandle>, IDisposable
{
    private readonly ResourceHandle _resourceHandle;

    internal ResourceHandle ResourceHandle => _resourceHandle;

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

    public void Dispose()
    {
        _resourceHandle.Dispose();
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

public readonly struct BufferHandle : IEquatable<BufferHandle>, IDisposable
{
    private readonly ResourceHandle _resourceHandle;

    internal ResourceHandle ResourceHandle => _resourceHandle;

    public static BufferHandle Invalid => new(ResourceHandle.Invalid);

    internal BufferHandle(ResourceHandle resourceHandle)
    {
        _resourceHandle = resourceHandle;
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

    public void Dispose()
    {
        _resourceHandle.Dispose();
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