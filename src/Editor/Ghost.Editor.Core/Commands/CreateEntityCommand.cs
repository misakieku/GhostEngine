using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Engine.Utilities;
using Ghost.Entities;

namespace Ghost.Editor.Core.Commands;

/// <summary>
/// Undoable command that creates a new entity in the editor scene and world.
/// </summary>
public sealed class CreateEntityCommand : IEditorCommand
{
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSelection _selection;
    private readonly EntityNode _entity;
    private object? _previousSelection;

    public EntityNode CreatedEntity => _entity;

    public ushort? TargetSceneId => _entity.SceneId;

    public string Name => $"Create {_entity.Name}";

    public CreateEntityCommand(string name, int? parentId = null, IReadOnlyList<Type>? componentTypes = null, EditorSyncBridge? bridge = null, EditorSelection? selection = null)
    {
        _bridge = bridge ?? EditorSyncBridge.Instance;
        _selection = selection ?? EditorSelection.Instance;

        var id = _bridge.AllocateEntityId();
        var components = new List<ComponentNode>();
        _entity = new EntityNode(id, name, components, parentId)
        {
            SceneId = _bridge.ActiveSceneId
        };

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
        _bridge.AddEntityNode(_entity);
        _selection.Select(_entity);

        var world = _bridge.World;
        if (world == null)
        {
            return;
        }

        var em = world.EntityManager;
        var created = em.CreateEntity();
        em.AddSharedComponent(created, new SceneID { value = _entity.SceneId });
        _entity.EcsEntity = created;
        _bridge.RegisterEntityMapping(created, _entity.Id);
        _bridge.RegisterEntityGuid(created, _entity.EditorGuid);
        _bridge.SetEntityName(created, _entity.Name);
        if (_entity.PrefabLink != null)
        {
            _bridge.SetPrefabLink(created, _entity.PrefabLink);
        }

        var hasLocalTransform = false;
        var hasLocalToWorld = false;
        for (var i = 0; i < _entity.Components.Count; i++)
        {
            var comp = _entity.Components[i];
            if (comp.ComponentType == typeof(LocalTransform))
            {
                hasLocalTransform = true;
            }
            else if (comp.ComponentType == typeof(LocalToWorld))
            {
                hasLocalToWorld = true;
            }

            ComponentReflectionAccessor.TryAddOrSet(em, created, comp);
        }

        if (hasLocalTransform && !hasLocalToWorld)
        {
            var ltw = new LocalToWorld { matrix = LocalTransform.Identity.ToMatrix() };
            ComponentReflectionAccessor.TryAddOrSet(em, created, typeof(LocalToWorld), ltw);
        }

        if (_entity.ParentId.HasValue)
        {
            var parentEcs = _bridge.ResolveEcsEntity(_entity.ParentId.Value);
            if (parentEcs.IsValid && em.Exists(parentEcs))
            {
                if (!em.HasComponent<Hierarchy>(created))
                {
                    em.AddComponent(created, Hierarchy.Root);
                }

                if (!em.HasComponent<Hierarchy>(parentEcs))
                {
                    em.AddComponent(parentEcs, Hierarchy.Root);
                }

                HierarchyUtility.SetParent(world, created, parentEcs);
            }
        }

        if (hasLocalTransform || hasLocalToWorld)
        {
            _bridge.DirtyTracker.MarkTransformDirty(created);
        }
    }

    public void Undo()
    {
        var world = _bridge.World;
        if (world != null)
        {
            var target = _entity.EcsEntity;
            if (target.IsValid && world.EntityManager.Exists(target))
            {
                _bridge.RemoveEntityMetadata(target);
                HierarchyUtility.DestroyEntityWithChildren(world, target);
            }

            _entity.EcsEntity = Entity.Invalid;
        }

        _bridge.MutateEntities(entities =>
        {
            for (var i = entities.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(entities[i], _entity) || entities[i].Id == _entity.Id)
                {
                    entities.RemoveAt(i);
                }
            }
        });

        if (ReferenceEquals(_selection.ActiveEntity, _entity))
        {
            _selection.Select(_previousSelection);
        }
    }

    public void Redo() => Execute();
}
