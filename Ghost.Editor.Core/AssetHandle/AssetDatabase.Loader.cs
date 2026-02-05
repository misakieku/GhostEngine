using Ghost.Core;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Ghost.Editor.Core.AssetHandle;

public partial class AssetService
{
    // Asset cache - stores loaded assets by GUID
    private readonly ConcurrentDictionary<Guid, Asset> _assetCache = new();

    // LRU tracking - stores access time for each cached asset
    private readonly ConcurrentDictionary<Guid, DateTime> _assetAccessTime = new();

    // Maximum number of cached assets before eviction starts
    private const int _MAX_CACHED_ASSETS = 1000;

    // Percentage of cache to evict when limit is reached (evict oldest 20%)
    private const float _CACHE_EVICTION_PERCENTAGE = 0.2f;

    private Result<string> GetImportedAssetsDirectory()
    {
        if (AssetsDirectory == null)
        {
            return Result<string>.Failure("AssetsDirectory not initialized");
        }

        var cacheDir = Path.Combine(AssetsDirectory.Parent!.FullName, EditorApplication.CACHES_FOLDER_NAME, "ImportedAssets");
        if (!Directory.Exists(cacheDir))
        {
            Directory.CreateDirectory(cacheDir);
        }

        return cacheDir;
    }

    private Result<string> GetImportedAssetPath(Guid guid)
    {
        var importedDirResult = GetImportedAssetsDirectory();
        if (importedDirResult.IsFailure)
        {
            return Result<string>.Failure(importedDirResult.Message);
        }

        // Store imported assets as {GUID}.asset
        var assetDataPath = Path.Combine(importedDirResult.Value, $"{guid}.asset");
        return assetDataPath;
    }

    private Result<T> LoadAssetInternal<T>(Guid guid) where T : Asset
    {
        // Check cache first
        if (_assetCache.TryGetValue(guid, out var cachedAsset))
        {
            // Update access time for LRU
            _assetAccessTime[guid] = DateTime.UtcNow;

            if (cachedAsset is T typedAsset)
            {
                return typedAsset;
            }
            else
            {
                return Result<T>.Failure($"Cached asset is of type {cachedAsset.GetType().Name}, expected {typeof(T).Name}");
            }
        }

        // Asset not in cache, load from disk
        var assetPathResult = GetImportedAssetPath(guid);
        if (assetPathResult.IsFailure)
        {
            return Result<T>.Failure(assetPathResult.Message);
        }

        var assetDataPath = assetPathResult.Value;
        if (!File.Exists(assetDataPath))
        {
            return Result<T>.Failure($"Imported asset data not found at {assetDataPath}. Asset may not have been imported yet.");
        }

        try
        {
            // Read and deserialize asset data
            var json = File.ReadAllText(assetDataPath);
            var asset = JsonSerializer.Deserialize<T>(json);
            if (asset == null)
            {
                return Result<T>.Failure("Failed to deserialize asset data");
            }

            // Add to cache
            CacheAsset(guid, asset);
            return asset;
        }
        catch (Exception ex)
        {
            return Result<T>.Failure($"Failed to load asset: {ex.Message}");
        }
    }

    public Result<T> LoadAssetAtPath<T>(string assetPath) where T : Asset
    {
        var guidResult = PathToGuid(assetPath);
        if (guidResult.IsFailure)
        {
            return Result<T>.Failure(guidResult.Message);
        }

        return LoadAsset<T>(guidResult.Value);
    }

    private void CacheAsset(Guid guid, Asset asset)
    {
        // Check if we need to evict old assets
        if (_assetCache.Count >= _MAX_CACHED_ASSETS)
        {
            EvictOldestAssets();
        }

        _assetCache[guid] = asset;
        _assetAccessTime[guid] = DateTime.UtcNow;
    }

    private void EvictOldestAssets()
    {
        var evictionCount = (int)(_MAX_CACHED_ASSETS * _CACHE_EVICTION_PERCENTAGE);

        // Sort by access time and remove oldest entries
        var oldestAssets = _assetAccessTime
            .OrderBy(kvp => kvp.Value)
            .Take(evictionCount)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var guid in oldestAssets)
        {
            _assetCache.TryRemove(guid, out _);
            _assetAccessTime.TryRemove(guid, out _);
        }
    }

    /// <summary>
    /// Unload a specific asset from cache.
    /// </summary>
    /// <param name="guid">GUID of the asset to unload.</param>
    public void UnloadAsset(Guid guid)
    {
        _assetCache.TryRemove(guid, out _);
        _assetAccessTime.TryRemove(guid, out _);
    }

    /// <summary>
    /// Unload all assets from cache.
    /// </summary>
    public void UnloadAllAssets()
    {
        _assetCache.Clear();
        _assetAccessTime.Clear();
    }

    /// <summary>
    /// Check if an asset is currently loaded in cache.
    /// </summary>
    /// <param name="guid">GUID of the asset.</param>
    /// <returns>True if the asset is in cache.</returns>
    public bool IsAssetLoaded(Guid guid)
    {
        return _assetCache.ContainsKey(guid);
    }

    /// <summary>
    /// Get cache statistics.
    /// </summary>
    /// <returns>Tuple of (current cache size, max cache size).</returns>
    public (int currentSize, int maxSize) GetCacheStats()
    {
        return (_assetCache.Count, _MAX_CACHED_ASSETS);
    }

    /// <summary>
    /// Save an imported asset to disk for later loading.
    /// This should be called by importers after processing the source file.
    /// </summary>
    /// <typeparam name="T">Type of asset data.</typeparam>
    /// <param name="guid">GUID of the asset.</param>
    /// <param name="assetData">Processed asset data to save.</param>
    /// <returns>Result indicating success or failure.</returns>
    public Result SaveImportedAsset<T>(Guid guid, T assetData)
        where T : Asset
    {
        var assetPathResult = GetImportedAssetPath(guid);
        if (assetPathResult.IsFailure)
        {
            return Result.Failure(assetPathResult.Message);
        }

        try
        {
            var json = JsonSerializer.Serialize(assetData, _defaultJsonOptions);
            File.WriteAllText(assetPathResult.Value, json);

            // Invalidate cache for this asset so it gets reloaded next time
            UnloadAsset(guid);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to save imported asset: {ex.Message}");
        }
    }
}
