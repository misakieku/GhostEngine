using Ghost.Engine.Streaming;
using Ghost.Entities;
using Ghost.Graphics.Services;
using System.Collections.Concurrent;

namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Central editor authoring state and main-thread dispatch bridge for the active editor <see cref="World"/>.
/// Holds editor-only entity/scene metadata, live authoring node hierarchies, dirty tracking, and asset retention.
/// </summary>
public sealed class EditorSyncBridge
{
    private readonly ConcurrentQueue<Action<World>> _pendingMainThreadActions = new();
    private readonly Dictionary<Entity, string> _entityNames = new();
    private readonly Dictionary<Entity, int> _entityNodeIds = new();
    private readonly Dictionary<Entity, Guid> _entityGuids = new();
    private readonly Dictionary<Entity, PrefabLinkInfo> _prefabLinks = new();
    private readonly Dictionary<Entity, (Guid MeshGuid, Guid[] MaterialGuids)> _initialMeshAuthoring = new();
    private readonly Dictionary<ushort, (string Name, string? Path, bool IsDirty)> _registeredScenes = new() { [0] = ("Untitled", null, false) };
    private readonly List<IAssetEntry> _sceneRetainedAssets = new();
    private readonly List<PropertyNode> _trackedResourceNodes = new();
    private readonly List<EntityNode> _entities = new();
    private readonly object _resourceNodesLock = new();

    private long _revision;
    private int _nextEntityId = 1;

    public static EditorSyncBridge Instance { get; } = new();

    public World? World { get; set; }

    public ushort ActiveSceneId { get; set; } = 0;

    public string? ActiveScenePath { get; set; }

    public bool HasUnsavedSceneChanges
    {
        get => AnySceneDirty;
        set => SetSceneDirty(ActiveSceneId, value);
    }

    public bool AnySceneDirty
    {
        get
        {
            foreach (var entry in _registeredScenes.Values)
            {
                if (entry.IsDirty)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public EditorDirtyTracker DirtyTracker { get; } = new();

    public AssetManager? AssetManager { get; set; }

    public ResourceManager? ResourceManager { get; set; }

    public IReadOnlyList<EntityNode> Entities => _entities;

    public int EntityCount => _entities.Count;

    public long Revision => _revision;

    public void BumpRevision()
    {
        _revision++;
    }

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

        _entityNames[entity] = name.Trim();
    }

    public bool TryGetEntityName(Entity entity, out string name)
    {
        if (!entity.IsValid)
        {
            name = string.Empty;
            return false;
        }

        return _entityNames.TryGetValue(entity, out name!);
    }

    public void RegisterEntityMapping(Entity entity, int nodeId)
    {
        if (!entity.IsValid)
        {
            return;
        }

        _entityNodeIds[entity] = nodeId;
    }

    public bool TryGetMappedNodeId(Entity entity, out int nodeId)
    {
        if (!entity.IsValid)
        {
            nodeId = 0;
            return false;
        }

        return _entityNodeIds.TryGetValue(entity, out nodeId);
    }

    public void RemoveEntityMetadata(Entity entity)
    {
        if (!entity.IsValid)
        {
            return;
        }

        _entityNames.Remove(entity);
        _entityNodeIds.Remove(entity);
        _entityGuids.Remove(entity);
        _prefabLinks.Remove(entity);
        _initialMeshAuthoring.Remove(entity);
    }

    public void RegisterEntityGuid(Entity entity, Guid editorGuid)
    {
        if (!entity.IsValid || editorGuid == Guid.Empty)
        {
            return;
        }

        _entityGuids[entity] = editorGuid;
    }

    public bool TryGetEntityGuid(Entity entity, out Guid editorGuid)
    {
        if (!entity.IsValid)
        {
            editorGuid = Guid.Empty;
            return false;
        }

        return _entityGuids.TryGetValue(entity, out editorGuid);
    }

    public void SetPrefabLink(Entity entity, PrefabLinkInfo? link)
    {
        if (!entity.IsValid)
        {
            return;
        }

        if (link == null)
        {
            _prefabLinks.Remove(entity);
        }
        else
        {
            _prefabLinks[entity] = link;
        }
    }

    public bool TryGetPrefabLink(Entity entity, out PrefabLinkInfo link)
    {
        if (!entity.IsValid)
        {
            link = null!;
            return false;
        }

        return _prefabLinks.TryGetValue(entity, out link!);
    }

    public void SetInitialMeshAuthoring(Entity entity, Guid meshGuid, Guid[] materialGuids)
    {
        if (!entity.IsValid)
        {
            return;
        }

        _initialMeshAuthoring[entity] = (meshGuid, materialGuids);
    }

    public bool TryConsumeInitialMeshAuthoring(Entity entity, out Guid meshGuid, out Guid[] materialGuids)
    {
        if (!entity.IsValid)
        {
            meshGuid = Guid.Empty;
            materialGuids = Array.Empty<Guid>();
            return false;
        }

        if (_initialMeshAuthoring.Remove(entity, out var authoring))
        {
            meshGuid = authoring.MeshGuid;
            materialGuids = authoring.MaterialGuids;
            return true;
        }

        meshGuid = Guid.Empty;
        materialGuids = Array.Empty<Guid>();
        return false;
    }

    public void RegisterScene(ushort sceneId, string? scenePath, string? nameOverride = null, bool? isDirty = null)
    {
        var displayName = !string.IsNullOrWhiteSpace(nameOverride)
            ? nameOverride.Trim()
            : (!string.IsNullOrWhiteSpace(scenePath) ? Path.GetFileNameWithoutExtension(scenePath) : "Untitled");

        var resolvedDirty = isDirty ?? (_registeredScenes.TryGetValue(sceneId, out var existing) && existing.IsDirty);
        _registeredScenes[sceneId] = (displayName, scenePath, resolvedDirty);
        _revision++;
    }

    public void SetSceneDirty(ushort sceneId, bool isDirty)
    {
        if (_registeredScenes.TryGetValue(sceneId, out var existing))
        {
            if (existing.IsDirty == isDirty)
            {
                return;
            }

            _registeredScenes[sceneId] = (existing.Name, existing.Path, isDirty);
            _revision++;
        }
        else if (sceneId == 0)
        {
            var displayName = !string.IsNullOrWhiteSpace(ActiveScenePath) ? Path.GetFileNameWithoutExtension(ActiveScenePath) : "Untitled";
            _registeredScenes[0] = (displayName, ActiveScenePath, isDirty);
            _revision++;
        }
    }

    public bool IsSceneDirty(ushort sceneId)
    {
        return _registeredScenes.TryGetValue(sceneId, out var existing) && existing.IsDirty;
    }

    public bool TryGetScenePath(ushort sceneId, out string? scenePath)
    {
        if (_registeredScenes.TryGetValue(sceneId, out var existing))
        {
            scenePath = existing.Path;
            return true;
        }

        scenePath = null;
        return false;
    }

    public ushort AllocateSceneId()
    {
        var candidate = unchecked((ushort)(ActiveSceneId + 1));
        while (_registeredScenes.ContainsKey(candidate))
        {
            candidate = unchecked((ushort)(candidate + 1));
        }

        return candidate;
    }

    public bool UnregisterScene(ushort sceneId)
    {
        if (_registeredScenes.Remove(sceneId))
        {
            _revision++;
            return true;
        }

        return false;
    }

    public IReadOnlyList<ushort> GetRegisteredSceneIds()
    {
        return _registeredScenes.Keys.OrderBy(static k => k).ToArray();
    }

    public IReadOnlyList<SceneNode> GetSceneNodes() => GetSceneNodes(_entities);

    public IReadOnlyList<SceneNode> GetSceneNodes(IReadOnlyList<EntityNode> entities)
    {
        if (_registeredScenes.Count == 0)
        {
            _registeredScenes[0] = ("Untitled", ActiveScenePath, false);
        }

        var entries = _registeredScenes.OrderBy(static kvp => kvp.Key).ToArray();
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

            result[i] = new SceneNode(-(sceneId + 1), info.Name, sceneId, info.Path, info.IsDirty, count);
        }

        return result;
    }

    public void ClearSceneMetadata()
    {
        _entityNames.Clear();
        _entityNodeIds.Clear();
        _entityGuids.Clear();
        _prefabLinks.Clear();
        _initialMeshAuthoring.Clear();
        _registeredScenes.Clear();
        _registeredScenes[0] = ("Untitled", ActiveScenePath, false);
        _entities.Clear();
        _revision++;
    }

    public void SetEntities(IReadOnlyList<EntityNode> entities)
    {
        _entities.Clear();
        var maxId = 0;
        for (var i = 0; i < entities.Count; i++)
        {
            var node = entities[i];
            _entities.Add(node);
            if (node.Id > maxId)
            {
                maxId = node.Id;
            }
        }

        AdvanceNextEntityId(maxId);
        _revision++;
    }

    public void AddEntityNode(EntityNode node)
    {
        if (!_entities.Contains(node))
        {
            _entities.Add(node);
            AdvanceNextEntityId(node.Id);
            _revision++;
        }
    }

    public void MutateEntities(Action<List<EntityNode>> mutator)
    {
        mutator(_entities);
        var maxId = 0;
        for (var i = 0; i < _entities.Count; i++)
        {
            if (_entities[i].Id > maxId)
            {
                maxId = _entities[i].Id;
            }
        }

        AdvanceNextEntityId(maxId);
        _revision++;
    }

    public Entity ResolveEcsEntity(int nodeId)
    {
        for (var i = 0; i < _entities.Count; i++)
        {
            if (_entities[i].Id == nodeId)
            {
                return _entities[i].EcsEntity;
            }
        }

        return Entity.Invalid;
    }

    public EntityNode? FindEntityNode(int nodeId)
    {
        for (var i = 0; i < _entities.Count; i++)
        {
            if (_entities[i].Id == nodeId)
            {
                return _entities[i];
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

        for (var i = 0; i < _entities.Count; i++)
        {
            if (_entities[i].EditorGuid == editorGuid)
            {
                return _entities[i];
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

        for (var i = 0; i < _entities.Count; i++)
        {
            if (_entities[i].EcsEntity == ecsEntity)
            {
                return _entities[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Queues an action from a background thread (such as async asset baking) to execute on the main editor thread.
    /// </summary>
    public void EnqueueMainThreadAction(Action<World> action)
    {
        _pendingMainThreadActions.Enqueue(action);
    }

    /// <summary>
    /// Drains and executes all queued background-thread actions against <paramref name="world"/> on the main thread.
    /// </summary>
    public void DrainMainThreadActions(World world)
    {
        while (_pendingMainThreadActions.TryDequeue(out var action))
        {
            action(world);
        }
    }

    /// <summary>
    /// Completes any pending async asset handle resolutions on tracked property nodes.
    /// </summary>
    public void PumpPendingPropertyResolutions()
    {
        for (var e = 0; e < _entities.Count; e++)
        {
            var components = _entities[e].Components;
            for (var c = 0; c < components.Count; c++)
            {
                var props = components[c].Properties;
                for (var p = 0; p < props.Count; p++)
                {
                    props[p].TryCompletePendingResolution();
                }
            }
        }
    }

    /// <summary>
    /// Binds an asset reference to the currently open editor scene so it stays alive until the scene is unloaded.
    /// </summary>
    public void RetainSceneAsset(IAssetEntry entry)
    {
        entry.AddRef();
        _sceneRetainedAssets.Add(entry);
    }

    /// <summary>
    /// Releases all asset references retained by the currently open editor scene.
    /// </summary>
    public void ReleaseSceneAssets()
    {
        if (_sceneRetainedAssets.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _sceneRetainedAssets.Count; i++)
        {
            _sceneRetainedAssets[i].Release();
        }

        _sceneRetainedAssets.Clear();
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

    private void AdvanceNextEntityId(int maxId)
    {
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
    }
}
