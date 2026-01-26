using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.Core.Serialization;
using Ghost.Entities;
using Ghost.Engine.Components;
using System.Text.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using Ghost.Engine.Core;

namespace Ghost.Editor.Core.Serialization;

/// <summary>
/// Provides functionality to save and load scenes with proper entity reference remapping.
/// </summary>
public class SceneSerializer
{
    private readonly World _world;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        IncludeFields = true
    };

    public SceneSerializer(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Saves a scene to a JSON file.
    /// </summary>
    /// <param name="sceneNode">The scene node to save.</param>
    /// <param name="filePath">The path to save the scene to.</param>
    public void SaveScene(SceneNode sceneNode, string filePath)
    {
        var sceneData = new SceneData
        {
            Name = sceneNode.Name
        };

        // Build a mapping of Entity -> FileLocalID
        var allEntities = sceneNode.GetAllEntities().ToList();
        var entityToLocalId = new Dictionary<Entity, int>();

        for (int i = 0; i < allEntities.Count; i++)
        {
            entityToLocalId[allEntities[i].Entity] = i;
        }

        // Serialize each entity
        foreach (var entityNode in allEntities)
        {
            var entityData = SerializeEntity(entityNode, entityToLocalId, sceneNode.Scene.ID);
            sceneData.Entities.Add(entityData);
        }

        // Write to file
        var json = JsonSerializer.Serialize(sceneData, s_jsonOptions);
        File.WriteAllText(filePath, json);

        sceneNode.FilePath = filePath;
        sceneNode.IsDirty = false;
    }

    /// <summary>
    /// Loads a scene from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to load the scene from.</param>
    /// <param name="sceneManager">The scene manager to create the scene in.</param>
    /// <param name="worldManager">The editor world manager.</param>
    /// <returns>The loaded scene node.</returns>
    public SceneNode LoadScene(string filePath, SceneManager sceneManager, EditorWorldManager worldManager)
    {
        var json = File.ReadAllText(filePath);
        var sceneData = JsonSerializer.Deserialize<SceneData>(json, s_jsonOptions);

        if (sceneData == null)
        {
            throw new InvalidOperationException($"Failed to deserialize scene from {filePath}");
        }

        // Create new scene
        var sceneNode = worldManager.CreateNewScene(sceneData.Name);
        sceneNode.FilePath = filePath;

        // Build file-local ID -> Entity mapping
        var localIdToEntity = new Dictionary<int, Entity>();
        var localIdToEntityNode = new Dictionary<int, EntityNode>();

        // First pass: Create all entities
        for (int i = 0; i < sceneData.Entities.Count; i++)
        {
            var entityDataItem = sceneData.Entities[i];

            // Create runtime entity
            var entity = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponent(entity, new SceneID { id = sceneNode.Scene.ID });

            // Create entity node
            var entityNode = new EntityNode(entity, entityDataItem.Name);

            localIdToEntity[i] = entity;
            localIdToEntityNode[i] = entityNode;
        }

        // Second pass: Deserialize components and setup hierarchy
        for (int i = 0; i < sceneData.Entities.Count; i++)
        {
            var entityDataItem = sceneData.Entities[i];
            var entity = localIdToEntity[i];
            var entityNode = localIdToEntityNode[i];

            // Deserialize each component
            foreach (var (typeName, componentData) in entityDataItem.Components)
            {
                DeserializeComponent(entity, componentData, localIdToEntity);
            }

            // Setup hierarchy in scene graph
            if (entityDataItem.ParentLocalId >= 0 && localIdToEntityNode.TryGetValue(entityDataItem.ParentLocalId, out var parentNode))
            {
                parentNode.AddChild(entityNode);
            }
            else
            {
                sceneNode.AddRootEntity(entityNode);
            }
        }

        sceneNode.IsDirty = false;
        return sceneNode;
    }

    private EntityData SerializeEntity(EntityNode entityNode, Dictionary<Entity, int> entityToLocalId, short sceneId)
    {
        var entityData = new EntityData
        {
            Name = entityNode.Name,
            ParentLocalId = entityNode.Parent != null && entityToLocalId.TryGetValue(entityNode.Parent.Entity, out var parentId)
                ? parentId
                : -1
        };

        // Get entity location
        var location = _world.EntityManager.GetEntityLocation(entityNode.Entity);
        if (!location.IsSuccess)
        {
            return entityData;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.Value.archetypeID);

        // Iterate through all components in the archetype
        for (int i = 0; i < archetype._layouts.Count; i++)
        {
            var layout = archetype._layouts[i];
            if (layout.enableBitsOffset == -1) continue; // Skip invalid layouts

            var componentTypeId = layout.componentID;

            // Skip SceneID component - it's implicit
            if (componentTypeId == ComponentTypeID<SceneID>.Value)
            {
                continue;
            }

            // Get component type
            if (!ComponentRegistry.s_runtimeIDToType.TryGetValue(componentTypeId, out var componentType))
            {
                continue;
            }

            // Serialize the component
            var componentData = SerializeComponent(
                entityNode.Entity,
                location.Value,
                componentType,
                componentTypeId,
                entityToLocalId,
                sceneId
            );

            if (componentData != null)
            {
                entityData.Components[componentType.FullName ?? componentType.Name] = componentData;
            }
        }

        return entityData;
    }

    private unsafe ComponentData? SerializeComponent(
        Entity entity,
        EntityLocation location,
        Type componentType,
        int componentTypeId,
        Dictionary<Entity, int> entityToLocalId,
        short sceneId)
    {
        // Get component data pointer
        var pComponent = _world.EntityManager.GetComponent(entity, componentTypeId);
        if (pComponent == null)
        {
            return null;
        }

        var componentData = new ComponentData
        {
            TypeName = componentType.FullName ?? componentType.Name
        };

        // Serialize each field
        var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            var fieldValue = field.GetValue(Marshal.PtrToStructure((IntPtr)pComponent, componentType));

            // Check if this field is an Entity reference
            if (field.FieldType == typeof(Entity))
            {
                var entityRef = (Entity)fieldValue!;

                if (entityRef.IsValid)
                {
                    // Validate: Entity must be in the same scene
                    if (_world.EntityManager.HasComponent<SceneID>(entityRef))
                    {
                        var refSceneId = _world.EntityManager.GetComponent<SceneID>(entityRef);
                        if (refSceneId.id != sceneId)
                        {
                            throw new InvalidOperationException(
                                $"Cross-scene reference detected! Entity {entity} references entity {entityRef} from different scene. This is not allowed.");
                        }
                    }

                    // Convert to file-local ID
                    if (entityToLocalId.TryGetValue(entityRef, out var localId))
                    {
                        componentData.Fields[field.Name] = localId;
                    }
                    else
                    {
                        // Entity not found in the scene - this shouldn't happen after validation
                        componentData.Fields[field.Name] = -1;
                    }
                }
                else
                {
                    componentData.Fields[field.Name] = -1; // Invalid entity
                }
            }
            else
            {
                // Store as-is for other types
                componentData.Fields[field.Name] = fieldValue;
            }
        }

        return componentData;
    }

    private unsafe void DeserializeComponent(Entity entity, ComponentData componentData, Dictionary<int, Entity> localIdToEntity)
    {
        // Get component type
        var componentType = Type.GetType(componentData.TypeName);
        if (componentType == null)
        {
            // Try to find in loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                componentType = assembly.GetType(componentData.TypeName);
                if (componentType != null) break;
            }

            if (componentType == null)
            {
                Console.WriteLine($"Warning: Component type {componentData.TypeName} not found. Skipping.");
                return;
            }
        }

        // Get component ID
        var componentTypeId = ComponentRegistry.GetComponentID(componentType);
        if (componentTypeId.IsInvalid)
        {
            Console.WriteLine($"Warning: Component {componentData.TypeName} not registered. Skipping.");
            return;
        }

        // Create instance
        var componentInstance = Activator.CreateInstance(componentType);
        if (componentInstance == null)
        {
            return;
        }

        // Deserialize fields
        var fields = componentType.GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (!componentData.Fields.TryGetValue(field.Name, out var fieldValue))
            {
                continue;
            }

            // Handle Entity references
            if (field.FieldType == typeof(Entity))
            {
                if (fieldValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
                {
                    var localId = jsonElement.GetInt32();

                    if (localId >= 0 && localIdToEntity.TryGetValue(localId, out var targetEntity))
                    {
                        field.SetValue(componentInstance, targetEntity);
                    }
                    else
                    {
                        field.SetValue(componentInstance, Entity.Invalid);
                    }
                }
            }
            else
            {
                // Handle other types - may need type conversion from JsonElement
                if (fieldValue is JsonElement jsonElem)
                {
                    var converted = JsonSerializer.Deserialize(jsonElem.GetRawText(), field.FieldType, s_jsonOptions);
                    field.SetValue(componentInstance, converted);
                }
                else
                {
                    field.SetValue(componentInstance, fieldValue);
                }
            }
        }

        // Add component to entity
        var componentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(componentType));
        try
        {
            Marshal.StructureToPtr(componentInstance, componentPtr, false);
            _world.EntityManager.AddComponent(entity, componentTypeId, (void*)componentPtr);
        }
        finally
        {
            Marshal.FreeHGlobal(componentPtr);
        }
    }
}
