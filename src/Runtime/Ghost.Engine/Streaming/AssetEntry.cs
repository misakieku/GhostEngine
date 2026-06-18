using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

public enum AssetType
{
    Texture = 0,
    Mesh = 1,
    Material = 2,
    Shader = 3,
    Scene = 4,
    Audio = 5,
    Video = 6,
    Json = 7,

    Unknown = 64,
}

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
    public static AssetEntry CreateNewEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
    {
        return assetType switch
        {
            AssetType.Texture => new TextureAssetEntry(manager, resourceDatabase, resourceManager, assetId, dependencies),
            AssetType.Mesh => new MeshAssetEntry(manager, resourceDatabase, resourceManager, assetId, dependencies),
            AssetType.Material => throw new NotImplementedException(),
            AssetType.Shader => throw new NotImplementedException(),
            AssetType.Scene => new SceneAssetEntry(manager, resourceDatabase, resourceManager, assetId, dependencies),
            AssetType.Audio => throw new NotImplementedException(),
            AssetType.Video => throw new NotImplementedException(),
            AssetType.Json => throw new NotImplementedException(),
            AssetType.Unknown => throw new NotImplementedException(),
            _ => throw new NotSupportedException($"Unsupported asset type {assetType}.")
        };
    }
}

// TODO: Progress report
internal abstract class AssetEntry : IAssetEntry
{
    private readonly AssetManager _assetManager;
    private readonly ResourceManager _resourceManager;
    private readonly IResourceDatabase _resourceDatabase;

    private readonly Guid _assetId;
    private readonly AssetType _assetType;
    private readonly Guid[] _dependencies;

    private JobHandle _loadJobHandle;
    private int _refCount;
    private int _state;

    private int _pendingReimport;

    protected ResourceManager ResourceManager => _resourceManager;
    protected IResourceDatabase ResourceDatabase => _resourceDatabase;
    internal AssetManager Manager => _assetManager;

    public Guid AssetId => _assetId;
    public JobHandle LoadJobHandle => _loadJobHandle;
    public AssetType AssetType => _assetType;
    public ReadOnlySpan<Guid> Dependencies => _dependencies;
    public int RefCount => Volatile.Read(ref _refCount);

    public ref int StateValue => ref _state;
    public AssetState State
    {
        get => (AssetState)Volatile.Read(ref _state);
        set
        {
            Volatile.Write(ref _state, (int)value);
            if (Volatile.Read(ref _state) == (int)AssetState.Ready)
            {
                if (Interlocked.Exchange(ref _pendingReimport, 0) == 1)
                {
                    _assetManager.ReimportAsset(_assetId);  // re-queue
                }
            }
        }
    }

    protected AssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
    {
        _assetManager = manager;
        _resourceDatabase = resourceDatabase;
        _resourceManager = resourceManager;

        _assetId = assetId;
        _assetType = assetType;
        _dependencies = dependencies;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetLoadJobHandle(JobHandle handle)
    {
        _loadJobHandle = handle;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetPendingReimport()
    {
        Volatile.Write(ref _pendingReimport, 1);
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
    AssetType AssetType { get; }
    ReadOnlySpan<Guid> Dependencies { get; }
    int RefCount { get; }
    AssetState State { get; set; }

    void AddRef();
    int Release();
    void ReadAssetData(Span<byte> dst);
    public abstract void ReadAssetData<T>(ref T dst)
        where T : struct;
}

internal interface ILoadableAssetEntry : IAssetEntry
{
    Result OnLoadContent(Stream contentStream);
}

internal interface IProcessableAssetEntry : IAssetEntry
{
    Result<JobHandle> OnProcessing();
}

internal interface IUploadableAssetEntry : IAssetEntry
{
    Result OnRecordUploadCommands(ResourceStreamingContext context);
    void OnUploadComplete(ResourceStreamingContext context);
}
