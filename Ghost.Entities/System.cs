using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public abstract class SystemBase
{
    /// <summary>
    /// Gets the execution order of the current operation or component.
    /// </summary>
    public virtual int ExecutionOrder => 0;

    /// <summary>
    /// Gets or sets a value indicating whether the feature is enabled.
    /// </summary>
    public virtual bool Enable
    {
        get;
        set;
    } = true;

    /// <summary>
    /// The world that this system belongs to.
    /// </summary>
    public World World
    {
        get;
        internal set;
    } = null!;

    public virtual void OnCreate()
    {
    }

    public virtual void OnUpdate()
    {
    }

    public virtual void OnDestroy()
    {
    }
}

public class SystemStorage : IDisposable
{
    private readonly List<SystemBase> _systems = new();
    private readonly List<SystemBase> _executionList = new();

    private readonly World _world;

    public event Action<SystemBase>? SystemAdded;
    public event Action<SystemBase>? SystemRemoved;

    internal SystemStorage(World world)
    {
        _world = world;
    }

    public void AddSystem<T>(T system)
        where T : SystemBase
    {
        _systems.Add(system);
        system.World = _world;
        if (system.Enable)
        {
            system.OnCreate();
        }

        SystemAdded?.Invoke(system);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSystem<T>()
        where T : SystemBase, new()
    {
        AddSystem(new T());
    }

    public void RemoveSystem<T>(T system)
        where T : SystemBase
    {
        system.World = null!;
        _systems.Remove(system);
        if (system.Enable)
        {
            system.OnDestroy();
        }

        SystemRemoved?.Invoke(system);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveSystem<T>()
        where T : SystemBase, new()
    {
        var system = _systems.FirstOrDefault(s => s is T);
        if (system != null)
        {
            RemoveSystem(system);
        }
    }

    internal void RebuildExecutionList()
    {
        _executionList.Clear();
        _executionList.AddRange(_systems.OrderBy(s => s.ExecutionOrder));
    }

    internal void UpdateSystems()
    {
        foreach (var system in _systems)
        {
            if (!system.Enable)
            {
                continue;
            }
            system.OnUpdate();
        }
    }

    public void Dispose()
    {
        foreach (var system in _systems)
        {
            if (!system.Enable)
            {
                continue;
            }
            system.OnDestroy();
        }

        _systems.Clear();
        _executionList.Clear();
    }
}