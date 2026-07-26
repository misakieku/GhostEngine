using Ghost.Core;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(SubAssetManifest))]
internal partial class SubAssetManifestContext : JsonSerializerContext { }

public class SubAssetManifest
{
    public List<SubAssetRecord> SubAssets { get; set; } = new();

    public record SubAssetRecord(string SubPath, AssetType Type);

    public void Save(string path)
    {
        using var fs = new System.IO.FileStream(path, System.IO.FileMode.Create);
        JsonSerializer.Serialize(fs, this, SubAssetManifestContext.Default.SubAssetManifest);
    }

    public static SubAssetManifest? Load(string path)
    {
        if (!System.IO.File.Exists(path)) return null;
        using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
        return JsonSerializer.Deserialize(fs, SubAssetManifestContext.Default.SubAssetManifest);
    }
}
