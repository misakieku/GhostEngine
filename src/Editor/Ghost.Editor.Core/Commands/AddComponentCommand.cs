using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;

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

        _bridge.EnqueueWorldAction(world =>
        {
            var em = world.EntityManager;
            var ecs = _entity.EcsEntity;
            if (!ecs.IsValid || !em.Exists(ecs))
            {
                return;
            }

            ComponentReflectionAccessor.TryAddOrSet(em, ecs, _component.ComponentType, _component.LiveValue);
            if (_component.ComponentType == typeof(LocalTransform) && !em.HasComponent<LocalToWorld>(ecs))
            {
                ComponentReflectionAccessor.TryAddOrSet(em, ecs, typeof(LocalToWorld), new LocalToWorld { matrix = LocalTransform.Identity.ToMatrix() });
            }

            if (_component.ComponentType == typeof(LocalTransform) || _component.ComponentType == typeof(LocalToWorld))
            {
                _bridge.DirtyTracker.MarkTransformDirty(ecs);
            }
            else if (_component.ComponentType == typeof(MeshInstance))
            {
                _bridge.DirtyTracker.MarkGpuInstanceDirty(ecs);
            }
        });
    }

    public void Undo()
    {
        var list = EnsureMutableList();
        if (list.Remove(_component))
        {
            _bridge.MutateSnapshot(current => new List<EntityNode>(current));
            _selection.Select(_entity);

            _bridge.EnqueueWorldAction(world =>
            {
                var ecs = _entity.EcsEntity;
                if (ecs.IsValid && world.EntityManager.Exists(ecs))
                {
                    ComponentReflectionAccessor.TryRemove(world.EntityManager, ecs, _component.ComponentType);
                }
            });
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
