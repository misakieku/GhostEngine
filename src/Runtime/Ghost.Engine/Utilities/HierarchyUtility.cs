using Ghost.Core;
using Ghost.Entities;
using System.Runtime.CompilerServices;

namespace Ghost.Engine;

public static class HierarchyUtility
{
    public static Error SetParent(World world, Entity child, Entity parent)
    {
        if (!child.IsValid)
        {
            return Error.InvalidArgument;
        }

        if (!parent.IsValid)
        {
            return Error.InvalidArgument;
        }

        if (child == parent)
        {
            return Error.InvalidArgument;
        }

        if (!world.EntityManager.HasComponent<Components.Hierarchy>(child))
        {
            return Error.NotFound;
        }

        if (!world.EntityManager.HasComponent<Components.Hierarchy>(parent))
        {
            return Error.NotFound;
        }

        if (IsAncestor(world, parent, child))
        {
            return Error.InvalidArgument;
        }

        ref var childHierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(child);
        if (Unsafe.IsNullRef(ref childHierarchy))
        {
            return Error.NotFound;
        }

        if (childHierarchy.parent.IsValid)
        {
            RemoveParent(world, child);
        }

        ref var parentHierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(parent);
        if (Unsafe.IsNullRef(ref parentHierarchy))
        {
            return Error.NotFound;
        }

        childHierarchy.parent = parent;
        childHierarchy.nextSibling = parentHierarchy.firstChild;
        parentHierarchy.firstChild = child;

        return Error.None;
    }

    public static Error RemoveParent(World world, Entity child)
    {
        if (!child.IsValid)
        {
            return Error.InvalidArgument;
        }

        if (!world.EntityManager.HasComponent<Components.Hierarchy>(child))
        {
            return Error.NotFound;
        }

        ref var childHierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(child);
        if (Unsafe.IsNullRef(ref childHierarchy))
        {
            return Error.NotFound;
        }

        var parent = childHierarchy.parent;
        if (!parent.IsValid)
        {
            return Error.None;
        }

        ref var parentHierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(parent);
        if (Unsafe.IsNullRef(ref parentHierarchy))
        {
            childHierarchy.parent = Entity.Invalid;
            childHierarchy.nextSibling = Entity.Invalid;
            return Error.None;
        }

        var prev = Entity.Invalid;
        var current = parentHierarchy.firstChild;

        while (current.IsValid)
        {
            ref var currentHierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(current);
            if (Unsafe.IsNullRef(ref currentHierarchy))
            {
                break;
            }

            if (current == child)
            {
                if (prev.IsValid)
                {
                    ref var prevHierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(prev);
                    prevHierarchy.nextSibling = childHierarchy.nextSibling;
                }
                else
                {
                    parentHierarchy.firstChild = childHierarchy.nextSibling;
                }

                break;
            }

            prev = current;
            current = currentHierarchy.nextSibling;
        }

        childHierarchy.parent = Entity.Invalid;
        childHierarchy.nextSibling = Entity.Invalid;

        return Error.None;
    }

    public static void DestroyEntityWithChildren(World world, Entity entity)
    {
        if (!entity.IsValid)
        {
            return;
        }

        if (world.EntityManager.HasComponent<Components.Hierarchy>(entity))
        {
            ref var hierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(entity);
            if (!Unsafe.IsNullRef(ref hierarchy))
            {
                var child = hierarchy.firstChild;
                while (child.IsValid)
                {
                    ref var childHierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(child);
                    var next = childHierarchy.nextSibling;
                    DestroyEntityWithChildren(world, child);
                    child = next;
                }

                RemoveParent(world, entity);
            }
        }

        world.EntityManager.DestroyEntity(entity);
    }

    public static bool IsAncestor(World world, Entity entity, Entity potentialAncestor)
    {
        if (!entity.IsValid || !potentialAncestor.IsValid)
        {
            return false;
        }

        if (!world.EntityManager.HasComponent<Components.Hierarchy>(entity))
        {
            return false;
        }

        ref var hierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(entity);
        if (Unsafe.IsNullRef(ref hierarchy))
        {
            return false;
        }

        var current = hierarchy.parent;
        while (current.IsValid)
        {
            if (current == potentialAncestor)
            {
                return true;
            }

            if (!world.EntityManager.HasComponent<Components.Hierarchy>(current))
            {
                break;
            }

            ref var currentHierarchy = ref world.EntityManager.GetComponent<Components.Hierarchy>(current);
            if (Unsafe.IsNullRef(ref currentHierarchy))
            {
                break;
            }

            current = currentHierarchy.parent;
        }

        return false;
    }

    public static Error RemoveEntity(World world, Entity entity)
    {
        if (!entity.IsValid)
        {
            return Error.InvalidArgument;
        }

        if (world.EntityManager.HasComponent<Components.Hierarchy>(entity))
        {
            RemoveParent(world, entity);
        }

        world.EntityManager.DestroyEntity(entity);

        return Error.None;
    }
}
