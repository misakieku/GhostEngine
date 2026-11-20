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
    private readonly List<PropertyNode> _trackedResourceNodes = new();
    private readonly object _metadataLock = new();
    private readonly object _snapshotLock = new();
    private readonly object _resourceNodesLock = new();
    private SceneSnapshot? _latestSnapshot;
    private long _snapshotVersion = 0;
    private int _nextEntityId = 1;

    public static EditorSyncBridge Instance { get; } = new();

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
            updated = new SceneSnapshot(nextEntities, current?.FrameTimeMs ?? 16.6f, current?.Fps ?? 60.0f);
            PublishSnapshot(updated);
        }

        return updated;
    }
}
