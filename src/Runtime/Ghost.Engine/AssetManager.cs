using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Engine;

public enum AssetState : byte
{
    Unloaded = 0,
    Scheduled = 1,
    Loading = 2,
    Loaded = 3,
    Uploading = 4,
    Ready = 5,
    Failed = 6,
}

public interface IContentProvider
{
    bool HasAsset(Guid guid);

    Result<Stream> OpenRead(Guid guid, CancellationToken token = default);

    Guid[] GetDependencies(Guid guid);

    AssetType GetAssetType(Guid guid);
}

internal partial class AssetEntry
{
    private static readonly Action<AssetEntry>[] s_onCreation = new Action<AssetEntry>[(int)AssetType.Unknown + 1];
    private static readonly Func<AssetEntry, Result>[] s_onParseRawData = new Func<AssetEntry, Result>[(int)AssetType.Unknown + 1];
    private static readonly Action<AssetEntry, ResourceStreamingContext>[] s_onRecordUpload = new Action<AssetEntry, ResourceStreamingContext>[(int)AssetType.Unknown + 1];
    private static readonly Action<AssetEntry, ResourceStreamingContext>[] s_onUploadComplete = new Action<AssetEntry, ResourceStreamingContext>[(int)AssetType.Unknown + 1];
    private static readonly Action<AssetEntry>[] s_onReleaseResource = new Action<AssetEntry>[(int)AssetType.Unknown + 1];

    static AssetEntry()
    {
        RegisterTextureCallback();
    }
}

internal unsafe partial class AssetEntry
{
    private struct Storage
    {
        public fixed byte data[64];
    }

    private readonly AssetManager _assetManager;
    private readonly IResourceDatabase _resourceDatabase;

    private readonly Guid _assetId;
    private readonly AssetType _assetType;
    private readonly Guid[] _dependencies;

    private Storage _storage;
    private MemoryBlock _rawData;
    private object? _parsedObject;

    private JobHandle _loadJobHandle;
    private int _refCount;
    private int _state;

    private bool _pendingReimport;

    public Guid AssetId => _assetId;
    public MemoryBlock RawData => _rawData;
    public JobHandle LoadJobHandle => _loadJobHandle;
    public AssetType AssetType => _assetType;
    public ReadOnlySpan<Guid> Dependencies => _dependencies;
    public int RefCount => Volatile.Read(ref _refCount);

    public ref int StateValue => ref _state;
    public AssetState State
    {
        get => (AssetState)Volatile.Read(ref _state);
        set => Volatile.Write(ref _state, (int)value);
    }

    public AssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, Guid assetId, AssetType assetType, Guid[] dependencies)
    {
        _assetManager = manager;
        _resourceDatabase = resourceDatabase;

        _assetId = assetId;
        _assetType = assetType;
        _dependencies = dependencies;

        s_onCreation[(int)_assetType]?.Invoke(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetStorage<T>(T asset)
        where T : unmanaged
    {
        Unsafe.WriteUnaligned(ref _storage.data[0], asset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetStorage<T>()
        where T : unmanaged
    {
        return Unsafe.ReadUnaligned<T>(ref _storage.data[0]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRawData([OwnershipTransfer] ref MemoryBlock data)
    {
        _rawData = data;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetLoadJobHandle(JobHandle handle)
    {
        _loadJobHandle = handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPendingReimport()
    {
        Volatile.Write(ref _pendingReimport, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRef()
    {
        Interlocked.Increment(ref _refCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Release()
    {
        Logger.DebugAssert(State == AssetState.Ready);

        var newRefCount = Interlocked.Decrement(ref _refCount);
        Logger.DebugAssert(newRefCount >= 0, "Reference count should not be negative");

        if (newRefCount == 0)
        {
            _assetManager.RemoveEntry(_assetId);
            OnReleaseResource();

            foreach (var dep in _dependencies)
            {
                if (_assetManager.TryGetEntry(dep, out var entry))
                {
                    entry.Release();
                }
            }
        }

        return newRefCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result OnParseRawData()
    {
        return s_onParseRawData[(int)_assetType]?.Invoke(this) ?? Result.Failure("Unsupported asset type.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnRecordUploadCommands(ResourceStreamingContext context)
    {
        s_onRecordUpload[(int)_assetType]?.Invoke(this, context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnUploadComplete(ResourceStreamingContext context)
    {
        s_onUploadComplete[(int)_assetType]?.Invoke(this, context);
        Volatile.Write(ref _state, (int)AssetState.Ready);

        if (Interlocked.CompareExchange(ref _pendingReimport, false, true))
        {
            _assetManager.InvalidateAsset(_assetId);  // re-queue
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnReleaseResource()
    {
        s_onReleaseResource[(int)_assetType]?.Invoke(this);
    }
}

internal struct LoadAssetJob : IJob
{
    public Guid assetID;
    public AssetType assetType;
    public GCHandle assetManagerHandle;

    private static Result LoadRawData(IContentProvider contentProvider, AssetEntry entry)
    {
        try
        {
            using var stream = contentProvider.OpenRead(entry.AssetId).GetValueOrThrow();

            var data = new MemoryBlock((nuint)stream.Length, MemoryUtility.AlignOf<IntPtr>(), AllocationHandle.Persistent);

            // C# built-in collections use int for indexing, so we need to ensure that the buffer size does not exceed int.MaxValue
            var maxChunkSize = (int)Math.Min(0x7fffffffu, data.Size);
            var offset = 0u;

            while (offset < data.Size)
            {
                using var mem = NativeMemoryManager<byte>.FromMemoryBlock(data, (int)offset, maxChunkSize);
                stream.ReadExactly(mem.Memory.Span);
                offset += (uint)mem.Memory.Length;
            }

            entry.SetRawData(ref data);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }

    public readonly void Execute(ref readonly JobExecutionContext ctx)
    {
        var assetManager = assetManagerHandle.Target as AssetManager;

        Logger.DebugAssert(assetManager is not null);

        if (!assetManager.TryGetEntry(assetID, out var entry))
        {
            Logger.Error($"Asset entry not found for {assetID}");
            return;
        }

        Logger.DebugAssert(entry.AssetType == assetType);
        Logger.DebugAssert(entry.State == AssetState.Scheduled);

        entry.State = AssetState.Loading;

        var result = LoadRawData(assetManager.ContentProvider, entry);
        if (result.IsFailure)
        {
            entry.State = AssetState.Failed;
            Logger.Error($"Failed to load asset {assetID}: {result.Message}");
            return;
        }

        result = entry.OnParseRawData();
        if (result.IsFailure)
        {
            entry.State = AssetState.Failed;
            Logger.Error($"Failed to parse asset {assetID}: {result.Message}");
            return;
        }

        entry.State = AssetState.Loaded;

        assetManager.StreamingProcessor.EnqueueForUpload(entry);
    }
}

// TODO: Support DirectStorage.
internal partial class AssetManager : IDisposable
{
    private readonly IResourceDatabase _resourceDatabase;
    private readonly IContentProvider _contentProvider;
    private readonly ResourceStreamingProcessor _streamingProcessor;
    private readonly JobScheduler _jobScheduler;

    private readonly ConcurrentDictionary<Guid, AssetEntry> _entries;

    private GCHandle _selfHandle;

    // TODO
    private Handle<GPUTexture> _fallbackTexture;
    private Handle<GPUTexture> _fallbackNormalMap;
    private Handle<Mesh> _fallbackMesh;
    private Handle<Material> _fallbackMaterial;

    public IContentProvider ContentProvider => _contentProvider;
    public ResourceStreamingProcessor StreamingProcessor => _streamingProcessor;

    public Handle<GPUTexture> FallbackTexture => _fallbackTexture;

    internal AssetManager(IResourceDatabase resourceDatabase, IContentProvider contentProvider, ResourceStreamingProcessor streamingProcessor, JobScheduler jobScheduler)
    {
        _resourceDatabase = resourceDatabase;
        _contentProvider = contentProvider;
        _streamingProcessor = streamingProcessor;
        _jobScheduler = jobScheduler;

        _entries = new ConcurrentDictionary<Guid, AssetEntry>();
        _selfHandle = GCHandle.Alloc(this, GCHandleType.Normal);
    }

    internal bool TryGetEntry(Guid guid, [NotNullWhen(true)] out AssetEntry? entry)
    {
        return _entries.TryGetValue(guid, out entry);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool RemoveEntry(Guid guid)
    {
        return _entries.TryRemove(guid, out var entry);
    }

    private void EnsureScheduled(AssetEntry entry)
    {
        if (Interlocked.CompareExchange(ref entry.StateValue, (int)AssetState.Scheduled, (int)AssetState.Unloaded) != (int)AssetState.Unloaded)
        {
            return;
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
                var handle = GetOrCreateEntry(list[i]).LoadJobHandle;
                Logger.DebugAssert(handle.IsValid);

                depHandles.Add(handle);
            }

            dependency = _jobScheduler.CombineDependencies(depHandles);
        }

        var job = new LoadAssetJob
        {
            assetID = entry.AssetId,
            assetType = entry.AssetType,
            assetManagerHandle = _selfHandle,
        };

        entry.SetLoadJobHandle(_jobScheduler.Schedule(ref job, dependency, JobPriority.Low)); // Use low priority to avoid blocking main thread critical tasks like rendering and physics.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AssetEntry GetOrCreateEntry(Guid guid)
    {
        var entry = _entries.GetOrAdd(guid, static (id, self) =>
        {
            var type = self._contentProvider.GetAssetType(id);
            var deps = self._contentProvider.GetDependencies(id);

            var entry = new AssetEntry(self, self._resourceDatabase, id, type, deps);

            self.EnsureScheduled(entry);
            return entry;
        }, this);

        entry.AddRef();
        return entry;
    }

    public void InvalidateAsset(Guid guid)
    {
        if (!_entries.TryGetValue(guid, out var entry))
        {
            return;
        }

        if (entry.State is AssetState.Loading or AssetState.Loaded or AssetState.Uploading)
        {
            entry.SetPendingReimport();
            return;
        }

        // Entry is in Ready state — the old texture is valid and will remain visible.
        // Go directly to Scheduled → Loading → Loaded → Uploading → Ready again.
        // The swap cycle in RecordTextureUpload/OnTextureUploadComplete handles the 
        // v1 → v2 transition exactly like the fallback → v1 transition.
        entry.State = AssetState.Scheduled;
        EnsureScheduled(entry);
    }

    public void Dispose()
    {
        foreach (var entry in _entries.Values)
        {
            entry.OnReleaseResource();
        }

        _entries.Clear();
        _selfHandle.Free();
    }
}
