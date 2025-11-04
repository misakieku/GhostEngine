using Ghost.Core;
using Ghost.Editor.Core.Utilities;
using Ghost.Engine.Services;
using System.Reflection;
using System.Text.Json;

namespace Ghost.Editor.Core.AssetHandle;

public static partial class AssetDatabase
{
    private static readonly Dictionary<string, Type> _importerTypeLookup = new();

    private static void InitializeMetaData()
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

        _watcher.Created += OnAssetCreated;
        _watcher.Deleted += OnAssetDeleted;
        _watcher.Renamed += OnAssetRenamed;
    }

    private static Result<string> GetMetaFilePath(string assetPath)
    {
        if (Directory.Exists(assetPath))
        {
            return Result<string>.Fail("Folder does not have meta data");
        }

        if (Path.GetExtension(assetPath).Equals(".meta", StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Fail("Asset path cannot be a meta file");
        }

        return Result<string>.Success(assetPath + ".meta");
    }

    private static ImporterSettings? GetDefaultSettingsForAsset(string assetPath)
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

    private static void WriteMetaFile(string metaFilePath, AssetMeta metaData)
    {
        using var fileStream = File.Create(metaFilePath);

        try
        {
            JsonSerializer.Serialize(fileStream, metaData);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }

    internal static void GenerateMetaFile(string assetPath)
    {
        var metaFileResult = GetMetaFilePath(assetPath);
        if (!metaFileResult.success)
        {
            Logger.LogError(metaFileResult.message);
            return;
        }

        if (File.Exists(metaFileResult.value))
        {
            var existingMeta = JsonSerializer.Deserialize<AssetMeta>(File.ReadAllText(metaFileResult.value));
            if (existingMeta != null && _assetPathLookup.TryGetValue(existingMeta.Guid, out var path))
            {
                if (assetPath != path)
                {
                    existingMeta.Guid = Guid.NewGuid();
                    WriteMetaFile(metaFileResult.value, existingMeta);
                }
            }

            return;
        }

        var defaultSettings = GetDefaultSettingsForAsset(assetPath);
        var metaData = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            Settings = defaultSettings
        };

        WriteMetaFile(metaFileResult.value, metaData);
    }

    private static void OnAssetCreated(object sender, FileSystemEventArgs e)
    {
        GenerateMetaFile(e.FullPath);
    }

    private static void OnAssetDeleted(object sender, FileSystemEventArgs e)
    {
        var metaFileResult = GetMetaFilePath(e.FullPath);
        if (metaFileResult.success && File.Exists(metaFileResult.value))
        {
            try
            {
                var meta = JsonSerializer.Deserialize<AssetMeta>(File.ReadAllText(metaFileResult.value));
                if (meta != null
                    && _assetPathLookup.TryGetValue(meta.Guid, out var path)
                    && path == e.FullPath)
                {
                    _assetPathLookup.Remove(meta.Guid);
                }

                File.Delete(metaFileResult.value);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }
    }

    private static void OnAssetRenamed(object sender, RenamedEventArgs e)
    {
        var oldMetaPath = e.OldFullPath + ".meta";
        var newMetaPath = e.FullPath + ".meta";

        if (File.Exists(oldMetaPath))
        {
            File.Move(oldMetaPath, newMetaPath);
        }
        else
        {
            GenerateMetaFile(e.FullPath);
        }
    }
}