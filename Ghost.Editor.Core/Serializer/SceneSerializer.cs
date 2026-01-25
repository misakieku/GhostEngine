using Ghost.Core;
using Ghost.Editor.Core.Serializer.Converters;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Engine.IO;
using Ghost.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ghost.Editor.Core.Serializer;

/// <summary>
/// Handles JSON serialization and deserialization of scenes.
/// </summary>
public static class SceneSerializer
{
    private static readonly JsonSerializerOptions s_serializerOptions;

    static SceneSerializer()
    {
        s_serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true,
            Converters =
            {
                new EntityJsonConverter(),
                new JsonStringEnumConverter()
            }
        };
    }

    /// <summary>
    /// Represents the serialized data for a single entity.
    /// </summary>
    private class SerializedEntity
    {
        public int FileID { get; set; }
        public List<SerializedComponent> Components { get; set; } = new();
    }

    /// <summary>
    /// Represents a serialized component with its type and data.
    /// </summary>
    private class SerializedComponent
    {
        public string TypeName { get; set; } = string.Empty;
        public JsonElement Data { get; set; }
    }

    /// <summary>
    /// Represents the complete scene file structure.
    /// </summary>
    private class SceneFile
    {
        public string Name { get; set; } = "Untitled Scene";
        public int Version { get; set; } = 1;
        public List<SerializedEntity> Entities { get; set; } = new();
    }

    /// <summary>
    /// Saves a scene to a JSON file.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="sceneID">The scene ID to save.</param>
    /// <param name="filePath">The path to save the scene file.</param>
    /// <param name="sceneName">Optional scene name.</param>
    public static async Task SaveSceneAsync(World world, short sceneID, string filePath, string? sceneName = null)
    {
        using var context = SerializationContext.Create();

        var sceneFile = new SceneFile
        {
            Name = sceneName ?? Path.GetFileNameWithoutExtension(filePath),
            Entities = new List<SerializedEntity>()
        };

        // Query all entities with the specified SceneID
        var queryID = new QueryBuilder()
            .WithAll<SceneID>()
            .Build(world);

        var entities = new List<Entity>();

        world.ComponentManager.GetEntityQueryReference(queryID).ForEach<SceneID>((Entity entity, ref SceneID sceneIDComponent) =>
        {
            if (sceneIDComponent.id == sceneID)
            {
                entities.Add(entity);
            }
        });

        // Serialize each entity
        foreach (var entity in entities)
        {
            var fileId = context.RegisterEntityForSerialization(entity);
            var serializedEntity = new SerializedEntity
            {
                FileID = fileId,
                Components = new List<SerializedComponent>()
            };

            // Get entity location to find archetype
            var locationResult = world.EntityManager.GetEntityLocation(entity);
            if (locationResult.Error != Error.None)
            {
                continue;
            }

            var location = locationResult.Value;
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.archetypeID);

            // Serialize each component
            foreach (var layout in archetype._layouts)
            {
                var componentType = ComponentRegistry.s_runtimeIDToType[layout.componentID];
                
                if (componentType == null || componentType.AssemblyQualifiedName == null)
                {
                    continue;
                }

                // Get component data
                unsafe
                {
                    var pComponentData = archetype.GetComponentData(location.chunkIndex, location.rowIndex, layout.componentID);
                    if (pComponentData == null)
                    {
                        continue;
                    }

                    // Serialize component to JSON
                    // We need to box the unmanaged data to serialize it
                    var boxedData = System.Runtime.InteropServices.Marshal.PtrToStructure((IntPtr)pComponentData, componentType);
                    var componentJson = JsonSerializer.Serialize(boxedData, componentType, s_serializerOptions);
                    var jsonElement = JsonDocument.Parse(componentJson).RootElement;

                    serializedEntity.Components.Add(new SerializedComponent
                    {
                        TypeName = componentType.AssemblyQualifiedName,
                        Data = jsonElement
                    });
                }
            }

            sceneFile.Entities.Add(serializedEntity);
        }

        // Write to file
        var json = JsonSerializer.Serialize(sceneFile, s_serializerOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Loads a scene from a JSON file into the specified world.
    /// </summary>
    /// <param name="world">The world to load the scene into.</param>
    /// <param name="filePath">The path to the scene file.</param>
    /// <param name="newSceneID">The new scene ID to assign to loaded entities.</param>
    /// <returns>The number of entities loaded.</returns>
    public static async Task<int> LoadSceneAsync(World world, string filePath, short newSceneID)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Scene file not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath);
        var sceneFile = JsonSerializer.Deserialize<SceneFile>(json, s_serializerOptions);

        if (sceneFile == null)
        {
            throw new InvalidOperationException("Failed to deserialize scene file.");
        }

        using var context = SerializationContext.Create();

        // Pass 1: Create all entities and build the ID mapping
        var fileIdToEntity = new Dictionary<int, Entity>();

        foreach (var serializedEntity in sceneFile.Entities)
        {
            var entity = world.EntityManager.CreateEntity();
            fileIdToEntity[serializedEntity.FileID] = entity;
            context.RegisterEntity(serializedEntity.FileID, entity);

            // Add SceneID component
            world.EntityManager.AddComponent(entity, new SceneID { id = newSceneID });
        }

        // Pass 2: Deserialize components (with automatic entity reference remapping)
        foreach (var serializedEntity in sceneFile.Entities)
        {
            if (!fileIdToEntity.TryGetValue(serializedEntity.FileID, out var entity))
            {
                continue;
            }

            foreach (var serializedComponent in serializedEntity.Components)
            {
                var componentType = Type.GetType(serializedComponent.TypeName);
                if (componentType == null)
                {
                    continue;
                }

                // Skip SceneID as we already added it
                if (componentType == typeof(SceneID))
                {
                    continue;
                }

                try
                {
                    // Deserialize the component data
                    var componentData = JsonSerializer.Deserialize(serializedComponent.Data.GetRawText(), componentType, s_serializerOptions);
                    
                    if (componentData == null)
                    {
                        continue;
                    }

                    // Add component to entity
                    unsafe
                    {
                        var componentID = ComponentRegistry.GetComponentID(componentType);
                        if (componentID.IsInvalid)
                        {
                            continue;
                        }

                        // For unmanaged components, we can use pointer magic
                        if (componentType.IsValueType)
                        {
                            var pinnedData = System.Runtime.InteropServices.GCHandle.Alloc(componentData, System.Runtime.InteropServices.GCHandleType.Pinned);
                            try
                            {
                                var ptr = pinnedData.AddrOfPinnedObject().ToPointer();
                                world.EntityManager.AddComponent(entity, componentID, ptr);
                            }
                            finally
                            {
                                pinnedData.Free();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue loading other components
                    Console.WriteLine($"Failed to deserialize component {serializedComponent.TypeName}: {ex.Message}");
                }
            }
        }

        return fileIdToEntity.Count;
    }
}
