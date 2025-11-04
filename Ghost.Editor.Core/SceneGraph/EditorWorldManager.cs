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

public static class EditorWorldManager
{
    // TODO: Use guid keys instead of string paths for better performance and uniqueness
    private static readonly Dictionary<string, WorldNode> _loadedWorlds = new();
    public static IEnumerable<WorldNode> LoadedWorlds => _loadedWorlds.Values;

    public static event Action<WorldNode>? OnWorldLoaded;
    public static event Action<WorldNode>? OnWorldUnloaded;

    public static async Task LoadWorld(string worldPath)
    {
        if (_loadedWorlds.ContainsKey(worldPath)
            || !File.Exists(worldPath)
            || Path.GetExtension(worldPath) != FileExtensions.SCENE_FILE_EXTENSION)
        {
            return;
        }

        var progressService = EditorApplication.GetService<IProgressService>();
        progressService.ShowIndeterminateProgress("Loading world...");

        foreach (var world in _loadedWorlds)
        {
            world.Value.Unload();
            OnWorldUnloaded?.Invoke(world.Value);
        }

        await using var readStream = new FileStream(worldPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var deserializedScene = await JsonSerializer.DeserializeAsync<WorldNode>(readStream, Engine.Resources.StaticResource.defaultSerializerOptions) ?? throw new Exception("Deserialization failed.");

        _loadedWorlds.Clear();

        _loadedWorlds[worldPath] = deserializedScene;
        await deserializedScene.LoadAsync();

        progressService.HideProgress();
        OnWorldLoaded?.Invoke(deserializedScene);
    }
}