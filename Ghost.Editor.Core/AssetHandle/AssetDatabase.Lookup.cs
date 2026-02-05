using Ghost.Core;
using System.Text.Json;

namespace Ghost.Editor.Core.AssetHandle;

public static partial class AssetService
{
    /// <summary>
    /// Get the relative path from the assets directory.
    /// </summary>
    private static Result<string> GetRelativePath(string fullPath)
    {
        if (AssetsDirectory == null)
        {
            return Result<string>.Failure("AssetsDirectory not initialized");
        }

        if (!fullPath.StartsWith(AssetsDirectory.FullName, StringComparison.OrdinalIgnoreCase))
        {
            return Result<string>.Failure("Path is not within assets directory");
        }

        return Path.GetRelativePath(AssetsDirectory.FullName, fullPath);
    }

    /// <summary>
    /// Get the full path from a relative path.
    /// </summary>
    private static Result<string> GetFullPath(string relativePath)
    {
        if (AssetsDirectory == null)
        {
            return Result<string>.Failure("AssetsDirectory not initialized");
        }

        return Path.Combine(AssetsDirectory.FullName, relativePath);
    }

    /// <summary>
    /// Find GUID by asset path.
    /// </summary>
    /// <param name="assetPath">Full or relative path to the asset.</param>
    /// <returns>The GUID of the asset if found.</returns>
    public static Result<Guid> PathToGuid(string assetPath)
    {
        var relativePath = assetPath;

        // Convert to relative path if it's a full path
        if (Path.IsPathRooted(assetPath))
        {
            var relResult = GetRelativePath(assetPath);
            if (relResult.IsFailure)
            {
                return Result<Guid>.Failure(relResult.Message);
            }
            relativePath = relResult.Value;
        }

        // Normalize path separators
        relativePath = relativePath.Replace('\\', '/');

        lock (s_dbLock)
        {
            if (s_pathAssetLookup.TryGetValue(relativePath, out var guid))
            {
                return guid;
            }
        }

        return Result<Guid>.Failure("Asset not found in database");
    }

    /// <summary>
    /// Find path by GUID.
    /// </summary>
    /// <param name="guid">GUID of the asset.</param>
    /// <returns>The relative path to the asset if found.</returns>
    public static Result<string> GuidToPath(Guid guid)
    {
        lock (s_dbLock)
        {
            if (s_assetPathLookup.TryGetValue(guid, out var path))
            {
                return path;
            }
        }

        return Result<string>.Failure("Asset GUID not found in database");
    }

    /// <summary>
    /// Load asset by GUID with caching.
    /// </summary>
    /// <typeparam name="T">Type of asset to load.</typeparam>
    /// <param name="guid">GUID of the asset.</param>
    /// <returns>The loaded asset.</returns>
    public static Result<T> LoadAsset<T>(Guid guid) where T : Asset
    {
        // Implemented in AssetService.Loader.cs
        return LoadAssetInternal<T>(guid);
    }

    /// <summary>
    /// Get asset tags by GUID.
    /// </summary>
    /// <param name="guid">GUID of the asset.</param>
    /// <returns>List of tags associated with the asset.</returns>
    public static async ValueTask<Result<List<string>>> GetAssetTagsAsync(Guid guid, CancellationToken token = default)
    {
        var pathResult = GuidToPath(guid);
        if (pathResult.IsFailure)
        {
            return Result<List<string>>.Failure(pathResult.Message);
        }

        var fullPathResult = GetFullPath(pathResult.Value);
        if (fullPathResult.IsFailure)
        {
            return Result<List<string>>.Failure(fullPathResult.Message);
        }

        var metaResult = await ReadMetaFileAsync(fullPathResult.Value, token);
        if (metaResult.IsFailure)
        {
            return Result<List<string>>.Failure(metaResult.Message);
        }

        return metaResult.Value.Tags;
    }

    /// <summary>
    /// Set asset tags by GUID.
    /// </summary>
    /// <param name="guid">GUID of the asset.</param>
    /// <param name="tags">New tags for the asset.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async ValueTask<Result> SetAssetTagsAsync(Guid guid, List<string> tags, CancellationToken token = default)
    {
        var pathResult = GuidToPath(guid);
        if (pathResult.IsFailure)
        {
            return Result.Failure(pathResult.Message);
        }

        var fullPathResult = GetFullPath(pathResult.Value);
        if (fullPathResult.IsFailure)
        {
            return Result.Failure(fullPathResult.Message);
        }

        var metaResult = await ReadMetaFileAsync(fullPathResult.Value, token);
        if (metaResult.IsFailure)
        {
            return Result.Failure(metaResult.Message);
        }

        metaResult.Value.Tags = tags;
        
        // Write updated metadata to .gmeta file
        var writeResult = await WriteMetaFileAsync(fullPathResult.Value + Utilities.FileExtensions.META_FILE_EXTENSION, metaResult.Value, token);
        if (writeResult.IsFailure)
        {
            return writeResult;
        }

        // Update database with new tags
        var fileHash = await CalculateFileHashAsync(fullPathResult.Value, token);
        return await UpsertAssetAsync(fullPathResult.Value, metaResult.Value, fileHash, null, token);
    }

    /// <summary>
    /// Search assets by name pattern.
    /// Supports SQL LIKE wildcards: * (any characters) and ? (single character).
    /// </summary>
    /// <param name="namePattern">Search pattern (e.g., "*.txt", "player?", "test*").</param>
    /// <returns>List of matching asset GUIDs.</returns>
    public static async Task<List<Guid>> FindAssetsByNameAsync(string namePattern, CancellationToken token = default)
    {
        return await GetAssetsByNameAsync(namePattern, token);
    }

    /// <summary>
    /// Find assets by tag.
    /// </summary>
    /// <param name="tag">Tag to search for.</param>
    /// <returns>List of asset GUIDs with the specified tag.</returns>
    public static async Task<List<Guid>> FindAssetsByTagAsync(string tag, CancellationToken token = default)
    {
        return await GetAssetsByTagAsync(tag, token);
    }

    /// <summary>
    /// Get all assets in the database.
    /// </summary>
    /// <returns>Dictionary mapping GUIDs to relative paths.</returns>
    public static IReadOnlyDictionary<Guid, string> GetAllAssets()
    {
        lock (s_dbLock)
        {
            return s_assetPathLookup.AsReadOnly();
        }
    }
}
