using Ghost.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.InteropServices;

namespace Ghost.Engine.AssetLoader;

public abstract class Asset : IResourceReleasable
{
    private bool _disposed;

    public Guid ID
    {
        get;
    }

    public abstract AssetType Type
    {
        get;
    }

    protected Asset(Guid id)
    {
        ID = id;
    }

    protected virtual void Release(IResourceDatabase resourceDatabase)
    {
    }

    public void ReleaseResource(IResourceDatabase database)
    {
        if (_disposed)
        {
            return;
        }

        Release(database);

        _disposed = true;
    }
}

public readonly struct AssetReference : IEquatable<AssetReference>
{
    private readonly int _value;

    /// <summary>
    /// The index of the asset in the dependency list.
    /// </summary>
    public int Index
    {
        get => Math.Abs(_value) - 1;
    }

    public static AssetReference Null => default;

    public readonly bool IsInternal => _value >= 0;
    public readonly bool IsExternal => _value < 0;

    public bool Equals(AssetReference other)
    {
        return _value == other._value;
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is AssetReference reference && Equals(reference);
    }

    public static bool operator ==(AssetReference left, AssetReference right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AssetReference left, AssetReference right)
    {
        return !(left == right);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 64)] // Leave extra space for future expansion without breaking compatibility
public struct TextureContentHeader
{
    public uint width;
    public uint height;
    public uint depth;
    public uint mipLevels;
    public uint dimension; // 1 for 1D, 2 for 2D, 3 for 3D
    public uint colorComponents;
}

public class TextureAsset : Asset
{
    private MemoryBlock _textureData;
    private readonly uint _width;
    private readonly uint _height;
    private readonly uint _depth;
    private readonly uint _colorComponents;
    private readonly uint _mipLevels;
    private readonly uint _dimension;

    private Handle<GPUTexture> _textureHandle;

    public override AssetType Type => AssetType.Texture;

    public uint Width => _width;
    public uint Height => _height;
    public uint Depth => _depth;
    public uint MipLevels => _mipLevels;
    public uint Dimension => _dimension;
    public uint ColorComponents => _colorComponents;

    public Handle<GPUTexture> TextureHandle => _textureHandle;

    internal TextureAsset([OwnershipTransfer] ref MemoryBlock data, TextureContentHeader header, Guid id)
        : base(id)
    {
        _textureData = data;
        _width = header.width;
        _height = header.height;
        _depth = header.depth;
        _mipLevels = header.mipLevels;
        _dimension = header.dimension;
        _colorComponents = header.colorComponents;
    }

    internal void SetTextureHandle(Handle<GPUTexture> handle, bool disposeCPUData = true)
    {
        _textureHandle = handle;
        if (disposeCPUData)
        {
            _textureData.Dispose();
        }
    }

    public ReadOnlySpan<T> GeData<T>()
        where T : unmanaged
    {
        return _textureData.AsSpan<T>();
    }

    protected override void Release(IResourceDatabase resourceDatabase)
    {
        _textureData.Dispose();
        resourceDatabase.ReleaseResource(_textureHandle.AsResource());
    }
}