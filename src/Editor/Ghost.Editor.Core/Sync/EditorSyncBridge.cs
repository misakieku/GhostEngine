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
    private SceneSnapshot? _latestSnapshot;
    private long _snapshotVersion = 0;

    public static EditorSyncBridge Instance { get; } = new();

    public long SnapshotVersion => Volatile.Read(ref _snapshotVersion);

    public SceneSnapshot? LatestSnapshot => Volatile.Read(ref _latestSnapshot);

    public event Action<SceneSnapshot>? SnapshotPublished;

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
        Volatile.Write(ref _latestSnapshot, snapshot);
        Interlocked.Increment(ref _snapshotVersion);
        SnapshotPublished?.Invoke(snapshot);
    }
}
