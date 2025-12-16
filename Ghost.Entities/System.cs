namespace Ghost.Entities;

public readonly ref struct SystemAPI
{
    public World World
    {
        get; init;
    }

    public Time Time
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

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class UpdateAfterAttribute : Attribute
{
    public Type SystemType { get; }

    public UpdateAfterAttribute(Type systemType)
    {
        SystemType = systemType;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class UpdateBeforeAttribute : Attribute
{
    public Type SystemType { get; }

    public UpdateBeforeAttribute(Type systemType)
    {
        SystemType = systemType;
    }
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
    private readonly List<ISystem> _systems = new ();
    private List<ISystem>? _sortedSystems;

    private uint _version = 0;
    private uint _sortedVersion = 0;

    // public SystemGroup()
    // {
    //     _systems = SystemGroupRegistry.GetSystemsForGroup(GetType());
    // }

    private static List<ISystem> Sort(List<ISystem> systems)
    {
        // 1. Build the Graph
        // Key: The System, Value: Systems that MUST run before the Key
        var dependencies = new Dictionary<Type, HashSet<Type>>();
        var systemMap = systems.ToDictionary(s => s.GetType(), s => s);

        foreach (var sys in systems)
        {
            var type = sys.GetType();
            if (!dependencies.ContainsKey(type)) dependencies[type] = new HashSet<Type>();

            // Handle [UpdateAfter(typeof(Other))] -> Other comes before This
            foreach (var attr in type.GetCustomAttributes(typeof(UpdateAfterAttribute), true))
            {
                var depType = ((UpdateAfterAttribute)attr).SystemType;
                dependencies[type].Add(depType);
            }

            // Handle [UpdateBefore(typeof(Other))] -> This comes before Other
            // Which means: Other depends on This
            foreach (var attr in type.GetCustomAttributes(typeof(UpdateBeforeAttribute), true))
            {
                var targetType = ((UpdateBeforeAttribute)attr).SystemType;
                if (!dependencies.ContainsKey(targetType)) dependencies[targetType] = new HashSet<Type>();
                dependencies[targetType].Add(type);
            }
        }

        // 2. Topological Sort (Kahn's Algorithm variant)
        var sortedList = new List<ISystem>();
        var visited = new HashSet<Type>();

        // We loop until we have sorted everyone
        while (sortedList.Count < systems.Count)
        {
            bool addedAny = false;

            foreach (var sys in systems)
            {
                var type = sys.GetType();
                if (visited.Contains(type)) continue;

                // Check if all dependencies for this system are already visited/sorted
                bool canRun = true;
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
        ThrowIfNotSorted();

        foreach (var system in _sortedSystems!)
        {
            system.Initialize(in systemAPI);
        }
    }

    public void Update(ref readonly SystemAPI systemAPI)
    {
        ThrowIfNotSorted();

        foreach (var system in _sortedSystems!)
        {
            system.Update(in systemAPI);
        }
    }

    public void Cleanup(ref readonly SystemAPI systemAPI)
    {
        ThrowIfNotSorted();

        foreach (var system in _sortedSystems!)
        {
            system.Cleanup(in systemAPI);
        }
    }
}

public class DefaultSystemGroup : SystemGroup
{
}

public class SystemManager
{
    private readonly World _world;

    private readonly List<ISystem> _systems = new ();

    internal SystemManager(World world)
    {
        _world = world;
        AddSystem<DefaultSystemGroup>();
    }

    public void AddSystem<T>()
        where T : ISystem, new()
    {
        var system = new T();
        _systems.Add(system);
    }

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

    internal void InitializeAll(ref readonly SystemAPI systemAPI)
    {
        foreach (var system in _systems)
        {
            system.Initialize(in systemAPI);
        }
    }

    internal void UpdateAll(ref readonly SystemAPI systemAPI)
    {
        foreach (var system in _systems)
        {
            system.Update(in systemAPI);
        }
    }

    internal void CleanupAll(ref readonly SystemAPI systemAPI)
    {
        foreach (var system in _systems)
        {
            system.Cleanup(in systemAPI);
        }
    }
}
