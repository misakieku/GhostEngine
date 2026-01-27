using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.AssetHandle;

/// <summary>
/// Metadata for an asset, stored in .gmeta files.
/// Contains GUID, version, tags, and importer settings.
/// FileHash and Dependencies are stored in the database only, not in .gmeta files.
/// </summary>
internal class AssetMeta
{
    /// <summary>
    /// Unique identifier for the asset.
    /// </summary>
    [JsonPropertyName("Guid")]
    public Guid Guid
    {
        get;
        set;
    }

    /// <summary>
    /// Version of the asset pipeline (not the asset itself).
    /// Used for migration when the asset pipeline is redesigned.
    /// </summary>
    [JsonPropertyName("Version")]
    public int Version
    {
        get;
        set;
    } = 1;

    /// <summary>
    /// Tags for categorizing and searching assets.
    /// </summary>
    [JsonPropertyName("Tags")]
    public List<string> Tags
    {
        get;
        set;
    } = new();

    /// <summary>
    /// Importer settings specific to this asset.
    /// The key is the importer type name, and the value is a JSON element containing the settings.
    /// Use GetImporterSettings&lt;T&gt;() and SetImporterSettings&lt;T&gt;() to work with strongly-typed settings.
    /// </summary>
    [JsonPropertyName("ImporterSettings")]
    public Dictionary<string, JsonElement> ImporterSettings
    {
        get;
        set;
    } = new();

    /// <summary>
    /// Get importer settings of a specific type.
    /// </summary>
    public T? GetImporterSettings<T>(string importerName) where T : ImporterSettings
    {
        if (ImporterSettings.TryGetValue(importerName, out var element))
        {
            return element.Deserialize<T>();
        }
        return null;
    }

    /// <summary>
    /// Set importer settings.
    /// </summary>
    public void SetImporterSettings<T>(string importerName, T settings) where T : ImporterSettings
    {
        var element = JsonSerializer.SerializeToElement(settings);
        ImporterSettings[importerName] = element;
    }

    /// <summary>
    /// Set importer settings (non-generic overload).
    /// </summary>
    internal void SetImporterSettings(string importerName, ImporterSettings settings)
    {
        var element = JsonSerializer.SerializeToElement(settings, settings.GetType());
        ImporterSettings[importerName] = element;
    }
}
