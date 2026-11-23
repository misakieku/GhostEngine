using Ghost.Editor.Core.Sync;
using Ghost.Entities;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that detaches all entities of a linked prefab instance from its source prefab asset,
/// turning them into plain standalone scene entities.
/// </summary>
public sealed class UnpackPrefabCommand : IEditorCommand
{
    private readonly Guid _instanceGuid;
    private readonly EditorSyncBridge _bridge;
    private readonly List<(EntityNode Node, PrefabLinkInfo Link)> _unpackedNodes = new();

    public string Name => "Unpack Prefab";

    public UnpackPrefabCommand(Guid instanceGuid, EditorSyncBridge? bridge = null)
    {
        _instanceGuid = instanceGuid;
        _bridge = bridge ?? EditorSyncBridge.Instance;
    }

    public void Execute()
    {
        _unpackedNodes.Clear();
        var snapshot = _bridge.LatestSnapshot;
        if (snapshot != null)
        {
            for (var i = 0; i < snapshot.Entities.Count; i++)
            {
                var node = snapshot.Entities[i];
                if (node.PrefabLink != null && node.PrefabLink.InstanceGuid == _instanceGuid)
                {
                    _unpackedNodes.Add((node, node.PrefabLink));
                    node.PrefabLink = null;
                }
            }
        }

        var snapshotCopy = _unpackedNodes.ToArray();
        _bridge.EnqueueWorldAction(world =>
        {
            for (var i = 0; i < snapshotCopy.Length; i++)
            {
                var e = snapshotCopy[i].Node.EcsEntity;
                if (e.IsValid && world.EntityManager.Exists(e))
                {
                    _bridge.SetPrefabLink(e, null);
                }
            }

            _bridge.HasUnsavedSceneChanges = true;
        });
    }

    public void Undo()
    {
        var snapshotCopy = _unpackedNodes.ToArray();
        for (var i = 0; i < snapshotCopy.Length; i++)
        {
            snapshotCopy[i].Node.PrefabLink = snapshotCopy[i].Link;
        }

        _bridge.EnqueueWorldAction(world =>
        {
            for (var i = 0; i < snapshotCopy.Length; i++)
            {
                var (node, link) = snapshotCopy[i];
                if (node.EcsEntity.IsValid && world.EntityManager.Exists(node.EcsEntity))
                {
                    _bridge.SetPrefabLink(node.EcsEntity, link);
                }
            }

            _bridge.HasUnsavedSceneChanges = true;
        });
    }

    public void Redo() => Execute();
}
