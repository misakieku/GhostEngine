using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Core;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Manifest))]
internal partial class SourceGenerationContext : JsonSerializerContext { }

public enum AssetType
{
    Unknown,
    Texture,
    Mesh,
    Material,
    Shader,
    Scene,
    Audio,
    Video,
    Json,
}

public enum CompressionMethod
{
    None = 0,
    Zstd = 1,
    LZ4 = 2
}

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct AssetHeader()
{
    public const uint MAGIC = 0x47415353; // "SSAG" in little-endian
    public const uint VERSION = 1;

    public uint magic = MAGIC;
    public uint version = VERSION;

    public AssetType assetType;
    public ulong size; // Size of the asset data after the header, in bytes. This is the size of the compressed data if compression is used.
}

[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct TextureContentHeader()
{
    public const uint MAGIC = 0x58455447; // GTEX
    public const uint VERSION = 1;

    public uint magic = MAGIC;
    public uint version = VERSION;

    public uint width;
    public uint height;
    public uint bpc;
    public uint mipLevels;
    public uint dimension; // 1 for 1D, 2 for 2D, 3 for 3D, 4 for Cube, etc. See TextureDimension
    public uint colorComponents;
}

public readonly struct AssetInfo()
{
    public Guid AssetId { get; init; }
    public AssetType AssetType { get; init; }
    public string PackFileName { get; init; } = string.Empty;
    public long Offset { get; init; }
    public long Size { get; init; }
}

public class Manifest
{
    public CompressionMethod CompressionMethod { get; init; } = CompressionMethod.LZ4;
    public Dictionary<string, AssetInfo> Assets { get; init; } = new();

    public void AddAsset(string assetName, AssetInfo location)
    {
        Assets[assetName] = location;
    }

    public async Task SaveToDiskAsync(string path, CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(this, SourceGenerationContext.Default.Manifest), cancellationToken);
    }

    public static async Task<Manifest> LoadFromDiskAsync(string path, CancellationToken cancellationToken = default)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize(json, SourceGenerationContext.Default.Manifest) ?? throw new InvalidOperationException("Failed to deserialize manifest.");
    }
}