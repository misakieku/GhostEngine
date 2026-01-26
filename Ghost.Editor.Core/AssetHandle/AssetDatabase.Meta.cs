using Ghost.Core;
using Ghost.Editor.Core.Utilities;
using System.Reflection;
using System.Text.Json;

namespace Ghost.Editor.Core.AssetHandle;

public static partial class AssetDatabase
{
    private static readonly Dictionary<string, Type> s_importerTypeLookup = new();
    private static readonly Dictionary<Guid, string> s_assetPathLookup = new();
    private static readonly Dictionary<string, Guid> s_pathAssetLookup = new();

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

    private static Result<string, Error> GetMetaFilePath(string assetPath)
    {
        if (Directory.Exists(assetPath))
        {
            return Error.NotFound;
        }

        if (Path.GetExtension(assetPath).Equals(FileExtensions.META_FILE_EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            return Error.InvalidState;
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

    private static async Task<Result> WriteMetaFileAsync(string metaFilePath, AssetMeta metaData)
    {
        using var fileStream = File.Create(metaFilePath);

        try
        {
            await JsonSerializer.SerializeAsync(fileStream, metaData);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }

        return Result.Success();
    }

    internal static async Task<Result> GenerateMetaFileAsync(string assetPath)
    {
        Result r;

        var metaFileResult = GetMetaFilePath(assetPath);
        if (metaFileResult.IsFailure)
        {
            return Result.Failure(metaFileResult.Error);
        }

        if (File.Exists(metaFileResult.Value))
        {
            using var fileStream = File.OpenRead(metaFileResult.Value);
            var existingMeta = await JsonSerializer.DeserializeAsync<AssetMeta>(fileStream);
            if (existingMeta != null && s_assetPathLookup.TryGetValue(existingMeta.Guid, out var path))
            {
                if (assetPath != path)
                {
                    existingMeta.Guid = Guid.NewGuid();
                    r = await WriteMetaFileAsync(metaFileResult.Value, existingMeta);
                    if (r.IsFailure)
                    {
                        return r;
                    }
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

        r = await WriteMetaFileAsync(metaFileResult.Value, metaData);

        return r;
    }

    private static async void OnAssetCreated(object sender, FileSystemEventArgs e)
    {
        await GenerateMetaFileAsync(e.FullPath);
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

    private static async void OnAssetRenamed(object sender, RenamedEventArgs e)
    {
        var oldMetaPath = e.OldFullPath + FileExtensions.META_FILE_EXTENSION;
        var newMetaPath = e.FullPath + FileExtensions.META_FILE_EXTENSION;

        if (File.Exists(oldMetaPath))
        {
            File.Move(oldMetaPath, newMetaPath);
        }
        else
        {
            await GenerateMetaFileAsync(e.FullPath);
        }
    }
}
