using Ghost.Core;
using System.Reflection;

namespace Ghost.Editor.Core.AssetHandle;

public static partial class AssetDatabase
{
    private static readonly Dictionary<Type, object> s_importerInstances = new();

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
            importerInstance = Activator.CreateInstance(importerType);
            if (importerInstance == null)
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

        // TODO: Avoid reflection.
        // Find and invoke the ImportAsync method. Support importers that accept (string, AssetMeta)
        // or (string, AssetMeta, CancellationToken).
        var importMethod = importerType.GetMethod("ImportAsync", BindingFlags.Public | BindingFlags.Instance);
        if (importMethod == null)
        {
            return Result.Failure($"ImportAsync method not found on importer {importerType.Name}");
        }

        try
        {
            var parameters = importMethod.GetParameters();
            object? invokeResult;

            if (parameters.Length == 2)
            {
                invokeResult = importMethod.Invoke(importerInstance, new object[] { assetPath, metaResult.Value });
            }
            else if (parameters.Length == 3 && parameters[2].ParameterType == typeof(CancellationToken))
            {
                invokeResult = importMethod.Invoke(importerInstance, new object[] { assetPath, metaResult.Value, token });
            }
            else
            {
                return Result.Failure($"Unsupported ImportAsync signature on importer {importerType.Name}");
            }

            if (invokeResult is not Task<Result> task)
            {
                return Result.Failure("Importer did not return a valid Task<Result>");
            }

            var result = await task;
            return result;
        }
        catch (Exception ex)
        {
            return Result.Failure($"Asset import failed: {ex.Message}");
        }
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
            importerInstance = Activator.CreateInstance(importerType);
            if (importerInstance == null)
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

        try
        {
            // Generate metadata for the new asset
            await GenerateMetaFileAsync(assetPath, token);
            
            var metaResult = await ReadMetaFileAsync(assetPath, token);
            if (metaResult.IsFailure)
            {
                return Result<Guid>.Failure($"Failed to generate metadata: {metaResult.Message}");
            }

            var parameters = exportMethod.GetParameters();
            object? invokeResult;

            if (parameters.Length == 3)
            {
                invokeResult = exportMethod.Invoke(importerInstance, new object[] { assetPath, assetData, metaResult.Value });
            }
            else if (parameters.Length == 4 && parameters[3].ParameterType == typeof(CancellationToken))
            {
                invokeResult = exportMethod.Invoke(importerInstance, new object[] { assetPath, assetData, metaResult.Value, token });
            }
            else
            {
                return Result<Guid>.Failure($"Unsupported ExportAsync signature on importer {importerType.Name}");
            }

            if (invokeResult is not Task<Result> task)
            {
                return Result<Guid>.Failure("Exporter did not return a valid Task<Result>");
            }

            var result = await task;
            if (result.IsFailure)
            {
                return Result<Guid>.Failure(result.Message);
            }

            // Calculate file hash and update database
            var fileHash = await CalculateFileHashAsync(assetPath, token);
            await UpsertAssetAsync(assetPath, metaResult.Value, fileHash, null, token);

            return metaResult.Value.Guid;
        }
        catch (Exception ex)
        {
            return Result<Guid>.Failure($"Asset export failed: {ex.Message}");
        }
    }
}
