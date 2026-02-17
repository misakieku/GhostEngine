using Ghost.Core;
using Ghost.Editor.Core.AssetHandler;

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
    string? GetAssetPath(Guid id);
    Guid GetAssetGuid(string assetPath);

    ValueTask<Result<Guid>> ImportAssetAsync(string sourceFilePath, string targetAssetPath, CancellationToken token = default);
    ValueTask<Result> ReimportAssetAsync(Guid assetId, string sourceFilePath, CancellationToken token = default);
    ValueTask<Result<Asset>> LoadAssetAsync(Guid id, CancellationToken token = default);
    ValueTask<Result> SaveAssetAsync(Asset asset, CancellationToken token = default);
}
