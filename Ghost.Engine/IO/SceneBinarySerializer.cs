using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Engine.IO;

/// <summary>
/// Handles binary serialization and deserialization of scenes for AOT-compatible runtime use.
/// </summary>
/// <remarks>
/// Binary format provides fast, compact scene loading suitable for AOT compilation.
/// Uses direct memory copying for component data without reflection.
/// </remarks>
public static unsafe class SceneBinarySerializer
{
    private const int MAGIC_NUMBER = 0x47534345; // "GSCE" (Ghost Scene)
    private const int VERSION = 1;

    /// <summary>
    /// Saves a scene to a binary file.
    /// </summary>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="sceneID">The scene ID to save.</param>
    /// <param name="filePath">The path to save the scene file.</param>
    public static void SaveScene(World world, short sceneID, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new BinaryWriter(stream);
        using var context = SerializationContext.Create();

        // Write header
        writer.Write(MAGIC_NUMBER);
        writer.Write(VERSION);
        writer.Write(sceneID);

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

        // Write entity count
        writer.Write(entities.Count);

        // Allocate buffer for zero-filled component data (reused across loop iterations)
        const int MaxComponentSize = 4096; // Reasonable max size for most components
        var zeroBuffer = stackalloc byte[MaxComponentSize];
        MemoryUtility.MemSet(zeroBuffer, 0, MaxComponentSize);

        // Write each entity
        foreach (var entity in entities)
        {
            var fileId = context.RegisterEntityForSerialization(entity);
            
            // Write entity file ID
            writer.Write(fileId);

            // Get entity location
            var locationResult = world.EntityManager.GetEntityLocation(entity);
            if (locationResult.Error != Error.None)
            {
                // Write 0 components for invalid entity
                writer.Write(0);
                continue;
            }

            var location = locationResult.Value;
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(location.archetypeID);

            // Write component count
            writer.Write(archetype._layouts.Count);

            // Write each component
            foreach (var layout in archetype._layouts)
            {
                // Write component type ID
                writer.Write((int)layout.componentID);

                // Write component size
                writer.Write(layout.size);

                // Get component data pointer
                var pComponentData = archetype.GetComponentData(location.chunkIndex, location.rowIndex, layout.componentID);
                if (pComponentData == null)
                {
                    // Write zero-filled data if component not found
                    if (layout.size > MaxComponentSize)
                    {
                        throw new InvalidOperationException($"Component size {layout.size} exceeds maximum buffer size {MaxComponentSize}");
                    }
                    writer.Write(new ReadOnlySpan<byte>(zeroBuffer, layout.size));
                }
                else
                {
                    // Write component data directly
                    writer.Write(new ReadOnlySpan<byte>(pComponentData, layout.size));
                }
            }
        }
    }

    /// <summary>
    /// Loads a scene from a binary file into the specified world.
    /// </summary>
    /// <param name="world">The world to load the scene into.</param>
    /// <param name="filePath">The path to the scene file.</param>
    /// <param name="newSceneID">The new scene ID to assign to loaded entities.</param>
    /// <returns>The number of entities loaded.</returns>
    public static int LoadScene(World world, string filePath, short newSceneID)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Scene file not found: {filePath}");
        }

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(stream);
        using var context = SerializationContext.Create();

        // Read and validate header
        var magic = reader.ReadInt32();
        if (magic != MAGIC_NUMBER)
        {
            throw new InvalidDataException("Invalid scene file format.");
        }

        var version = reader.ReadInt32();
        if (version != VERSION)
        {
            throw new InvalidDataException($"Unsupported scene file version: {version}");
        }

        var savedSceneID = reader.ReadInt16();

        // Read entity count
        var entityCount = reader.ReadInt32();

        // Pass 1: Create all entities and build ID mapping
        var fileIdToEntity = new Dictionary<int, Entity>(entityCount);
        var entityComponents = new List<(int fileId, List<(Identifier<IComponent> componentID, int size, byte[] data)> components)>(entityCount);

        for (var i = 0; i < entityCount; i++)
        {
            var fileId = reader.ReadInt32();
            var componentCount = reader.ReadInt32();

            var components = new List<(Identifier<IComponent> componentID, int size, byte[] data)>(componentCount);

            // Read component data
            for (var j = 0; j < componentCount; j++)
            {
                var componentID = new Identifier<IComponent>(reader.ReadInt32());
                var size = reader.ReadInt32();
                var data = reader.ReadBytes(size);

                components.Add((componentID, size, data));
            }

            entityComponents.Add((fileId, components));

            // Create entity
            var entity = world.EntityManager.CreateEntity();
            fileIdToEntity[fileId] = entity;
            context.RegisterEntity(fileId, entity);

            // Add SceneID component
            world.EntityManager.AddComponent(entity, new SceneID { id = newSceneID });
        }

        // Pass 2: Add components to entities (with automatic entity reference remapping)
        foreach (var (fileId, components) in entityComponents)
        {
            if (!fileIdToEntity.TryGetValue(fileId, out var entity))
            {
                continue;
            }

            foreach (var (componentID, size, data) in components)
            {
                // Skip SceneID as we already added it
                if (componentID == ComponentTypeID<SceneID>.Value)
                {
                    continue;
                }

                fixed (byte* pData = data)
                {
                    // Remap Entity references in the component data
                    RemapEntityReferences(pData, componentID, context);

                    // Add component
                    world.EntityManager.AddComponent(entity, componentID, pData);
                }
            }
        }

        return fileIdToEntity.Count;
    }

    /// <summary>
    /// Remaps Entity references within component data.
    /// </summary>
    /// <remarks>
    /// This is a simple implementation that checks if the component contains Entity fields.
    /// For Hierarchy, it remaps parent, firstChild, and nextSibling fields.
    /// </remarks>
    private static void RemapEntityReferences(byte* pComponentData, Identifier<IComponent> componentID, SerializationContext context)
    {
        // Check if this is the Hierarchy component
        if (componentID == ComponentTypeID<Hierarchy>.Value)
        {
            var hierarchy = (Hierarchy*)pComponentData;

            // Remap parent
            if (hierarchy->parent.IsValid && context.TryGetFileId(hierarchy->parent, out var parentFileId))
            {
                if (context.TryGetEntity(parentFileId, out var newParent))
                {
                    hierarchy->parent = newParent;
                }
            }

            // Remap firstChild
            if (hierarchy->firstChild.IsValid && context.TryGetFileId(hierarchy->firstChild, out var firstChildFileId))
            {
                if (context.TryGetEntity(firstChildFileId, out var newFirstChild))
                {
                    hierarchy->firstChild = newFirstChild;
                }
            }

            // Remap nextSibling
            if (hierarchy->nextSibling.IsValid && context.TryGetFileId(hierarchy->nextSibling, out var nextSiblingFileId))
            {
                if (context.TryGetEntity(nextSiblingFileId, out var newNextSibling))
                {
                    hierarchy->nextSibling = newNextSibling;
                }
            }
        }

        // TODO: Add remapping for other components with Entity fields
        // This could be automated using source generators in the future
    }
}
