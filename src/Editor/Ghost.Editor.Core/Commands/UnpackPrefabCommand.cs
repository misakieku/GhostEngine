using Ghost.Editor.Core.Sync;

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

    public ushort? TargetSceneId => _unpackedNodes.Count > 0 ? _unpackedNodes[0].Node.SceneId : null;

    public string Name => "Unpack Prefab";

    public UnpackPrefabCommand(Guid instanceGuid, EditorSyncBridge? bridge = null)
    {
        _instanceGuid = instanceGuid;
        _bridge = bridge ?? EditorSyncBridge.Instance;
    }

    public void Execute()
    {
        _unpackedNodes.Clear();
        var world = _bridge.World;
        var entities = _bridge.Entities;
        for (var i = 0; i < entities.Count; i++)
        {
            var node = entities[i];
            if (node.PrefabLink != null && node.PrefabLink.InstanceGuid == _instanceGuid)
            {
                _unpackedNodes.Add((node, node.PrefabLink));
                node.PrefabLink = null;
                if (world != null && node.EcsEntity.IsValid && world.EntityManager.Exists(node.EcsEntity))
                {
                    _bridge.SetPrefabLink(node.EcsEntity, null);
                }

                _bridge.SetSceneDirty(node.SceneId, true);
            }
        }

        _bridge.BumpRevision();
    }

    public void Undo()
    {
        var world = _bridge.World;
        for (var i = 0; i < _unpackedNodes.Count; i++)
        {
            var (node, link) = _unpackedNodes[i];
            node.PrefabLink = link;
            if (world != null && node.EcsEntity.IsValid && world.EntityManager.Exists(node.EcsEntity))
            {
                _bridge.SetPrefabLink(node.EcsEntity, link);
            }

            _bridge.SetSceneDirty(node.SceneId, true);
        }

        _bridge.BumpRevision();
    }

    public void Redo() => Execute();
}
