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

public interface IContentProvider
{
    bool HasAsset(Guid guid);

    Result<Stream> OpenRead(Guid guid, CancellationToken token = default);

    Guid[] GetDependencies(Guid guid);

    AssetType GetAssetType(Guid guid);
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
            var openResult = assetManager.ContentProvider.OpenRead(entry.AssetId);
            if (openResult.IsFailure)
            {
                entry.State = AssetState.Failed;
                Logger.Error($"Failed to open asset {assetID}: {openResult.Message}");
                return;
            }

            using var stream = openResult.Value;
            var result = loadable.OnLoadContent(stream);
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

    private readonly ConcurrentDictionary<Guid, AssetEntry> _entries;

    internal IContentProvider ContentProvider => _contentProvider;
    internal ResourceStreamingProcessor StreamingProcessor => _streamingProcessor;

    internal AssetManager(IResourceDatabase resourceDatabase, ResourceManager resourceManager, IContentProvider contentProvider, ResourceStreamingProcessor streamingProcessor, JobScheduler jobScheduler)
    {
        _resourceDatabase = resourceDatabase;
        _resourceManager = resourceManager;
        _contentProvider = contentProvider;
        _streamingProcessor = streamingProcessor;
        _jobScheduler = jobScheduler;

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
            if (previousState != (int)AssetState.Scheduled || entry.LoadJobHandle.IsValid)
            {
                return;
            }
        }

        // TODO: Can this be jobified? If the dependency tree is not deep, it should be fine to do it in main thread, otherwise we might need to schedule a job to do it.
        var dependency = JobHandle.Invalid;
        if (entry.Dependencies.Length > 0)
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

            // Schedule all dependencies first (depth-first)
            for (var i = list.Count - 1; i >= 0; i--)
            {
                // This should create the entry and schedule the job on those assets does not have any dependency first.
                var depHandle = GetOrCreateEntry(list[i]).LoadJobHandle;
                Logger.DebugAssert(depHandle.IsValid);

                depHandles.Add(depHandle);
            }

            dependency = _jobScheduler.CombineDependencies(depHandles);
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
            entry.SetLoadJobHandle(JobHandle.Invalid);
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

    public void Dispose()
    {
        Logger.DebugAssert(_entries.IsEmpty, $"There are still {_entries.Count} assets in the manager. Make sure to release all assets before disposing the manager.");

        _entries.Clear();

        GC.SuppressFinalize(this);
    }
}
