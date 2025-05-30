using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Entities;
using Ghost.Entities.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ghost.Editor.Models;

public partial class GameObject : ObservableObject
{
    [ObservableProperty]
    public partial bool IsActive
    {
        get;
        set;
    }

    [ObservableProperty]
    public partial bool IsActiveHierarchy
    {
        get;
        set;
    }

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

    [ObservableProperty]
    public partial ObservableCollection<IComponentData>? Components
    {
        get;
        private set;
    }

    [ObservableProperty]
    public partial IEnumerable<ScriptComponent>? ScriptComponents
    {
        get;
        private set;
    }

    [ObservableProperty]
    public partial ObservableCollection<GameObject>? Children
    {
        get;
        private set;
    }

    public GameObject(Scene scene, string name)
    {
        Entity = scene.World.EntityManager.CreateEntity();
        Scene = scene;
        Name = name;
        IsActive = true;
    }

    partial void OnIsActiveChanged(bool value)
    {
        IsActiveHierarchy = value && (Parent?.IsActiveHierarchy ?? true);
        HandleActiveStateChanged();

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.IsActiveHierarchy = value && IsActiveHierarchy;
            }
        }
    }

    partial void OnIsActiveHierarchyChanged(bool value)
    {
        HandleActiveStateChanged();
    }

    private void HandleActiveStateChanged()
    {
        if (IsActive && IsActiveHierarchy)
        {
            OnEnable();
        }
        else
        {
            OnDisable();
        }
    }

    internal void OnEnable()
    {
        if (ScriptComponents != null)
        {
            foreach (var script in ScriptComponents)
            {
                if (!script.Enable)
                {
                    continue;
                }
                script.OnEnable();
            }
        }
    }

    internal void OnDisable()
    {
        if (ScriptComponents != null)
        {
            foreach (var script in ScriptComponents)
            {
                if (!script.Enable)
                {
                    continue;
                }
                script.OnDisable();
            }
        }
    }

    public void AddChild(GameObject child)
    {
        if (child.Scene != Scene)
        {
            throw new InvalidOperationException("Child GameObject must belong to the same Scene.");
        }

        Children ??= new();
        Children.Add(child);
        child.Parent = this;
    }

    public bool RemoveChild(GameObject child)
    {
        if (Children is null)
        {
            return false;
        }

        if (!Children.Remove(child))
        {
            return false;
        }

        child.Parent = null;
        return true;
    }

    public void Destroy()
    {
        if (ScriptComponents != null)
        {
            foreach (var component in ScriptComponents)
            {
                if (!component.Enable)
                {
                    continue;
                }
                component.OnDestroy();
            }
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.Destroy();
            }

            Children.Clear();
        }

        Parent?.Children?.Remove(this);
        Entity.Destroy();
    }
}

public partial class GameObject
{
    // TODO: Implement a more efficient synchronization mechanism for components
    internal void SyncComponents()
    {
        foreach (var (typeHandle, mask) in Scene.World.ComponentStorage.ComponentEntityMasks)
        {
            if (!mask.IsSet(Entity.ID))
            {
                continue;
            }

            var pool = Scene.World.ComponentStorage.ComponentPools[typeHandle];
        }
    }

    internal void SyncScripts()
    {
        var scriptsPool = Scene.World.ComponentStorage.ScriptComponentPool.ScriptComponents;
        if (scriptsPool == null)
        {
            return;
        }

        scriptsPool.TryGetValue(Entity, out var scripts);
        ScriptComponents = scripts;
    }

    public void AddComponent<T>(T component)
        where T : struct, IComponentData
    {
        Entity.AddComponent<T>(component);
        SyncComponents();
    }

    public bool RemoveComponent<T>()
        where T : struct, IComponentData
    {
        var result = Entity.RemoveComponent<T>();
        SyncComponents();

        return result;
    }

    public void AddScript<T>()
        where T : ScriptComponent, new()
    {
        Entity.AddScript<T>();
        SyncScripts();
    }

    public void AddScript(Type type)
    {
        Entity.AddScript(type);
        SyncScripts();
    }

    public bool RemoveScript<T>()
        where T : ScriptComponent
    {
        var result = Scene.World.EntityManager.RemoveScript<T>(Entity);
        SyncScripts();

        return result;
    }

    public bool RemoveScriptAt(int index)
    {
        var result = Scene.World.EntityManager.RemoveScriptAt(Entity, index);
        SyncScripts();

        return result;
    }
}