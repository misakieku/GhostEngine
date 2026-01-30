using Ghost.Core;

namespace Ghost.Editor.Core.AssetHandle;

public abstract class AssetImporter
{
    /// <summary>
    /// Import the asset at the specified path with the given settings.
    /// </summary>
    /// <param name="assetPath">Full path to the source asset file.</param>
    /// <param name="meta">Metadata for the asset.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public abstract ValueTask<Result> ImportAsync(string assetPath, AssetMeta meta, CancellationToken token = default);

    /// <summary>
    /// Export in-memory asset data to disk.
    /// Override this method to support creating assets from code.
    /// </summary>
    /// <typeparam name="T">Type of asset data to export.</typeparam>
    /// <param name="assetPath">Full path where the asset should be saved.</param>
    /// <param name="assetData">In-memory asset data to serialize.</param>
    /// <param name="meta">Metadata for the asset.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    public virtual ValueTask<Result> ExportAsync<T>(string assetPath, T assetData, AssetMeta meta, CancellationToken token = default)
        where T : class
    {
        return ValueTask.FromResult(Result.Failure("This importer does not support exporting assets."));
    }

    /// <summary>
    /// Validate dependencies referenced by this asset.
    /// Dependencies are extracted from asset content during import and stored in the database.
    /// </summary>
    /// <param name="dependencies">List of dependency GUIDs extracted from the asset.</param>
    /// <returns>Result indicating if all dependencies are valid.</returns>
    protected virtual ValueTask<Result> ValidateDependenciesAsync(List<Guid> dependencies, CancellationToken token = default)
    {
        foreach (var dependencyGuid in dependencies)
        {
            var path = AssetDatabase.GuidToPath(dependencyGuid);
            if (path.IsFailure)
            {
                return ValueTask.FromResult(Result.Failure($"Missing dependency: {dependencyGuid}"));
            }

            if (!File.Exists(path.Value))
            {
                return ValueTask.FromResult(Result.Failure($"Dependency file does not exist: {path.Value}"));
            }
        }

        return ValueTask.FromResult(Result.Success());
    }
}

public abstract class AssetImporter<TSettings> : AssetImporter
    where TSettings : ImporterSettings, new()
{
    /// <summary>
    /// Get the settings for this importer from the metadata.
    /// Creates default settings if none exist.
    /// </summary>
    /// <param name="meta">Asset metadata.</param>
    /// <returns>The importer settings.</returns>
    protected TSettings GetSettings(AssetMeta meta)
    {
        var typeName = GetType().Name;
        var settings = meta.GetImporterSettings<TSettings>(typeName);

        if (settings != null)
        {
            return settings;
        }

        var defaultSettings = new TSettings();
        meta.SetImporterSettings(typeName, defaultSettings);
        return defaultSettings;
    }
}
