using Ghost.Core;
using Ghost.Editor.Core.Assets;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Engine;
using Ghost.Engine.Core;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.Services;

internal class EditorWorldService : IEditorWorldService
{
    private readonly ConcurrentQueue<Action> _deferredActions = new();
    private readonly ConcurrentQueue<Action> _pendingEvents = new();
    private readonly ConcurrentDictionary<ushort, SceneAsset> _sceneAssetMap = new();

    public World EditorWorld
    {
        get;
    }

    public ObservableCollection<SceneNode> RootNodes
    {
        get;
    } = new();

    public event Action<Entity, string, ushort>? EntityCreated;
    public event Action<Entity>? EntityDestroyed;
    public event Action<Entity, Entity, Entity>? EntityParentChanged; // (child, oldParent, newParent)
    public event Action<Entity, string>? EntityNameChanged;
    public event Action? SceneGraphRebuilt;

    public EditorWorldService(JobScheduler? jobScheduler = null)
    {
        EditorWorld = World.Create(jobScheduler, 1024);
    }

    public void Defer(Action action)
    {
        _deferredActions.Enqueue(action);
    }

    public void FlushCommands()
    {
        while (_deferredActions.TryDequeue(out var action))
        {
            action();
        }
    }

    public void FirePendingEvents()
    {
        while (_pendingEvents.TryDequeue(out var evt))
        {
            evt();
        }
    }

    public void CreateEntity(string name, ushort sceneID, Entity parent = default)
    {
        Defer(() =>
        {
            var entity = EditorWorld.EntityManager.CreateEntity();

            EditorWorld.EntityManager.AddComponent(entity, new Engine.Components.Hierarchy
            {
                parent = Entity.Invalid,
                firstChild = Entity.Invalid,
                nextSibling = Entity.Invalid
            });

            EditorWorld.EntityManager.AddSharedComponent(entity, new Engine.Components.SceneID
            {
                value = sceneID
            });

            if (parent.IsValid)
            {
                HierarchyUtility.SetParent(EditorWorld, entity, parent);
            }

            _pendingEvents.Enqueue(() =>
            {
                EntityCreated?.Invoke(entity, name, sceneID);
                if (parent.IsValid)
                {
                    EntityParentChanged?.Invoke(entity, Entity.Invalid, parent);
                }
            });
        });
    }

    public void DestroyEntity(Entity entity)
    {
        Defer(() =>
        {
            if (!entity.IsValid) return;
            DestroyEntityRecursive(entity);
        });
    }

    private void DestroyEntityRecursive(Entity entity)
    {
        if (EditorWorld.EntityManager.HasComponent<Engine.Components.Hierarchy>(entity))
        {
            ref var hierarchy = ref EditorWorld.EntityManager.GetComponent<Engine.Components.Hierarchy>(entity);
            var child = hierarchy.firstChild;
            while (child.IsValid)
            {
                ref var childHierarchy = ref EditorWorld.EntityManager.GetComponent<Engine.Components.Hierarchy>(child);
                var next = childHierarchy.nextSibling;
                DestroyEntityRecursive(child);
                child = next;
            }
        }

        HierarchyUtility.RemoveParent(EditorWorld, entity);
        EditorWorld.EntityManager.DestroyEntity(entity);
        _pendingEvents.Enqueue(() => EntityDestroyed?.Invoke(entity));
    }

    private void UpdateSceneIDRecursive(Entity entity, ushort sceneID)
    {
        if (EditorWorld.EntityManager.HasComponent<Engine.Components.SceneID>(entity))
        {
            EditorWorld.EntityManager.SetSharedComponent(entity, new Engine.Components.SceneID { value = sceneID });
        }

        if (EditorWorld.EntityManager.HasComponent<Engine.Components.Hierarchy>(entity))
        {
            ref var hierarchy = ref EditorWorld.EntityManager.GetComponent<Engine.Components.Hierarchy>(entity);
            var child = hierarchy.firstChild;
            while (child.IsValid)
            {
                ref var childHierarchy = ref EditorWorld.EntityManager.GetComponent<Engine.Components.Hierarchy>(child);
                var next = childHierarchy.nextSibling;
                UpdateSceneIDRecursive(child, sceneID);
                child = next;
            }
        }
    }

    public void ChangeEntityScene(Entity entity, ushort sceneID)
    {
        Defer(() =>
        {
            if (!entity.IsValid) return;

            UpdateSceneIDRecursive(entity, sceneID);
            _pendingEvents.Enqueue(() => EntityParentChanged?.Invoke(entity, Entity.Invalid, Entity.Invalid));
        });
    }

    public Error SetParent(Entity child, Entity parent)
    {
        if (!child.IsValid) return Error.InvalidArgument;

        var err = Error.None;
        if (parent.IsValid)
        {
            err = HierarchyUtility.IsValidParent(EditorWorld, child, parent);
        }
        else
        {
            if (!EditorWorld.EntityManager.HasComponent<Engine.Components.Hierarchy>(child))
            {
                err = Error.NotFound;
            }
        }

        if (err != Error.None)
        {
            return err;
        }

        Defer(() =>
        {
            var oldParent = Entity.Invalid;
            if (EditorWorld.EntityManager.HasComponent<Engine.Components.Hierarchy>(child))
            {
                oldParent = EditorWorld.EntityManager.GetComponent<Engine.Components.Hierarchy>(child).parent;
            }

            if (parent.IsValid)
            {
                HierarchyUtility.SetParent(EditorWorld, child, parent);
            }
            else
            {
                HierarchyUtility.RemoveParent(EditorWorld, child);
            }

            if (parent.IsValid && EditorWorld.EntityManager.HasComponent<Engine.Components.SceneID>(parent))
            {
                var locRes = EditorWorld.EntityManager.GetEntityLocation(parent);
                if (locRes.IsSuccess)
                {
                    ref var archetype = ref EditorWorld.ComponentManager.GetArchetypeReference(locRes.Value.archetypeID);
                    ref var chunk = ref archetype.GetChunkReference(locRes.Value.chunkIndex);
                    var chunkView = new ChunkView(in archetype, in chunk);
                    var parentSceneID = chunkView.GetSharedComponent<Engine.Components.SceneID>().value;
                    UpdateSceneIDRecursive(child, parentSceneID);
                }
            }

            _pendingEvents.Enqueue(() => EntityParentChanged?.Invoke(child, oldParent, parent));
        });

        return Error.None;
    }

    public Error RemoveParent(Entity child)
    {
        return SetParent(child, Entity.Invalid);
    }

    public ushort GetEntitySceneID(Entity entity)
    {
        if (!entity.IsValid)
        {
            return Scene.INVALID_ID;
        }

        if (EditorWorld.EntityManager.HasComponent<Engine.Components.SceneID>(entity))
        {
            var locRes = EditorWorld.EntityManager.GetEntityLocation(entity);
            if (locRes.IsSuccess)
            {
                ref var archetype = ref EditorWorld.ComponentManager.GetArchetypeReference(locRes.Value.archetypeID);
                ref var chunk = ref archetype.GetChunkReference(locRes.Value.chunkIndex);
                var chunkView = new ChunkView(in archetype, in chunk);
                return chunkView.GetSharedComponent<Engine.Components.SceneID>().value;
            }
        }

        return Scene.INVALID_ID;
    }

    public SceneAsset? GetAssetForScene(ushort sceneID)
    {
        _sceneAssetMap.TryGetValue(sceneID, out var asset);
        return asset;
    }

    public void RegisterSceneAsset(ushort sceneID, SceneAsset asset)
    {
        _sceneAssetMap[sceneID] = asset;
    }

    public void RenameEntity(Entity entity, string newName)
    {
        Defer(() =>
        {
            if (!entity.IsValid) return;
            _pendingEvents.Enqueue(() => EntityNameChanged?.Invoke(entity, newName));
        });
    }

    public void CreateDefaultScene()
    {
        var scene = SceneManager.CreateScene();
        CreateEntity("Entity", scene.ID);
    }
    public void RebuildSceneGraph(Dictionary<Entity, string>? initialNames = null)
    {
        Defer(() =>
        {
            var sceneNodes = SceneGraphBuilder.Build(EditorWorld, initialNames);
            _pendingEvents.Enqueue(() =>
            {
                RootNodes.Clear();
                foreach (var node in sceneNodes)
                {
                    RootNodes.Add(node);
                }
                SceneGraphRebuilt?.Invoke();
            });
        });
    }

    public void Dispose()
    {
        World.Destroy(EditorWorld.ID);
        GC.SuppressFinalize(this);
    }
}
