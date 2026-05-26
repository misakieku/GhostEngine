using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;

namespace Ghost.Editor.Core.SceneGraph;

public static class SceneGraphBuilder
{
    public static List<SceneNode> Build(World world, Dictionary<Entity, string>? initialNames = null)
    {
        var sceneNodes = new List<SceneNode>();
        var sceneEntities = GroupEntitiesByScene(world);

        foreach (var (scene, entities) in sceneEntities)
        {
            var sceneName = GetDefaultSceneName(scene);
            var sceneNode = new SceneNode(world, new Scene(scene), sceneName);
            BuildEntityTree(entities, sceneNode, initialNames);
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

            if (scene.value == Scene.INVALID_ID)
            {
                continue;
            }

            for (var i = 0; i < chunk.EntityCount; i++)
            {
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

    private static void BuildEntityTree(List<Entity> entities, SceneGraphNode parentNode, Dictionary<Entity, string>? initialNames = null)
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
            var name = initialNames != null && initialNames.TryGetValue(rootEntity, out var n) ? n : "Entity";
            var entityNode = new EntityNode(parentNode.World, rootEntity, name);
            parentNode.Children.Add(entityNode);
            BuildSubtree(entityNode, childrenByParent, initialNames);
        }
    }

    private static void BuildSubtree(EntityNode parentNode, Dictionary<Entity, List<Entity>> childrenByParent, Dictionary<Entity, string>? initialNames = null)
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
                var name = initialNames != null && initialNames.TryGetValue(childEntity, out var n) ? n : "Entity";
                var childNode = new EntityNode(parentNode.World, childEntity, name);
                parentNode.Children.Add(childNode);
                BuildSubtree(childNode, childrenByParent, initialNames);
            }

            return;
        }

        var sibling = parentHierarchy.firstChild;
        while (sibling.IsValid)
        {
            if (childList.Contains(sibling))
            {
                var name = initialNames != null && initialNames.TryGetValue(sibling, out var n) ? n : "Entity";
                var childNode = new EntityNode(parentNode.World, sibling, name);
                parentNode.Children.Add(childNode);
                BuildSubtree(childNode, childrenByParent, initialNames);
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
