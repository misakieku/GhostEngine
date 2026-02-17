using Ghost.Core;
using Ghost.Editor.Core.AssetHandler;
using Ghost.Editor.Core.Contracts;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestProject.AssetDB;

internal class PathComparer : IEqualityComparer<string>
{
    private static string ToCanonicalPath(string? path)
    {
        return path?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;
    }

    public bool Equals(string? x, string? y)
    {
        return string.Equals(
            ToCanonicalPath(x),
            ToCanonicalPath(y),
            StringComparison.Ordinal);
    }

    public int GetHashCode(string str)
    {
        return ToCanonicalPath(str).GetHashCode(StringComparison.Ordinal);
    }
}

// TODO: Path based locking for multi-threaded access?
// Is it actually necessary since this is mostly used in editor environment where single-threaded access is common (99.999%)?
internal partial class AssetRegistry : IAssetRegistry
{
    public const string ASSET_EXTENSION = ".gasset";
    public const string TEMP_EXTENSION = ".gtemp";

    private readonly string _rootDirectory;
    private readonly FileSystemWatcher _watcher;

    private readonly ConcurrentDictionary<string, Guid> _pathToGuid;
    private readonly ConcurrentDictionary<Guid, string> _guidToPath;

    private readonly ConcurrentDictionary<nint, IAssetHandler> _cachedHander;
    private readonly ConcurrentDictionary<Guid, WeakReference<Asset>> _loadedAssets;

    private readonly Dictionary<Guid, HashSet<Guid>> _referencerGraph;
    private readonly Dictionary<Guid, HashSet<Guid>> _dependencyCache;

    private readonly ConcurrentDictionary<string, bool> _ignoreFileChanges;

    private readonly SemaphoreSlim _cacheSlim;
    private readonly Lock _pathLock;

    public event EventHandler<IAssetRegistry, AssetChangedEventArgs>? OnAssetChanged;

    public AssetRegistry(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
        {
            throw new DirectoryNotFoundException("The specified root directory does not exist.");
        }

        if (!Path.IsPathFullyQualified(rootDirectory))
        {
            throw new InvalidOperationException("The specified root directory must be an absolute path.");
        }

        _rootDirectory = rootDirectory;
        _watcher = new FileSystemWatcher(rootDirectory)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
        };

        _pathToGuid = new ConcurrentDictionary<string, Guid>(4, 512, new PathComparer());
        _guidToPath = new ConcurrentDictionary<Guid, string>(4, 512);
        _cachedHander = new ConcurrentDictionary<nint, IAssetHandler>(4, 16);
        _loadedAssets = new ConcurrentDictionary<Guid, WeakReference<Asset>>(4, 512);

        _referencerGraph = new Dictionary<Guid, HashSet<Guid>>();
        _dependencyCache = new Dictionary<Guid, HashSet<Guid>>();

        _ignoreFileChanges = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        _cacheSlim = new SemaphoreSlim(1, 1);
        _pathLock = new Lock();

        LoadExistingAssets();

        _watcher.Created += OnFileSystemOp;
        _watcher.Deleted += OnFileSystemOp;
        _watcher.Changed += OnFileSystemOp;
        _watcher.Renamed += OnFileSystemRenameOp;
    }

    // TODO: DB Cache
    private unsafe void LoadExistingAssets()
    {
        Span<byte> guidBuffer = stackalloc byte[sizeof(Guid)];
        foreach (var filePath in Directory.EnumerateFiles(_rootDirectory, $"*{ASSET_EXTENSION}", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(_rootDirectory, filePath);

            try
            {
                var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    fs.Seek(4, SeekOrigin.Begin); // Skip format version
                    fs.ReadExactly(guidBuffer);

                    var guid = Unsafe.ReadUnaligned<Guid>(ref MemoryMarshal.GetReference(guidBuffer));
                    UpdatePathMapping(relativePath, guid);
                }
                finally
                {
                    fs.Dispose();
                }
            }
            catch (Exception
#if DEBUG
                    ex
#endif
                    )
            {
#if DEBUG
                System.Diagnostics.Debugger.BreakForUserUnhandledException(ex);
#endif
                continue;
            }
        }
    }

    private void UpdateGraph(Guid assetId, IEnumerable<Guid> newDependencies)
    {
        // 1. Clean up old references (reverse)
        if (_dependencyCache.TryGetValue(assetId, out var oldDeps))
        {
            foreach (var dep in oldDeps)
            {
                if (_referencerGraph.TryGetValue(dep, out var refs))
                {
                    refs.Remove(assetId);
                }
            }
        }

        // 2. Set new forward dependencies
        var newDepSet = new HashSet<Guid>(newDependencies);
        _dependencyCache[assetId] = newDepSet;

        // 3. Add new references (reverse)
        foreach (var dep in newDepSet)
        {
            ref var referencers = ref CollectionsMarshal.GetValueRefOrAddDefault(_referencerGraph, dep, out var exists);
            if (!exists || referencers is null)
            {
                referencers = new HashSet<Guid>();
            }

            referencers.Add(assetId);
        }
    }

    private void UpdatePathMapping(string relativePath, Guid guid)
    {
        lock (_pathLock)
        {
            _pathToGuid[relativePath] = guid;
            _guidToPath[guid] = relativePath;
        }
    }

    private bool RemovePathMappingByPath(string relativePath)
    {
        lock (_pathLock)
        {
            if (_pathToGuid.Remove(relativePath, out var guid))
            {
                return _guidToPath.TryRemove(guid, out _);
            }
        }

        return false;
    }

    private async void OnFileSystemOp(object sender, FileSystemEventArgs e)
    {
        if (_ignoreFileChanges.TryRemove(e.FullPath, out _))
        {
            return;
        }

        var relativePath = Path.GetRelativePath(_rootDirectory, e.FullPath);
        var ext = Path.GetExtension(relativePath);

        var changeType = AssetChangeType.None;
        var fireEvent = false;
        var isAsset = ext.Equals(ASSET_EXTENSION, StringComparison.Ordinal);
        var isTemp = ext.Equals(TEMP_EXTENSION, StringComparison.Ordinal);

        switch (e.ChangeType)
        {
            case WatcherChangeTypes.Created:
                changeType = AssetChangeType.Created;
                if (!isAsset && !isTemp)
                {
                    var handler = GetAssetHandlerForExtension(ext);
                    if (handler is IImportableAssetHandler importableHandler)
                    {
                        var assetPath = string.Create(e.FullPath.Length - ext.Length + ASSET_EXTENSION.Length, e.FullPath, (destSpan, source) =>
                        {
                            source.AsSpan(0, source.Length - ext.Length).CopyTo(destSpan);
                            ASSET_EXTENSION.AsSpan().CopyTo(destSpan.Slice(source.Length - ext.Length));
                        });

                        var newGuid = Guid.NewGuid();
                        await using var sourceStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read);
                        await using var targetStream = new FileStream(assetPath, FileMode.Create, FileAccess.Write);
                        await importableHandler.ImportAsync(sourceStream, targetStream, newGuid);

                        File.Delete(assetPath);
                        UpdatePathMapping(relativePath, newGuid);

                        fireEvent = true;
                    }
                }
                break;

            case WatcherChangeTypes.Deleted:
                changeType = AssetChangeType.Deleted;
                if (isAsset)
                {
                    fireEvent = RemovePathMappingByPath(relativePath);
                }
                break;

            case WatcherChangeTypes.Changed:
                changeType = AssetChangeType.Modified;
                fireEvent = isAsset;
                break;
            case WatcherChangeTypes.All:
                // Can this even happen?
                break;
            default:
                break;
        }

        if (fireEvent)
        {
            OnAssetChanged?.Invoke(this, new AssetChangedEventArgs(relativePath, null, changeType));
        }
    }

    private void OnFileSystemRenameOp(object sender, RenamedEventArgs e)
    {
        var ext = Path.GetExtension(e.FullPath);
        if (!ext.Equals(ASSET_EXTENSION, StringComparison.Ordinal))
        {
            return;
        }

        var oldRelativePath = Path.GetRelativePath(_rootDirectory, e.OldFullPath);
        var newRelativePath = Path.GetRelativePath(_rootDirectory, e.FullPath);

        if (_pathToGuid.Remove(oldRelativePath, out var guid))
        {
            UpdatePathMapping(newRelativePath, guid);
            OnAssetChanged?.Invoke(this, new AssetChangedEventArgs(newRelativePath, oldRelativePath, AssetChangeType.Renamed));
        }
    }

    public string? GetAssetPath(Guid id)
    {
        lock (_pathLock)
        {
            if (_guidToPath.TryGetValue(id, out var path))
            {
                return path;
            }
        }

        return null;
    }

    public Guid GetAssetGuid(string path)
    {
        lock (_pathLock)
        {
            if (_pathToGuid.TryGetValue(path, out var guid))
            {
                return guid;
            }
        }

        return Guid.Empty;
    }

    private IAssetHandler GetAssetHandler(Type type)
    {
        var typeHandle = type.TypeHandle.Value;
        if (_cachedHander.TryGetValue(typeHandle, out var handler))
        {
            return handler;
        }

        var obj = Activator.CreateInstance(type);
        if (obj is not IAssetHandler newHandler)
        {
            throw new InvalidOperationException($"Type {type.FullName} is not an IAssetHandler.");
        }

        var attr = type.GetCustomAttribute<CustomAssetHandlerAttribute>(false);
        if (attr is null || attr.AllowCaching)
        {
            _cachedHander[typeHandle] = newHandler;
        }

        return newHandler;
    }

    private IAssetHandler? GetAssetHandlerForExtension(string extension)
    {
        foreach (var handlerType in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(assembly => assembly.GetTypes())
                     .Where(type => typeof(IAssetHandler).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract))
        {
            var attr = handlerType.GetCustomAttribute<CustomAssetHandlerAttribute>(false);
            if (attr is not null && attr.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return GetAssetHandler(handlerType);
            }
        }

        return null;
    }

    private IAssetHandler? GetAssetHandlerForTypeId(Guid typeId)
    {
        foreach (var handlerType in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(assembly => assembly.GetTypes())
                     .Where(type => typeof(IAssetHandler).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract))
        {
            var attr = handlerType.GetCustomAttribute<CustomAssetHandlerAttribute>(false);
            if (attr is not null && new Guid(attr.ID) == typeId)
            {
                return GetAssetHandler(handlerType);
            }
        }

        return null;
    }

    public async ValueTask<Result<Guid>> ImportAssetAsync(string sourceFilePath, string targetAssetPath, CancellationToken token = default)
    {
        if (!File.Exists(sourceFilePath))
        {
            return Result.Failure("Source file not found.");
        }

        var ext = Path.GetExtension(sourceFilePath);
        var handler = GetAssetHandlerForExtension(ext);
        if (handler is not IImportableAssetHandler importableHandler)
        {
            return Result.Failure("No importable asset handler found for the given file extension.");
        }

        var guid = Guid.NewGuid();
        var fullTargetPath = Path.GetFullPath(targetAssetPath, _rootDirectory);
        if (!await importableHandler.ImportAsync(sourceFilePath, fullTargetPath, guid, token: token))
        {
            return Result.Failure("Asset import failed.");
        }

        UpdatePathMapping(targetAssetPath, guid);
        return guid;
    }

    public async ValueTask<Result> ReimportAssetAsync(Guid assetId, string sourceFilePath, CancellationToken token = default)
    {
        var assetPath = GetAssetPath(assetId);
        if (string.IsNullOrEmpty(assetPath))
        {
            return Result.Failure("Asset not found in DB");
        }

        var fullAssetPath = Path.GetFullPath(assetPath, _rootDirectory);

        // 2. Identify the Handler
        // (You might want to store SourcePath in metadata later so you don't need to pass it here)
        var ext = Path.GetExtension(sourceFilePath);
        var handler = GetAssetHandlerForExtension(ext);
        if (handler is not IImportableAssetHandler importableHandler)
        {
            return Result.Failure("No importable asset handler found for the given file extension.");
        }

        _ignoreFileChanges[fullAssetPath] = true;

        await using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
        await using var targetStream = new FileStream(fullAssetPath, FileMode.Create, FileAccess.Write);

        await importableHandler.ImportAsync(sourceStream, targetStream, assetId, token);
        if (_loadedAssets.TryGetValue(assetId, out var weakRef) && weakRef.TryGetTarget(out var liveAsset))
        {
            await liveAsset.RefreshAsync(this, token);
        }

        return Result.Success();
    }

    public async ValueTask<Result<Asset>> LoadAssetAsync(Guid id, CancellationToken token = default)
    {
        // TODO: weakRef based locking instead of global lock for better concurrency.
        // We should use GetOrAdd here.
        if (_loadedAssets.TryGetValue(id, out var weakRef)
            && weakRef.TryGetTarget(out var existingAsset))
        {
            return existingAsset;
        }

        await _cacheSlim.WaitAsync(token);

        // Double check after acquiring the lock to make sure the assetResult wasn't loaded while waiting.
        if (_loadedAssets.TryGetValue(id, out weakRef)
                && weakRef.TryGetTarget(out existingAsset))
        {
            return existingAsset;
        }

        try
        {
            var path = GetAssetPath(id);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var assetPath = Path.GetFullPath(path, _rootDirectory);
            await using var fs = new FileStream(assetPath, FileMode.Open, FileAccess.Read, FileShare.Read);

            int sizeofGuid;
            unsafe
            {
                sizeofGuid = sizeof(Guid);
            }

            Span<byte> typeIdBuffer = stackalloc byte[sizeofGuid];
            fs.Seek(sizeof(int) + sizeofGuid, SeekOrigin.Begin);
            fs.ReadExactly(typeIdBuffer);

            var guid = Unsafe.ReadUnaligned<Guid>(ref MemoryMarshal.GetReference(typeIdBuffer));
            var handler = GetAssetHandlerForTypeId(guid);
            if (handler == null)
            {
                return null;
            }

            var assetResult = await handler.LoadAsync(fs, this, token);
            if (assetResult.IsFailure)
            {
                return assetResult;
            }

            var asset = assetResult.Value;
            _loadedAssets.AddOrUpdate(id, new WeakReference<Asset>(asset), (key, oldRef) =>
            {
                // If the early return fails (find existing assetResult), it means either the assetResult haven't been loaded before, or the previous reference has been collected.
                // If the assetResult haven't been loaded before, we are in the addValue path, not here.
                // If the previous reference has been collected, we can just replace it with the new one.
                // Since we are using _cacheSlim to protect this section, we don't need check if the oldRef is still valid because only one thread can be here at a time.
                oldRef.SetTarget(asset);
                return oldRef;
            });

            return assetResult;
        }
        finally
        {
            _cacheSlim.Release();
        }
    }

    public async ValueTask<Result> SaveAssetAsync(Asset asset, CancellationToken token = default)
    {
        var path = GetAssetPath(asset.ID);
        if (path == null)
        {
            return Result.Failure("Asset not found.");
        }

        var handler = GetAssetHandlerForTypeId(asset.TypeID);
        if (handler == null)
        {
            return Result.Failure("No asset handler found for the given asset type.");
        }

        var fullPath = Path.GetFullPath(path, _rootDirectory);
        await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        return await handler.SaveAsync(asset, fs, this, token);
    }

    public void Dispose()
    {
        _cacheSlim.Dispose();
        _watcher.Dispose();
    }
}
