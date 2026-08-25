using Ghost.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public partial class World
{
    public struct Enumerator
    {
        private int _index;

        public Enumerator()
        {
            _index = -1;
        }

        public readonly World Current => s_worlds[_index]!;

        public readonly Enumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            while (++_index < s_worlds.Count)
            {
                if (s_worlds[_index] is not null)
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            _index = -1;
        }
    }

    private static readonly List<World?> s_worlds = new(4);
    private static readonly Queue<Identifier<World>> s_freeWorldSlots = new();

    internal static Identifier<Archetype> EmptyArchetypeID => new(0);

    public static int WorldCount => s_worlds.Count - s_freeWorldSlots.Count;

    public static World Create(JobScheduler? jobScheduler = null, int entityCapacity = 16)
    {
        if (s_freeWorldSlots.TryDequeue(out var index))
        {
            s_worlds[index.Value] = new World(index, entityCapacity, jobScheduler);
        }
        else
        {
            index = new Identifier<World>(s_worlds.Count);
            s_worlds.Add(new World(index, entityCapacity, jobScheduler));
        }

        return s_worlds[index.Value]!;
    }

    public static void Destroy(Identifier<World> id)
    {
        if (id.Value < 0 || id.Value >= s_worlds.Count)
        {
            return;
        }

        var world = s_worlds[id.Value];
        world?.Dispose();
    }

    public static void Destroy(World world)
    {
        world.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static World GetWorldUncheck(Identifier<World> id)
    {
#if GHOST_SAFETY_CHECKS
        if (id.Value < 0 || id.Value >= s_worlds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "World ID is out of range.");
        }

        var world = s_worlds[id.Value];
        return world is null ? throw new InvalidOperationException("World not found.") : world;
#else
        return s_worlds[id.Value]!;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static World? GetWorld(Identifier<World> id)
    {
        if (id.Value < 0 || id.Value >= s_worlds.Count)
        {
            return null;
        }

        return s_worlds[id.Value];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Enumerator GetWorldEnumerator()
    {
        return new Enumerator();
    }
}

public partial class World : IDisposable, IEquatable<World>
{
    private readonly Identifier<World> _id;
    private readonly JobScheduler? _jobScheduler;

    private readonly EntityManager _entityManager;
    private readonly EntityCommandBuffer _entityCommandBuffer;
    private readonly UnsafeArray<EntityCommandBuffer> _threadLocalECBs;

    private readonly ComponentManager _componentManager;
    private readonly SystemManager _systemManager;

    private readonly Dictionary<Type, object> _services;

    private uint _version;
    private bool _disposed = false;

    /// <summary>
    /// Gets the unique identifier of this world.
    /// </summary>
    public Identifier<World> ID => _id;

    /// <summary>
    /// Gets the job scheduler associated with this world.
    /// </summary>
    public JobScheduler? JobScheduler => _jobScheduler;

    /// <summary>
    /// Gets the publicntity manager for this world.
    /// </summary>
    public EntityManager EntityManager => _entityManager;

    /// <summary>
    /// Gets the component manager for this world.
    /// </summary>
    public ComponentManager ComponentManager => _componentManager;

    /// <summary>
    /// Gets the system manager for this world.
    /// </summary>
    public SystemManager SystemManager => _systemManager;

    /// <summary>
    /// Gets the current version number of the world.
    /// </summary>
    public uint Version => Volatile.Read(ref _version);

    /// <summary>
    /// Gets the main entity command buffer for this world.
    /// </summary>
    /// <remarks>
    /// Use <see cref="GetThreadLocalEntityCommandBuffer(int)"/> to get thread-local command buffers for multi-threaded jobs.
    /// </remarks>
    public EntityCommandBuffer EntityCommandBuffer => _entityCommandBuffer;

    private World(Identifier<World> id, int entityCapacity, JobScheduler? jobScheduler)
    {
        _id = id;
        _jobScheduler = jobScheduler;

        _entityManager = new EntityManager(this, entityCapacity);
        _entityCommandBuffer = new EntityCommandBuffer(4096, AllocationHandle.Persistent);

        _componentManager = new ComponentManager(this);
        _systemManager = new SystemManager(this);

        _services = new Dictionary<Type, object>();

        if (jobScheduler != null)
        {
            _threadLocalECBs = new UnsafeArray<EntityCommandBuffer>(jobScheduler.ThreadLocalCount, AllocationHandle.Persistent);
            for (var i = 0; i < _threadLocalECBs.Length; i++)
            {
                _threadLocalECBs[i] = new EntityCommandBuffer(4096, AllocationHandle.Persistent);
            }
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void PlaybackEntityCommandBuffers()
    {
        _entityCommandBuffer.Playback(_entityManager);
        _entityCommandBuffer.Reset();

        for (var i = 0; i < _threadLocalECBs.Length; i++)
        {
            _threadLocalECBs[i].Playback(_entityManager);
            _threadLocalECBs[i].Reset();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal uint AdvanceVersion()
    {
        return _version++;
    }

    /// <summary>
    /// Gets the thread-local entity command buffer for the specified thread index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCommandBuffer GetThreadLocalEntityCommandBuffer(int threadIndex)
    {
        if (!_threadLocalECBs.IsCreated || _threadLocalECBs.Length == 0)
        {
            throw new InvalidOperationException("This world does not have a JobScheduler associated with it.");
        }

        return _threadLocalECBs[threadIndex];
    }

    /// <summary>
    /// Registers or overwrites a global resource in the world.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddService<T>(T resource)
        where T : class
    {
        _services[typeof(T)] = resource;
    }

    /// <summary>
    /// Retrieves a global resource from the world.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetService<T>()
        where T : class
    {
        if (_services.TryGetValue(typeof(T), out var resource))
        {
            return (T)resource;
        }

        throw new InvalidOperationException($"Resource of type {typeof(T).FullName} has not been registered in the World.");
    }

    public bool TryGetService<T>([MaybeNullWhen(false)] out T resource)
        where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj))
        {
            resource = (T)obj;
            return true;
        }

        resource = null;
        return false;
    }

    /// <summary>
    /// Checks if a global resource exists.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasService<T>()
        where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _entityManager.Clear();
        _entityCommandBuffer.Reset();

        for (var i = 0; i < _threadLocalECBs.Length; i++)
        {
            _threadLocalECBs[i].Reset();
        }

        _componentManager.Clear();

        _systemManager.CleanupAll();
        _systemManager.InitializeAll();
    }

    public bool Equals(World? other)
    {
        return other is not null && _id == other._id;
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is World other && Equals(other);
    }

    public static bool operator ==(World? left, World? right)
    {
        return left?.Equals(right) ?? right is null;
    }

    public static bool operator !=(World? left, World? right)
    {
        return !(left == right);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _entityManager.Dispose();
        _entityCommandBuffer.Dispose();

        if (_threadLocalECBs.IsCreated)
        {
            for (var i = 0; i < _threadLocalECBs.Length; i++)
            {
                _threadLocalECBs[i].Dispose();
            }

            _threadLocalECBs.Dispose();
        }

        _componentManager.Dispose();
        _systemManager.Dispose();

        s_worlds[_id] = null;
        s_freeWorldSlots.Enqueue(_id);

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

