using Ghost.Engine.Components;
using Ghost.SparseEntities;

namespace Ghost.Editor.Core.SceneGraph;

public class SceneGraphHelpers
{
    /// <summary>
    /// Creates a new <see cref="EntityNode"/> entity with default components.
    /// </summary>
    /// <param name="world">The world context where the entity will be created.</param>
    /// <param name="entity">The entity to be wrapped in the <see cref="EntityNode"/>.</param>
    public static EntityNode CreateEntityNode(WorldNode owner, Entity entity, string name)
    {
        owner.World.EntityManager.AddComponent(entity, LocalToWorld.Identity);
        owner.World.EntityManager.AddComponent(entity, Hierarchy.Root);
        return new EntityNode(owner, entity, name);
    }

    /// <summary>
    /// Creates a new <see cref="Entity"/> and <see cref="EntityNode"/> entity with default components.
    /// </summary>
    /// <param name="owner">The world context where the entity will be created.</param>
    public static EntityNode CreateEntityNode(WorldNode owner, string name)
    {
        var entity = owner.World.EntityManager.CreateEntity();
        return CreateEntityNode(owner, entity, name);
    }

    /// <summary>
    /// Attaches childEntity to parentEntity in the scene graph.
    /// </summary>
    /// <param name="world">The world context where the entities exist.</param>
    /// <param name="parentNode">The parent entity to which the child will be attached.</param>
    /// <param name="childNode">The child entity to be attached.</param>
    public static void AttachChild(WorldNode scene, EntityNode parentNode, EntityNode childNode)
    {
        // 1) If the child already has a parent, detach it first
        var childHierarchy = scene.World.EntityManager.GetComponent<Hierarchy>(childNode.Entity);
        if (childHierarchy.ValueRO.parent != Entity.Invalid)
        {
            DetachFromParent(scene, childNode);
        }

        // 2) Link child to new parent
        childHierarchy.ValueRW.parent = parentNode.Entity;

        // 3) Insert child at the head of parent's child list
        var parentHierarchy = scene.World.EntityManager.GetComponent<Hierarchy>(parentNode.Entity);

        childHierarchy.ValueRW.nextSibling = parentHierarchy.ValueRO.firstChild;
        parentHierarchy.ValueRW.firstChild = childNode.Entity;

        // 4) Write back
        scene.World.EntityManager.SetComponent(parentNode.Entity, in parentHierarchy.ValueRO);
        scene.World.EntityManager.SetComponent(childNode.Entity, in childHierarchy.ValueRO);

        // 5) Update children list in parent node
        parentNode.AddChild(childNode);
    }

    /// <summary>
    /// Detaches the specified entity from its parent in the scene graph.
    /// </summary>
    /// <param name="world">The world context where the entities exist.</param>
    /// <param name="node">The entity to detach from its parent.</param>
    public static void DetachFromParent(WorldNode scene, EntityNode node)
    {
        var hierarchy = scene.World.EntityManager.GetComponent<Hierarchy>(node.Entity);
        var parent = hierarchy.ValueRO.parent;
        if (parent == Entity.Invalid)
        {
            return; // already root
        }

        var parentHierarchy = scene.World.EntityManager.GetComponent<Hierarchy>(parent);

        // If entity is the first child, simply move head
        if (parentHierarchy.ValueRO.firstChild == node.Entity)
        {
            parentHierarchy.ValueRW.firstChild = hierarchy.ValueRO.nextSibling;
        }
        else
        {
            // Otherwise, find the previous sibling in the linked list
            var prevSibling = parentHierarchy.ValueRO.firstChild;
            while (prevSibling != Entity.Invalid)
            {
                var prevHierarchy = scene.World.EntityManager.GetComponent<Hierarchy>(prevSibling);
                if (prevHierarchy.ValueRW.nextSibling == node.Entity)
                {
                    prevHierarchy.ValueRW.nextSibling = hierarchy.ValueRO.nextSibling;
                    scene.World.EntityManager.SetComponent(prevSibling, in prevHierarchy.ValueRO);
                    break;
                }

                prevSibling = prevHierarchy.ValueRO.nextSibling;
            }
        }

        // Clear child's references
        hierarchy.ValueRW.parent = Entity.Invalid;
        hierarchy.ValueRW.nextSibling = Entity.Invalid;

        // Write back
        scene.World.EntityManager.SetComponent(parent, in parentHierarchy.ValueRO);
        scene.World.EntityManager.SetComponent(node.Entity, in hierarchy.ValueRO);

        // Remove from parent's children list
        scene.EntityNodeLookup[parent].RemoveChild(node);
    }
}