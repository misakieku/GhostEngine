using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

/// <summary>
/// Immutable snapshot of a project's bake/pack configuration, captured once when the
/// project is loaded (via <c>ProjectService.GetContext()</c>) and shared by the
/// <c>BakeService</c> and <c>PackService</c>. All members are set from a single
/// initialization path, so consumers never observe a partially-configured project.
/// </summary>
public sealed record ProjectContext(
    Project Project,
    IReadOnlyList<string> AssetDirectories,
    string CacheDirectory,
    string BuildDirectory,
    IReadOnlyList<string> ShaderMetadataPaths)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Enumerates all source asset files across <see cref="AssetDirectories"/> and maps
    /// each file's virtual path (forward-slash path relative to its asset directory) to
    /// its absolute path. Later directories overwrite earlier ones for the same virtual
    /// path. Files ending in <c>.meta</c> are excluded.
    /// </summary>
    public Dictionary<string, string> EnumerateAssetFiles()
    {
        var virtualPathToFile = new Dictionary<string, string>();

        foreach (var assetDir in AssetDirectories)
        {
            if (!Directory.Exists(assetDir))
            {
                continue;
            }

            var filesInDir = Directory.GetFiles(assetDir, "*.*", SearchOption.AllDirectories)
                                      .Where(f => !f.EndsWith(".meta", StringComparison.OrdinalIgnoreCase));

            foreach (var file in filesInDir)
            {
                var relativePath = Path.GetRelativePath(assetDir, file);
                var virtualPath = relativePath.Replace('\\', '/');
                virtualPathToFile[virtualPath] = file;
            }
        }

        return virtualPathToFile;
    }

    /// <summary>
    /// Loads an <see cref="AssetMetadata"/> from the given <c>.meta</c> file,
    /// or <c>null</c> when the file does not exist.
    /// </summary>
    public AssetMetadata? LoadMetadata(string metaFilePath)
    {
        if (!File.Exists(metaFilePath))
        {
            return null;
        }

        var json = File.ReadAllText(metaFilePath);
        return JsonSerializer.Deserialize<AssetMetadata>(json, s_jsonOptions);
    }

    /// <summary>
    /// Serializes <paramref name="metadata"/> to the given <c>.meta</c> file.
    /// </summary>
    public void SaveMetadata(string metaFilePath, AssetMetadata metadata)
    {
        File.WriteAllText(metaFilePath, JsonSerializer.Serialize(metadata, s_jsonOptions));
    }
}
