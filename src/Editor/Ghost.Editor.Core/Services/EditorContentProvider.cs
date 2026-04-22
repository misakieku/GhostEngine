using Ghost.Core;
using Ghost.Editor.Core.AssetHandler;
using Ghost.Engine;

namespace Ghost.Editor.Core.Services;

internal class EditorContentProvider : IContentProvider
{
    private readonly AssetCatalog _catalog;

    public EditorContentProvider(AssetCatalog catalog)
    {
        _catalog = catalog;
    }

    public bool HasAsset(Guid guid)
    {
        return _catalog.GetSourcePath(guid) != null;
    }

    public Result<Stream> OpenRead(Guid guid, CancellationToken token = default)
    {
        var importedPath = Path.Combine(EditorApplication.LibraryImportsFolderPath, $"{guid:N}{ImportCoordinator.IMPORTED_EXTENSION}");
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
        var handlerID = _catalog.GetHandlerTypeId(guid);
        var handler = AssetHandlerRegistry.GetByTypeId(handlerID);
        return handler?.RuntimeAssetType ?? AssetType.Unknown;
    }
}
