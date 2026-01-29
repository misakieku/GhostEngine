using Ghost.Core;
using System.Reflection;

namespace Ghost.Editor.Core.AssetHandle;

public static partial class AssetDatabase
{
    private static readonly Dictionary<Type, AssetImporter> s_importerInstances = new();

    /// <summary>
    /// Import an asset at the specified path.
    /// </summary>
    /// <param name="assetPath">Full path to the asset file.</param>
    /// <returns>Result indicating success or failure.</returns>
    private static async ValueTask<Result> ImportAssetAsync(string assetPath, CancellationToken token = default)
    {
        var extension = Path.GetExtension(assetPath);

        if (!s_importerTypeLookup.TryGetValue(extension, out var importerType))
        {
            // No importer registered for this file type
            return Result.Success();
        }

        // Get or create importer instance
        if (!s_importerInstances.TryGetValue(importerType, out var importerInstance))
        {
            importerInstance = Activator.CreateInstance(importerType) as AssetImporter;
            if (importerInstance is null)
            {
                return Result.Failure($"Failed to create importer instance for type {importerType.Name}");
            }

            s_importerInstances[importerType] = importerInstance;
        }

        // Read metadata
        var metaResult = await ReadMetaFileAsync(assetPath, token);
        if (metaResult.IsFailure)
        {
            return Result.Failure($"Failed to read asset metadata: {metaResult.Message}");
        }

        return await importerInstance.ImportAsync(assetPath, metaResult.Value, token);
    }

    /// <summary>
    /// Get the importer type for a specific file extension.
    /// </summary>
    /// <param name="extension">File extension (e.g., ".png").</param>
    /// <returns>The importer type if found, otherwise null.</returns>
    public static Type? GetImporterType(string extension)
    {
        s_importerTypeLookup.TryGetValue(extension, out var importerType);
        return importerType;
    }

    /// <summary>
    /// Get all registered importer types and their supported extensions.
    /// </summary>
    /// <returns>Dictionary mapping extensions to importer types.</returns>
    public static Dictionary<string, Type> GetAllImporters()
    {
        return new Dictionary<string, Type>(s_importerTypeLookup);
    }

    /// <summary>
    /// Export in-memory asset data to disk.
    /// The importer will serialize the data into a format it can later import.
    /// </summary>
    /// <typeparam name="T">Type of asset data to export.</typeparam>
    /// <param name="assetPath">Full path where the asset should be saved.</param>
    /// <param name="assetData">In-memory asset data to export.</param>
    /// <returns>Result with the GUID of the exported asset.</returns>
    public static async ValueTask<Result<Guid>> ExportAssetAsync<T>(string assetPath, T assetData, CancellationToken token = default) where T : class
    {
        var extension = Path.GetExtension(assetPath);

        if (!s_importerTypeLookup.TryGetValue(extension, out var importerType))
        {
            return Result<Guid>.Failure($"No importer registered for extension {extension}");
        }

        // Get or create importer instance
        if (!s_importerInstances.TryGetValue(importerType, out var importerInstance))
        {
            importerInstance = Activator.CreateInstance(importerType) as AssetImporter;
            if (importerInstance is null)
            {
                return Result<Guid>.Failure($"Failed to create importer instance for type {importerType.Name}");
            }

            s_importerInstances[importerType] = importerInstance;
        }

        // Find and invoke the ExportAsync method
        var exportMethod = importerType.GetMethod("ExportAsync", BindingFlags.Public | BindingFlags.Instance);
        if (exportMethod == null)
        {
            return Result<Guid>.Failure($"ExportAsync method not found on importer {importerType.Name}. This importer does not support exporting.");
        }

        // Generate metadata for the new asset
        var result = await GenerateMetaFileAsync(assetPath, token);
        if (result.IsFailure)
        {
            return Result<Guid>.Failure($"Failed to generate metadata: {result.Message}");
        }

        var metaResult = await ReadMetaFileAsync(assetPath, token);
        if (metaResult.IsFailure)
        {
            return Result<Guid>.Failure($"Failed to read metadata: {metaResult.Message}");
        }

        result = await importerInstance.ExportAsync(assetPath, assetData, metaResult.Value, token);
        if (result.IsFailure)
        {
            return Result<Guid>.Failure(result.Message);
        }

        // Calculate file hash and update database
        var fileHash = await CalculateFileHashAsync(assetPath, token);
        await UpsertAssetAsync(assetPath, metaResult.Value, fileHash, null, token);

        return metaResult.Value.Guid;
    }
}
