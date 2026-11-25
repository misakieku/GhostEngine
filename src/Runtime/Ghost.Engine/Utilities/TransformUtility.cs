using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Entities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.Utilities;

/// <summary>
/// Helpers for computing hierarchical world transform matrices for individual entities and their descendants.
/// </summary>
public static class TransformUtility
{
    private const int MAX_HIERARCHY_DEPTH = 256;

    /// <summary>
    /// Updates <see cref="LocalToWorld"/> for <paramref name="entity"/> and any attached descendants, optionally recording touched entities.
    /// </summary>
    public static void UpdateEntityAndDescendants(EntityManager em, Entity entity, List<Entity>? outUpdatedEntities = null)
    {
        if (!entity.IsValid || !em.Exists(entity) || !em.HasComponent<LocalToWorld>(entity))
        {
            return;
        }

        float4x4 worldMatrix;
        if (em.HasComponent<LocalTransform>(entity))
        {
            var localMatrix = em.GetComponent<LocalTransform>(entity).ToMatrix();
            if (em.HasComponent<Hierarchy>(entity))
            {
                ref readonly var hierarchy = ref em.GetComponent<Hierarchy>(entity);
                if (hierarchy.parent != Entity.Invalid && em.Exists(hierarchy.parent) && em.HasComponent<LocalToWorld>(hierarchy.parent))
                {
                    worldMatrix = math.mul(em.GetComponent<LocalToWorld>(hierarchy.parent).matrix, localMatrix);
                }
                else
                {
                    worldMatrix = localMatrix;
                }
            }
            else
            {
                worldMatrix = localMatrix;
            }

            em.GetComponent<LocalToWorld>(entity).matrix = worldMatrix;
        }
        else
        {
            worldMatrix = em.GetComponent<LocalToWorld>(entity).matrix;
        }

        outUpdatedEntities?.Add(entity);

        if (em.HasComponent<Hierarchy>(entity))
        {
            var firstChild = em.GetComponent<Hierarchy>(entity).firstChild;
            if (firstChild != Entity.Invalid)
            {
                UpdateChildTransforms(em, firstChild, in worldMatrix, 0, outUpdatedEntities);
            }
        }
    }

    /// <summary>
    /// Recursively updates <see cref="LocalToWorld"/> for sibling/child chains starting from <paramref name="firstChild"/>.
    /// </summary>
    public static void UpdateChildTransforms(EntityManager em, Entity firstChild, scoped in float4x4 parentMatrix, int depth, List<Entity>? outUpdatedEntities = null)
    {
        if (depth > MAX_HIERARCHY_DEPTH)
        {
            Logger.Warning("Hierarchy depth exceeded 256 or cyclic parent detected in TransformUtility.");
            return;
        }

        var current = firstChild;
        while (current != Entity.Invalid)
        {
            if (!em.Exists(current) || !em.HasComponent<Hierarchy>(current))
            {
                break;
            }

            ref readonly var h = ref em.GetComponent<Hierarchy>(current);
            var nextSibling = h.nextSibling;
            var childFirstChild = h.firstChild;

            var childWorldMatrix = parentMatrix;
            if (em.HasComponent<LocalTransform>(current))
            {
                ref readonly var local = ref em.GetComponent<LocalTransform>(current);
                childWorldMatrix = math.mul(parentMatrix, local.ToMatrix());
            }

            if (em.HasComponent<LocalToWorld>(current))
            {
                ref var ltw = ref em.GetComponent<LocalToWorld>(current);
                ltw.matrix = childWorldMatrix;
                outUpdatedEntities?.Add(current);
            }

            if (childFirstChild != Entity.Invalid)
            {
                UpdateChildTransforms(em, childFirstChild, in childWorldMatrix, depth + 1, outUpdatedEntities);
            }

            current = nextSibling;
        }
    }
}
