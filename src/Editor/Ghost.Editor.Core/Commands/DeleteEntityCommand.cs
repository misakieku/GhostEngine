using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;
using Ghost.Engine.Components;
using Ghost.Engine.Utilities;
using Ghost.Entities;

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

    public ushort? TargetSceneId => _target.SceneId;

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

        _bridge.EnqueueWorldAction(world =>
        {
            var em = world.EntityManager;
            for (var i = 0; i < _removedEntries.Count; i++)
            {
                var node = _removedEntries[i].Node;
                if (node.EcsEntity.IsValid)
                {
                    _bridge.RemoveEntityMetadata(node.EcsEntity);
                }
            }

            if (_target.EcsEntity.IsValid && em.Exists(_target.EcsEntity))
            {
                HierarchyUtility.DestroyEntityWithChildren(world, _target.EcsEntity);
            }

            for (var i = 0; i < _removedEntries.Count; i++)
            {
                _removedEntries[i].Node.EcsEntity = Entity.Invalid;
            }
        });
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

        _bridge.EnqueueWorldAction(world =>
        {
            var em = world.EntityManager;
            for (var i = 0; i < _removedEntries.Count; i++)
            {
                var node = _removedEntries[i].Node;
                var recreated = em.CreateEntity();
                em.AddSharedComponent(recreated, new SceneID { value = node.SceneId });
                node.EcsEntity = recreated;
                _bridge.RegisterEntityMapping(recreated, node.Id);
                _bridge.RegisterEntityGuid(recreated, node.EditorGuid);
                _bridge.SetEntityName(recreated, node.Name);
                if (node.PrefabLink != null)
                {
                    _bridge.SetPrefabLink(recreated, node.PrefabLink);
                }

                var hasLocalTransform = false;
                var hasLocalToWorld = false;
                for (var c = 0; c < node.Components.Count; c++)
                {
                    var comp = node.Components[c];
                    if (comp.ComponentType == typeof(LocalTransform))
                    {
                        hasLocalTransform = true;
                    }
                    else if (comp.ComponentType == typeof(LocalToWorld))
                    {
                        hasLocalToWorld = true;
                    }

                    ComponentReflectionAccessor.TryAddOrSet(em, recreated, comp);
                }

                if (hasLocalTransform && !hasLocalToWorld)
                {
                    ComponentReflectionAccessor.TryAddOrSet(em, recreated, typeof(LocalToWorld), new LocalToWorld { matrix = LocalTransform.Identity.ToMatrix() });
                }
            }

            for (var i = 0; i < _removedEntries.Count; i++)
            {
                var node = _removedEntries[i].Node;
                if (!node.ParentId.HasValue)
                {
                    continue;
                }

                var parentEcs = _bridge.ResolveEcsEntity(node.ParentId.Value);
                if (parentEcs.IsValid && em.Exists(parentEcs) && node.EcsEntity.IsValid)
                {
                    if (!em.HasComponent<Hierarchy>(node.EcsEntity))
                    {
                        em.AddComponent(node.EcsEntity, Hierarchy.Root);
                    }

                    if (!em.HasComponent<Hierarchy>(parentEcs))
                    {
                        em.AddComponent(parentEcs, Hierarchy.Root);
                    }

                    HierarchyUtility.SetParent(world, node.EcsEntity, parentEcs);
                }
            }

            for (var i = 0; i < _removedEntries.Count; i++)
            {
                var ecs = _removedEntries[i].Node.EcsEntity;
                if (ecs.IsValid && em.Exists(ecs) && (em.HasComponent<LocalTransform>(ecs) || em.HasComponent<LocalToWorld>(ecs)))
                {
                    _bridge.DirtyTracker.MarkTransformDirty(ecs);
                }
            }
        });
    }

    public void Redo() => Execute();
}
