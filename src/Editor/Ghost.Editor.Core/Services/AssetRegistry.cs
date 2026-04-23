using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Editor.Core.AssetHandler;
using Ghost.Editor.Core.Contracts;
using System.Collections.Concurrent;
using System.IO.MemoryMappedFiles;

namespace Ghost.Editor.Core.Services;

/// <summary>
/// Central asset registry for the GhostEngine editor.
/// </summary>
internal sealed class AssetRegistry : IAssetRegistry, IDisposable
{
    private readonly AssetCatalog _catalog;
    private readonly ImportCoordinator _importCoordinator;
    private readonly FileSystemWatcher _watcher;

    private readonly ConcurrentDictionary<Guid, WeakReference<IAsset>> _loadedAssets;
    private readonly SemaphoreSlim _loadLock;

    private readonly ConcurrentDictionary<string, bool> _ignoreMetaWrites;
    private readonly ConcurrentHashSet<Guid> _dirtyAssets;

    public event EventHandler<AssetChangedEventArgs>? OnAssetChanged;
    public event EventHandler<Guid>? OnAssetImported
    {
        add => _importCoordinator.OnImportCompleted += value;
        remove => _importCoordinator.OnImportCompleted -= value;
    }

    public AssetRegistry()
    {
        var dbPath = Path.Combine(EditorApplication.LibraryFolderPath, "AssetDB.sqlite");

        _catalog = new AssetCatalog(dbPath);
        _importCoordinator = new ImportCoordinator(_catalog);

        _loadedAssets = new ConcurrentDictionary<Guid, WeakReference<IAsset>>();
        _loadLock = new SemaphoreSlim(1, 1);

        _ignoreMetaWrites = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        _dirtyAssets = new ConcurrentHashSet<Guid>();

        SyncCatalogWithDisk();

        _watcher = new FileSystemWatcher(EditorApplication.AssetsFolderPath)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
        };

        _watcher.Created += OnFileSystemEvent;
        _watcher.Deleted += OnFileSystemEvent;
        _watcher.Changed += OnFileSystemEvent;
        _watcher.Renamed += OnFileSystemRenameEvent;
    }

    private void SyncCatalogWithDisk()
    {
        if (!Directory.Exists(EditorApplication.AssetsFolderPath))
        {
            return;
        }

        var metaFiles = Directory.EnumerateFiles(EditorApplication.AssetsFolderPath, "*.gmeta", SearchOption.AllDirectories);
        var foundGuids = new HashSet<Guid>();

        foreach (var metaPath in metaFiles)
        {
            var meta = AssetMetaIO.ReadAsync(metaPath).AsTask().Result;
            if (meta != null)
            {
                var sourceRelative = AssetMetaIO.GetSourcePath(Path.GetRelativePath(EditorApplication.AssetsFolderPath, metaPath));
                _catalog.Upsert(meta, sourceRelative);
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
        var relativePath = Path.GetRelativePath(EditorApplication.AssetsFolderPath, e.FullPath);

        if (_ignoreMetaWrites.TryRemove(e.FullPath, out _))
        {
            return;
        }

        if (ext is ".tmp" or ".gtemp")
        {
            return;
        }

        if (ext == AssetMetaIO.META_EXTENSION)
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

        var changeType = AssetChangeType.None;
        if (e.ChangeType == WatcherChangeTypes.Created)
        {
            await HandleNewSourceFileAsync(relativePath);
            changeType = AssetChangeType.Created;
        }
        else if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            var guid = _catalog.GetGuid(relativePath);
            if (guid != Guid.Empty)
            {
                await _importCoordinator.EnqueueAsync(new ImportJob(guid, relativePath, AssetMetaIO.GetMetaPath(e.FullPath), ImportReason.SourceChanged));
                changeType = AssetChangeType.Modified;
            }
        }
        else if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            var guid = _catalog.GetGuid(relativePath);
            if (guid != Guid.Empty)
            {
                _catalog.Remove(guid);
                changeType = AssetChangeType.Deleted;
            }
        }

        if (changeType != AssetChangeType.None)
        {
            OnAssetChanged?.Invoke(this, new AssetChangedEventArgs(relativePath, null, changeType));
        }
    }

    private void OnFileSystemRenameEvent(object sender, RenamedEventArgs e)
    {
        var oldRelative = Path.GetRelativePath(EditorApplication.AssetsFolderPath, e.OldFullPath);
        var newRelative = Path.GetRelativePath(EditorApplication.AssetsFolderPath, e.FullPath);

        var guid = _catalog.GetGuid(oldRelative);
        if (guid != Guid.Empty)
        {
            _catalog.Remove(guid);
            var metaFile = AssetMetaIO.GetMetaPath(newRelative);
            if (File.Exists(metaFile))
            {
                var meta = AssetMetaIO.ReadAsync(metaFile).AsTask().Result;
                if (meta != null)
                {
                    _catalog.Upsert(meta, newRelative);
                }
            }
        }

        OnAssetChanged?.Invoke(this, new AssetChangedEventArgs(newRelative, oldRelative, AssetChangeType.Renamed));
    }

    private async Task HandleNewSourceFileAsync(string relativePath)
    {
        var ext = Path.GetExtension(relativePath);
        var handler = AssetHandlerRegistry.GetByExtension(ext);

        var metaPath = AssetMetaIO.GetMetaPath(relativePath);
        if (File.Exists(metaPath))
        {
            return;
        }

        var handlerTypeId = handler?.EditorAssetTypeID;
        var meta = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            HandlerTypeId = handlerTypeId,
            HandlerVersion = 1,
            Settings = handler?.CreateDefaultSettings()
        };

        _ignoreMetaWrites[metaPath] = true;
        await AssetMetaIO.WriteAsync(metaPath, meta);

        _catalog.Upsert(meta, relativePath);

        await _importCoordinator.EnqueueAsync(new ImportJob(meta.Guid, relativePath, metaPath, ImportReason.NewAsset));
    }

    public AssetCatalog GetAssetCatalog()
    {
        return _catalog;
    }

    public string? GetAssetPath(Guid id)
    {
        return _catalog.GetSourcePath(id);
    }

    public Guid GetAssetGuid(string path)
    {
        return _catalog.GetGuid(path);
    }

    public async ValueTask<Result<Guid>> ImportAssetAsync(string sourceFilePath, string targetAssetPath, CancellationToken token = default)
    {
        // Simple copy + wait for FSW or manually trigger?
        // Current requirement: "returns the new GUID immediately (import happens in background)"

        Directory.CreateDirectory(Path.GetDirectoryName(targetAssetPath)!);
        File.Copy(sourceFilePath, targetAssetPath, true);

        // FSW will trigger but we can speed it up
        await HandleNewSourceFileAsync(targetAssetPath);

        var guid = _catalog.GetGuid(targetAssetPath);
        return Result.Success(guid);
    }

    public async ValueTask<Result> ReimportAssetAsync(Guid assetId, string sourceFilePath, CancellationToken token = default)
    {
        var path = _catalog.GetSourcePath(assetId);
        if (path == null)
        {
            return Result.Failure("Asset not found");
        }

        var metaPath = AssetMetaIO.GetMetaPath(path);

        await _importCoordinator.EnqueueAsync(new ImportJob(assetId, path, metaPath, ImportReason.ManualReimport), token);
        return Result.Success();
    }

    public async ValueTask<Result<IAsset>> LoadAssetAsync(Guid id, CancellationToken token = default)
    {
        if (_loadedAssets.TryGetValue(id, out var weakRef) && weakRef.TryGetTarget(out var asset))
        {
            return Result.Success(asset);
        }

        await _loadLock.WaitAsync(token);
        try
        {
            if (_loadedAssets.TryGetValue(id, out weakRef) && weakRef.TryGetTarget(out asset))
            {
                return Result.Success(asset);
            }

            var path = GetAssetPath(id);
            if (!File.Exists(path))
            {
                return Result.Failure("Asset does not exist.");
            }

            var handler = AssetHandlerRegistry.GetByExtension(Path.GetExtension(path));
            if (handler is null)
            {
                return Result.Failure("No Available handler type.");
            }

            var meta = await AssetMetaIO.ReadAsync(AssetMetaIO.GetMetaPath(path), token);
            if (meta is null)
            {
                return Result.Failure("Meta file does not exist.");
            }

            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await handler.LoadAssetAsync(stream, id, meta.Settings, token);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async ValueTask<Result> SaveAssetAsync(IAsset asset, CancellationToken token = default)
    {
        try
        {
            var path = GetAssetPath(asset.ID);
            if (!File.Exists(path))
            {
                return Result.Failure("Asset does not exist.");
            }

            var handler = AssetHandlerRegistry.GetByAssetTypeId(asset.TypeID);
            if (handler is null)
            {
                return Result.Failure("No Avaliable handler type.");
            }

            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            // This will trigger the fsw and reimport automatically.
            return await handler.SaveAssetAsync(stream, asset, token);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public async ValueTask<Result> SaveAssetAsync(Guid id, CancellationToken token = default)
    {
        var result = await LoadAssetAsync(id, token);
        if (result.IsFailure)
        {
            return result;
        }

        return await SaveAssetAsync(result.Value, token);
    }

    public void SetAssetDirty(Guid id)
    {
        _dirtyAssets.Add(id);
    }

    public async ValueTask<Result> SaveAssetIfDirtyAsync(IAsset asset, CancellationToken token = default)
    {
        if (_dirtyAssets.Contains(asset.ID))
        {
            var result = await SaveAssetAsync(asset, token);
            _dirtyAssets.Remove(asset.ID);

            return result;
        }

        return Result.Success();
    }

    public async ValueTask<Result> SaveAssetIfDirtyAsync(Guid id, CancellationToken token = default)
    {
        var result = await LoadAssetAsync(id, token);
        if (result.IsFailure)
        {
            return result;
        }

        return await SaveAssetIfDirtyAsync(result.Value, token);
    }

    public async ValueTask<Result[]> SaveDirtyAssetsAsync()
    {
        if (_dirtyAssets.IsEmpty)
        {
            return Array.Empty<Result>();
        }

        var tasks = new Task<Result>[_dirtyAssets.Count];

        var i = 0;
        foreach (var id in _dirtyAssets)
        {
            tasks[i++] = SaveAssetIfDirtyAsync(id).AsTask();
        }

        _dirtyAssets.Clear();
        return await Task.WhenAll(tasks);
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _importCoordinator.Dispose();
        _catalog.Dispose();
        _loadLock.Dispose();
    }
}
