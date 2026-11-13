using System.Collections.Concurrent;
using Ghost.Entities;

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
    private readonly object _snapshotLock = new();
    private SceneSnapshot? _latestSnapshot;
    private long _snapshotVersion = 0;
    private int _nextEntityId = 1;

    public static EditorSyncBridge Instance { get; } = new();

    public long SnapshotVersion => Volatile.Read(ref _snapshotVersion);

    public SceneSnapshot? LatestSnapshot => Volatile.Read(ref _latestSnapshot);

    public event Action<SceneSnapshot>? SnapshotPublished;

    public int AllocateEntityId()
    {
        return Interlocked.Increment(ref _nextEntityId);
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
