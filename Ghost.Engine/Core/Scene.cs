using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Engine.Core;

/// <summary>
/// Represents a runtime scene - a collection of entities with the same SceneID.
/// </summary>
public readonly struct Scene : IEquatable<Scene>
{
    private readonly short _id;

    /// <summary>
    /// Gets the unique identifier of this scene.
    /// </summary>
    public short ID => _id;

    /// <summary>
    /// Gets whether this scene is valid.
    /// </summary>
    public bool IsValid => _id >= 0;

    /// <summary>
    /// Gets an invalid scene instance.
    /// </summary>
    public static Scene Invalid => new(-1);

    internal Scene(short id)
    {
        _id = id;
    }

    public bool Equals(Scene other)
    {
        return _id == other._id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Scene other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(Scene left, Scene right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Scene left, Scene right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"Scene {{ ID: {_id} }}";
    }
}

/// <summary>
/// Manages scenes within a world.
/// </summary>
/// <remarks>
/// This is a minimal runtime representation. All metadata (like scene names) 
/// should be stored in editor-only classes (SceneNode).
/// </remarks>
public class SceneManager
{
    private readonly World _world;
    private short _nextSceneID;

    internal SceneManager(World world)
    {
        _world = world;
        _nextSceneID = 0;
    }

    /// <summary>
    /// Creates a new scene in the world.
    /// </summary>
    /// <returns>The created scene.</returns>
    public Scene CreateScene()
    {
        var scene = new Scene(_nextSceneID++);
        return scene;
    }

    /// <summary>
    /// Destroys all entities belonging to the specified scene.
    /// </summary>
    /// <param name="scene">The scene to unload.</param>
    public void UnloadScene(Scene scene)
    {
        // Build query for entities with SceneID
        var builder = new QueryBuilder();
        builder.WithAll([ComponentTypeID<Components.SceneID>.Value]);
        var queryID = builder.Build(_world);
        ref var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        using var scope = AllocationManager.CreateStackScope();
        var entitiesToDestroy = new UnsafeList<Entity>(128, scope.AllocationHandle);

        // Iterate through all matching entities
        foreach (var chunk in query.GetChunkIterator())
        {
            var entities = chunk.GetEntities();
            var sceneIDs = chunk.GetComponentData<Components.SceneID>();

            for (var i = 0; i < chunk.Count; i++)
            {
                if (sceneIDs[i].id == scene.ID)
                {
                    entitiesToDestroy.Add(entities[i]);
                }
            }
        }

        _world.EntityManager.DestroyEntities(entitiesToDestroy.AsSpan());
    }

    /// <summary>
    /// Gets all entities belonging to the specified scene.
    /// </summary>
    /// <param name="scene">The scene to query.</param>
    /// <param name="entities">Span to store the entities.</param>
    /// <returns>The number of entities written to the span.</returns>
    public int GetSceneEntities(Scene scene, Span<Entity> entities)
    {
        // Build query for entities with SceneID
        var builder = new QueryBuilder();
        builder.WithAll([ComponentTypeID<Components.SceneID>.Value]);
        var queryID = builder.Build(_world);
        ref var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        var index = 0;

        // Iterate through all matching entities
        foreach (var chunk in query.GetChunkIterator())
        {
            var chunkEntities = chunk.GetEntities();
            var sceneIDs = chunk.GetComponentData<Components.SceneID>();

            for (var i = 0; i < chunk.Count && index < entities.Length; i++)
            {
                if (sceneIDs[i].id == scene.ID)
                {
                    entities[index++] = chunkEntities[i];
                }
            }
        }

        return index;
    }
}
