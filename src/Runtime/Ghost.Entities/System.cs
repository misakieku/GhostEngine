using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities;

public readonly ref struct SystemAPI
{
    /// <summary>
    /// Gets the tIme data of current frame.
    /// </summary>
    /// <remarks>
    /// The data will always be <see langword="default"/> in <see cref="ISystem.Initialize(ref readonly SystemAPI)"/> and <see cref="ISystem.Cleanup(ref readonly SystemAPI)"/>
    /// </remarks>
    public TimeData Time
    {
        get; init;
    }

    /// <summary>
    /// Gets the world current system is running on.
    /// </summary>
    public World World
    {
        get; init;
    }
}

public interface ISystem
{
    void Initialize(ref readonly SystemAPI systemAPI);
    void Update(ref readonly SystemAPI systemAPI);
    void Cleanup(ref readonly SystemAPI systemAPI);
}

public abstract class SystemBase : ISystem
{
    private UnsafeList<int> _requiredQueries;

    /// <summary>
    /// Gets the world that the system is running on currently.
    /// </summary>
    public World World
    {
        get; init;
    } = null!;

    /// <summary>
    /// Gets the last version that the system update.
    /// </summary>
    public uint LastSystemVersion
    {
        get; internal set;
    } = uint.MaxValue - 1;

    private bool ShouldUpdate()
    {
        if (!_requiredQueries.IsCreated || _requiredQueries.Count == 0)
        {
            return true;
        }

        for  (var i = 0; i < _requiredQueries.Count; i++)
        {
            var queryID = _requiredQueries[i];
            ref var query = ref World.ComponentManager.GetEntityQueryReference(new Identifier<EntityQuery>(queryID));
            if (!query.HasMatchingEntity())
            {
                return false;
            }
        }

        return true;
    }

    protected void RequireQueryForUpdate(Identifier<EntityQuery> queryID)
    {
        if (!_requiredQueries.IsCreated)
        {
            _requiredQueries = new UnsafeList<int>(4, Misaki.HighPerformance.LowLevel.Buffer.AllocationHandle.Persistent);
        }

        _requiredQueries.Add(queryID.Value);
    }

    void ISystem.Initialize(ref readonly SystemAPI systemAPI)
    {
        OnInitialize(in systemAPI);
    }

    void ISystem.Update(ref readonly SystemAPI systemAPI)
    {
        if (ShouldUpdate())
        {
            if (World.Version - LastSystemVersion > 1)
            {
                OnStartRunning();
            }

            OnUpdate(in systemAPI);
            LastSystemVersion = World.Version;
        }
        else
        {
            if (World.Version - LastSystemVersion <= 1)
            {
                OnStopRunning();
            }
        }
    }

    void ISystem.Cleanup(ref readonly SystemAPI systemAPI)
    {
        _requiredQueries.Dispose();
        OnCleanup(in systemAPI);
    }

    protected virtual void OnInitialize(ref readonly SystemAPI systemAPI)
    {
    }

    protected virtual void OnUpdate(ref readonly SystemAPI systemAPI)
    {
    }

    protected virtual void OnCleanup(ref readonly SystemAPI systemAPI)
    {
    }

    protected virtual void OnStopRunning()
    {
    }

    protected virtual void OnStartRunning()
    {
    }
}

public abstract class UpdateAfterAttribute : Attribute
{
    public abstract Type SystemType { get; }
}

public abstract class UpdateBeforeAttribute : Attribute
{
    public abstract Type SystemType { get; }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class UpdateAfterAttribute<T> : UpdateAfterAttribute
{
    public override Type SystemType => typeof(T);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class UpdateBeforeAttribute<T> : UpdateBeforeAttribute
{
    public override Type SystemType => typeof(T);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public class SystemGroupAttribute : Attribute
{
    public Type GroupType { get; }

    public SystemGroupAttribute(Type groupType)
    {
        GroupType = groupType;
    }
}

#if false
internal static partial class SystemGroupRegistry
{
    private static readonly Dictionary<Type, List<ISystem>> _systemGroupMap = new();

    // TODO: Use Source Generators to generate group registrations at compile time.

    public static void RegisterSystemGroup<T>(Type groupType)
        where T : ISystem, new()
    {
        if (!_systemGroupMap.ContainsKey(typeof(T)))
        {
            _systemGroupMap[typeof(T)] = new();
        }

        _systemGroupMap[groupType].Add(new T());
    }

    public static List<ISystem> GetSystemsForGroup(Type groupType)
    {
        if (_systemGroupMap.TryGetValue(groupType, out var systems))
        {
            return systems;
        }

        throw new InvalidOperationException($"No systems registered for System Group of type {groupType.FullName}");
    }
}
#endif

public abstract class SystemGroup : ISystem
{
    private readonly List<ISystem> _systems = [];
    private List<ISystem>? _sortedSystems;

    private uint _version = 0;
    private uint _sortedVersion = 0;

    public World World
    {
        get; init;
    } = null!;

    // public SystemGroup()
    // {
    //     _systems = SystemGroupRegistry.GetSystemsForGroup(GetType());
    // }

    // TODO: Use Source Generators to generate group registrations at compile time, and remove the need for this public constructor.
    private static List<ISystem> Sort(List<ISystem> systems)
    {
        // 1. Build the Graph
        // Key64: The System, Value: Systems that MUST run before the Key64
        var dependencies = new Dictionary<Type, HashSet<Type>>();
        var systemMap = systems.ToDictionary(s => s.GetType(), s => s);

        foreach (var sys in systems)
        {
            var type = sys.GetType();
            ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(dependencies, type, out var exists);
            if (!exists || value == null)
            {
                value = new HashSet<Type>();
            }

            // Handle [UpdateAfter(typeof(Other))] -> Other comes before This
            foreach (var attr in type.GetCustomAttributes(typeof(UpdateAfterAttribute), true))
            {
                var depType = ((UpdateAfterAttribute)attr).SystemType;
                value.Add(depType);
            }

            // Handle [UpdateBefore(typeof(Other))] -> This comes before Other
            // Which means: Other depends on This
            foreach (var attr in type.GetCustomAttributes(typeof(UpdateBeforeAttribute), true))
            {
                var targetType = ((UpdateBeforeAttribute)attr).SystemType;
                ref var targetDeps = ref CollectionsMarshal.GetValueRefOrAddDefault(dependencies, targetType, out exists);
                if (!exists || targetDeps == null)
                {
                    targetDeps = new HashSet<Type>();
                }

                targetDeps.Add(type);
            }
        }

        // 2. Topological Sort (Kahn's Algorithm variant)
        var sortedList = new List<ISystem>();
        var visited = new HashSet<Type>();

        // We loop until we have sorted everyone
        while (sortedList.Count < systems.Count)
        {
            var addedAny = false;

            foreach (var sys in systems)
            {
                var type = sys.GetType();
                if (visited.Contains(type))
                {
                    continue;
                }

                // Check if all dependencies for this system are already visited/sorted
                var canRun = true;
                if (dependencies.TryGetValue(type, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        // If the dependency exists in our list but hasn't run yet, we can't run.
                        // (We check systemMap to ignore dependencies that don't exist in this world)
                        if (systemMap.ContainsKey(dep) && !visited.Contains(dep))
                        {
                            canRun = false;
                            break;
                        }
                    }
                }

                if (canRun)
                {
                    sortedList.Add(sys);
                    visited.Add(type);
                    addedAny = true;
                }
            }

            if (!addedAny)
            {
                throw new InvalidOperationException("Circular Dependency detected in Systems! Check your [UpdateAfter] attributes.");
            }
        }

        return sortedList;
    }

    public void AddSystem<T>()
        where T : ISystem, new()
    {
        _systems.Add(new T());
        _version++;
    }

    public void SortSystems()
    {
        if (_sortedVersion == _version)
        {
            return;
        }

        if (_systems.Count == 0)
        {
            _sortedSystems = new List<ISystem>();
            _sortedVersion = _version;
            return;
        }

        _sortedSystems = Sort(_systems);
        _sortedVersion = _version;
    }

    private void ThrowIfNotSorted()
    {
        if (_sortedSystems == null || _sortedVersion != _version)
        {
            throw new InvalidOperationException("Systems must be sorted before calling this method. Call SortSystems() after adding all systems.");
        }
    }

    public void Initialize(ref readonly SystemAPI systemAPI)
    {
        if (_systems.Count == 0)
        {
            return;
        }

        ThrowIfNotSorted();

        foreach (var system in _sortedSystems!)
        {
            system.Initialize(in systemAPI);
        }
    }

    public void Update(ref readonly SystemAPI systemAPI)
    {
        if (_systems.Count == 0)
        {
            return;
        }

        ThrowIfNotSorted();

        foreach (var system in _sortedSystems!)
        {
            system.Update(in systemAPI);
        }
    }

    public void Cleanup(ref readonly SystemAPI systemAPI)
    {
        if (_systems.Count == 0)
        {
            return;
        }

        ThrowIfNotSorted();

        foreach (var system in _sortedSystems!)
        {
            system.Cleanup(in systemAPI);
        }
    }
}

public sealed class DefaultSystemGroup : SystemGroup;

public sealed class SystemManager : IDisposable
{
    private readonly World _world;

    private readonly List<ISystem> _systems = [];

    internal IReadOnlyList<ISystem> Systems => _systems;

    internal SystemManager(World world)
    {
        _world = world;
        AddSystem<DefaultSystemGroup>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSystem<T>()
        where T : ISystem, new()
    {
        _systems.Add(new T());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetSystem<T>()
        where T : ISystem
    {
        foreach (var system in _systems)
        {
            if (system is T typedSystem)
            {
                return typedSystem;
            }
        }

        throw new InvalidOperationException($"System of type {typeof(T).FullName} not found in SystemManager.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void InitializeAll()
    {
        if (_systems.Count == 0)
        {
            return;
        }

        var systemAPI = new SystemAPI
        {
            World = _world
        };

        foreach (var system in _systems)
        {
            system.Initialize(in systemAPI);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateAll(TimeData timeData)
    {
        if (_systems.Count == 0)
        {
            return;
        }

        var systemAPI = new SystemAPI
        {
            Time = timeData,
            World = _world
        };

        foreach (var system in _systems)
        {
            system.Update(in systemAPI);
        }
    }

    internal void CleanupAll()
    {
        if (_systems.Count == 0)
        {
            return;
        }

        var systemAPI = new SystemAPI
        {
            World = _world
        };

        foreach (var system in _systems)
        {
            system.Cleanup(in systemAPI);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Clear()
    {
        _systems.Clear();
    }

    public void Dispose()
    {
        CleanupAll();
    }
}
