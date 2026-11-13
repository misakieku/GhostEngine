using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that removes a component from an <see cref="EntityNode"/>.
/// </summary>
public sealed class RemoveComponentCommand : IEditorCommand
{
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSelection _selection;
    private readonly EntityNode _entity;
    private readonly ComponentNode _component;
    private int _removedIndex = -1;

    public string Name => $"Remove {_component.TypeName}";

    public RemoveComponentCommand(EntityNode entity, ComponentNode component, EditorSyncBridge? bridge = null, EditorSelection? selection = null)
    {
        _entity = entity;
        _component = component;
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;
    }

    public void Execute()
    {
        var list = EnsureMutableList();
        var idx = list.IndexOf(_component);
        if (idx < 0)
        {
            return;
        }

        _removedIndex = idx;
        list.RemoveAt(idx);
        _bridge.MutateSnapshot(current => new List<EntityNode>(current));
        _selection.Select(_entity);
    }

    public void Undo()
    {
        if (_removedIndex < 0)
        {
            return;
        }

        var list = EnsureMutableList();
        if (!list.Contains(_component))
        {
            var insertAt = Math.Clamp(_removedIndex, 0, list.Count);
            list.Insert(insertAt, _component);
            _bridge.MutateSnapshot(current => new List<EntityNode>(current));
            _selection.Select(_entity);
        }
    }

    public void Redo() => Execute();

    private List<ComponentNode> EnsureMutableList()
    {
        if (_entity.Components is List<ComponentNode> list)
        {
            return list;
        }

        list = new List<ComponentNode>(_entity.Components);
        _entity.Components = list;
        return list;
    }
}
