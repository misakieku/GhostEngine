using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MaterialAssetDocument))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(float[]))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(uint))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
public partial class MaterialAssetDocumentContext : JsonSerializerContext
{
}

public sealed class MaterialAssetDocument
{
    public const int CURRENT_VERSION = 1;

    public int Version { get; set; } = CURRENT_VERSION;
    public Guid Shader { get; set; } = Guid.Empty;

    public Dictionary<string, JsonElement> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, Guid> Textures { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
