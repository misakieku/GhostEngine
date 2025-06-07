using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities.Systems;

[SkipLocalsInit]
public readonly struct SystemStorage
{
    private readonly List<Type> _systems = new();
    private readonly List<ISystem> _executionList = new();

    private readonly World _world;

    internal ReadOnlySpan<Type> Systems => CollectionsMarshal.AsSpan(_systems);

    internal SystemStorage(World world)
    {
        _world = world;
    }

    public readonly void AddSystem(Type systemType)
    {
        _systems.Add(systemType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void AddSystem<T>()
        where T : ISystem, new()
    {
        AddSystem(typeof(T));
    }

    public readonly void RemoveSystem(Type systemType)
    {
        _systems.Remove(systemType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void RemoveSystem<T>()
        where T : ISystem, new()
    {
        RemoveSystem(typeof(T));
    }

    internal void CreateSystems()
    {
        var builder = new SystemDependencyBuilder(_systems);
        builder.BuildDependencyGraph();
        var executionOrder = builder.BuildExecutionOrder();

        var state = new SystemState()
        {
            World = _world,
        };

        foreach (var systemType in executionOrder)
        {
            var system = (ISystem?)Activator.CreateInstance(systemType) ?? throw new InvalidOperationException($"Failed to create instance of system type {systemType.Name}.");

            _executionList.Add(system);
            system.OnCreate(in state);
        }
    }

    internal void UpdateSystems()
    {
        var state = new SystemState()
        {
            World = _world,
        };

        foreach (var system in _executionList)
        {
            system.OnUpdate(in state);
        }
    }

    internal void Dispose()
    {
        var state = new SystemState()
        {
            World = _world,
        };

        foreach (var system in _executionList)
        {
            system.OnDestroy(in state);
        }

        _systems.Clear();
        _executionList.Clear();
    }
}