using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that deletes an entity and all of its descendants from the scene snapshot.
/// </summary>
public sealed class DeleteEntityCommand : IEditorCommand
{
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSelection _selection;
    private readonly EntityNode _target;
    private readonly List<(int Index, EntityNode Node)> _removedEntries = new();

    public string Name => $"Delete {_target.Name}";

    public DeleteEntityCommand(EntityNode target, EditorSyncBridge? bridge = null, EditorSelection? selection = null)
    {
        _target = target;
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;
    }

    public void Execute()
    {
        _removedEntries.Clear();
        var removedIds = new HashSet<int> { _target.Id };

        _bridge.MutateSnapshot(entities =>
        {
            var added = true;
            while (added)
            {
                added = false;
                for (var i = 0; i < entities.Count; i++)
                {
                    var e = entities[i];
                    if (e.ParentId.HasValue && removedIds.Contains(e.ParentId.Value) && removedIds.Add(e.Id))
                    {
                        added = true;
                    }
                }
            }

            var next = new List<EntityNode>(entities.Count);
            for (var i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (removedIds.Contains(e.Id))
                {
                    _removedEntries.Add((i, e));
                }
                else
                {
                    next.Add(e);
                }
            }

            return next;
        });

        if (_selection.ActiveEntity != null && removedIds.Contains(_selection.ActiveEntity.Id))
        {
            _selection.Clear();
        }
    }

    public void Undo()
    {
        if (_removedEntries.Count == 0)
        {
            return;
        }

        _bridge.MutateSnapshot(entities =>
        {
            var next = new List<EntityNode>(entities.Count + _removedEntries.Count);
            for (var i = 0; i < entities.Count; i++)
            {
                next.Add(entities[i]);
            }

            for (var i = 0; i < _removedEntries.Count; i++)
            {
                var (index, node) = _removedEntries[i];
                var insertAt = Math.Clamp(index, 0, next.Count);
                next.Insert(insertAt, node);
            }

            return next;
        });

        _selection.Select(_target);
    }

    public void Redo() => Execute();
}
