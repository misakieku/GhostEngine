using Ghost.Core;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Services;

namespace Ghost.Editor.Core.Contracts;

public enum AssetChangeType
{
    None = 0,
    Created,
    Deleted,
    Modified,
    Renamed,
}

public sealed class AssetChangedEventArgs : EventArgs
{
    public string AssetPath
    {
        get;
    }

    public string? OldAssetPath
    {
        get;
    }

    public AssetChangeType ChangeType
    {
        get;
    }

    internal AssetChangedEventArgs(string assetPath, string? oldAssetPath, AssetChangeType changeType)
    {
        AssetPath = assetPath;
        OldAssetPath = oldAssetPath;
        ChangeType = changeType;
    }
}

public interface IAssetRegistry : IDisposable
{
    event EventHandler<AssetChangedEventArgs>? OnAssetChanged;
    event EventHandler<Guid>? OnAssetImported;

    AssetCatalog GetAssetCatalog();

    string? GetAssetPath(Guid id);
    Guid GetAssetGuid(string assetPath);

    ValueTask<Result<Guid>> ImportAssetAsync(string sourceFilePath, string targetAssetPath, CancellationToken token = default);
    ValueTask<Result> ReimportAssetAsync(Guid assetId, string sourceFilePath, CancellationToken token = default);
    ValueTask<Result<Asset>> LoadAssetAsync(Guid id, CancellationToken token = default);
    ValueTask<Result> SaveAssetAsync(Asset asset, CancellationToken token = default);
    ValueTask<Result> SaveAssetAsync(Guid id, CancellationToken token = default);

    void SetAssetDirty(Guid id);
    ValueTask<Result> SaveAssetIfDirtyAsync(Asset asset, CancellationToken token = default);
    ValueTask<Result> SaveAssetIfDirtyAsync(Guid id, CancellationToken token = default);
    ValueTask<Result[]> SaveDirtyAssetsAsync();

    Task<Result> OpenAssetAsync(Guid id);
    Task<Result> OpenAssetAsync(string assetPath);
}
