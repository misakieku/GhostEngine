using Ghost.Engine.Components;
using Ghost.Engine.Services;
using System.Collections.ObjectModel;

namespace Ghost.Engine;

public class GameObject
{
    private readonly ObservableCollection<Component> _components = new();

    public string name = string.Empty;
    public bool isActive = true;

    public Transform Transform { get; } = new();

    private GameObject()
    {
        AddComponent(Transform);
    }

    public static GameObject Create(string name = "")
    {
        var gameObject = new GameObject
        {
            name = name
        };

        GameLoopService.RegisterGameObject(gameObject);
        return gameObject;
    }

    public void AddComponent(Component component)
    {
        _components.Add(component);
    }

    public void RemoveComponent(Component component)
    {
        _components.Remove(component);
    }

    public T? GetComponent<T>() where T : Component
    {
        foreach (var component in _components)
        {
            if (component is T t)
            {
                return t;
            }
        }

        return null;
    }

    internal void Start()
    {
        foreach (var component in _components)
        {
            component.Start();
        }
    }

    internal void Update()
    {
        foreach (var component in _components)
        {
            component.Update();
        }
    }

    internal void LateUpdate()
    {
        foreach (var component in _components)
        {
            component.LateUpdate();
        }
    }

    internal void FixedUpdate()
    {
        foreach (var component in _components)
        {
            component.FixedUpdate();
        }
    }

    public void Destroy()
    {
        foreach (var component in _components)
        {
            component.OnDestroy();
        }

        GameLoopService.UnregisterGameObject(this);
    }
}