using System.Collections.Concurrent;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Ghost.Graphics.Services;

namespace Ghost.Editor.Core.Sync;

public readonly struct PendingPropertyMutation
{
    public required Entity Entity { get; init; }
    public required Type ComponentType { get; init; }
    public required string FieldName { get; init; }
    public required object? Value { get; init; }
}

public sealed class EditorSyncBridge
{
    private readonly ConcurrentQueue<PendingPropertyMutation> _pendingMutations = new();
    private readonly ConcurrentQueue<Action<World>> _pendingWorldActions = new();
    private readonly Dictionary<Entity, string> _entityNames = new();
    private readonly Dictionary<Entity, int> _entityNodeIds = new();
    private readonly Dictionary<Entity, Guid> _entityGuids = new();
    private readonly Dictionary<Entity, PrefabLinkInfo> _prefabLinks = new();
    private readonly Dictionary<Entity, (Guid MeshGuid, Guid[] MaterialGuids)> _initialMeshAuthoring = new();
    private readonly Dictionary<ushort, (string Name, string? Path)> _registeredScenes = new() { [0] = ("Untitled", null) };
    private readonly List<IAssetEntry> _sceneRetainedAssets = new();
    private readonly List<PropertyNode> _trackedResourceNodes = new();
    private readonly object _metadataLock = new();
    private readonly object _snapshotLock = new();
    private readonly object _resourceNodesLock = new();
    private SceneSnapshot? _latestSnapshot;
    private long _snapshotVersion = 0;
    private int _nextEntityId = 1;

    public static EditorSyncBridge Instance { get; } = new();

    public ushort ActiveSceneId { get; set; } = 0;

    public string? ActiveScenePath { get; set; }

    public bool HasUnsavedSceneChanges { get; set; }

    public EditorDirtyTracker DirtyTracker { get; } = new();

    public AssetManager? AssetManager { get; set; }

    public ResourceManager? ResourceManager { get; set; }

    public long SnapshotVersion => Volatile.Read(ref _snapshotVersion);

    public SceneSnapshot? LatestSnapshot => Volatile.Read(ref _latestSnapshot);

    public event Action<SceneSnapshot>? SnapshotPublished;

    public int AllocateEntityId()
    {
        return Interlocked.Increment(ref _nextEntityId);
    }

    public void SetEntityName(Entity entity, string name)
    {
        if (!entity.IsValid || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        lock (_metadataLock)
        {
            _entityNames[entity] = name.Trim();
        }
    }

    public bool TryGetEntityName(Entity entity, out string name)
    {
        if (!entity.IsValid)
        {
            name = string.Empty;
            return false;
        }

        lock (_metadataLock)
        {
            return _entityNames.TryGetValue(entity, out name!);
        }
    }

    public void RegisterEntityMapping(Entity entity, int nodeId)
    {
        if (!entity.IsValid)
        {
            return;
        }

        lock (_metadataLock)
        {
            _entityNodeIds[entity] = nodeId;
        }
    }

    public bool TryGetMappedNodeId(Entity entity, out int nodeId)
    {
        if (!entity.IsValid)
        {
            nodeId = 0;
            return false;
        }

        lock (_metadataLock)
        {
            return _entityNodeIds.TryGetValue(entity, out nodeId);
        }
    }

    public void RemoveEntityMetadata(Entity entity)
    {
        if (!entity.IsValid)
        {
            return;
        }

        lock (_metadataLock)
        {
            _entityNames.Remove(entity);
            _entityNodeIds.Remove(entity);
            _entityGuids.Remove(entity);
            _prefabLinks.Remove(entity);
            _initialMeshAuthoring.Remove(entity);
        }
    }

    public void RegisterEntityGuid(Entity entity, Guid editorGuid)
    {
        if (!entity.IsValid || editorGuid == Guid.Empty)
        {
            return;
        }

        lock (_metadataLock)
        {
            _entityGuids[entity] = editorGuid;
        }
    }

    public bool TryGetEntityGuid(Entity entity, out Guid editorGuid)
    {
        if (!entity.IsValid)
        {
            editorGuid = Guid.Empty;
            return false;
        }

        lock (_metadataLock)
        {
            return _entityGuids.TryGetValue(entity, out editorGuid);
        }
    }

    public void SetPrefabLink(Entity entity, PrefabLinkInfo? link)
    {
        if (!entity.IsValid)
        {
            return;
        }

        lock (_metadataLock)
        {
            if (link == null)
            {
                _prefabLinks.Remove(entity);
            }
            else
            {
                _prefabLinks[entity] = link;
            }
        }
    }

    public bool TryGetPrefabLink(Entity entity, out PrefabLinkInfo link)
    {
        if (!entity.IsValid)
        {
            link = null!;
            return false;
        }

        lock (_metadataLock)
        {
            return _prefabLinks.TryGetValue(entity, out link!);
        }
    }

    public void SetInitialMeshAuthoring(Entity entity, Guid meshGuid, Guid[] materialGuids)
    {
        if (!entity.IsValid)
        {
            return;
        }

        lock (_metadataLock)
        {
            _initialMeshAuthoring[entity] = (meshGuid, materialGuids);
        }
    }

    public bool TryConsumeInitialMeshAuthoring(Entity entity, out Guid meshGuid, out Guid[] materialGuids)
    {
        if (!entity.IsValid)
        {
            meshGuid = Guid.Empty;
            materialGuids = Array.Empty<Guid>();
            return false;
        }

        lock (_metadataLock)
        {
            if (_initialMeshAuthoring.Remove(entity, out var authoring))
            {
                meshGuid = authoring.MeshGuid;
                materialGuids = authoring.MaterialGuids;
                return true;
            }
        }

        meshGuid = Guid.Empty;
        materialGuids = Array.Empty<Guid>();
        return false;
    }

    public void RegisterScene(ushort sceneId, string? scenePath, string? nameOverride = null)
    {
        var displayName = !string.IsNullOrWhiteSpace(nameOverride)
            ? nameOverride.Trim()
            : (!string.IsNullOrWhiteSpace(scenePath) ? Path.GetFileNameWithoutExtension(scenePath) : "Untitled");

        lock (_metadataLock)
        {
            _registeredScenes[sceneId] = (displayName, scenePath);
        }
    }

    public IReadOnlyList<SceneNode> GetSceneNodeSnapshots(IReadOnlyList<EntityNode> entities)
    {
        KeyValuePair<ushort, (string Name, string? Path)>[] entries;
        lock (_metadataLock)
        {
            if (_registeredScenes.Count == 0)
            {
                _registeredScenes[0] = ("Untitled", ActiveScenePath);
            }

            entries = _registeredScenes.OrderBy(static kvp => kvp.Key).ToArray();
        }

        var result = new SceneNode[entries.Length];
        for (var i = 0; i < entries.Length; i++)
        {
            var sceneId = entries[i].Key;
            var info = entries[i].Value;
            var count = 0;
            for (var e = 0; e < entities.Count; e++)
            {
                if (entities[e].SceneId == sceneId)
                {
                    count++;
                }
            }

            var isDirty = HasUnsavedSceneChanges && sceneId == ActiveSceneId;
            result[i] = new SceneNode(-(sceneId + 1), info.Name, sceneId, info.Path, isDirty, count);
        }

        return result;
    }

    public void ClearSceneMetadata()
    {
        lock (_metadataLock)
        {
            _entityNames.Clear();
            _entityNodeIds.Clear();
            _entityGuids.Clear();
            _prefabLinks.Clear();
            _initialMeshAuthoring.Clear();
            _registeredScenes.Clear();
            _registeredScenes[0] = ("Untitled", ActiveScenePath);
        }
    }

    public Entity ResolveEcsEntity(int nodeId)
    {
        var snapshot = LatestSnapshot;
        if (snapshot == null)
        {
            return Entity.Invalid;
        }

        for (var i = 0; i < snapshot.Entities.Count; i++)
        {
            if (snapshot.Entities[i].Id == nodeId)
            {
                return snapshot.Entities[i].EcsEntity;
            }
        }

        return Entity.Invalid;
    }

    public EntityNode? FindEntityNode(int nodeId)
    {
        var snapshot = LatestSnapshot;
        if (snapshot == null)
        {
            return null;
        }

        for (var i = 0; i < snapshot.Entities.Count; i++)
        {
            if (snapshot.Entities[i].Id == nodeId)
            {
                return snapshot.Entities[i];
            }
        }

        return null;
    }

    public EntityNode? FindEntityNode(Guid editorGuid)
    {
        if (editorGuid == Guid.Empty)
        {
            return null;
        }

        var snapshot = LatestSnapshot;
        if (snapshot == null)
        {
            return null;
        }

        for (var i = 0; i < snapshot.Entities.Count; i++)
        {
            if (snapshot.Entities[i].EditorGuid == editorGuid)
            {
                return snapshot.Entities[i];
            }
        }

        return null;
    }

    public EntityNode? FindEntityNode(Entity ecsEntity)
    {
        if (!ecsEntity.IsValid)
        {
            return null;
        }

        var snapshot = LatestSnapshot;
        if (snapshot == null)
        {
            return null;
        }

        for (var i = 0; i < snapshot.Entities.Count; i++)
        {
            if (snapshot.Entities[i].EcsEntity == ecsEntity)
            {
                return snapshot.Entities[i];
            }
        }

        return null;
    }

    public void EnqueueMutation(Entity entity, Type componentType, string fieldName, object? value)
    {
        _pendingMutations.Enqueue(new PendingPropertyMutation
        {
            Entity = entity,
            ComponentType = componentType,
            FieldName = fieldName,
            Value = value
        });
    }

    public void EnqueueWorldAction(Action<World> action)
    {
        _pendingWorldActions.Enqueue(action);
    }

    public void DrainMutations(List<PendingPropertyMutation> destination)
    {
        destination.Clear();
        while (_pendingMutations.TryDequeue(out var mutation))
        {
            destination.Add(mutation);
        }
    }

    /// <summary>
    /// Binds an asset reference to the currently open editor scene so it stays alive until the scene is unloaded.
    /// </summary>
    public void RetainSceneAsset(IAssetEntry entry)
    {
        entry.AddRef();
        lock (_metadataLock)
        {
            _sceneRetainedAssets.Add(entry);
        }
    }

    /// <summary>
    /// Releases all asset references retained by the currently open editor scene.
    /// </summary>
    public void ReleaseSceneAssets()
    {
        IAssetEntry[] snapshot;
        lock (_metadataLock)
        {
            if (_sceneRetainedAssets.Count == 0)
            {
                return;
            }

            snapshot = _sceneRetainedAssets.ToArray();
            _sceneRetainedAssets.Clear();
        }

        for (var i = 0; i < snapshot.Length; i++)
        {
            snapshot[i].Release();
        }
    }

    public void DrainWorldActions(World world)
    {
        while (_pendingWorldActions.TryDequeue(out var action))
        {
            action(world);
        }
    }

    /// <summary>
    /// Registers a property node that may hold streaming asset references so editor shutdown can release them deterministically.
    /// </summary>
    public void TrackResourceNode(PropertyNode node)
    {
        lock (_resourceNodesLock)
        {
            _trackedResourceNodes.Add(node);
        }
    }

    /// <summary>
    /// Releases all streaming asset references held by tracked property nodes and clears the tracking list.
    /// </summary>
    public void ReleaseAllTrackedResources()
    {
        PropertyNode[] snapshot;
        lock (_resourceNodesLock)
        {
            if (_trackedResourceNodes.Count == 0)
            {
                return;
            }

            snapshot = _trackedResourceNodes.ToArray();
            _trackedResourceNodes.Clear();
        }

        for (var i = 0; i < snapshot.Length; i++)
        {
            snapshot[i].ReleaseResources();
        }
    }

    public void PublishSnapshot(SceneSnapshot snapshot)
    {
        var maxId = 0;
        for (var i = 0; i < snapshot.Entities.Count; i++)
        {
            if (snapshot.Entities[i].Id > maxId)
            {
                maxId = snapshot.Entities[i].Id;
            }
        }

        while (true)
        {
            var current = Volatile.Read(ref _nextEntityId);
            if (maxId <= current)
            {
                break;
            }

            if (Interlocked.CompareExchange(ref _nextEntityId, maxId, current) == current)
            {
                break;
            }
        }

        Volatile.Write(ref _latestSnapshot, snapshot);
        Interlocked.Increment(ref _snapshotVersion);
        SnapshotPublished?.Invoke(snapshot);
    }

    /// <summary>
    /// Atomically transforms the current snapshot entity list and publishes a new <see cref="SceneSnapshot"/>.
    /// </summary>
    public SceneSnapshot MutateSnapshot(Func<IReadOnlyList<EntityNode>, List<EntityNode>> mutator)
    {
        SceneSnapshot updated;
        lock (_snapshotLock)
        {
            var current = LatestSnapshot;
            var currentEntities = current?.Entities ?? Array.Empty<EntityNode>();
            var nextEntities = mutator(currentEntities);
            var scenes = GetSceneNodeSnapshots(nextEntities);
            updated = new SceneSnapshot(scenes, nextEntities, current?.FrameTimeMs ?? 16.6f, current?.Fps ?? 60.0f);
            PublishSnapshot(updated);
        }

        return updated;
    }
}
