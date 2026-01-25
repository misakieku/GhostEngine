using Ghost.Editor.Core.Progress;
using Ghost.Editor.Core.Resources;
using Ghost.Editor.Core.Serializer;
using Ghost.Editor.Core.Utilities;
using Ghost.Engine.Core;
using System.Text.Json;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Editor-specific scene manager that uses JSON serialization for human-readable scene files.
/// </summary>
/// <remarks>
/// This manager provides JSON-based serialization suitable for editor workflows.
/// Runtime applications should use SceneManager with binary serialization.
/// </remarks>
public static class EditorSceneManager
{
    // TODO: Use guid keys instead of string paths for better performance and uniqueness
    private static readonly Dictionary<string, SceneNode> s_loadedWorlds = new();
    public static IEnumerable<SceneNode> LoadedWorlds => s_loadedWorlds.Values;

    public static event Action<SceneNode>? OnWorldLoaded;
    public static event Action<SceneNode>? OnWorldUnloaded;

    /// <summary>
    /// Loads a scene from a JSON file.
    /// </summary>
    /// <param name="worldPath">The path to the JSON scene file.</param>
    public static async Task LoadSceneAsync(string worldPath)
    {
        if (s_loadedWorlds.ContainsKey(worldPath)
            || !File.Exists(worldPath)
            || Path.GetExtension(worldPath) != FileExtensions.SCENE_FILE_EXTENSION)
        {
            return;
        }

        var progressService = EditorApplication.GetService<IProgressService>();
        progressService.ShowIndeterminateProgress("Loading world...");

        // Unload existing scenes
        foreach (var world in s_loadedWorlds)
        {
            world.Value.Unload();
            OnWorldUnloaded?.Invoke(world.Value);
        }

        s_loadedWorlds.Clear();

        // TODO: Get or create World instance for editor
        // For now, keep compatibility with old SceneNode deserialization
        await using var readStream = new FileStream(worldPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var deserializedScene = await JsonSerializer.DeserializeAsync<SceneNode>(readStream, Engine.Resources.EngineResource.defaultSerializerOptions) ?? throw new Exception("Deserialization failed.");

        s_loadedWorlds[worldPath] = deserializedScene;
        await deserializedScene.LoadAsync();

        progressService.HideProgress();
        OnWorldLoaded?.Invoke(deserializedScene);
    }

    /// <summary>
    /// Saves a scene to a JSON file using the new serializer.
    /// </summary>
    /// <param name="scene">The scene to save.</param>
    /// <param name="filePath">The path to save the JSON scene file.</param>
    public static async Task SaveSceneAsync(Scene scene, string filePath)
    {
        await SceneSerializer.SaveSceneAsync(scene.World, scene.ID, filePath, scene.Name);
    }
}
