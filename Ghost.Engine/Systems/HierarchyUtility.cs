using Ghost.Entities;
using Ghost.Engine.Components;

namespace Ghost.Engine.Systems;

/// <summary>
/// Provides utility methods for working with entity hierarchies.
/// </summary>
public static class HierarchyUtility
{
    /// <summary>
    /// Sets the parent of an entity, updating the Hierarchy component accordingly.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="child">The child entity.</param>
    /// <param name="parent">The parent entity, or Entity.Invalid to make the entity a root.</param>
    public static void SetParent(World world, Entity child, Entity parent)
    {
        if (!world.EntityManager.HasComponent<Hierarchy>(child))
        {
            world.EntityManager.AddComponent(child, Hierarchy.Root);
        }

        ref var childHierarchy = ref world.EntityManager.GetComponent<Hierarchy>(child);

        // Remove from old parent's children list
        if (childHierarchy.parent.IsValid)
        {
            RemoveFromSiblingList(world, child, childHierarchy.parent);
        }

        // Set new parent
        childHierarchy.parent = parent;

        if (parent.IsValid)
        {
            if (!world.EntityManager.HasComponent<Hierarchy>(parent))
            {
                world.EntityManager.AddComponent(parent, Hierarchy.Root);
            }

            ref var parentHierarchy = ref world.EntityManager.GetComponent<Hierarchy>(parent);

            // Add to parent's children list
            childHierarchy.nextSibling = parentHierarchy.firstChild;
            parentHierarchy.firstChild = child;
        }
        else
        {
            childHierarchy.nextSibling = Entity.Invalid;
        }
    }

    /// <summary>
    /// Gets the parent of an entity.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="entity">The entity to get the parent of.</param>
    /// <returns>The parent entity, or Entity.Invalid if the entity has no parent.</returns>
    public static Entity GetParent(World world, Entity entity)
    {
        if (!world.EntityManager.HasComponent<Hierarchy>(entity))
        {
            return Entity.Invalid;
        }

        ref var hierarchy = ref world.EntityManager.GetComponent<Hierarchy>(entity);
        return hierarchy.parent;
    }

    /// <summary>
    /// Gets all children of an entity.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="parent">The parent entity.</param>
    /// <param name="children">Span to store the children.</param>
    /// <returns>The number of children written to the span.</returns>
    public static int GetChildren(World world, Entity parent, Span<Entity> children)
    {
        if (!world.EntityManager.HasComponent<Hierarchy>(parent))
        {
            return 0;
        }

        ref var hierarchy = ref world.EntityManager.GetComponent<Hierarchy>(parent);
        var currentChild = hierarchy.firstChild;
        var count = 0;

        while (currentChild.IsValid && count < children.Length)
        {
            children[count++] = currentChild;

            if (world.EntityManager.HasComponent<Hierarchy>(currentChild))
            {
                ref var childHierarchy = ref world.EntityManager.GetComponent<Hierarchy>(currentChild);
                currentChild = childHierarchy.nextSibling;
            }
            else
            {
                break;
            }
        }

        return count;
    }

    /// <summary>
    /// Gets all descendants of an entity (children, grandchildren, etc.) in depth-first order.
    /// </summary>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="root">The root entity.</param>
    /// <param name="descendants">List to store the descendants.</param>
    public static void GetDescendants(World world, Entity root, List<Entity> descendants)
    {
        Span<Entity> children = stackalloc Entity[32];
        var childCount = GetChildren(world, root, children);

        for (int i = 0; i < childCount; i++)
        {
            var child = children[i];
            descendants.Add(child);
            GetDescendants(world, child, descendants);
        }
    }

    /// <summary>
    /// Removes a child from its parent's sibling list.
    /// </summary>
    private static void RemoveFromSiblingList(World world, Entity child, Entity parent)
    {
        if (!world.EntityManager.HasComponent<Hierarchy>(parent))
        {
            return;
        }

        ref var parentHierarchy = ref world.EntityManager.GetComponent<Hierarchy>(parent);
        ref var childHierarchy = ref world.EntityManager.GetComponent<Hierarchy>(child);

        // If child is the first child
        if (parentHierarchy.firstChild.Equals(child))
        {
            parentHierarchy.firstChild = childHierarchy.nextSibling;
            childHierarchy.nextSibling = Entity.Invalid;
            return;
        }

        // Find the previous sibling
        var currentSibling = parentHierarchy.firstChild;

        while (currentSibling.IsValid)
        {
            if (!world.EntityManager.HasComponent<Hierarchy>(currentSibling))
            {
                break;
            }

            ref var siblingHierarchy = ref world.EntityManager.GetComponent<Hierarchy>(currentSibling);

            if (siblingHierarchy.nextSibling.Equals(child))
            {
                siblingHierarchy.nextSibling = childHierarchy.nextSibling;
                childHierarchy.nextSibling = Entity.Invalid;
                return;
            }

            currentSibling = siblingHierarchy.nextSibling;
        }
    }

    /// <summary>
    /// Checks if an entity is an ancestor of another entity.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="potentialAncestor">The potential ancestor entity.</param>
    /// <param name="descendant">The descendant entity.</param>
    /// <returns>True if potentialAncestor is an ancestor of descendant, false otherwise.</returns>
    public static bool IsAncestor(World world, Entity potentialAncestor, Entity descendant)
    {
        var current = GetParent(world, descendant);

        while (current.IsValid)
        {
            if (current.Equals(potentialAncestor))
            {
                return true;
            }

            current = GetParent(world, current);
        }

        return false;
    }
}
