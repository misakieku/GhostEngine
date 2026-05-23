using Ghost.Core;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Engine;
using Ghost.Engine.Core;
using Ghost.Entities;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Core.Services;

public class EditorWorldService : IDisposable
{
    private const int DEFAULT_ENTITY_CAPACITY = 1024;

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

    public EditorWorldService()
    {
        EditorWorld = World.Create(entityCapacity: DEFAULT_ENTITY_CAPACITY);
    }

    public Entity CreateEntity(string name, ushort sceneID, Entity parent = default)
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

        EditorWorld.AdvanceVersion();
        EntityCreated?.Invoke(entity, name, sceneID);

        if (parent.IsValid)
        {
            EntityParentChanged?.Invoke(entity, Entity.Invalid, parent);
        }

        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        if (!entity.IsValid)
        {
            return;
        }

        DestroyEntityRecursive(entity);
        EditorWorld.AdvanceVersion();
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
        EntityDestroyed?.Invoke(entity);
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
        if (!entity.IsValid)
        {
            return;
        }

        UpdateSceneIDRecursive(entity, sceneID);
        EditorWorld.AdvanceVersion();
        EntityParentChanged?.Invoke(entity, Entity.Invalid, Entity.Invalid);
    }

    public Error SetParent(Entity child, Entity parent)
    {
        if (!child.IsValid)
        {
            return Error.InvalidArgument;
        }

        var oldParent = Entity.Invalid;
        if (EditorWorld.EntityManager.HasComponent<Engine.Components.Hierarchy>(child))
        {
            oldParent = EditorWorld.EntityManager.GetComponent<Engine.Components.Hierarchy>(child).parent;
        }

        Error err;
        if (parent.IsValid)
        {
            err = HierarchyUtility.SetParent(EditorWorld, child, parent);
        }
        else
        {
            err = HierarchyUtility.RemoveParent(EditorWorld, child);
        }

        if (err == Error.None)
        {
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

            EditorWorld.AdvanceVersion();
            EntityParentChanged?.Invoke(child, oldParent, parent);
        }

        return err;
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

    public void RenameEntity(Entity entity, string newName)
    {
        if (!entity.IsValid)
        {
            return;
        }

        EntityNameChanged?.Invoke(entity, newName);
    }

    public void CreateDefaultScene()
    {
        var scene = SceneManager.CreateScene();
        CreateEntity("Entity", scene.ID);
    }
    public void RebuildSceneGraph(Dictionary<Entity, string>? initialNames = null)
    {
        RootNodes.Clear();
        var sceneNodes = SceneGraphBuilder.Build(EditorWorld, initialNames);
        foreach (var node in sceneNodes)
        {
            RootNodes.Add(node);
        }
        SceneGraphRebuilt?.Invoke();
    }

    public void Dispose()
    {
        World.Destroy(EditorWorld.ID);
        GC.SuppressFinalize(this);
    }
}
