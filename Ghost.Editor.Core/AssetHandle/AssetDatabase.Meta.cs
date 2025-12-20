using Ghost.Core;
using Ghost.Editor.Core.Utilities;
using System.Reflection;
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
    }

    private static Result<string, ErrorStatus> GetMetaFilePath(string assetPath)
    {
        if (Directory.Exists(assetPath))
        {
            return ErrorStatus.NotFound;
        }

        if (Path.GetExtension(assetPath).Equals(".meta", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorStatus.InvalidState;
        }

        return assetPath + ".meta";
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

    internal static Result GenerateMetaFile(string assetPath)
    {
        var metaFileResult = GetMetaFilePath(assetPath);
        if (!metaFileResult.IsSuccess)
        {
            return Result.Failure(metaFileResult.Error.ToString());
        }

        if (File.Exists(metaFileResult.Value))
        {
            var existingMeta = JsonSerializer.Deserialize<AssetMeta>(File.ReadAllText(metaFileResult.Value));
            if (existingMeta != null && s_assetPathLookup.TryGetValue(existingMeta.Guid, out var path))
            {
                if (assetPath != path)
                {
                    existingMeta.Guid = Guid.NewGuid();
                    WriteMetaFile(metaFileResult.Value, existingMeta);
                }
            }

            return Result.Success();
        }

        var defaultSettings = GetDefaultSettingsForAsset(assetPath);
        var metaData = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            Settings = defaultSettings
        };

        WriteMetaFile(metaFileResult.Value, metaData);

        return Result.Success();
    }

    private static void OnAssetCreated(object sender, FileSystemEventArgs e)
    {
        GenerateMetaFile(e.FullPath);
    }

    private static void OnAssetDeleted(object sender, FileSystemEventArgs e)
    {
        var metaFileResult = GetMetaFilePath(e.FullPath);
        if (metaFileResult.IsSuccess && File.Exists(metaFileResult.Value))
        {
            try
            {
                var meta = JsonSerializer.Deserialize<AssetMeta>(File.ReadAllText(metaFileResult.Value));
                if (meta != null
                    && s_assetPathLookup.TryGetValue(meta.Guid, out var path)
                    && path == e.FullPath)
                {
                    s_assetPathLookup.Remove(meta.Guid);
                }

                File.Delete(metaFileResult.Value);
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