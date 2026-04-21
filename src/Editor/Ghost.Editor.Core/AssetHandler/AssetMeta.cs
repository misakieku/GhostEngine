using Ghost.Engine;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// Mark IAssetSettings for polymorphic serialization.
/// Each handler type will register its own derived type.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DefaultAssetSettings), "Default")]
public interface IAssetSettings;

public sealed class DefaultAssetSettings : IAssetSettings;

/// <summary>
/// Persisted as a JSON sidecar (.gmeta) next to every source asset.
/// This is the single source of truth for asset identity and import settings.
/// </summary>
public sealed class AssetMeta
{
    /// <summary>
    /// Globally unique identifier for this asset. Generated once, never changes.
    /// </summary>
    public required Guid Guid { get; init; }

    /// <summary>
    /// The Guid that identifies which IAssetHandler processes this asset.
    /// </summary>
    public Guid? HandlerTypeId { get; set; }

    /// <summary>
    /// Version of the handler that last imported this asset.
    /// </summary>
    public int HandlerVersion { get; set; }

    /// <summary>
    /// xxHash64 of the source file content at last successful import.
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// xxHash64 of the serialized import settings at last successful import.
    /// </summary>
    public string? SettingsHash { get; set; }

    /// <summary>
    /// UTC timestamp of last successful import.
    /// </summary>
    public DateTime? LastImportedUtc { get; set; }

    /// <summary>
    /// GUIDs of other assets this asset depends on.
    /// </summary>
    public Guid[] Dependencies { get; set; } = [];

    /// <summary>
    /// Optional user-facing labels for search/filtering in the editor.
    /// </summary>
    public string[] Labels { get; set; } = [];

    /// <summary>
    /// Handler-specific import settings.
    /// </summary>
    public IAssetSettings? Settings { get; set; }
}

internal static class AssetMetaIO
{
    public const string META_EXTENSION_NAME = "gmeta";
    public const string META_EXTENSION = ".gmeta";

    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static async ValueTask<AssetMeta?> ReadAsync(string metaPath, CancellationToken token = default)
    {
        if (!File.Exists(metaPath))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(metaPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await JsonSerializer.DeserializeAsync<AssetMeta>(stream, s_options, token).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public static async ValueTask WriteAsync(string metaPath, AssetMeta meta, CancellationToken token = default)
    {
        var tempPath = metaPath + ".tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, meta, s_options, token).ConfigureAwait(false);
        }

        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
        }

        File.Move(tempPath, metaPath);
    }

    public static string GetMetaPath(string sourceFilePath)
    {
        return sourceFilePath + META_EXTENSION;
    }

    public static string GetSourcePath(string metaPath)
    {
        return metaPath[..^META_EXTENSION.Length];
    }
}
