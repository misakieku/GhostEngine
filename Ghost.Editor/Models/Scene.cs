using Ghost.Entities;
using System.Collections.Generic;

namespace Ghost.Editor.Models;

public class Scene
{
    private readonly HashSet<GameObject> _rootObjects = new();
    private readonly World _world = World.Create();

    public IEnumerable<GameObject> RootObjects => _rootObjects;
    public World World => _world;

    internal Scene()
    {
    }

    internal void Load()
    {
        foreach (var gameObject in _rootObjects)
        {
            gameObject.OnEnable();
        }
    }

    internal void Unload()
    {
        foreach (var gameObject in _rootObjects)
        {
            gameObject.OnDisable();
            gameObject.Destroy();
        }

        _rootObjects.Clear();
    }
}