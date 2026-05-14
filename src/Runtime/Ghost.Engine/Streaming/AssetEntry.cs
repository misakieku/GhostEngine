using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

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
internal abstract class AssetEntry
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

    private bool _pendingReimport;

    protected ResourceManager ResourceManager => _resourceManager;
    protected IResourceDatabase ResourceDatabase => _resourceDatabase;

    public AssetManager AssetManager => _assetManager;
    public Guid AssetId => _assetId;
    public JobHandle LoadJobHandle => _loadJobHandle;
    public AssetType AssetType => _assetType;
    public ReadOnlySpan<Guid> Dependencies => _dependencies;
    public int RefCount => Volatile.Read(ref _refCount);

    public ref bool PendingReimport => ref _pendingReimport;
    public ref int StateValue => ref _state;
    public AssetState State
    {
        get => (AssetState)Volatile.Read(ref _state);
        set => Volatile.Write(ref _state, (int)value);
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

    public abstract Result OnLoadContent(Stream contentStream);
    public abstract void OnReleaseResource();
}

internal abstract class ProcessableAssetEntry : AssetEntry
{
    protected ProcessableAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies)
    {
    }

    public abstract Result<JobHandle> OnProcessing(object? context);
}

internal abstract class UploadableAssetEntry : AssetEntry
{
    protected UploadableAssetEntry(AssetManager manager, IResourceDatabase resourceDatabase, ResourceManager resourceManager, Guid assetId, AssetType assetType, Guid[] dependencies)
        : base(manager, resourceDatabase, resourceManager, assetId, assetType, dependencies)
    {
    }

    public abstract Result OnRecordUploadCommands(ResourceStreamingContext context);
    public abstract void OnUploadComplete(ResourceStreamingContext context);
}
