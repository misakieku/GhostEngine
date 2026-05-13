using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Text.Json.Serialization;

namespace Ghost.Engine.Core;

/// <summary>
/// Represents a runtime scene - a collection of entities with the same SceneID.
/// </summary>
public struct Scene : IEquatable<Scene>
{
    public ushort id;

    /// <summary>
    /// Gets whether this scene is valid.
    /// </summary>
    [JsonIgnore]
    public readonly bool IsValid => id != 65535;

    /// <summary>
    /// Gets an invalid scene instance.
    /// </summary>
    public static Scene Invalid => new Scene { id = 65535 };

    public readonly bool Equals(Scene other)
    {
        return id == other.id;
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Scene other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return id.GetHashCode();
    }

    public static bool operator ==(Scene left, Scene right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Scene left, Scene right)
    {
        return !left.Equals(right);
    }

    public readonly override string ToString()
    {
        return $"Scene(ID: {id})";
    }
}

/// <summary>
/// Manages scenes within a world.
/// </summary>
/// <remarks>
/// This is a minimal runtime representation. All metadata (like scene names) 
/// should be stored in editor-only classes (SceneNode).
/// </remarks>
public static class SceneManager
{
    private static ushort s_nextSceneID;
    private static readonly Queue<ushort> s_recycledSceneIDs = new();

    private static readonly Lock s_creationLock = new();

    /// <summary>
    /// Creates a new scene in the world.
    /// </summary>
    /// <returns>The created scene.</returns>
    public static Scene CreateScene()
    {
        lock (s_creationLock)
        {
            if (!s_recycledSceneIDs.TryDequeue(out var id))
            {
                id = s_nextSceneID++;
            }

            return new Scene { id = id };
        }
    }

    /// <summary>
    /// Destroys all entities belonging to the specified scene.
    /// </summary>
    /// <param name="scene">The scene to unload.</param>
    /// <param name="world">The world containing the entities.</param>
    public static void UnloadScene(Scene scene, World world)
    {
        var queryID = new QueryBuilder().WithAll<Components.SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);

        using var scope = AllocationManager.CreateStackScope();
        var entitiesToDestroy = new UnsafeList<Entity>(128, scope.AllocationHandle);

        // Iterate through all matching entities
        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            var sceneIDs = chunk.GetComponentData<Components.SceneID>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                if (sceneIDs[i].scene.id == scene.id)
                {
                    entitiesToDestroy.Add(entities[i]);
                }
            }
        }

        world.EntityManager.DestroyEntities(entitiesToDestroy.AsSpan());
        s_recycledSceneIDs.Enqueue(scene.id);
    }

    /// <summary>
    /// Gets all entities belonging to the specified scene.
    /// </summary>
    /// <param name="scene">The scene to query.</param>
    /// <param name="world">The world containing the entities.</param>
    /// <param name="entities">Span to store the entities.</param>
    /// <returns>The number of entities written to the span.</returns>
    public static UnsafeList<Entity> GetSceneEntities(Scene scene, World world, AllocationHandle handle)
    {
        var queryID = new QueryBuilder().WithAll<Components.SceneID>().Build(world);
        ref var query = ref world.ComponentManager.GetEntityQueryReference(queryID);

        var entities = new UnsafeList<Entity>(128, handle);

        // Iterate through all matching entities
        foreach (var chunk in query.GetChunkIterator())
        {
            var chunkEntities = chunk.GetEntities();
            var sceneIDs = chunk.GetComponentData<Components.SceneID>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                if (sceneIDs[i].scene.id == scene.id)
                {
                    entities.Add(chunkEntities[i]);
                }
            }
        }

        return entities;
    }
}
