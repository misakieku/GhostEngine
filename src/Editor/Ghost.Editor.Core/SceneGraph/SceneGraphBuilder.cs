using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;

namespace Ghost.Editor.Core.SceneGraph;

public static class SceneGraphBuilder
{
    public static List<SceneNode> Build(World world)
    {
        var sceneNodes = new List<SceneNode>();
        var sceneEntities = GroupEntitiesByScene(world);

        foreach (var (scene, entities) in sceneEntities)
        {
            var sceneName = GetDefaultSceneName(scene);
            var sceneNode = new SceneNode(world, new Scene(scene), sceneName);
            BuildEntityTree(entities, sceneNode);
            sceneNodes.Add(sceneNode);
        }

        return sceneNodes;
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

    private static void BuildEntityTree(List<Entity> entities, SceneGraphNode parentNode)
    {
        var entitySet = new HashSet<Entity>(entities);
        var childrenByParent = new Dictionary<Entity, List<Entity>>();
        var roots = new List<Entity>();

        foreach (var entity in entities)
        {
            Hierarchy hierarchy = default;
            var hasHierarchy = TryGetHierarchyComponent(parentNode.World, entity, ref hierarchy);

            if (hasHierarchy && hierarchy.parent.IsValid && entitySet.Contains(hierarchy.parent))
            {
                if (!childrenByParent.TryGetValue(hierarchy.parent, out var list))
                {
                    list = new List<Entity>();
                    childrenByParent[hierarchy.parent] = list;
                }

                list.Add(entity);
            }
            else
            {
                roots.Add(entity);
            }
        }

        foreach (var rootEntity in roots)
        {
            var entityNode = new EntityNode(parentNode.World, rootEntity, "Entity");
            parentNode.Children.Add(entityNode);
            BuildSubtree(entityNode, childrenByParent);
        }
    }

    private static void BuildSubtree(EntityNode parentNode, Dictionary<Entity, List<Entity>> childrenByParent)
    {
        if (!childrenByParent.TryGetValue(parentNode.Entity, out var childList))
        {
            return;
        }

        Hierarchy parentHierarchy = default;
        if (!TryGetHierarchyComponent(parentNode.World, parentNode.Entity, ref parentHierarchy))
        {
            foreach (var childEntity in childList)
            {
                var childNode = new EntityNode(parentNode.World, childEntity, "Entity");
                parentNode.Children.Add(childNode);
                BuildSubtree(childNode, childrenByParent);
            }

            return;
        }

        var sibling = parentHierarchy.firstChild;
        while (sibling.IsValid)
        {
            if (childList.Contains(sibling))
            {
                var childNode = new EntityNode(parentNode.World, sibling, "Entity");
                parentNode.Children.Add(childNode);
                BuildSubtree(childNode, childrenByParent);
            }

            Hierarchy siblingHierarchy = default;
            if (!TryGetHierarchyComponent(parentNode.World, sibling, ref siblingHierarchy))
            {
                break;
            }

            sibling = siblingHierarchy.nextSibling;
        }
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

    private static string GetDefaultSceneName(ushort sceneID)
    {
        return $"NewScene ({sceneID})";
    }
}
