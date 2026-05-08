using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Contracts;
using System.Collections.Concurrent;

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
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _eventDebouncers;

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
        _eventDebouncers = new ConcurrentDictionary<string, CancellationTokenSource>();

        SyncCatalogWithDisk();

        _watcher = new FileSystemWatcher(EditorApplication.AssetsFolderPath)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
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
                var sourceRelative = AssetMetaIO.GetSourcePath(Path.GetRelativePath(EditorApplication.ProjectPath, metaPath));
                _catalog.Upsert(meta, sourceRelative);
                foundGuids.Add(meta.Guid);
            }
        }

        foreach (var (guid, path) in _catalog.EnumerateAll())
        {
            if (path.Contains('#', StringComparison.Ordinal))
            {
                continue;
            }

            if (!foundGuids.Contains(guid))
            {
                _catalog.Remove(guid);
            }
        }
    }

    private async void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        var ext = Path.GetExtension(e.FullPath);

        if (ext is ".tmp" or ".gtemp")
        {
            return;
        }

        if (_eventDebouncers.TryGetValue(e.FullPath, out var existingCts))
        {
            existingCts.Cancel();
            existingCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _eventDebouncers[e.FullPath] = cts;

        try
        {
            // Add a small delay to group rapid sequential triggers together (250ms is usually sufficient)
            await Task.Delay(250, cts.Token);
        }
        catch (TaskCanceledException)
        {
            // A newer event for this file interrupted us; abort this duplicate handling
            return;
        }
        finally
        {
            if (_eventDebouncers.TryGetValue(e.FullPath, out var currentCts) && currentCts == cts)
            {
                _eventDebouncers.TryRemove(e.FullPath, out _);
                cts.Dispose();
            }
        }

        if (_ignoreMetaWrites.TryRemove(e.FullPath, out _))
        {
            return;
        }

        try
        {
            var relativePath = Path.GetRelativePath(EditorApplication.ProjectPath, e.FullPath);
            var fileExists = File.Exists(e.FullPath);

            if (ext == AssetMetaIO.META_EXTENSION)
            {
                if (fileExists)
                {
                    var meta = await AssetMetaIO.ReadAsync(e.FullPath);
                    if (meta != null)
                    {
                        _catalog.Upsert(meta, AssetMetaIO.GetSourcePath(relativePath));
                        await _importCoordinator.EnqueueAsync(new ImportJob(meta.Guid, AssetMetaIO.GetSourcePath(relativePath), relativePath, ImportReason.SettingsChanged));
                    }
                }
                return;
            }

            var changeType = AssetChangeType.None;
            var guid = _catalog.GetGuid(relativePath);

            if (!fileExists)
            {
                // The file is no longer on disk. Wait safely completed.
                if (guid != Guid.Empty)
                {
                    _catalog.Remove(guid);
                    changeType = AssetChangeType.Deleted;
                }

                Logger.DebugAssert(e.ChangeType == WatcherChangeTypes.Deleted);
            }
            else if (guid == Guid.Empty)
            {
                // The file exists but isn't located inside our catalog yet -> Essentially a Creation
                await HandleNewSourceFileAsync(relativePath);
                changeType = AssetChangeType.Created;
            }
            else
            {
                // The file exists and is tracked in the catalog, but triggered an event -> Modification
                await _importCoordinator.EnqueueAsync(new ImportJob(guid, relativePath, AssetMetaIO.GetMetaPath(relativePath), ImportReason.SourceChanged));
                changeType = AssetChangeType.Modified;
            }

            if (changeType != AssetChangeType.None)
            {
                OnAssetChanged?.Invoke(this, new AssetChangedEventArgs(relativePath, null, changeType));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
    }

    private void OnFileSystemRenameEvent(object sender, RenamedEventArgs e)
    {
        var oldRelative = Path.GetRelativePath(EditorApplication.ProjectPath, e.OldFullPath);
        var newRelative = Path.GetRelativePath(EditorApplication.ProjectPath, e.FullPath);

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

        var assetTypeId = Guid.Empty;
        if (AssetHandlerRegistry.TryGetHandlerInfoByExtension(ext, out var handlerInfo))
        {
            assetTypeId = handlerInfo.EditorAssetTypeID;
        }

        var meta = new AssetMeta
        {
            Guid = Guid.NewGuid(),
            AssetTypeId = assetTypeId,
            HandlerVersion = 1,
            Settings = handler?.CreateDefaultSettings(ext)
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

            return await handler.LoadAssetAsync(path, id, meta.Settings, token);
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

            // This will trigger the fsw and reimport automatically.
            return await handler.SaveAssetAsync(path, asset, token);
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
        _loadLock.Dispose();
    }
}
