namespace Ghost.Entities;

public abstract class SystemBase
{
    public virtual int ExecutionOrder => 0;

    public virtual bool Enable
    {
        get;
        set;
    } = true;

    public World World
    {
        get;
        init;
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

internal class SystemStorage : IDisposable
{
    private readonly List<SystemBase> _systems = new();
    private readonly List<SystemBase> _executionList = new();

    public void AddSystem<T>(T system)
        where T : SystemBase
    {
        _systems.Add(system);
        if (system.Enable)
        {
            system.OnCreate();
        }
    }

    public void RemoveSystem<T>(T system)
        where T : SystemBase
    {
        _systems.Remove(system);
        if (system.Enable)
        {
            system.OnDestroy();
        }
    }

    public void RebuildExecutionList()
    {
        _executionList.Clear();
        _executionList.AddRange(_systems.OrderBy(s => s.ExecutionOrder));
    }

    public void UpdateSystems()
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