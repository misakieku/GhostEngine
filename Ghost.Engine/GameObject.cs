using Ghost.Engine.Models;
using Ghost.Entities;
using System.ComponentModel;

namespace Ghost.Engine;

public unsafe class GameObject : INotifyPropertyChanged
{
    private readonly Dictionary<Type, ScriptComponent> _components = new();
    private readonly List<GameObject> _children = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public Entity Entity
    {
        get;
    }

    public Scene Scene
    {
        get;
        internal set;
    }

    public GameObject? Parent
    {
        get;
        internal set;
    }

    public string Name
    {
        get;
        set;
    }

    public bool IsActive
    {
        get;
        set;
    }

    public IEnumerable<ScriptComponent> Components => _components.Values;
    public IEnumerable<GameObject> Children => _children;

    public GameObject(Scene scene, string name)
    {
        // TODO: Initialize Entity properly
        //Entity =
        Scene = scene;
        Name = name;
        IsActive = true;
    }

    public void AddComponent<T>(T component)
        where T : ScriptComponent
    {
        _components.Add(typeof(T), component);
        component.Owner = Entity;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Components)));
    }

    public void RemoveComponent<T>()
        where T : ScriptComponent
    {
        var key = typeof(T);
        if (_components.Remove(key, out var component))
        {
            component.OnDestroy();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Components)));
        }
    }

    public T? GetComponent<T>()
        where T : ScriptComponent
    {
        if (_components.TryGetValue(typeof(T), out var component))
        {
            return (T)component;
        }

        return null;
    }

    public void AddChild(GameObject child)
    {
        if (child.Scene != Scene)
        {
            throw new InvalidOperationException("Child GameObject must belong to the same Scene.");
        }

        _children.Add(child);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
    }

    public void RemoveChild(GameObject child)
    {
        if (_children.Remove(child))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Children)));
        }
    }

    internal void OnEnable()
    {
        foreach (var component in Components)
        {
            if (!component.Enable)
            {
                continue;
            }
            component.OnEnable();
        }

        foreach (var child in _children)
        {
            child.OnEnable();
        }
    }

    internal void Start()
    {
        foreach (var component in Components)
        {
            if (!component.Enable)
            {
                continue;
            }
            component.Start();
        }
    }

    internal void Update()
    {
        foreach (var component in Components)
        {
            if (!component.Enable)
            {
                continue;
            }
            component.Update();
        }
    }

    internal void LateUpdate()
    {
        foreach (var component in Components)
        {
            if (!component.Enable)
            {
                continue;
            }
            component.LateUpdate();
        }
    }

    internal void FixedUpdate()
    {
        foreach (var component in Components)
        {
            if (!component.Enable)
            {
                continue;
            }
            component.FixedUpdate();
        }
    }

    public void Destroy()
    {
        foreach (var component in Components)
        {
            if (!component.Enable)
            {
                continue;
            }
            component.OnDestroy();
        }

        foreach (var child in _children)
        {
            child.Destroy();
        }

        _children.Clear();
        _components.Clear();
        Parent?._children.Remove(this);
    }
}