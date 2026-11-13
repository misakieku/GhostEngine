using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that creates a new entity in the editor scene snapshot.
/// </summary>
public sealed class CreateEntityCommand : IEditorCommand
{
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSelection _selection;
    private readonly EntityNode _entity;
    private object? _previousSelection;

    public EntityNode CreatedEntity => _entity;

    public string Name => $"Create {_entity.Name}";

    public CreateEntityCommand(string name, int? parentId = null, IReadOnlyList<Type>? componentTypes = null, EditorSyncBridge? bridge = null, EditorSelection? selection = null)
    {
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;

        var id = _bridge.AllocateEntityId();
        var components = new List<ComponentNode>();
        _entity = new EntityNode(id, name, components, parentId);

        if (componentTypes != null)
        {
            for (var i = 0; i < componentTypes.Count; i++)
            {
                components.Add(ComponentNodeFactory.CreateComponentNode(componentTypes[i], _entity, null, _bridge));
            }
        }
    }

    public CreateEntityCommand(EntityNode entity, EditorSyncBridge? bridge = null, EditorSelection? selection = null)
    {
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;
        _entity = entity;
    }

    public void Execute()
    {
        _previousSelection = _selection.ActiveObject;
        _bridge.MutateSnapshot(entities =>
        {
            var next = new List<EntityNode>(entities.Count + 1);
            for (var i = 0; i < entities.Count; i++)
            {
                next.Add(entities[i]);
            }

            if (!next.Contains(_entity))
            {
                next.Add(_entity);
            }

            return next;
        });

        _selection.Select(_entity);
    }

    public void Undo()
    {
        _bridge.MutateSnapshot(entities =>
        {
            var next = new List<EntityNode>(entities.Count);
            for (var i = 0; i < entities.Count; i++)
            {
                if (!ReferenceEquals(entities[i], _entity) && entities[i].Id != _entity.Id)
                {
                    next.Add(entities[i]);
                }
            }

            return next;
        });

        if (ReferenceEquals(_selection.ActiveEntity, _entity))
        {
            _selection.Select(_previousSelection);
        }
    }

    public void Redo() => Execute();
}
