namespace Ghost.Engine.Models;

public class Scene
{
    private readonly HashSet<GameObject> _rootObjects = new();

    public IEnumerable<GameObject> RootObjects => _rootObjects;

    internal Scene()
    {
    }

    internal void Load()
    {
        foreach (var gameObject in _rootObjects)
        {
            gameObject.Start();
        }
    }

    internal void Unload()
    {
        foreach (var gameObject in _rootObjects)
        {
            gameObject.Destroy();
        }

        _rootObjects.Clear();
    }
}