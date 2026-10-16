using Ghost.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class MainThreadAttribute : Attribute;

public readonly ref struct SystemAPI
{
    /// <summary>
    /// Gets the world current system is running on.
    /// </summary>
    public required World World
    {
        get; init;
    }
}

public readonly ref struct SystemUpdateAPI
{
    /// <summary>
    /// Gets the tIme data of current frame.
    /// </summary>
    public TimeData Time
    {
        get; init;
    }

    /// <summary>
    /// Gets the world current system is running on.
    /// </summary>
    public required World World
    {
        get; init;
    }

    /// <summary>
    /// Gets the entity command buffer for the current world.
    /// </summary>
    public ref EntityCommandBuffer EntityCommandBuffer => ref World.GetThreadLocalEntityCommandBufferRef(ExecutionContext.ThreadIndex);

    /// <summary>
    /// Gets the execution context when the system is running inside a worker job.
    /// </summary>
    /// <remarks>
    /// Only valid during <see cref="ISystem.Update(in SystemAPI)"/>. Will be <see langword="default"/> in <see cref="ISystem.Initialize(in SystemAPI)"/> and <see cref="ISystem.Cleanup(in SystemAPI)"/>.
    /// </remarks>
    public JobExecutionContext ExecutionContext
    {
        get; init;
    }

    /// <summary>
    /// Gets the handle for the currently executing system job, or <see cref="JobHandle.Invalid"/> if executing on the main thread.
    /// </summary>
    public JobHandle SelfJobHandle => ExecutionContext.SelfHandle;
}

internal struct DynamicSystemJob : IJob
{
    public ISystem system;
    public TimeData time;
    public World world;

    public void Execute(ref readonly JobExecutionContext ctx)
    {
        var api = new SystemUpdateAPI
        {
            Time = time,
            World = world,
            ExecutionContext = ctx,
        };

        system.Update(in api);
    }
}

public interface ISystem
{
    void Initialize(scoped in SystemAPI systemAPI);
    void Update(scoped in SystemUpdateAPI systemAPI);
    void Cleanup(scoped in SystemAPI systemAPI);
}

public abstract class SystemBase : ISystem
{
    private UnsafeList<int> _requiredQueries;

    /// <summary>
    /// Gets the last version that the system update.
    /// </summary>
    public uint LastSystemVersion
    {
        get; internal set;
    } = uint.MaxValue - 1;

    protected virtual bool ShouldUpdate(scoped in SystemUpdateAPI systemAPI)
    {
        if (!_requiredQueries.IsCreated || _requiredQueries.Count == 0)
        {
            return true;
        }

        for (var i = 0; i < _requiredQueries.Count; i++)
        {
            var queryID = _requiredQueries[i];
            ref var query = ref systemAPI.World.ComponentManager.GetEntityQueryReference(new Identifier<EntityQuery>(queryID));
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
            _requiredQueries = new UnsafeList<int>(4, AllocationHandle.Persistent);
        }

        _requiredQueries.Add(queryID.Value);
    }

    void ISystem.Initialize(scoped in SystemAPI systemAPI)
    {
        OnInitialize(in systemAPI);
    }

    void ISystem.Update(scoped in SystemUpdateAPI systemAPI)
    {
        if (ShouldUpdate(in systemAPI))
        {
            if (systemAPI.World.Version - LastSystemVersion > 1)
            {
                OnStartRunning();
            }

            OnUpdate(in systemAPI);
            LastSystemVersion = systemAPI.World.Version;
        }
        else
        {
            if (systemAPI.World.Version - LastSystemVersion <= 1)
            {
                OnStopRunning();
            }
        }
    }

    void ISystem.Cleanup(scoped in SystemAPI systemAPI)
    {
        _requiredQueries.Dispose();
        OnCleanup(in systemAPI);
    }

    protected virtual void OnInitialize(scoped in SystemAPI systemAPI)
    {
    }

    protected virtual void OnUpdate(scoped in SystemUpdateAPI systemAPI)
    {
    }

    protected virtual void OnCleanup(scoped in SystemAPI systemAPI)
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

[MainThread]
public abstract class SystemGroup : ISystem
{
    private struct SystemNode
    {
        public ISystem system;
        public bool isMainThread;
        public int[] dependencyIndices;
    }

    private readonly List<ISystem> _systems = new List<ISystem>();
    private List<ISystem>? _sortedSystems;
    private SystemNode[]? _compiledNodes;
    private int[]? _leafNodeIndices;
    private int _maxDependencyCount;

    private uint _version = 0;
    private uint _sortedVersion = 0;

    public World World
    {
        get; init;
    } = null!;

    // TODO: Use Source Generators to generate group registrations and system sorting at compile time.
    // TODO: Also analyze resource read/write dependencies to determine if systems can run in parallel or need barriers.
    private static List<ISystem> Sort(List<ISystem> systems, out Dictionary<Type, HashSet<Type>> outDependencies)
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

        outDependencies = dependencies;
        return sortedList;
    }

    public void AddSystem<T>()
        where T : ISystem, new()
    {
        _systems.Add(new T());
        _version++;
    }

    public void AddSystem<T>(T system)
        where T : ISystem
    {
        _systems.Add(system);
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
            _compiledNodes = Array.Empty<SystemNode>();
            _leafNodeIndices = Array.Empty<int>();
            _maxDependencyCount = 0;
            _sortedVersion = _version;
            return;
        }

        _sortedSystems = Sort(_systems, out var dependencies);

        var nodeCount = _sortedSystems.Count;
        var indexMap = new Dictionary<Type, int>(nodeCount);
        for (var i = 0; i < nodeCount; i++)
        {
            indexMap[_sortedSystems[i].GetType()] = i;
        }

        var compiledNodes = new SystemNode[nodeCount];
        var hasDependents = new bool[nodeCount];
        var maxDepCount = 0;

        for (var i = 0; i < nodeCount; i++)
        {
            var sys = _sortedSystems[i];
            var type = sys.GetType();
            var isMainThread = type.IsDefined(typeof(MainThreadAttribute), true);

            int[] depIndices;
            if (dependencies.TryGetValue(type, out var deps) && deps.Count > 0)
            {
                var depList = new List<int>(deps.Count);
                foreach (var depType in deps)
                {
                    if (indexMap.TryGetValue(depType, out var depIndex))
                    {
                        depList.Add(depIndex);
                        hasDependents[depIndex] = true;
                    }
                }
                depIndices = depList.ToArray();
            }
            else
            {
                depIndices = Array.Empty<int>();
            }

            if (depIndices.Length > maxDepCount)
            {
                maxDepCount = depIndices.Length;
            }

            compiledNodes[i] = new SystemNode
            {
                system = sys,
                isMainThread = isMainThread,
                dependencyIndices = depIndices
            };
        }

        var leafList = new List<int>();
        for (var i = 0; i < nodeCount; i++)
        {
            if (!hasDependents[i])
            {
                leafList.Add(i);
            }
        }

        _compiledNodes = compiledNodes;
        _leafNodeIndices = leafList.ToArray();
        _maxDependencyCount = maxDepCount;
        _sortedVersion = _version;
    }

    private void ThrowIfNotSorted()
    {
        if (_sortedSystems == null || _sortedVersion != _version)
        {
            throw new InvalidOperationException("Systems must be sorted before calling this method. Call SortSystems() after adding all systems.");
        }
    }

    public void Initialize(scoped in SystemAPI systemAPI)
    {
        if (_systems.Count == 0)
        {
            return;
        }

        ThrowIfNotSorted();

        var nodes = _compiledNodes!;
        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i].system.Initialize(in systemAPI);
        }
    }

    public void Update(scoped in SystemUpdateAPI systemAPI)
    {
        if (_systems.Count == 0)
        {
            return;
        }

        ThrowIfNotSorted();

        var nodes = _compiledNodes!;
        var scheduler = systemAPI.World.JobScheduler;

        if (scheduler == null)
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i].system.Update(in systemAPI);
            }
            return;
        }

        var nodeCount = nodes.Length;

        using var scope = AllocationManager.CreateStackScope();
        using var handles = new UnsafeArray<JobHandle>(nodeCount, scope.AllocationHandle);
        using var depHandlesBuffer = _maxDependencyCount == 0
            ? default
            : new UnsafeArray<JobHandle>(_maxDependencyCount, scope.AllocationHandle);

        for (var i = 0; i < nodeCount; i++)
        {
            ref readonly var node = ref nodes[i];
            var deps = node.dependencyIndices;

            var validDepCount = 0;
            for (var d = 0; d < deps.Length; d++)
            {
                var h = handles[deps[d]];
                if (h.IsValid)
                {
                    depHandlesBuffer[validDepCount++] = h;
                }
            }

            var depHandles = depHandlesBuffer.AsSpan().Slice(0, validDepCount);

            if (node.isMainThread)
            {
                if (depHandles.Length > 0)
                {
                    scheduler.WaitAll(depHandles);
                }
                node.system.Update(in systemAPI);
                handles[i] = JobHandle.Invalid;
            }
            else
            {
                var job = new DynamicSystemJob
                {
                    system = node.system,
                    time = systemAPI.Time,
                    world = systemAPI.World,
                };

                handles[i] = scheduler.Schedule(in job, preferLocal: false, JobPriority.Normal, default, depHandles);
            }
        }

        if (_leafNodeIndices != null && _leafNodeIndices.Length > 0)
        {
            Span<JobHandle> leafHandles = stackalloc JobHandle[_leafNodeIndices.Length];
            var validLeafCount = 0;
            for (var k = 0; k < _leafNodeIndices.Length; k++)
            {
                var h = handles[_leafNodeIndices[k]];
                if (h.IsValid)
                {
                    leafHandles[validLeafCount++] = h;
                }
            }

            if (validLeafCount > 0)
            {
                scheduler.WaitAll(true, leafHandles.Slice(0, validLeafCount));
            }
        }
    }

    public void Cleanup(scoped in SystemAPI systemAPI)
    {
        if (_systems.Count == 0)
        {
            return;
        }

        ThrowIfNotSorted();

        var nodes = _compiledNodes!;
        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i].system.Cleanup(in systemAPI);
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
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSystem<T>()
        where T : ISystem, new()
    {
        _systems.Add(new T());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSystem<T>(T system)
        where T : ISystem
    {
        _systems.Add(system);
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

        var systemAPI = new SystemUpdateAPI
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
    }
}
