using Ghost.Core;
using Ghost.Editor.Core.AssetHandle;

namespace Ghost.Editor.Core.Contracts;

public interface IAssetService
{
    DirectoryInfo? AssetsDirectory { get; }

    // Lifecycle
    Task<Result> RefreshAsync(CancellationToken token = default);

    // Dirty tracking
    void MarkDirty(Guid assetGuid);
    bool IsDirty(Guid assetGuid);
    Guid[] GetDirtyAssets();
    void ClearDirty(Guid assetGuid);
    void ClearAllDirty();
    void SetAutoRefresh(bool enabled);

    // Path <-> GUID lookup
    Result<Guid> PathToGuid(string assetPath);
    Result<string> GuidToPath(Guid guid);

    // Asset loading
    Result<T> LoadAsset<T>(Guid guid) where T : Asset;
    Result<T> LoadAssetAtPath<T>(string assetPath) where T : Asset;
    void UnloadAsset(Guid guid);
    void UnloadAllAssets();
    bool IsAssetLoaded(Guid guid);
    (int currentSize, int maxSize) GetCacheStats();
    Result SaveImportedAsset<T>(Guid guid, T assetData) where T : Asset;

    // Asset tags
    ValueTask<Result<List<string>>> GetAssetTagsAsync(Guid guid, CancellationToken token = default);
    ValueTask<Result> SetAssetTagsAsync(Guid guid, List<string> tags, CancellationToken token = default);

    // Asset search
    Task<List<Guid>> FindAssetsByNameAsync(string namePattern, CancellationToken token = default);
    Task<List<Guid>> FindAssetsByTagAsync(string tag, CancellationToken token = default);
    IReadOnlyDictionary<Guid, string> GetAllAssets();

    // Asset file operations
    ValueTask<Result> CreateAssetAsync(string assetPath, ReadOnlyMemory<byte> content, CancellationToken token = default);
    ValueTask<Result> CreateAssetAsync(string assetPath, CancellationToken token = default);
    ValueTask<Result> DeleteAssetAsync(Guid guid, CancellationToken token = default);
    ValueTask<Result> DeleteAssetAsync(string assetPath, CancellationToken token = default);
    ValueTask<Result> MoveAssetAsync(Guid guid, string newPath, CancellationToken token = default);
    ValueTask<Result> MoveAssetAsync(string oldPath, string newPath, CancellationToken token = default);
    ValueTask<Result<Guid>> CopyAssetAsync(Guid guid, string newPath, CancellationToken token = default);
    ValueTask<Result<Guid>> CopyAssetAsync(string sourcePath, string destPath, CancellationToken token = default);
    Result MarkDirtyAsync(Guid guid, CancellationToken token = default);
    Task<Result> ImportDirtyAssetsAsync(CancellationToken token = default);

    // Importer management
    Type? GetImporterType(string extension);
    Dictionary<string, Type> GetAllImporters();
    ValueTask<Result<Guid>> ExportAssetAsync<T>(string assetPath, T assetData, CancellationToken token = default) where T : class;

    // Asset opening
    void OpenAsset(string path);
}
