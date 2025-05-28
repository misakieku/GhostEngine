using Ghost.Engine.Models;

namespace Ghost.Engine.Services;

public enum SceneLoadMode
{
    Single,
    Additive
}

public static class SceneManager
{
    private readonly static HashSet<Scene> _activeScenes = new();

    internal static IEnumerable<GameObject> QueryRootGameObjects()
    {
        foreach (var scene in _activeScenes)
        {
            foreach (var gameObject in scene.RootObjects)
            {
                if (!gameObject.IsActive)
                {
                    continue;
                }

                yield return gameObject;
            }
        }
    }

    public static void LoadScene(Scene scene, SceneLoadMode loadMode)
    {
        if (loadMode == SceneLoadMode.Single)
        {
            foreach (var activeScene in _activeScenes)
            {
                activeScene.Unload();
            }
            _activeScenes.Clear();
        }

        _activeScenes.Add(scene);
        scene.Load();
    }

    public static Task LoadSceneAsync(Scene scene, SceneLoadMode loadMode)
    {
        return Task.Run(() => LoadScene(scene, loadMode));
    }
}