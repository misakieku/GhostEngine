using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

public struct AssetReadData : IDisposable
{
    public Guid assetId;
    public AssetType assetType;
    public Stream stream;

    public readonly void Dispose()
    {
        stream?.Dispose();
    }
}

public interface IContentProvider
{
    IReadOnlyList<ShaderCatalogEntry> ShaderCatalog { get; }
    Guid VirtualPathToGuid(string path);
    bool HasAsset(Guid guid);
    Guid[] GetDependencies(Guid guid);
    AssetType GetAssetType(Guid guid);
    Result<AssetReadData> OpenReadAsync(Guid guid, CancellationToken token = default);
}

internal struct LoadAssetJob : IJob
{
    public Guid assetID;
    public AssetType assetType;
    public AssetManager assetManager;

    public readonly void Execute(ref readonly JobExecutionContext ctx)
    {
        Logger.DebugAssert(assetManager is not null);

        if (!assetManager.TryGetEntry(assetID, out var entry))
        {
            Logger.Error($"Asset entry not found for {assetID}");
            return;
        }

        Logger.DebugAssert(entry.AssetType == assetType);
        Logger.DebugAssert(entry.State == AssetState.Scheduled);

        entry.State = AssetState.Loading;

        if (entry is not ILoadableAssetEntry loadable)
        {
            entry.State = AssetState.Loaded;
            if (!assetManager.StreamingProcessor.EnqueueForProcess(entry))
            {
                entry.State = AssetState.Ready;
            }

            return;
        }

        try
        {
            var openResult = assetManager.ContentProvider.OpenReadAsync(entry.AssetId);
            if (openResult.IsFailure)
            {
                entry.State = AssetState.Failed;
                Logger.Error($"Failed to open asset {assetID}: {openResult.Message}");
                return;
            }

            using var readData = openResult.Value;
            var result = loadable.OnLoadContent(readData.stream);
            if (result.IsFailure)
            {
                entry.State = AssetState.Failed;
                Logger.Error($"Failed to load asset {assetID}: {result.Message}");
                return;
            }

            entry.State = AssetState.Loaded;
            if (!assetManager.StreamingProcessor.EnqueueForProcess(entry))
            {
                // This mean the asset don't need any further processing anymore.
                entry.State = AssetState.Ready;
            }
        }
        catch (Exception ex)
        {
            entry.State = AssetState.Failed;
            Logger.Error($"Failed to load asset {assetID}: {ex.Message}");
            return;
        }
    }
}

// TODO: Support DirectStorage.
public partial class AssetManager : IDisposable
{
    private readonly IResourceDatabase _resourceDatabase;
    private readonly IContentProvider _contentProvider;
    private readonly ResourceManager _resourceManager;
    private readonly ResourceStreamingProcessor _streamingProcessor;
    private readonly JobScheduler _jobScheduler;
    private readonly ShaderVariantRegistry _shaderVariants;
    private readonly ComputeShaderRegistry _computeShaders;

    private readonly ConcurrentDictionary<Guid, AssetEntry> _entries;

    internal IContentProvider ContentProvider => _contentProvider;
    internal ResourceStreamingProcessor StreamingProcessor => _streamingProcessor;
    /// <summary>
    /// Dense metadata registry for graphics shader variants.
    /// </summary>
    public ShaderVariantRegistry ShaderVariants => _shaderVariants;
    /// <summary>
    /// Metadata registry for standalone compute shaders.
    /// </summary>
    public ComputeShaderRegistry ComputeShaders => _computeShaders;

    internal AssetManager(IResourceDatabase resourceDatabase, ResourceManager resourceManager, IContentProvider contentProvider, ResourceStreamingProcessor streamingProcessor, JobScheduler jobScheduler)
    {
        _resourceDatabase = resourceDatabase;
        _resourceManager = resourceManager;
        _contentProvider = contentProvider;
        _streamingProcessor = streamingProcessor;
        _jobScheduler = jobScheduler;
        _shaderVariants = new ShaderVariantRegistry(resourceManager, contentProvider.ShaderCatalog);
        _computeShaders = new ComputeShaderRegistry(resourceManager, contentProvider.ShaderCatalog);

        _entries = new ConcurrentDictionary<Guid, AssetEntry>();
    }

    internal bool TryGetEntry(Guid guid, [NotNullWhen(true)] out AssetEntry? entry)
    {
        return _entries.TryGetValue(guid, out entry);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool RemoveEntry(Guid guid)
    {
        return _entries.TryRemove(guid, out var _);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AssetEntry GetOrCreateEntry(Guid guid)
    {
        var entry = _entries.GetOrAdd(guid, static (id, self) =>
        {
            var type = self._contentProvider.GetAssetType(id);
            var deps = self._contentProvider.GetDependencies(id);

            var entry = AssetEntryFactory.CreateNewEntry(self, self._resourceDatabase, self._resourceManager, id, type, deps);

            self.EnsureScheduled(entry);
            return entry;
        }, this);

        entry.AddRef();
        return entry;
    }

    private void EnsureScheduled(AssetEntry entry)
    {
        var previousState = Interlocked.CompareExchange(ref entry.StateValue, (int)AssetState.Scheduled, (int)AssetState.Unloaded);
        if (previousState != (int)AssetState.Unloaded)
        {
            // Reimport path: the entry is already Scheduled. Only the thread that can atomically
            // claim the outstanding reimport request may re-schedule it; any other thread bails
            // out here (it is re-queued once the entry returns to Ready). This closes the TOCTOU
            // window between ReimportAsset invalidating the old load job and EnsureScheduled
            // re-scheduling, so the entry can never be scheduled by two threads at once (BUG-08).
            if (previousState != (int)AssetState.Scheduled || !entry.TryConsumePendingReimport())
            {
                return;
            }
        }

        // TODO: Can this be jobified? If the dependency tree is not deep, it should be fine to do it in main thread, otherwise we might need to schedule a job to do it.
        // The combined dependency handle is resolved once and cached on the entry; subsequent
        // re-schedules (e.g. reimport) reuse it instead of re-traversing the whole graph (PERF-07).
        var dependency = entry.CombinedDependencyJobHandle;

        // If the entry has no dependencies and it's not a loadable asset, we can skip the job scheduling and directly mark the entry as loaded.
        if ((dependency.IsValid || entry.Dependencies.Length == 0) && entry is not ILoadableAssetEntry)
        {
            entry.State = AssetState.Loaded;
            if (!StreamingProcessor.EnqueueForProcess(entry))
            {
                entry.State = AssetState.Ready;
            }

            return;
        }

        // TODO: We are rescheduling the job for ervery dependencies even if it is already loaded. We should only schedule the job for the dependencies that are not loaded yet.
        if (!dependency.IsValid && entry.Dependencies.Length > 0)
        {
            // Avoid stack overflow for deep dependency tree like a scene.

            // Stack allocator here is fine, because it use virtual memory and has 32 mb capacity per thread.
            using var scope = AllocationManager.CreateStackScope();

            using var list = new UnsafeList<Guid>(entry.Dependencies.Length * 2, scope.AllocationHandle);
            using var stack = new UnsafeStack<Guid>(entry.Dependencies.Length * 2, scope.AllocationHandle);
            using var visited = new UnsafeHashSet<Guid>(entry.Dependencies.Length * 2, scope.AllocationHandle);

            for (var i = 0; i < entry.Dependencies.Length; i++)
            {
                stack.Push(entry.Dependencies[i]);
            }

            while (stack.TryPop(out var guid))
            {
                if (visited.Contains(guid))
                {
                    continue;
                }

                visited.Add(guid);
                list.Add(guid);

                var depss = _contentProvider.GetDependencies(guid);
                foreach (var d in depss)
                {
                    if (!visited.Contains(d))
                    {
                        stack.Push(d);
                    }
                }
            }

            using var depHandles = new UnsafeList<JobHandle>(list.Count, scope.AllocationHandle);

            // Schedule all dependencies first. Direct dependencies are visited before transitive
            // ones so that, by the time the transient reference taken by GetOrCreateEntry below is
            // released, the entry is already referenced by at least one of its parents and is not
            // removed/re-created by the Release cascade.
            for (var i = 0; i < list.Count; i++)
            {
                // This should create the entry and schedule the job on those assets does not have any dependency first.
                var depEntry = GetOrCreateEntry(list[i]);
                var depHandle = depEntry.LoadJobHandle;
                Logger.DebugAssert(depHandle.IsValid);

                depHandles.Add(depHandle);

                // GetOrCreateEntry takes a reference on the entry. References on direct dependencies
                // are balanced by this entry's Release() cascade; the transient reference on a
                // transitive-only dependency must be released here or it leaks (BUG-04).
                if (!IsDirectDependency(entry, list[i]))
                {
                    depEntry.Release();
                }
            }

            dependency = _jobScheduler.CombineDependencies(depHandles);
            entry.SetCombinedDependencyJobHandle(dependency);
        }

        var job = new LoadAssetJob
        {
            assetID = entry.AssetId,
            assetType = entry.AssetType,
            assetManager = this,
        };

        var handle = _jobScheduler.Schedule(ref job, JobPriority.Low, dependency);
        entry.SetLoadJobHandle(handle); // Use low priority to avoid blocking main thread critical tasks like rendering and physics.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDirectDependency(AssetEntry entry, Guid guid)
    {
        foreach (var dep in entry.Dependencies)
        {
            if (dep == guid)
            {
                return true;
            }
        }

        return false;
    }

    internal void ReimportAsset(Guid guid)
    {
        if (!_entries.TryGetValue(guid, out var entry))
        {
            return;
        }

        if (Interlocked.CompareExchange(ref entry.StateValue, (int)AssetState.Scheduled, (int)AssetState.Ready) == (int)AssetState.Ready)
        {
            // Entry is in Ready state - the old texture is valid and will remain visible.
            // Go directly to Scheduled -> Loading -> Loaded -> Uploading -> Ready again.
            // The swap cycle in RecordTextureUpload/OnTextureUploadComplete handles the 
            // v1 to v2 transition exactly like the fallback to v1 transition.
            // Request the re-schedule atomically: EnsureScheduled claims this request via
            // TryConsumePendingReimport, so no other thread can double-schedule the entry (BUG-08).
            entry.SetPendingReimport();
            EnsureScheduled(entry);
        }
        else
        {
            entry.SetPendingReimport();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAssetEntry ResolveAsset(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(assetID));
        }

        return GetOrCreateEntry(assetID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAssetEntry ResolveAsset(string virtualPath)
    {
        return ResolveAsset(_contentProvider.VirtualPathToGuid(virtualPath));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReleaseAsset(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            throw new ArgumentNullException(nameof(assetID));
        }

        if (!_entries.TryGetValue(assetID, out var entry))
        {
            return 0;
        }

        Logger.DebugAssert(entry.AssetType != AssetType.Unknown);
        return entry.Release();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReleaseAsset(string virtualPath)
    {
        return ReleaseAsset(_contentProvider.VirtualPathToGuid(virtualPath));
    }

    public void Dispose()
    {
        Logger.DebugAssert(_entries.IsEmpty, $"There are still {_entries.Count} assets in the manager. Make sure to release all assets before disposing the manager.");

        _entries.Clear();
        _computeShaders.Dispose();
        _shaderVariants.Dispose();
        if (_contentProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
