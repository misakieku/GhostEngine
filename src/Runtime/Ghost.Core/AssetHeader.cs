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
/// The layout of the shader asset in binary will be:
/// [ShaderContentHeader]
/// [PassHeader([EntryPointHeader] * entryPointCount)] * passCount
/// [Bytecode]
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct ShaderContentHeader()
{
    public struct PassHeader
    {
        public uint entryPointCount;
    }

    public struct EntryPointHeader
    {
        public ShaderStage stage;
        public long byteCodeOffset; // Offset to the shader bytecode for this entry point in the file
        public long byteCodeSize;
    }

    public const uint MAGIC = 0x52484453; // SHDR
    public const uint VERSION = 1;
    
    public uint magic = MAGIC;
    public uint version = VERSION;

    public ShaderType shaderType;
    public uint passCount;
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