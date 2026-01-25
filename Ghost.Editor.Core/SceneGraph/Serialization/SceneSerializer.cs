using System.Reflection;
using Ghost.Entities;

namespace Ghost.Editor.Core.SceneGraph.Serialization;

/// <summary>
/// Handles serialization and deserialization of scenes to/from JSON format.
/// This is editor-only and uses reflection for flexibility.
/// </summary>
public class SceneSerializer
{
    /// <summary>
    /// Saves a scene to JSON-serializable format.
    /// Queries all entities with the given sceneId and converts them to SceneAssetData.
    /// </summary>
    public SceneAssetData SaveScene(SceneGraph sceneGraph, short sceneId, World editorWorld)
    {
        var sceneNode = sceneGraph.GetSceneNode(sceneId);
        if (sceneNode == null)
        {
            throw new InvalidOperationException($"Scene {sceneId} not found in scene graph.");
        }

        var sceneData = new SceneAssetData
        {
            SceneGuid = sceneNode.SceneGuid,
            Name = sceneNode.Name,
            SceneId = sceneId,
            Version = 1
        };

        // Get all entities in this scene
        var entitiesInScene = sceneGraph.GetEntitiesInScene(sceneId).ToList();

        // Create entity data in order
        for (int i = 0; i < entitiesInScene.Count; i++)
        {
            var entityNode = entitiesInScene[i];
            var entityData = new EntityData
            {
                FileLocalId = i,
                Name = entityNode.Name,
                ParentFileLocalId = entityNode.ParentNode != null 
                    ? GetFileLocalId(entitiesInScene, entityNode.ParentNode) 
                    : -1,
                Components = SerializeEntityComponents(editorWorld, entityNode.EntityId)
            };

            sceneData.Entities.Add(entityData);
        }

        // Validate for cross-scene references
        ValidateNoInvalidReferences(sceneData);

        return sceneData;
    }

    /// <summary>
    /// Loads a scene from JSON-serializable format.
    /// Creates entities in the editor world and sets up all relationships.
    /// </summary>
    public void LoadScene(SceneGraph sceneGraph, SceneAssetData sceneData, World editorWorld, 
                          SceneSerializationContext? context = null)
    {
        context ??= new SceneSerializationContext(sceneData.SceneId, editorWorld);

        // Add scene node to graph
        var sceneNode = sceneGraph.AddScene(sceneData.Name, sceneData.SceneId, sceneData.SceneGuid);

        // Create all entities first (without relationships)
        var createdEntities = new List<Entity>();
        foreach (var entityData in sceneData.Entities)
        {
            var entity = editorWorld.CreateEntity();
            createdEntities.Add(entity);
            context.EntityOrder.Add(entity);
            context.IdRemap.Register(entityData.FileLocalId, entity);

            // Add SceneID component
            // TODO: Add SceneID component to entity

            // Deserialize components
            DeserializeEntityComponents(editorWorld, entity, entityData.Components);
        }

        // Now establish parent-child relationships
        for (int i = 0; i < sceneData.Entities.Count; i++)
        {
            var entityData = sceneData.Entities[i];
            var entityNode = sceneGraph.GetEntityNode(createdEntities[i]);

            if (entityNode == null)
            {
                context.AddValidationError($"Entity node for {createdEntities[i]} not found.");
                continue;
            }

            // Add to scene or to parent entity
            if (entityData.ParentFileLocalId == -1)
            {
                // Root entity in scene - should already be added
            }
            else if (entityData.ParentFileLocalId < 0 || entityData.ParentFileLocalId >= createdEntities.Count)
            {
                context.AddValidationError($"Invalid parent file-local ID {entityData.ParentFileLocalId} for entity {entityData.Name}.");
            }
            else
            {
                var parentEntity = createdEntities[entityData.ParentFileLocalId];
                var parentNode = sceneGraph.GetEntityNode(parentEntity);
                if (parentNode != null)
                {
                    sceneGraph.SetEntityParent(createdEntities[i], parentEntity);
                }
            }
        }

        // Report any validation errors
        if (context.HasErrors)
        {
            // Log or handle errors
            System.Diagnostics.Debug.WriteLine($"Scene load validation errors:\n{context.GetErrorsSummary()}");
        }
    }

    /// <summary>
    /// Serializes all components on an entity.
    /// </summary>
    private List<ComponentData> SerializeEntityComponents(World editorWorld, Entity entity)
    {
        var components = new List<ComponentData>();

        // TODO: Query entity components and serialize them
        // This requires integration with the ECS world

        return components;
    }

    /// <summary>
    /// Deserializes components onto an entity.
    /// </summary>
    private void DeserializeEntityComponents(World editorWorld, Entity entity, List<ComponentData> componentDataList)
    {
        // TODO: Deserialize component data and add to entity
        // This requires integration with the ECS world and reflection
    }

    /// <summary>
    /// Validates that no entity references entities in other scenes.
    /// </summary>
    private void ValidateNoInvalidReferences(SceneAssetData sceneData)
    {
        var validFileLocalIds = sceneData.Entities.Select(e => e.FileLocalId).ToHashSet();

        foreach (var entity in sceneData.Entities)
        {
            // TODO: Check component data for cross-scene entity references
        }
    }

    /// <summary>
    /// Gets the file-local ID for an entity node within a scene.
    /// </summary>
    private int GetFileLocalId(List<EntityNode> entities, EntityNode node)
    {
        return entities.IndexOf(node);
    }
}
