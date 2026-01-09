using Ghost.Editor.Core.Progress;
using Ghost.Editor.Core.Resources;
using Ghost.Editor.Core.Utilities;
using System.Text.Json;

namespace Ghost.Editor.Core.SceneGraph;

public enum OpenWorldMode
{
    Single,
    Additive,
    AdditiveWithoutLoading
}

public static class EditorSceneManager
{
    // TODO: Use guid keys instead of string paths for better performance and uniqueness
    private static readonly Dictionary<string, SceneNode> s_loadedWorlds = new();
    public static IEnumerable<SceneNode> LoadedWorlds => s_loadedWorlds.Values;

    public static event Action<SceneNode>? OnWorldLoaded;
    public static event Action<SceneNode>? OnWorldUnloaded;

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

        foreach (var world in s_loadedWorlds)
        {
            world.Value.Unload();
            OnWorldUnloaded?.Invoke(world.Value);
        }

        await using var readStream = new FileStream(worldPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var deserializedScene = await JsonSerializer.DeserializeAsync<SceneNode>(readStream, Engine.Resources.EngineResource.defaultSerializerOptions) ?? throw new Exception("Deserialization failed.");

        s_loadedWorlds.Clear();

        s_loadedWorlds[worldPath] = deserializedScene;
        await deserializedScene.LoadAsync();

        progressService.HideProgress();
        OnWorldLoaded?.Invoke(deserializedScene);
    }
}
