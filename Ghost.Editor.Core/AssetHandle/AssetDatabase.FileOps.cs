using Ghost.Core;

namespace Ghost.Editor.Core.AssetHandle;

public static partial class AssetDatabase
{
    /// <summary>
    /// Create a new asset at the specified path.
    /// Generates metadata and adds it to the database.
    /// </summary>
    /// <param name="assetPath">Path to create the asset at.</param>
    /// <param name="content">Content to write to the asset file.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<Result> CreateAssetAsync(string assetPath, byte[] content)
    {
        if (AssetsDirectory == null)
        {
            return Result.Failure("AssetsDirectory not initialized");
        }

        if (!assetPath.StartsWith(AssetsDirectory.FullName, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure("Asset path must be within the Assets directory");
        }

        if (File.Exists(assetPath))
        {
            return Result.Failure("Asset already exists");
        }

        try
        {
            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(assetPath, content);

            // GenerateMetaFileAsync will be called automatically by the file watcher
            // But we'll call it directly to ensure it's created immediately
            await GenerateMetaFileAsync(assetPath);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to create asset: {ex.Message}");
        }
    }

    /// <summary>
    /// Create an empty asset at the specified path.
    /// Generates metadata and adds it to the database.
    /// </summary>
    /// <param name="assetPath">Path to create the asset at.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<Result> CreateAssetAsync(string assetPath)
    {
        return await CreateAssetAsync(assetPath, Array.Empty<byte>());
    }

    /// <summary>
    /// Delete an asset and its metadata.
    /// </summary>
    /// <param name="guid">GUID of the asset to delete.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<Result> DeleteAssetAsync(Guid guid)
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

        try
        {
            var assetPath = fullPathResult.Value;

            // Delete the asset file
            if (File.Exists(assetPath))
            {
                File.Delete(assetPath);
            }

            // Delete the .gmeta file
            var metaPath = assetPath + Utilities.FileExtensions.META_FILE_EXTENSION;
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            // Remove from database
            await RemoveAssetFromDatabaseAsync(guid);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to delete asset: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete an asset and its metadata by path.
    /// </summary>
    /// <param name="assetPath">Path to the asset to delete.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<Result> DeleteAssetAsync(string assetPath)
    {
        var guidResult = PathToGuid(assetPath);
        if (guidResult.IsFailure)
        {
            return Result.Failure(guidResult.Message);
        }

        return await DeleteAssetAsync(guidResult.Value);
    }

    /// <summary>
    /// Move an asset to a new location.
    /// </summary>
    /// <param name="guid">GUID of the asset to move.</param>
    /// <param name="newPath">New path for the asset (relative or absolute).</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<Result> MoveAssetAsync(Guid guid, string newPath)
    {
        var oldPathResult = GuidToPath(guid);
        if (oldPathResult.IsFailure)
        {
            return Result.Failure(oldPathResult.Message);
        }

        var oldFullPathResult = GetFullPath(oldPathResult.Value);
        if (oldFullPathResult.IsFailure)
        {
            return Result.Failure(oldFullPathResult.Message);
        }

        if (AssetsDirectory == null)
        {
            return Result.Failure("AssetsDirectory not initialized");
        }

        // Ensure new path is absolute and within assets directory
        if (!Path.IsPathRooted(newPath))
        {
            newPath = Path.Combine(AssetsDirectory.FullName, newPath);
        }

        if (!newPath.StartsWith(AssetsDirectory.FullName, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure("New path must be within the Assets directory");
        }

        if (File.Exists(newPath))
        {
            return Result.Failure("A file already exists at the new path");
        }

        try
        {
            var directory = Path.GetDirectoryName(newPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Read metadata and calculate hash before moving
            var metaResult = await ReadMetaFileAsync(oldFullPathResult.Value);
            if (metaResult.IsFailure)
            {
                return Result.Failure(metaResult.Message);
            }

            var fileHash = await CalculateFileHashAsync(oldFullPathResult.Value);

            // Temporarily disable file watcher to prevent race conditions
            var watcherWasEnabled = s_watcher?.EnableRaisingEvents ?? false;
            if (s_watcher != null)
            {
                s_watcher.EnableRaisingEvents = false;
            }

            try
            {
                // Move the asset file
                File.Move(oldFullPathResult.Value, newPath);

                // Move the .gmeta file
                var oldMetaPath = oldFullPathResult.Value + Utilities.FileExtensions.META_FILE_EXTENSION;
                var newMetaPath = newPath + Utilities.FileExtensions.META_FILE_EXTENSION;
                if (File.Exists(oldMetaPath))
                {
                    File.Move(oldMetaPath, newMetaPath);
                }

                // Update database with new path (hash remains the same since content didn't change)
                await UpsertAssetAsync(newPath, metaResult.Value, fileHash);
            }
            finally
            {
                // Re-enable file watcher
                if (s_watcher != null && watcherWasEnabled)
                {
                    s_watcher.EnableRaisingEvents = true;
                }
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to move asset: {ex.Message}");
        }
    }

    /// <summary>
    /// Move an asset to a new location by path.
    /// </summary>
    /// <param name="oldPath">Current path of the asset.</param>
    /// <param name="newPath">New path for the asset (relative or absolute).</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<Result> MoveAssetAsync(string oldPath, string newPath)
    {
        var guidResult = PathToGuid(oldPath);
        if (guidResult.IsFailure)
        {
            return Result.Failure(guidResult.Message);
        }

        return await MoveAssetAsync(guidResult.Value, newPath);
    }

    /// <summary>
    /// Copy an asset to a new location with a new GUID.
    /// </summary>
    /// <param name="guid">GUID of the asset to copy.</param>
    /// <param name="newPath">New path for the copied asset (relative or absolute).</param>
    /// <returns>Result containing the new asset's GUID.</returns>
    public static async Task<Result<Guid>> CopyAssetAsync(Guid guid, string newPath)
    {
        var oldPathResult = GuidToPath(guid);
        if (oldPathResult.IsFailure)
        {
            return Result<Guid>.Failure(oldPathResult.Message);
        }

        var oldFullPathResult = GetFullPath(oldPathResult.Value);
        if (oldFullPathResult.IsFailure)
        {
            return Result<Guid>.Failure(oldFullPathResult.Message);
        }

        if (AssetsDirectory == null)
        {
            return Result<Guid>.Failure("AssetsDirectory not initialized");
        }

        // Ensure new path is absolute and within assets directory
        if (!Path.IsPathRooted(newPath))
        {
            newPath = Path.Combine(AssetsDirectory.FullName, newPath);
        }

        if (!newPath.StartsWith(AssetsDirectory.FullName, StringComparison.OrdinalIgnoreCase))
        {
            return Result<Guid>.Failure("New path must be within the Assets directory");
        }

        if (File.Exists(newPath))
        {
            return Result<Guid>.Failure("A file already exists at the new path");
        }

        try
        {
            var directory = Path.GetDirectoryName(newPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(oldFullPathResult.Value, newPath);

            // Generate new metadata with new GUID
            await GenerateMetaFileAsync(newPath);

            // Get the new GUID
            var newGuidResult = PathToGuid(newPath);
            if (newGuidResult.IsFailure)
            {
                return Result<Guid>.Failure(newGuidResult.Message);
            }

            return newGuidResult.Value;
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"Failed to copy asset: {ex.Message}");
        }
    }

    /// <summary>
    /// Copy an asset to a new location by path.
    /// </summary>
    /// <param name="sourcePath">Path of the asset to copy.</param>
    /// <param name="destPath">New path for the copied asset (relative or absolute).</param>
    /// <returns>Result containing the new asset's GUID.</returns>
    public static async Task<Result<Guid>> CopyAssetAsync(string sourcePath, string destPath)
    {
        var guidResult = PathToGuid(sourcePath);
        if (guidResult.IsFailure)
        {
            return Result<Guid>.Failure(guidResult.Message);
        }

        return await CopyAssetAsync(guidResult.Value, destPath);
    }

    /// <summary>
    /// Mark an asset as dirty for re-importing.
    /// </summary>
    /// <param name="guid">GUID of the asset to mark dirty.</param>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<Result> MarkDirtyAsync(Guid guid)
    {
        return await MarkAssetDirtyAsync(guid, true);
    }

    /// <summary>
    /// Import all dirty assets.
    /// </summary>
    /// <returns>Result indicating success or failure.</returns>
    public static async Task<Result> ImportDirtyAssetsAsync()
    {
        var dirtyAssets = await GetDirtyAssetsAsync();

        foreach (var (guid, path) in dirtyAssets)
        {
            var fullPathResult = GetFullPath(path);
            if (fullPathResult.IsFailure)
            {
                continue;
            }

            var result = await ImportAssetAsync(fullPathResult.Value);
            if (result.IsSuccess)
            {
                await MarkAssetDirtyAsync(guid, false);
            }
        }

        return Result.Success();
    }
}
