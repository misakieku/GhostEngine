using Ghost.Core;
using Ghost.Editor.Core.Utilities;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ghost.Editor.Core.AssetHandle;

public static partial class AssetDatabase
{
    private static readonly Dictionary<string, Type> s_importerTypeLookup = new();

    private static void InitializeMetaData()
    {
        if (s_watcher == null)
        {
            throw new InvalidOperationException("AssetDatabase is not initialized. Ensure that Initialize() is called before registering asset importers.");
        }

        var importerTypes = TypeCache.GetTypes().Where(t => t.GetCustomAttribute<AssetImporterAttribute>() != null);
        foreach (var type in importerTypes)
        {
            var attribute = type.GetCustomAttribute<AssetImporterAttribute>()!;
            foreach (var extension in attribute.SupportedExtensions)
            {
                s_importerTypeLookup[extension] = type;
            }
        }

        s_watcher.Created += OnAssetCreated;
        s_watcher.Deleted += OnAssetDeleted;
        s_watcher.Renamed += OnAssetRenamed;
        s_watcher.Changed += OnAssetChanged;
    }

    private static Result<string> GetMetaFilePath(string assetPath)
    {
        if (Directory.Exists(assetPath))
        {
            return Result<string>.Failure("Cannot create metadata for directories");
        }

        if (Path.GetExtension(assetPath).Equals(FileExtensions.META_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Failure("Cannot create metadata for metadata files");
        }

        return assetPath + FileExtensions.META_FILE_EXTENSION;
    }

    private static ImporterSettings? GetDefaultSettingsForAsset(string assetPath)
    {
        var extension = Path.GetExtension(assetPath);

        if (s_importerTypeLookup.TryGetValue(extension, out var importerType))
        {
            var settingsType = importerType.BaseType?.GetGenericArguments()[0];
            if (settingsType == null || !typeof(ImporterSettings).IsAssignableFrom(settingsType))
            {
                return null;
            }

            return (ImporterSettings?)Activator.CreateInstance(settingsType);
        }

        return null;
    }

    /// <summary>
    /// Calculate SHA256 hash of a file for change detection.
    /// </summary>
    private static async Task<string> CalculateFileHashAsync(string filePath, CancellationToken token = default)
    {
        try
        {
            await using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream, token);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static async Task<Result> WriteMetaFileAsync(string metaFilePath, AssetMeta metaData, CancellationToken token = default)
    {
        try
        {
            await using var fileStream = File.Create(metaFilePath);
            await JsonSerializer.SerializeAsync(fileStream, metaData, s_defaultJsonOptions, token);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Read metadata from a .gmeta file.
    /// </summary>
    private static async ValueTask<Result<AssetMeta>> ReadMetaFileAsync(string assetPath, CancellationToken token = default)
    {
        var metaFileResult = GetMetaFilePath(assetPath);
        if (metaFileResult.IsFailure)
        {
            return Result<AssetMeta>.Failure(metaFileResult.Message);
        }

        if (!File.Exists(metaFileResult.Value))
        {
            return Result<AssetMeta>.Failure("Metadata file does not exist");
        }

        try
        {
            await using var fileStream = File.OpenRead(metaFileResult.Value);
            var meta = await JsonSerializer.DeserializeAsync<AssetMeta>(fileStream, s_defaultJsonOptions, token);
            if (meta == null)
            {
                return Result<AssetMeta>.Failure("Failed to deserialize metadata");
            }

            return meta;
        }
        catch (Exception ex)
        {
            return Result<AssetMeta>.Failure($"Failed to read metadata: {ex.Message}");
        }
    }

    internal static async ValueTask<Result> GenerateMetaFileAsync(string assetPath, CancellationToken token = default)
    {
        Result r;

        var metaFileResult = GetMetaFilePath(assetPath);
        if (metaFileResult.IsFailure)
        {
            return Result.Failure(metaFileResult.Message);
        }

        if (File.Exists(metaFileResult.Value))
        {
            var existingMetaResult = await ReadMetaFileAsync(assetPath, token);
            if (existingMetaResult.IsSuccess)
            {
                var existingMeta = existingMetaResult.Value;
                if (s_assetPathLookup.TryGetValue(existingMeta.Guid, out var path))
                {
                    var relResult = GetRelativePath(assetPath);
                    if (relResult.IsSuccess && assetPath != path)
                    {
                        // GUID conflict - regenerate
                        existingMeta.Guid = Guid.NewGuid();
                        r = await WriteMetaFileAsync(metaFileResult.Value, existingMeta, token);
                        if (r.IsFailure)
                        {
                            return r;
                        }
                    }
                }

                // Calculate file hash and update database
                var fileHash = await CalculateFileHashAsync(assetPath, token);
                await UpsertAssetAsync(assetPath, existingMeta, fileHash, null, token);
                return Result.Success();
            }
        }

        // Calculate initial file hash
        var fileHash2 = await CalculateFileHashAsync(assetPath, token);

        var defaultSettings = GetDefaultSettingsForAsset(assetPath);
        var metaData = new AssetMeta
        {
            Guid = Guid.NewGuid()
        };

        if (defaultSettings != null)
        {
            metaData.SetImporterSettings(defaultSettings.GetType().Name, defaultSettings);
        }

        r = await WriteMetaFileAsync(metaFileResult.Value, metaData, token);
        if (r.IsFailure)
        {
            return r;
        }

        // Add to database
        await UpsertAssetAsync(assetPath, metaData, fileHash2, null, token);

        return r;
    }

    private static void OnAssetCreated(object sender, FileSystemEventArgs e)
    {
        // Skip meta files
        if (Path.GetExtension(e.FullPath).Equals(FileExtensions.META_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PostCommand(new AssetCommand(AssetCommandType.FileCreated, e.FullPath, Timestamp: DateTime.UtcNow));
    }

    private static void OnAssetDeleted(object sender, FileSystemEventArgs e)
    {
        // Skip meta files
        if (Path.GetExtension(e.FullPath).Equals(FileExtensions.META_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PostCommand(new AssetCommand(AssetCommandType.FileDeleted, e.FullPath, Timestamp: DateTime.UtcNow));
    }

    private static void OnAssetRenamed(object sender, RenamedEventArgs e)
    {
        // Skip meta files
        if (Path.GetExtension(e.FullPath).Equals(FileExtensions.META_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PostCommand(new AssetCommand(AssetCommandType.FileRenamed, e.FullPath, e.OldFullPath, DateTime.UtcNow));
    }

    private static void OnAssetChanged(object sender, FileSystemEventArgs e)
    {
        // Skip meta files
        if (Path.GetExtension(e.FullPath).Equals(FileExtensions.META_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PostCommand(new AssetCommand(AssetCommandType.FileModified, e.FullPath, Timestamp: DateTime.UtcNow));
    }

    /// <summary>
    /// Mark all assets that depend on the specified asset as dirty.
    /// </summary>
    private static async Task MarkDependentAssetsDirtyAsync(Guid assetGuid)
    {
        // Query database for all assets and check their dependencies
        var allAssets = GetAllAssets();

        foreach (var kvp in allAssets)
        {
            var dependencies = await GetDependenciesAsync(kvp.Key, CancellationToken.None);
            if (dependencies.Contains(assetGuid))
            {
                MarkDirty(kvp.Key);
            }
        }
    }
}
