using Ghost.Core;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Contracts;
using Ghost.Engine;

namespace Ghost.Editor.Core.Services;

internal class EditorContentProvider : IContentProvider
{
    private readonly AssetCatalog _catalog;

    public EditorContentProvider(IAssetRegistry assetRegistry)
    {
        _catalog = assetRegistry.GetAssetCatalog();
    }

    public bool HasAsset(Guid guid)
    {
        return _catalog.GetSourcePath(guid) != null;
    }

    public Result<Stream> OpenRead(Guid guid, CancellationToken token = default)
    {
        var importedPath = ImportCoordinator.GetImportedAssetPath(guid);
        if (!File.Exists(importedPath))
        {
            return Result.Failure($"Imported asset not found for GUID: {guid}");
        }

        return new FileStream(importedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public Guid[] GetDependencies(Guid guid)
    {
        return _catalog.GetDependencies(guid).ToArray();
    }

    public AssetType GetAssetType(Guid guid)
    {
        var assetTypeID = _catalog.GetAssetTypeId(guid);
        if (AssetHandlerRegistry.TryGetHandlerInfoByAssetTypeId(assetTypeID, out var info))
        {
            return info.RuntimeAssetType;
        }

        return AssetType.Unknown;
    }
}
