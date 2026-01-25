using Ghost.Entities;
using Ghost.Engine.Components;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ghost.Editor.Core.Validation;

/// <summary>
/// Validation result for scene integrity checks.
/// </summary>
public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];

    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
}

/// <summary>
/// Provides validation for scenes, checking for cross-scene references and other integrity issues.
/// </summary>
public class SceneValidator
{
    private readonly World _world;

    public SceneValidator(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Validates that all entity references within a scene point to entities in the same scene.
    /// </summary>
    /// <param name="sceneId">The ID of the scene to validate.</param>
    /// <param name="entities">The list of entities in the scene.</param>
    /// <returns>A validation result with any errors or warnings found.</returns>
    public ValidationResult ValidateSceneReferences(short sceneId, IEnumerable<Entity> entities)
    {
        var result = new ValidationResult();

        foreach (var entity in entities)
        {
            // Get entity location
            var location = _world.EntityManager.GetEntityLocation(entity);
            if (!location.IsSuccess)
            {
                result.AddError($"Entity {entity} not found in world.");
                continue;
            }

            ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.Value.archetypeID);

            // Check each component for entity references
            for (int i = 0; i < archetype._layouts.Count; i++)
            {
                var layout = archetype._layouts[i];
                if (layout.enableBitsOffset == -1) continue;

                var componentTypeId = layout.componentID;

                // Get component type
                if (!ComponentRegistry.s_runtimeIDToType.TryGetValue(componentTypeId.Value, out var componentType))
                {
                    continue;
                }

                // Get component data
                var pComponent = _world.EntityManager.GetComponent(entity, componentTypeId);
                if (pComponent == null)
                {
                    continue;
                }

                // Check fields for entity references
                ValidateComponentReferences(entity, componentType, pComponent, sceneId, result);
            }
        }

        return result;
    }

    /// <summary>
    /// Validates that a scene has no circular hierarchy references.
    /// </summary>
    /// <param name="entities">The list of entities in the scene.</param>
    /// <returns>A validation result with any errors or warnings found.</returns>
    public ValidationResult ValidateHierarchy(IEnumerable<Entity> entities)
    {
        var result = new ValidationResult();
        var entitySet = new HashSet<Entity>(entities);

        foreach (var entity in entities)
        {
            if (!_world.EntityManager.HasComponent<Hierarchy>(entity))
            {
                continue;
            }

            var visited = new HashSet<Entity>();
            var current = entity;

            // Traverse up the hierarchy
            while (current.IsValid)
            {
                if (visited.Contains(current))
                {
                    result.AddError($"Circular hierarchy detected involving entity {entity}");
                    break;
                }

                visited.Add(current);

                ref var hierarchy = ref _world.EntityManager.GetComponent<Hierarchy>(current);
                current = hierarchy.parent;

                // Check that parent is in the same scene if it exists
                if (current.IsValid && !entitySet.Contains(current))
                {
                    result.AddWarning($"Entity {entity} has parent {current} outside of the scene.");
                }
            }
        }

        return result;
    }

    private unsafe void ValidateComponentReferences(
        Entity entity,
        Type componentType,
        void* pComponent,
        short sceneId,
        ValidationResult result)
    {
        var componentInstance = Marshal.PtrToStructure((IntPtr)pComponent, componentType);
        if (componentInstance == null)
        {
            return;
        }

        var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(Entity))
            {
                var entityRef = (Entity)field.GetValue(componentInstance)!;

                if (!entityRef.IsValid)
                {
                    continue;
                }

                // Check if the referenced entity exists
                if (!_world.EntityManager.Exists(entityRef))
                {
                    result.AddError($"Entity {entity} in component {componentType.Name} references invalid entity {entityRef} in field {field.Name}");
                    continue;
                }

                // Check if the referenced entity has a SceneID
                if (!_world.EntityManager.HasComponent<SceneID>(entityRef))
                {
                    result.AddWarning($"Entity {entity} in component {componentType.Name} references entity {entityRef} without a SceneID in field {field.Name}");
                    continue;
                }

                // Check if the referenced entity is in the same scene
                var refSceneId = _world.EntityManager.GetComponent<SceneID>(entityRef);
                if (refSceneId.id != sceneId)
                {
                    result.AddError($"Cross-scene reference detected! Entity {entity} in scene {sceneId} references entity {entityRef} in scene {refSceneId.id} via component {componentType.Name}.{field.Name}");
                }
            }
        }
    }
}
