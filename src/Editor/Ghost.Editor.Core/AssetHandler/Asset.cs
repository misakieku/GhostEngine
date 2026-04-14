using Ghost.Editor.Core.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.AssetHandler;

public abstract class Asset
{
    public Guid ID
    {
        get;
    }

    public abstract Guid TypeID
    {
        get;
    }

    public Guid[] Dependencies
    {
        get;
    }

    public IAssetSettings? Settings
    {
        get;
    }

    protected Asset(Guid id, Guid[] dependencies, IAssetSettings? settings)
    {
        ID = id;
        Dependencies = dependencies;
        Settings = settings;
    }

    public virtual ValueTask RefreshAsync(IAssetRegistry db, CancellationToken token = default)
    {
        return ValueTask.CompletedTask;
    }
}

// Do not change the order of the fields in this struct, as it is used for binary serialization/deserialization.
[StructLayout(LayoutKind.Sequential, Size = SIZE)]
internal struct AssetMetadata
{
    public const int CURRENT_FORMAT_VERSION = 1;
    public const int SIZE = 128; // Fixed size for metadata header. We choose 128 bytes to allow future expansion without breaking compatibility.

    public AssetMetadata(Guid id, Guid typeID)
    {
        FormatVersion = CURRENT_FORMAT_VERSION;
        ID = id;
        TypeID = typeID;
    }

    public int FormatVersion
    {
        get;
    }

    public Guid ID
    {
        get;
    }

    public Guid TypeID
    {
        get;
    }

    public int HandlerVersion
    {
        get; set;
    }

    public int DependencyCount
    {
        get; set;
    }

    public long DependenciesOffset
    {
        get; set;
    }

    public long SettingsOffset
    {
        get; set;
    }

    public long SettingsSize
    {
        get; set;
    }

    public long ContentOffset
    {
        get; set;
    }

    public long ContentSize
    {
        get; set;
    }

    public static void WriteToStream(Stream stream, scoped ref readonly AssetMetadata metadata)
    {
        var buffer = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in metadata, 1));
        stream.Write(buffer);
    }

    public static AssetMetadata ReadFromStream(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[SIZE];
        stream.ReadExactly(buffer);
        return Unsafe.ReadUnaligned<AssetMetadata>(ref MemoryMarshal.GetReference(buffer));
    }
}

[StructLayout(LayoutKind.Sequential, Size = SIZE)]
public readonly struct DependencyInfo
{
    public const int SIZE = 16;

    public Guid ID
    {
        get; init;
    }

    public readonly ReadOnlySpan<byte> AsBytes()
    {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in this, 1));
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
