using Ghost.Engine.Components;
using Ghost.Engine.Core;
using Ghost.Engine.IO;
using Ghost.Entities;
using System.Runtime.InteropServices;

namespace Ghost.Engine.Services;

public enum SceneLoadMode
{
    /// <summary>
    /// Unloads all currently loaded scenes before loading the new scene.
    /// </summary>
    Single,

    /// <summary>
    /// Loads the scene additively without unloading existing scenes.
    /// </summary>
    Additive
}

/// <summary>
/// Manages scene loading, unloading, and saving operations using binary serialization.
/// </summary>
/// <remarks>
/// This runtime scene manager uses binary serialization for AOT compatibility.
/// For editor JSON serialization, use EditorSceneManager in Ghost.Editor.Core.
/// </remarks>
public static class SceneManager
{
    private static readonly Dictionary<short, Scene> s_loadedScenes = new();

    /// <summary>
    /// Gets all currently loaded scenes.
    /// </summary>
    public static IReadOnlyCollection<Scene> LoadedScenes => s_loadedScenes.Values;

    /// <summary>
    /// Loads a scene from a binary file into the specified world.
    /// </summary>
    /// <param name="world">The world to load the scene into.</param>
    /// <param name="filePath">The path to the scene file.</param>
    /// <param name="loadMode">The load mode (Single or Additive).</param>
    /// <returns>The loaded scene.</returns>
    public static Scene LoadScene(World world, string filePath, SceneLoadMode loadMode = SceneLoadMode.Single)
    {
        if (loadMode == SceneLoadMode.Single)
        {
            // Unload all currently loaded scenes in this world
            var scenesToUnload = s_loadedScenes.Values.Where(s => s.World == world).ToList();
            foreach (var scene in scenesToUnload)
            {
                UnloadScene(scene);
            }
        }

        // Generate a new scene ID for this load
        var sceneName = Path.GetFileNameWithoutExtension(filePath);
        var newScene = new Scene(world, sceneName);

        // Load the scene data using binary serialization
        SceneBinarySerializer.LoadScene(world, filePath, newScene.ID);

        // Register the loaded scene
        s_loadedScenes[newScene.ID] = newScene;

        return newScene;
    }

    /// <summary>
    /// Saves a scene to a binary file.
    /// </summary>
    /// <param name="scene">The scene to save.</param>
    /// <param name="filePath">The path to save the scene file.</param>
    public static void SaveScene(Scene scene, string filePath)
    {
        SceneBinarySerializer.SaveScene(scene.World, scene.ID, filePath);
    }

    /// <summary>
    /// Unloads a scene, destroying all entities belonging to it.
    /// </summary>
    /// <param name="scene">The scene to unload.</param>
    public static void UnloadScene(Scene scene)
    {
        if (!s_loadedScenes.ContainsKey(scene.ID))
        {
            return;
        }

        // Query all entities with the scene's ID
        var queryID = new QueryBuilder()
            .WithAll<SceneID>()
            .Build(scene.World);

        var entitiesToDestroy = new List<Entity>();

        scene.World.ComponentManager.GetEntityQueryReference(queryID).ForEach<SceneID>((Entity entity, ref SceneID sceneIDComponent) =>
        {
            if (sceneIDComponent.id == scene.ID)
            {
                entitiesToDestroy.Add(entity);
            }
        });
        
        // Destroy all entities in this scene
        scene.World.EntityManager.DestroyEntities(CollectionsMarshal.AsSpan(entitiesToDestroy));

        // Remove from loaded scenes
        s_loadedScenes.Remove(scene.ID);

        // Dispose the scene handle
        scene.Dispose();
    }

    /// <summary>
    /// Unloads all scenes in the specified world.
    /// </summary>
    /// <param name="world">The world whose scenes to unload.</param>
    public static void UnloadAllScenes(World world)
    {
        var scenesToUnload = s_loadedScenes.Values.Where(s => s.World == world).ToList();
        foreach (var scene in scenesToUnload)
        {
            UnloadScene(scene);
        }
    }

    /// <summary>
    /// Tries to get a loaded scene by its ID.
    /// </summary>
    /// <param name="sceneID">The scene ID to find.</param>
    /// <param name="scene">The found scene, or null if not loaded.</param>
    /// <returns>True if the scene was found, false otherwise.</returns>
    public static bool TryGetScene(short sceneID, out Scene? scene)
    {
        return s_loadedScenes.TryGetValue(sceneID, out scene);
    }
}