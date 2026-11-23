using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

public enum AssetState
{
    Unloaded = 0,
    Scheduled = 1,
    Loading = 2,
    Loaded = 3,
    Processing = 4,
    Ready = 5,
    Failed = 6,
}

internal static class AssetEntryFactory
{
    public static AssetEntry CreateNewEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies, string assetName)
    {
        return assetType switch
        {
            AssetType.Texture => new TextureAssetEntry(manager, resourceDatabase, resourceManager, assetId, dependencies, assetName),
            AssetType.Mesh => new MeshAssetEntry(manager, resourceDatabase, resourceManager, assetId, dependencies, assetName),
            AssetType.Material => new MaterialAssetEntry(manager, resourceDatabase, resourceManager, assetId, dependencies, assetName),
            AssetType.EntityPrefab => new PrefabAssetEntry(manager, resourceDatabase, resourceManager, assetId, dependencies, assetName),
            AssetType.Shader => new ShaderAssetEntry(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies, assetName),
            AssetType.ComputeShader => new ComputeShaderAssetEntry(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies, assetName),
            AssetType.WorkGraph => new WorkGraphAssetEntry(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies, assetName),
            AssetType.Scene => new SceneAssetEntry(manager, resourceDatabase, resourceManager, assetId, dependencies, assetName),
            AssetType.Audio => throw new NotImplementedException(),
            AssetType.Video => throw new NotImplementedException(),
            AssetType.Json => throw new NotImplementedException(),
            AssetType.Unknown => throw new NotImplementedException(),
            _ => throw new NotSupportedException($"Unsupported asset type {assetType}.")
        };
    }
}

public readonly struct AssetLoadProgress
{
    public int ReadyCount { get; init; }
    public int LoadedCount { get; init; }
    public int TotalCount { get; init; }
    public float Progress { get; init; }
}

internal abstract class AssetEntry : IAssetEntry
{
    private readonly AssetManager _assetManager;
    private readonly ResourceManager _resourceManager;
    private readonly IResourceDatabase _resourceDatabase;

    private readonly Guid _assetId;
    private readonly AssetType _assetType;
    private readonly string _assetName;

    private Guid[] _dependencies;
    private Guid[]? _transitiveDependencies;
    private JobHandle _loadJobHandle;
    private JobHandle _combinedDependencyJobHandle;
    private int _refCount;
    private int _state;
    private int _contentVersion;

    private int _pendingReimport;
    private int _pendingDeferredRelease;
    private int _unloaded;
    private bool _isReimporting;

    protected ResourceManager ResourceManager => _resourceManager;
    protected IResourceDatabase ResourceDatabase => _resourceDatabase;
    internal AssetManager Manager => _assetManager;

    public Guid AssetId => _assetId;
    public JobHandle LoadJobHandle => _loadJobHandle;
    internal JobHandle CombinedDependencyJobHandle => _combinedDependencyJobHandle;
    public AssetType AssetType => _assetType;
    public ReadOnlySpan<Guid> Dependencies => _dependencies;
    public int RefCount => Volatile.Read(ref _refCount);
    public string AssetName => _assetName;
    public int ContentVersion => Volatile.Read(ref _contentVersion);

    internal virtual AssetState FailureState => AssetState.Failed;

    public ref int StateValue => ref _state;
    public AssetState State
    {
        get => (AssetState)Volatile.Read(ref _state);
        set
        {
            Volatile.Write(ref _state, (int)value);
            if (value is AssetState.Ready or AssetState.Failed)
            {
                if (Interlocked.Exchange(ref _pendingDeferredRelease, 0) == 1 && TryCompleteUnload())
                {
                    return;
                }
            }

            if (Volatile.Read(ref _state) == (int)AssetState.Ready)
            {
                if (Interlocked.Exchange(ref _pendingReimport, 0) == 1)
                {
                    _assetManager.ReimportAsset(_assetId);  // re-queue
                }
            }
        }
    }

    protected AssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies, string assetName)
    {
        _assetManager = manager;
        _resourceDatabase = resourceDatabase;
        _resourceManager = resourceManager;

        _assetId = assetId;
        _assetType = assetType;
        _dependencies = dependencies;
        _assetName = assetName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetLoadJobHandle(JobHandle handle)
    {
        _loadJobHandle = handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetCombinedDependencyJobHandle(JobHandle handle)
    {
        _combinedDependencyJobHandle = handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateDependencies(Guid[] dependencies)
    {
        _dependencies = dependencies;
        Volatile.Write(ref _transitiveDependencies, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetTransitiveDependencies(Guid[] transitiveDependencies)
    {
        Volatile.Write(ref _transitiveDependencies, transitiveDependencies);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool MarkReimporting()
    {
        _isReimporting = true;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool ConsumeReimporting()
    {
        var value = _isReimporting;
        _isReimporting = false;
        return value;
    }

    public AssetLoadProgress GetLoadProgress()
    {
        var deps = Volatile.Read(ref _transitiveDependencies);
        if (deps == null)
        {
            deps = _assetManager.CollectUniqueTransitiveDependencies(_assetId);
            Volatile.Write(ref _transitiveDependencies, deps);
        }

        var totalCount = 1 + deps.Length;
        var readyCount = 0;
        var loadedCount = 0;
        var weightSum = AccumulateStateProgress(State, ref readyCount, ref loadedCount);

        for (var i = 0; i < deps.Length; i++)
        {
            if (_assetManager.TryGetEntry(deps[i], out var depEntry))
            {
                weightSum += AccumulateStateProgress(depEntry.State, ref readyCount, ref loadedCount);
            }
        }

        var progress = readyCount == totalCount ? 1.0f : MathF.Min(0.999f, weightSum / totalCount);
        return new AssetLoadProgress
        {
            ReadyCount = readyCount,
            LoadedCount = loadedCount,
            TotalCount = totalCount,
            Progress = progress
        };
    }

    internal bool HasFailedAssetInGraph()
    {
        if (State == AssetState.Failed)
        {
            return true;
        }

        var deps = Volatile.Read(ref _transitiveDependencies);
        if (deps == null)
        {
            deps = _assetManager.CollectUniqueTransitiveDependencies(_assetId);
            Volatile.Write(ref _transitiveDependencies, deps);
        }

        for (var i = 0; i < deps.Length; i++)
        {
            if (_assetManager.TryGetEntry(deps[i], out var depEntry) && depEntry.State == AssetState.Failed)
            {
                return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float AccumulateStateProgress(AssetState state, ref int readyCount, ref int loadedCount)
    {
        switch (state)
        {
            case AssetState.Ready:
                readyCount++;
                loadedCount++;
                return 1.0f;
            case AssetState.Loaded:
            case AssetState.Processing:
                loadedCount++;
                return 0.60f;
            case AssetState.Loading:
                return 0.25f;
            default:
                return 0.0f;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementContentVersion()
    {
        Interlocked.Increment(ref _contentVersion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetPendingReimport()
    {
        Volatile.Write(ref _pendingReimport, 1);
    }

    /// <summary>
    /// Atomically claims an outstanding reimport request. Only one thread can succeed, which
    /// guarantees at most one thread re-schedules the entry through <c>EnsureScheduled</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryConsumePendingReimport()
    {
        return Interlocked.Exchange(ref _pendingReimport, 0) == 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool TryMarkUnloaded()
    {
        return Interlocked.CompareExchange(ref _unloaded, 1, 0) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRef()
    {
        Interlocked.Increment(ref _refCount);
        Volatile.Write(ref _pendingDeferredRelease, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Release()
    {
        var newRefCount = Interlocked.Decrement(ref _refCount);
        Logger.DebugAssert(newRefCount >= 0, "Reference count should not be negative");

        if (newRefCount == 0)
        {
            if (State is AssetState.Ready or AssetState.Failed)
            {
                TryCompleteUnload();
            }
            else
            {
                // Defer teardown until loading/streaming reaches a terminal state instead of asserting mid-flight.
                Volatile.Write(ref _pendingDeferredRelease, 1);
                if (State is AssetState.Ready or AssetState.Failed && Interlocked.Exchange(ref _pendingDeferredRelease, 0) == 1)
                {
                    TryCompleteUnload();
                }
            }
        }

        return newRefCount;
    }

    private bool TryCompleteUnload() => TryUnloadInternal(requireTerminalState: true);

    internal bool TryForceUnloadOnDispose() => TryUnloadInternal(requireTerminalState: false);

    private bool TryUnloadInternal(bool requireTerminalState)
    {
        if (_assetManager != null)
        {
            var claimed = requireTerminalState ? _assetManager.TryUnloadIfZero(this) : _assetManager.TryForceUnloadZeroRefOnDispose(this);
            if (!claimed)
            {
                return false;
            }
        }
        else if (Volatile.Read(ref _refCount) != 0 || !TryMarkUnloaded())
        {
            return false;
        }

        OnReleaseResource();

        foreach (var dep in _dependencies)
        {
            if (_assetManager != null && _assetManager.TryGetEntry(dep, out var entry))
            {
                entry.Release();
            }
        }

        return true;
    }

    public abstract void ReadAssetData(Span<byte> dst);
    public abstract void ReadAssetData<T>(ref T dst)
        where T : struct;

    protected virtual void OnReleaseResource()
    {
    }
}

public interface IAssetEntry
{
    Guid AssetId { get; }
    JobHandle LoadJobHandle { get; }
    AssetType AssetType { get; }
    ReadOnlySpan<Guid> Dependencies { get; }
    int RefCount { get; }
    int ContentVersion { get; }
    AssetState State { get; set; }

    void AddRef();
    int Release();
    AssetLoadProgress GetLoadProgress();
    void ReadAssetData(Span<byte> dst);
    void ReadAssetData<T>(ref T dst)
        where T : struct;
}

internal interface ILoadableAssetEntry : IAssetEntry
{
    Result OnLoadContent([Owner] ref AssetPayload payload);
}

internal interface IProcessableAssetEntry : IAssetEntry
{
    Result<JobHandle> OnProcessing();
}

internal interface IUploadableAssetEntry : IAssetEntry
{
    Result OnRecordUploadCommands(scoped in RenderContext context, ICommandBuffer copyCommandBuffer);
    void OnUploadComplete(scoped in RenderContext context, ICommandBuffer commandBuffer);
}

internal interface IShaderCommitableAssetEntry : IAssetEntry
{
    Result CommitShaderBytecode(ShaderLibrary shaderLibrary);
}
