using Ghost.Core;
using Ghost.Engine.RenderPipeline;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ghost.Engine.Streaming;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class ResolveAssetAttribute : Attribute
{
    public string VirtualPath { get; }

    public ResolveAssetAttribute(string virtualPath)
    {
        VirtualPath = virtualPath;
    }
}

public interface IContentProvider : IDisposable
{
    IReadOnlyList<ShaderCatalogEntry> ShaderCatalog { get; }
    Guid VirtualPathToGuid(string path);
    string VirtualPathFromGuid(Guid guid);
    bool HasAsset(Guid guid);
    Guid[] GetDependencies(Guid guid);
    AssetType GetAssetType(Guid guid);
    Result<AssetPayload> OpenReadAsync(Guid guid, CancellationToken token = default);
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
                entry.State = entry.FailureState;
                Logger.Error($"Failed to open asset {assetID}: {openResult.Message}");
                return;
            }

            var payload = openResult.Value;
            var result = loadable.OnLoadContent(ref payload);
            if (result.IsFailure)
            {
                entry.State = entry.FailureState;
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
            entry.State = entry.FailureState;
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
    private readonly MeshletStreamingManager _meshletStreamingManager;
    private readonly JobScheduler _jobScheduler;
    private readonly ShaderVariantRegistry _shaderVariants;
    private readonly ShaderBytecodePool? _bytecodePool;
    private readonly Lock _scheduleLock = new();

    private readonly ConcurrentDictionary<Guid, AssetEntry> _entries;

    internal IContentProvider ContentProvider => _contentProvider;
    internal ResourceStreamingProcessor StreamingProcessor => _streamingProcessor;
    internal ResourceManager ResourceManager => _resourceManager;

    /// <summary>
    /// Dense metadata registry for graphics shader variants.
    /// </summary>
    public ShaderVariantRegistry ShaderVariants => _shaderVariants;

    /// <summary>
    /// Coordinates meshlet/page-level streaming into the GPU page pool.
    /// Owned and managed by AssetManager.
    /// </summary>
    public MeshletStreamingManager MeshletStreamingManager => _meshletStreamingManager;

    internal AssetManager(RenderEngine renderEngine, IContentProvider contentProvider, ResourceStreamingProcessor streamingProcessor, JobScheduler jobScheduler, uint frameBufferCount, IRenderPipelineSettings? pipelineSettings = null)
        : this(renderEngine.GraphicsEngine.ResourceDatabase, renderEngine.ResourceManager, contentProvider, streamingProcessor, jobScheduler, renderEngine.GraphicsEngine, renderEngine.ShaderLibrary, frameBufferCount, pipelineSettings)
    {
    }

    internal AssetManager(IResourceDatabase resourceDatabase, ResourceManager resourceManager, IContentProvider contentProvider, ResourceStreamingProcessor streamingProcessor, JobScheduler jobScheduler, IGraphicsEngine graphicsEngine, ShaderLibrary shaderLibrary, uint frameBufferCount, IRenderPipelineSettings? pipelineSettings = null)
    {
        _resourceDatabase = resourceDatabase;
        _resourceManager = resourceManager;
        _contentProvider = contentProvider;
        _streamingProcessor = streamingProcessor;
        _jobScheduler = jobScheduler;

        _shaderVariants = new ShaderVariantRegistry(graphicsEngine, graphicsEngine.PipelineLibrary,
            shaderLibrary, resourceManager, contentProvider.ShaderCatalog);

        var pagePoolSlots = pipelineSettings?.GetPagePoolSlotCount() ?? MeshletStreamingManager.DEFAULT_PAGE_POOL_SLOTS;
        var maxRequestsPerFrame = pipelineSettings?.MaxStreamingRequestsPerFrame ?? MeshletStreamingManager.DEFAULT_MAX_REQUESTS_PER_FRAME;
        var maxPagesPerFrame = pipelineSettings?.MaxStreamingPagesPerFrame ?? MeshletStreamingManager.DEFAULT_MAX_PAGES_PER_FRAME;
        _meshletStreamingManager = new MeshletStreamingManager(graphicsEngine.ResourceAllocator, resourceDatabase, frameBufferCount, pagePoolSlots, maxRequestsPerFrame: maxRequestsPerFrame, maxPagesPerFrame: maxPagesPerFrame, jobScheduler: jobScheduler);

        if (contentProvider.HasAsset(ShaderBytecodePool.POOL_ASSET_ID))
        {
            var readResult = contentProvider.OpenReadAsync(ShaderBytecodePool.POOL_ASSET_ID);
            if (readResult.IsSuccess && readResult.Value.IsValid)
            {
                try
                {
                    var payload = readResult.Value;
                    _bytecodePool = new ShaderBytecodePool(ref payload);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Failed to initialize shader bytecode pool: {ex.Message}");
                }
            }
        }

        _entries = new ConcurrentDictionary<Guid, AssetEntry>();
    }

    internal bool TryGetPooledBytecode(ulong hash, out ShaderByteCode byteCode)
    {
        if (_bytecodePool != null)
        {
            return _bytecodePool.TryGetBytecode(hash, out byteCode);
        }

        byteCode = default;
        return false;
    }

    internal bool TryGetEntry(Guid guid, [NotNullWhen(true)] out AssetEntry? entry)
    {
        return _entries.TryGetValue(guid, out entry);
    }

    internal void RegisterEntryForTesting(Guid guid, AssetEntry entry)
    {
        _entries[guid] = entry;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool RemoveEntry(Guid guid)
    {
        return _entries.TryRemove(guid, out var _);
    }

    internal bool TryUnloadIfZero(AssetEntry entry)
    {
        lock (_scheduleLock)
        {
            if (entry.RefCount != 0 || entry.State is not (AssetState.Ready or AssetState.Failed) || !entry.TryMarkUnloaded())
            {
                return false;
            }

            _entries.TryRemove(entry.AssetId, out _);
            return true;
        }
    }

    internal bool TryForceUnloadZeroRefOnDispose(AssetEntry entry)
    {
        lock (_scheduleLock)
        {
            if (entry.RefCount != 0 || !entry.TryMarkUnloaded())
            {
                return false;
            }

            _entries.TryRemove(entry.AssetId, out _);
            return true;
        }
    }

    private readonly struct ScheduleFrame(AssetEntry entry, bool childrenVisited)
    {
        public readonly AssetEntry Entry = entry;
        public readonly bool ChildrenVisited = childrenVisited;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private AssetEntry GetOrCreateEntry(Guid guid)
    {
        lock (_scheduleLock)
        {
            var entry = GetOrCreateUnscheduled(guid, out var needsSchedule);
            if (needsSchedule)
            {
                ScheduleEntryGraph(entry);
            }

            return entry;
        }
    }

    private AssetEntry GetOrCreateUnscheduled(Guid guid, out bool needsSchedule)
    {
        if (!_entries.TryGetValue(guid, out var entry))
        {
            var type = _contentProvider.GetAssetType(guid);
            var deps = _contentProvider.GetDependencies(guid);
            var name = _contentProvider.VirtualPathFromGuid(guid);

            var newEntry = AssetEntryFactory.CreateNewEntry(this, _resourceDatabase, _resourceManager, guid, type, deps, name);
            entry = _entries.GetOrAdd(guid, newEntry);
        }

        entry.AddRef();
        needsSchedule = entry.State == AssetState.Unloaded;
        return entry;
    }

    private void ScheduleEntryGraph(AssetEntry root, bool forceReimportRoot = false)
    {
        if (!forceReimportRoot && root.State != AssetState.Unloaded)
        {
            return;
        }

        using var scope = AllocationManager.CreateStackScope();
        var stack = new Stack<ScheduleFrame>();
        var expandedSet = new HashSet<Guid>();
        var scheduledInCall = new HashSet<Guid>();

        stack.Push(new ScheduleFrame(root, false));

        while (stack.Count > 0)
        {
            var frame = stack.Pop();
            var entry = frame.Entry;

            if (frame.ChildrenVisited)
            {
                if (!scheduledInCall.Add(entry.AssetId))
                {
                    continue;
                }

                var isReimport = entry.ConsumeReimporting();
                if (!isReimport)
                {
                    var prev = Interlocked.CompareExchange(ref entry.StateValue, (int)AssetState.Scheduled, (int)AssetState.Unloaded);
                    if (prev != (int)AssetState.Unloaded)
                    {
                        continue;
                    }
                }

                var directDeps = entry.Dependencies;
                using var depHandles = new UnsafeList<JobHandle>(Math.Max(directDeps.Length, 1), scope.AllocationHandle);
                for (var i = 0; i < directDeps.Length; i++)
                {
                    var depGuid = directDeps[i];
                    if (depGuid == Guid.Empty)
                    {
                        continue;
                    }

                    if (_entries.TryGetValue(depGuid, out var depEntry) && depEntry.State != AssetState.Ready && depEntry.LoadJobHandle.IsValid)
                    {
                        depHandles.Add(depEntry.LoadJobHandle);
                    }
                }

                var dependency = depHandles.Count > 0 ? _jobScheduler.CombineDependencies(depHandles) : default;
                entry.SetCombinedDependencyJobHandle(dependency);

                if (!dependency.IsValid && entry is not ILoadableAssetEntry)
                {
                    entry.State = AssetState.Loaded;
                    if (!StreamingProcessor.EnqueueForProcess(entry))
                    {
                        entry.State = AssetState.Ready;
                    }

                    continue;
                }

                var job = new LoadAssetJob
                {
                    assetID = entry.AssetId,
                    assetType = entry.AssetType,
                    assetManager = this,
                };

                // Use low priority to avoid blocking main thread critical tasks like rendering and physics.
                var handle = _jobScheduler.Schedule(ref job, JobPriority.Low, dependency);
                entry.SetLoadJobHandle(handle);
                continue;
            }

            if (scheduledInCall.Contains(entry.AssetId) || !expandedSet.Add(entry.AssetId))
            {
                continue;
            }

            stack.Push(new ScheduleFrame(entry, true));

            var deps = entry.Dependencies;
            var skipAddRef = forceReimportRoot && ReferenceEquals(entry, root);
            for (var i = 0; i < deps.Length; i++)
            {
                var depGuid = deps[i];
                if (depGuid == Guid.Empty || depGuid == entry.AssetId)
                {
                    continue;
                }

                if (skipAddRef)
                {
                    if (_entries.TryGetValue(depGuid, out var existingDep) && existingDep.State == AssetState.Unloaded && !expandedSet.Contains(depGuid))
                    {
                        stack.Push(new ScheduleFrame(existingDep, false));
                    }

                    continue;
                }

                var depEntry = GetOrCreateUnscheduled(depGuid, out var depNeedsSchedule);
                if (depNeedsSchedule && !expandedSet.Contains(depGuid))
                {
                    stack.Push(new ScheduleFrame(depEntry, false));
                }
            }
        }
    }

    internal Guid[] CollectUniqueTransitiveDependencies(Guid rootId)
    {
        var queue = new Queue<Guid>();
        var visited = new HashSet<Guid> { rootId };
        var result = new List<Guid>();

        EnqueueDirectDeps(rootId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);
            EnqueueDirectDeps(current);
        }

        return result.Count == 0 ? Array.Empty<Guid>() : result.ToArray();

        void EnqueueDirectDeps(Guid id)
        {
            var deps = _entries.TryGetValue(id, out var existing)
                ? existing.Dependencies
                : _contentProvider.GetDependencies(id);

            for (var i = 0; i < deps.Length; i++)
            {
                var dep = deps[i];
                if (dep != Guid.Empty && visited.Add(dep))
                {
                    queue.Enqueue(dep);
                }
            }
        }
    }

    /// <summary>
    /// Re-schedules a loaded asset entry to reload its baked content and reconcile updated dependencies.
    /// </summary>
    public void ReimportAsset(Guid guid)
    {
        lock (_scheduleLock)
        {
            if (!_entries.TryGetValue(guid, out var entry))
            {
                return;
            }

            if (Interlocked.CompareExchange(ref entry.StateValue, (int)AssetState.Scheduled, (int)AssetState.Ready) == (int)AssetState.Ready)
            {
                ReconcileDependenciesForReimport(entry);
                entry.MarkReimporting();
                ScheduleEntryGraph(entry, forceReimportRoot: true);
            }
            else
            {
                entry.SetPendingReimport();
            }
        }
    }

    private void ReconcileDependenciesForReimport(AssetEntry entry)
    {
        var updatedDeps = _contentProvider.GetDependencies(entry.AssetId);
        var oldDeps = entry.Dependencies;
        var depsChanged = !oldDeps.SequenceEqual(updatedDeps);

        for (var i = 0; i < updatedDeps.Length; i++)
        {
            var depGuid = updatedDeps[i];
            if (depGuid == Guid.Empty)
            {
                continue;
            }

            if (!ContainsGuid(oldDeps, depGuid) || !_entries.TryGetValue(depGuid, out _))
            {
                GetOrCreateUnscheduled(depGuid, out _);
            }
        }

        if (depsChanged)
        {
            for (var i = 0; i < oldDeps.Length; i++)
            {
                var oldGuid = oldDeps[i];
                if (oldGuid != Guid.Empty && !ContainsGuid(updatedDeps, oldGuid) && _entries.TryGetValue(oldGuid, out var oldEntry) && oldEntry.State == AssetState.Ready)
                {
                    oldEntry.Release();
                }
            }
        }

        entry.UpdateDependencies(updatedDeps);
    }

    private static bool ContainsGuid(ReadOnlySpan<Guid> span, Guid target)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == target)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolve the asset ID from the virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual path of the asset.</param>
    /// <returns>The asset ID, or Guid.Empty if the asset is not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Guid ResolveAssetID(string virtualPath)
    {
        return _contentProvider.VirtualPathToGuid(virtualPath);
    }

    /// <summary>
    /// Try to resolve the asset entry from the virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual path of the asset.</param>
    /// <param name="asset">The resolved asset entry, or null if the asset is not found.</param>
    /// <returns>true if the asset is found, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryResolveAsset(string virtualPath, [NotNullWhen(true)] out IAssetEntry? asset)
    {
        var guid = _contentProvider.VirtualPathToGuid(virtualPath);
        if (guid == Guid.Empty)
        {
            asset = null;
            return false;
        }

        asset = GetOrCreateEntry(guid);
        return true;
    }

    /// <summary>
    /// Try to resolve the asset entry from the asset ID.
    /// </summary>
    /// <param name="assetID">The ID of the asset to resolve.</param>
    /// <param name="asset">The resolved asset entry, or null if the asset is not found.</param>
    /// <returns>true if the asset is found, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryResolveAsset(Guid assetID, [NotNullWhen(true)] out IAssetEntry? asset)
    {
        if (assetID == Guid.Empty)
        {
            asset = null;
            return false;
        }

        asset = GetOrCreateEntry(assetID);
        return true;
    }

    /// <summary>
    /// Resolve the asset entry from the asset ID.
    /// </summary>
    /// <param name="assetID">The ID of the asset to resolve.</param>
    /// <returns>The resolved asset entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the asset is not found.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAssetEntry ResolveAsset(Guid assetID)
    {
        if (!TryResolveAsset(assetID, out var asset))
        {
            throw new ArgumentException($"Asset not found for ID: {assetID}", nameof(assetID));
        }

        return asset;
    }

    /// <summary>
    /// Resolve the asset entry from the virtual path.
    /// </summary>
    /// <param name="virtualPath">The virtual path of the asset to resolve.</param>
    /// <returns>The resolved asset entry.</returns>
    /// <exception cref="ArgumentException">Thrown when the asset is not found.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAssetEntry ResolveAsset(string virtualPath)
    {
        if (!TryResolveAsset(virtualPath, out var asset))
        {
            throw new ArgumentException($"Asset not found for virtual path: {virtualPath}", nameof(virtualPath));
        }

        return asset;
    }

    /// <summary>
    /// Release the asset entry from the asset ID. This will decrease the reference count of the asset entry, and if the reference count reaches zero, the asset entry will be removed from the manager.
    /// </summary>
    /// <param name="assetID">The ID of the asset to release.</param>
    /// <returns>The new reference count of the asset entry.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReleaseAsset(Guid assetID)
    {
        if (assetID == Guid.Empty)
        {
            return 0;
        }

        if (!_entries.TryGetValue(assetID, out var entry))
        {
            return 0;
        }

        Logger.DebugAssert(entry.AssetType != AssetType.Unknown);
        return entry.Release();
    }

    /// <summary>
    /// Release the asset entry from the virtual path. This will decrease the reference count of the asset entry, and if the reference count reaches zero, the asset entry will be removed from the manager.
    /// </summary>
    /// <param name="virtualPath">The virtual path of the asset to release.</param>
    /// <returns>The new reference count of the asset entry.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReleaseAsset(string virtualPath)
    {
        var assetID = _contentProvider.VirtualPathToGuid(virtualPath);
        if (assetID == Guid.Empty)
        {
            return 0;
        }

        return ReleaseAsset(assetID);
    }

    public void Dispose()
    {
        foreach (var kvp in _entries)
        {
            var handle = kvp.Value.LoadJobHandle;
            if (handle.IsValid)
            {
                _jobScheduler.Wait(handle);
            }
        }

        bool drainedAny;
        do
        {
            drainedAny = false;
            foreach (var kvp in _entries)
            {
                if (kvp.Value.RefCount == 0 && kvp.Value.TryForceUnloadOnDispose())
                {
                    drainedAny = true;
                }
            }
        }
        while (drainedAny);

        Logger.DebugAssert(_entries.IsEmpty, $"There are still {_entries.Count} assets in the manager. Make sure to release all assets before disposing the manager.");

        _entries.Clear();
        _shaderVariants.Dispose();
        _bytecodePool?.Dispose();
        _contentProvider.Dispose();
        _meshletStreamingManager.Dispose();

        GC.SuppressFinalize(this);
    }
}
