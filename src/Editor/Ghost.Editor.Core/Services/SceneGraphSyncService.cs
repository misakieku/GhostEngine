using Ghost.Editor.Core.SceneGraph;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;

namespace Ghost.Editor.Core.Services;

public class SceneGraphSyncService
{
    private readonly EditorWorldService _worldService;
    private uint _lastSyncedVersion;

    public SceneGraphSyncService(EditorWorldService worldService)
    {
        _worldService = worldService;
    }

    public bool Tick()
    {
        var currentVersion = _worldService.EditorWorld.Version;
        if (currentVersion == _lastSyncedVersion)
        {
            return false;
        }

        _lastSyncedVersion = currentVersion;
        SyncScenesAndEntities(_worldService.EditorWorld);
        return true;
    }

    private void SyncScenesAndEntities(World world)
    {
        var sceneEntities = GroupEntitiesByScene(world);

        foreach (var (scene, entities) in sceneEntities)
        {
            var sceneNode = FindOrCreateSceneNode(world, scene, _worldService.RootNodes);
            SyncEntityTree(sceneNode, entities);
            RemoveStaleEntityNodes(sceneNode, entities);
        }

        var activeScenes = new HashSet<ushort>(sceneEntities.Keys);
        RemoveStaleSceneNodes(_worldService.RootNodes, activeScenes);
    }

    private static SceneNode FindOrCreateSceneNode(World world, ushort sceneID, System.Collections.ObjectModel.ObservableCollection<SceneNode> rootNodes)
    {
        foreach (var existing in rootNodes)
        {
            if (existing.Scene.ID == sceneID)
            {
                return existing;
            }
        }

        var sceneName = $"NewScene ({sceneID})";
        var newSceneNode = new SceneNode(world, new Scene(sceneID), sceneName);
        rootNodes.Add(newSceneNode);
        return newSceneNode;
    }

    private static void SyncEntityTree(SceneGraphNode parentNode, List<Entity> entities)
    {
        var entitySet = new HashSet<Entity>(entities);
        var children = new Dictionary<Entity, List<Entity>>();
        var roots = new List<Entity>();

        foreach (var entity in entities)
        {
            Hierarchy hierarchy = default;
            var hasHierarchy = TryGetHierarchyComponent(parentNode.World, entity, ref hierarchy);

            if (hasHierarchy && hierarchy.parent.IsValid && entitySet.Contains(hierarchy.parent))
            {
                if (!children.TryGetValue(hierarchy.parent, out var list))
                {
                    list = new List<Entity>();
                    children[hierarchy.parent] = list;
                }

                list.Add(entity);
            }
            else
            {
                roots.Add(entity);
            }
        }

        SyncExistingNodes(parentNode, roots, children);
    }

    private static void SyncExistingNodes(SceneGraphNode parentNode, List<Entity> roots, Dictionary<Entity, List<Entity>> children)
    {
        var existingNodeMap = new Dictionary<Entity, EntityNode>();
        for (var i = parentNode.Children.Count - 1; i >= 0; i--)
        {
            if (parentNode.Children[i] is EntityNode entityNode)
            {
                if (!roots.Contains(entityNode.Entity) && !children.ContainsKey(entityNode.Entity))
                {
                    parentNode.Children.RemoveAt(i);
                }
                else
                {
                    existingNodeMap[entityNode.Entity] = entityNode;
                }
            }
        }

        for (var i = 0; i < roots.Count; i++)
        {
            var entity = roots[i];
            if (existingNodeMap.TryGetValue(entity, out var existingNode))
            {
                existingNodeMap.Remove(entity);

                if (i >= parentNode.Children.Count || parentNode.Children[i] != existingNode)
                {
                    parentNode.Children.Remove(existingNode);
                    parentNode.Children.Insert(i, existingNode);
                }
            }
            else
            {
                var newNode = new EntityNode(parentNode.World, entity, "Entity");
                parentNode.Children.Insert(i, newNode);
                existingNode = newNode;
            }

            if (children.TryGetValue(entity, out var childList))
            {
                SyncExistingNodes(existingNode, childList, children);
            }
            else
            {
                for (var j = existingNode.Children.Count - 1; j >= 0; j--)
                {
                    if (existingNode.Children[j] is EntityNode)
                    {
                        existingNode.Children.RemoveAt(j);
                    }
                }
            }
        }
    }

    private static void RemoveStaleEntityNodes(SceneGraphNode parentNode, List<Entity> entities)
    {
        var entitySet = new HashSet<Entity>(entities);

        for (var i = parentNode.Children.Count - 1; i >= 0; i--)
        {
            if (parentNode.Children[i] is EntityNode entityNode && !entitySet.Contains(entityNode.Entity))
            {
                parentNode.Children.RemoveAt(i);
            }
        }
    }

    private static void RemoveStaleSceneNodes(System.Collections.ObjectModel.ObservableCollection<SceneNode> rootNodes, HashSet<ushort> activeScenes)
    {
        for (var i = rootNodes.Count - 1; i >= 0; i--)
        {
            if (!activeScenes.Contains(rootNodes[i].Scene.ID))
            {
                rootNodes.RemoveAt(i);
            }
        }
    }

    private static Dictionary<ushort, List<Entity>> GroupEntitiesByScene(World world)
    {
        var sceneMap = new Dictionary<ushort, List<Entity>>();
        var queryID = new QueryBuilder().WithAll<SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);

        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            var scene = chunk.GetSharedComponent<SceneID>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                if (scene.value == Scene.INVALID_ID)
                {
                    continue;
                }

                if (!sceneMap.TryGetValue(scene.value, out var list))
                {
                    list = new List<Entity>();
                    sceneMap[scene.value] = list;
                }

                list.Add(entities[i]);
            }
        }

        return sceneMap;
    }

    private static unsafe bool TryGetHierarchyComponent(World world, Entity entity, ref Hierarchy hierarchy)
    {
        var location = world.EntityManager.GetEntityLocation(entity);
        if (!location.IsSuccess)
        {
            return false;
        }

        ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.Value.archetypeID);
        var hierarchyID = ComponentTypeID<Hierarchy>.Value;
        var pData = archetype.GetComponentData(location.Value.chunkIndex, location.Value.rowIndex, hierarchyID);
        if (pData == null)
        {
            return false;
        }

        hierarchy = *(Hierarchy*)pData;
        return true;
    }
}
