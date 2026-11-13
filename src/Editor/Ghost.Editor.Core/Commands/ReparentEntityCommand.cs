using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that changes an entity's parent in the scene hierarchy while preventing cyclic parenting.
/// </summary>
public sealed class ReparentEntityCommand : IEditorCommand
{
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSelection _selection;
    private readonly EntityNode _entity;
    private readonly int? _oldParentId;
    private readonly int? _newParentId;

    public string Name => _newParentId.HasValue ? $"Reparent {_entity.Name}" : $"Unparent {_entity.Name}";

    public ReparentEntityCommand(EntityNode entity, int? newParentId, EditorSyncBridge? bridge = null, EditorSelection? selection = null)
    {
        _entity = entity;
        _oldParentId = entity.ParentId;
        _newParentId = newParentId;
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;
    }

    public void Execute()
    {
        if (_oldParentId == _newParentId || _newParentId == _entity.Id)
        {
            return;
        }

        var entities = _bridge.LatestSnapshot?.Entities ?? Array.Empty<EntityNode>();
        if (_newParentId.HasValue && WouldCreateCycle(entities, _entity.Id, _newParentId.Value))
        {
            return;
        }

        _entity.ParentId = _newParentId;
        _bridge.MutateSnapshot(current => new List<EntityNode>(current));
        _selection.Select(_entity);
    }

    public void Undo()
    {
        _entity.ParentId = _oldParentId;
        _bridge.MutateSnapshot(current => new List<EntityNode>(current));
        _selection.Select(_entity);
    }

    public void Redo() => Execute();

    private static bool WouldCreateCycle(IReadOnlyList<EntityNode> entities, int entityId, int candidateParentId)
    {
        var byId = new Dictionary<int, EntityNode>(entities.Count);
        for (var i = 0; i < entities.Count; i++)
        {
            byId[entities[i].Id] = entities[i];
        }

        int? cursor = candidateParentId;
        var guard = 0;
        while (cursor.HasValue && guard++ <= entities.Count)
        {
            if (cursor.Value == entityId)
            {
                return true;
            }

            cursor = byId.TryGetValue(cursor.Value, out var node) ? node.ParentId : null;
        }

        return false;
    }
}
