using System.Runtime.InteropServices;
using System.Text;
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
    EntityPrefab
}

public enum CompressionMethod
{
    None = 0,
    Zstd = 1,
    LZ4 = 2
}

public enum TextureDimension : uint
{
    Unknown = unchecked((uint)-1),
    None = 0,
    Texture1D = 1,
    Texture2D = 2,
    Texture3D = 3,
    TextureCube = 4,
    Texture2DArray = 5,
    TextureCubeArray = 6
}

public enum ShaderType : uint
{
    Graphics = 0,
    Compute = 1,
}

public enum ShaderStage : uint
{
    AmplificationShader,
    MeshShader,
    PixelShader,
    ComputeShader,
    Library // For ray tracing shaders or work graph shaders that don't fit into the traditional shader stages
}

public readonly struct AssetInfo()
{
    public Guid AssetId { get; init; }
    public AssetType AssetType { get; init; }
    public string PackFileName { get; init; } = string.Empty;
    public long Offset { get; init; }
    public long Size { get; init; }
    public long UncompressedSize { get; init; }
}

/// <summary>
/// The header for a texture asset, containing metadata about the texture's properties.
/// </summary>
/// <remarks>
/// The layout of the texture asset in binary will be:
/// [TextureContentHeader]
/// [TextureData]
/// </remarks>
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

    public TextureDimension dimension;
    public uint colorComponents;
}

/// <summary>
/// The header for a shader asset, containing metadata about the shader's properties and its passes.
/// </summary>
/// <remarks>
/// The layout of the shader asset in binary (v3) will be:
/// [ShaderContentHeader]
/// For each pass:
///   [PassHeader]
///   [VariantEntry * variantCount]
///   For each variant:
///     [EntryPointHeader * entryPointCount]
///     [Bytecode]
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct ShaderContentHeader()
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BindingRecord
    {
        public ulong interfaceId;
        public ulong implementationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VariantEntry
    {
        public ulong variantKey; // 64-bit composition key
        public ulong programContentHash;
        public long dataOffset;  // Offset relative to the start of the asset
        public long dataSize;
        public uint bindingCount;
        public uint reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PassHeader
    {
        public ulong passId;
        public uint entryPointCount;
        public uint variantCount;
        public uint isTemplateShared;
        public ulong templatePassId;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EntryPointHeader
    {
        public ShaderStage stage;
        public long byteCodeOffset; // Offset to the shader bytecode for this entry point, relative to the start of the variant data
        public long byteCodeSize;
    }

    public const uint MAGIC = 0x52484453; // SHDR
    public const uint VERSION = 3;

    public uint magic = MAGIC;
    public uint version = VERSION;

    public ShaderType shaderType;
    public uint passCount;
    public ulong shaderId;
    public ulong schemaId;
    public uint propertyBufferSize;
    public uint reserved;
}

/// <summary>
/// The header written at offset 0 of every cache file produced by the asset baker.
/// </summary>
/// <remarks>
/// The layout of the cache file in binary will be:
/// [CacheFileHeader]
/// [Baker payload (e.g. TextureContentHeader + TextureData)]
///
/// The <see cref="bakerVersion"/> is a stable FNV-1a hash of the baker class name and
/// settings type name, so cache files produced by an older baker version or a different
/// settings type are detected and force a rebake instead of silently producing corrupt packs.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct CacheFileHeader()
{
    public const uint MAGIC = 0x46435347; // "GSCF" — little-endian byte order, matching GTEX/SHDR
    public const int SIZE = 16;

    public uint magic = MAGIC;
    public uint bakerVersion;
    public ulong reserved;

    /// <summary>
    /// Computes a stable content-format version for a baker/settings type pair.
    /// </summary>
    /// <param name="bakerType">The concrete baker implementation type.</param>
    /// <param name="settingsType">The concrete bake-settings type consumed by the baker.</param>
    /// <returns>
    /// A 32-bit FNV-1a hash over the UTF-8 bytes of <c>bakerType.FullName + ":" + settingsType.FullName</c>.
    /// </returns>
    public static uint ComputeBakerVersion(Type bakerType, Type settingsType)
    {
        return Fnv1a($"{bakerType.FullName}:{settingsType.FullName}");
    }

    /// <summary>
    /// Computes a stable 32-bit FNV-1a hash over the UTF-8 bytes of <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="string.GetHashCode"/> (which is randomized per process), this is
    /// deterministic across runs and frameworks, making it safe to persist to disk.
    /// </remarks>
    public static uint Fnv1a(string value)
    {
        const uint OFFSET_BASIS = 2166136261;
        const uint PRIME = 16777619;

        var hash = OFFSET_BASIS;
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= PRIME;
        }

        return hash;
    }

    /// <summary>
    /// Writes this header to the given stream at its current position.
    /// </summary>
    public void WriteTo(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[SIZE];
        MemoryMarshal.Write(bytes, ref this);
        stream.Write(bytes);
    }

    /// <summary>
    /// Attempts to read a <see cref="CacheFileHeader"/> from the given stream at its current position.
    /// </summary>
    /// <returns><c>true</c> when a full header was read; <c>false</c> at end-of-stream or on a short read.</returns>
    public static bool TryReadFrom(Stream stream, out CacheFileHeader header)
    {
        Span<byte> bytes = stackalloc byte[SIZE];
        var totalRead = 0;
        while (totalRead < SIZE)
        {
            var read = stream.Read(bytes[totalRead..]);
            if (read == 0)
            {
                header = default;
                return false;
            }
            totalRead += read;
        }

        header = MemoryMarshal.Read<CacheFileHeader>(bytes);
        return true;
    }
}

/// <summary>
/// The header written at offset 0 of every pack file.
/// </summary>
/// <remarks>
/// Layout:
/// [4] Magic "GSPK"
/// [4] Format version
/// [8] Reserved
/// [N] Packed asset payloads — manifest offsets are absolute stream positions that include this header.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 16)]
public struct PackFileHeader()
{
    public const uint MAGIC = 0x4B505347; // "GSPK" — little-endian byte order, matching GTEX/SHDR
    public const uint VERSION = 1;
    public const int SIZE = 16;

    public uint magic = MAGIC;
    public uint version = VERSION;
    public ulong reserved;

    /// <summary>
    /// Writes this header to the given stream at its current position.
    /// </summary>
    public void WriteTo(Stream stream)
    {
        Span<byte> bytes = stackalloc byte[SIZE];
        MemoryMarshal.Write(bytes, ref this);
        stream.Write(bytes);
    }

    /// <summary>
    /// Attempts to read a <see cref="PackFileHeader"/> from the given stream at its current position.
    /// </summary>
    /// <returns><c>true</c> when a full header was read; <c>false</c> at end-of-stream or on a short read.</returns>
    public static bool TryReadFrom(Stream stream, out PackFileHeader header)
    {
        Span<byte> bytes = stackalloc byte[SIZE];
        var totalRead = 0;
        while (totalRead < SIZE)
        {
            var read = stream.Read(bytes[totalRead..]);
            if (read == 0)
            {
                header = default;
                return false;
            }
            totalRead += read;
        }

        header = MemoryMarshal.Read<PackFileHeader>(bytes);
        return true;
    }
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