using System.Collections.Concurrent;
using System.Reflection;
using Ghost.Core;
using Ghost.Editor.Core.AssetHandler;
using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.Core.Services;

/// <summary>
/// Central asset registry for the GhostEngine editor.
/// </summary>
internal sealed class AssetRegistry : IAssetRegistry, IDisposable
{
    private readonly string _assetsRoot;
    private readonly string _libraryRoot;
    private readonly AssetCatalog _catalog;
    private readonly AssetHandlerRegistry _handlerRegistry;
    private readonly ImportCoordinator _importCoordinator;
    private readonly FileSystemWatcher _watcher;

    private readonly ConcurrentDictionary<Guid, WeakReference<Asset>> _loadedAssets;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly ConcurrentDictionary<string, bool> _ignoreMetaWrites = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<IAssetRegistry, AssetChangedEventArgs>? OnAssetChanged;

    public AssetRegistry(string assetsRoot)
    {
        _assetsRoot = Path.GetFullPath(assetsRoot);
        _libraryRoot = Path.Combine(Path.GetDirectoryName(_assetsRoot)!, EditorApplication.LIBRARY_FOLDER_NAME);

        // TODO: This should be handled by EditorApplication.
        Directory.CreateDirectory(_assetsRoot);
        Directory.CreateDirectory(_libraryRoot);

        var dbPath = Path.Combine(_libraryRoot, "AssetDB.sqlite");

        _catalog = new AssetCatalog(dbPath);
        _handlerRegistry = new AssetHandlerRegistry();
        _importCoordinator = new ImportCoordinator(_catalog, _handlerRegistry, _assetsRoot, _libraryRoot);

        _loadedAssets = new ConcurrentDictionary<Guid, WeakReference<Asset>>();

        SyncCatalogWithDisk();

        _watcher = new FileSystemWatcher(_assetsRoot)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
        };

        _watcher.Created += OnFileSystemEvent;
        _watcher.Deleted += OnFileSystemEvent;
        _watcher.Changed += OnFileSystemEvent;
        _watcher.Renamed += OnFileSystemRenameEvent;

        _importCoordinator.EnqueueDirtyAssetsAsync().AsTask().Wait();
    }

    private void SyncCatalogWithDisk()
    {
        if (!Directory.Exists(_assetsRoot))
        {
            return;
        }

        var metaFiles = Directory.EnumerateFiles(_assetsRoot, "*.gmeta", SearchOption.AllDirectories);
        var foundGuids = new HashSet<Guid>();

        foreach (var metaPath in metaFiles)
        {
            var meta = AssetMetaIO.ReadAsync(metaPath).AsTask().Result;
            if (meta != null)
            {
                var sourceRelative = AssetMetaIO.GetSourcePath(Path.GetRelativePath(_assetsRoot, metaPath));
                _catalog.Upsert(meta, sourceRelative.Replace('\\', '/'));
                foundGuids.Add(meta.Guid);
            }
        }

        foreach (var (guid, path) in _catalog.EnumerateAll())
        {
            if (!foundGuids.Contains(guid))
            {
                _catalog.Remove(guid);
            }
        }
    }

    private async void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        var ext = Path.GetExtension(e.FullPath);
        var relativePath = Path.GetRelativePath(_assetsRoot, e.FullPath).Replace('\\', '/');

        if (_ignoreMetaWrites.TryRemove(e.FullPath, out _))
        {
            return;
        }

        if (ext is ".tmp" or ".gtemp")
        {
            return;
        }

        if (ext == ".gmeta")
        {
            if (e.ChangeType == WatcherChangeTypes.Changed || e.ChangeType == WatcherChangeTypes.Created)
            {
                var meta = AssetMetaIO.ReadAsync(e.FullPath).AsTask().Result;
                if (meta != null)
                {
                    _catalog.Upsert(meta, AssetMetaIO.GetSourcePath(relativePath));
                    await _importCoordinator.EnqueueAsync(new ImportJob(meta.Guid, AssetMetaIO.GetSourcePath(relativePath), e.FullPath, ImportReason.SettingsChanged));
                }
            }
            return;
        }

        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            await HandleNewSourceFileAsync(e.FullPath, relativePath);
        }
        else if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            var guid = _catalog.GetGuid(relativePath);
            if (guid != Guid.Empty)
            {
                await _importCoordinator.EnqueueAsync(new ImportJob(guid, relativePath, AssetMetaIO.GetMetaPath(e.FullPath), ImportReason.SourceChanged));
            }
        }
    }

    private void OnFileSystemRenameEvent(object sender, RenamedEventArgs e)
    {
        var oldRelative = Path.GetRelativePath(_assetsRoot, e.OldFullPath).Replace('\\', '/');
        var newRelative = Path.GetRelativePath(_assetsRoot, e.FullPath).Replace('\\', '/');

        var guid = _catalog.GetGuid(oldRelative);
        if (guid != Guid.Empty)
        {
            _catalog.Remove(guid);
            var metaFile = AssetMetaIO.GetMetaPath(e.FullPath);
            if (File.Exists(metaFile))
            {
                var meta = AssetMetaIO.ReadAsync(metaFile).AsTask().Result;
                if (meta != null)
                {
                    _catalog.Upsert(meta, newRelative);
                }
            }
        }
    }

    private async Task HandleNewSourceFileAsync(string fullPath, string relativePath)
    {
        var ext = Path.GetExtension(relativePath);

        var handler = _handlerRegistry.GetByExtension(ext);
        var importable = handler as IImportableAssetHandler;

        var metaPath = AssetMetaIO.GetMetaPath(fullPath);
        if (File.Exists(metaPath))
        {
            return;
        }

        var handlerTypeId = handler?.GetType().GetCustomAttributesData().FirstOrDefault(d => d.AttributeType == typeof(CustomAssetHandlerAttribute))?.ConstructorArguments[0].Value;
        var meta = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            HandlerTypeId = handlerTypeId is string str? Guid.Parse(str) : null,
            HandlerVersion = 1,
            Settings = importable?.CreateDefaultSettings()
        };

        _ignoreMetaWrites[metaPath] = true;
        await AssetMetaIO.WriteAsync(metaPath, meta);

        _catalog.Upsert(meta, relativePath);

        await _importCoordinator.EnqueueAsync(new ImportJob(meta.Guid, relativePath, metaPath, ImportReason.NewAsset));
    }

    public string? GetAssetPath(Guid id)
    {
        return _catalog.GetSourcePath(id);
    }

    public Guid GetAssetGuid(string path)
    {
        return _catalog.GetGuid(path.Replace(Path.DirectorySeparatorChar, '/'));
    }

    public async ValueTask<Result<Guid>> ImportAssetAsync(string sourceFilePath, string targetAssetPath, CancellationToken token = default)
    {
        // Simple copy + wait for FSW or manually trigger?
        // Current requirement: "returns the new GUID immediately (import happens in background)"

        var ext = Path.GetExtension(sourceFilePath);
        var relativePath = targetAssetPath.Replace(Path.DirectorySeparatorChar, '/');
        var fullPath = Path.Combine(_assetsRoot, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.Copy(sourceFilePath, fullPath, true);

        // FSW will trigger but we can speed it up
        await HandleNewSourceFileAsync(fullPath, relativePath);

        var guid = _catalog.GetGuid(relativePath);
        return Result.Success(guid);
    }

    public async ValueTask<Result> ReimportAssetAsync(Guid assetId, string sourceFilePath, CancellationToken token = default)
    {
        var path = _catalog.GetSourcePath(assetId);
        if (path == null)
        {
            return Result.Failure("Asset not found");
        }

        var fullPath = Path.Combine(_assetsRoot, path);
        var metaPath = AssetMetaIO.GetMetaPath(fullPath);

        await _importCoordinator.EnqueueAsync(new ImportJob(assetId, path, metaPath, ImportReason.ManualReimport), token);
        return Result.Success();
    }

    public async ValueTask<Result<Asset>> LoadAssetAsync(Guid id, CancellationToken token = default)
    {
        if (_loadedAssets.TryGetValue(id, out var weakRef) && weakRef.TryGetTarget(out var asset))
        {
            return asset;
        }

        await _loadLock.WaitAsync(token);
        try
        {
            if (_loadedAssets.TryGetValue(id, out weakRef) && weakRef.TryGetTarget(out asset))
            {
                return asset;
            }

            var importedPath = Path.Combine(_libraryRoot, "Imports", $"{id:N}.imported");
            if (!File.Exists(importedPath))
            {
                return Result.Failure<Asset>("Asset not imported");
            }

            // For now, we use a basic LoadAsync implementation.
            // In a better design, we'd read the handler ID from the header.
            // Here we we assume the catalog is correct (it's synced with gmeta).

            // Looking up TypeId from catalog isn't implemented in AssetCatalog yet. 
            // We should add it or use the header.
            // The existing Asset class might still be tied to the old binary format.

            return Result.Failure<Asset>("Full asset loading would require updating all assets to the new format first.");
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public ValueTask<Result> SaveAssetAsync(Asset asset, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _importCoordinator.Dispose();
        _catalog.Dispose();
        _loadLock.Dispose();
    }
}
