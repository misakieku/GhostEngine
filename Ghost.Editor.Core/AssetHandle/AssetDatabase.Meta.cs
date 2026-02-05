using Ghost.Core;
using Ghost.Editor.Core.Utilities;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace Ghost.Editor.Core.AssetHandle;

public partial class AssetService
{
    private readonly Dictionary<string, Type> _importerTypeLookup = new();

    private void InitializeMetaData()
    {
        if (_watcher == null)
        {
            throw new InvalidOperationException("AssetDatabase is not initialized. Ensure that Initialize() is called before registering asset importers.");
        }

        var importerTypes = TypeCache.GetTypes().Where(t => t.GetCustomAttribute<AssetImporterAttribute>() != null);
        foreach (var type in importerTypes)
        {
            var attribute = type.GetCustomAttribute<AssetImporterAttribute>()!;
            foreach (var extension in attribute.SupportedExtensions)
            {
                _importerTypeLookup[extension] = type;
            }
        }

        _watcher.Created += OnFSEvent;
        _watcher.Deleted += OnFSEvent;
        _watcher.Changed += OnFSEvent;
        _watcher.Renamed += OnAssetRenamed;
    }

    private Result<string> GetMetaFilePath(string assetPath)
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

    private ImporterSettings? GetDefaultSettingsForAsset(string assetPath)
    {
        var extension = Path.GetExtension(assetPath);

        if (_importerTypeLookup.TryGetValue(extension, out var importerType))
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
    private async Task<string> CalculateFileHashAsync(string filePath, CancellationToken token = default)
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

    private async Task<Result> WriteMetaFileAsync(string metaFilePath, AssetMeta metaData, CancellationToken token = default)
    {
        try
        {
            await using var fileStream = File.Create(metaFilePath);
            await JsonSerializer.SerializeAsync(fileStream, metaData, _defaultJsonOptions, token);
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
    private async ValueTask<Result<AssetMeta>> ReadMetaFileAsync(string assetPath, CancellationToken token = default)
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
            var meta = await JsonSerializer.DeserializeAsync<AssetMeta>(fileStream, _defaultJsonOptions, token);
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

    internal async ValueTask<Result> GenerateMetaFileAsync(string assetPath, CancellationToken token = default)
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
                if (_assetPathLookup.TryGetValue(existingMeta.Guid, out var path))
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsMetaFile(string path)
    {
        return Path.GetExtension(path).Equals(FileExtensions.META_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase);
    }

    private async void OnFSEvent(object sender, FileSystemEventArgs e)
    {
        if (IsMetaFile(e.FullPath))
        {
            return;
        }

        var type = e.ChangeType switch
        {
            WatcherChangeTypes.Created => AssetCommandType.FileCreated,
            WatcherChangeTypes.Deleted => AssetCommandType.FileDeleted,
            WatcherChangeTypes.Changed => AssetCommandType.FileModified,
            _ => throw new InvalidOperationException("Unsupported file system event type")
        };

        await PostCommandAsync(new AssetCommand(type, e.FullPath, Timestamp: DateTime.UtcNow));
    }

    private async void OnAssetRenamed(object sender, RenamedEventArgs e)
    {
        if (IsMetaFile(e.FullPath))
        {
            return;
        }

        await PostCommandAsync(new AssetCommand(AssetCommandType.FileRenamed, e.FullPath, e.OldFullPath, DateTime.UtcNow));
    }

    /// <summary>
    /// Mark all assets that depend on the specified asset as dirty.
    /// </summary>
    private async Task MarkDependentAssetsDirtyAsync(Guid assetGuid)
    {
        // TODO: We should have a reverse dependency lookup in the database to avoid scanning all assets.

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
