using System.Collections.Concurrent;
using Ghost.Entities;

namespace Ghost.Editor.PoC.Sync;

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
    private volatile Dictionary<(Entity Entity, Type ComponentType, string FieldName), object?> _latestSnapshot = new();
    private long _snapshotVersion = 0;

    public long SnapshotVersion => Volatile.Read(ref _snapshotVersion);

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

    public void DrainMutations(List<PendingPropertyMutation> destination)
    {
        destination.Clear();
        while (_pendingMutations.TryDequeue(out var mutation))
        {
            destination.Add(mutation);
        }
    }

    public void PublishSnapshot(Dictionary<(Entity Entity, Type ComponentType, string FieldName), object?> snapshot)
    {
        Volatile.Write(ref _latestSnapshot, snapshot);
        Interlocked.Increment(ref _snapshotVersion);
    }

    public bool TryGetSnapshotValue(Entity entity, Type componentType, string fieldName, out object? value)
    {
        var snapshot = Volatile.Read(ref _latestSnapshot);
        return snapshot.TryGetValue((entity, componentType, fieldName), out value);
    }
}
