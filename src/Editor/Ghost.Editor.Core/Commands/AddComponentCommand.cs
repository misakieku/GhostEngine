using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that adds a component to an <see cref="EntityNode"/>.
/// </summary>
public sealed class AddComponentCommand : IEditorCommand
{
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSelection _selection;
    private readonly EntityNode _entity;
    private readonly ComponentNode _component;

    public ComponentNode AddedComponent => _component;

    public string Name => $"Add {_component.TypeName}";

    public AddComponentCommand(EntityNode entity, Type componentType, EditorSyncBridge? bridge = null, EditorSelection? selection = null)
    {
        _entity = entity;
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;
        _component = ComponentNodeFactory.CreateComponentNode(componentType, entity, null, _bridge);
    }

    public void Execute()
    {
        var list = EnsureMutableList();
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].ComponentType == _component.ComponentType)
            {
                return;
            }
        }

        list.Add(_component);
        _bridge.MutateSnapshot(current => new List<EntityNode>(current));
        _selection.Select(_entity);
    }

    public void Undo()
    {
        var list = EnsureMutableList();
        if (list.Remove(_component))
        {
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
