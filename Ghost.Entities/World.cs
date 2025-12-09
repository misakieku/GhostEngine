using Ghost.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public partial class World
{
    private static readonly List<World?> s_worlds = new(4);
    private static readonly Queue<Identifier<World>> s_freeWorldSlots = new();

    internal static Identifier<Archetype> EmptyArchetypeID => new (0);

    public static int WorldCount => s_worlds.Count - s_freeWorldSlots.Count;

    public static World Create(JobScheduler jobScheduler, int entityCapacity = 16)
    {
        lock (s_worlds)
        {
            if (s_freeWorldSlots.TryDequeue(out var index))
            {
                s_worlds[index.value] = new World(index, entityCapacity, jobScheduler);
            }
            else
            {
                index = new Identifier<World>(s_worlds.Count);
                s_worlds.Add(new World(index, entityCapacity, jobScheduler));
            }

            return s_worlds[index.value]!;
        }
    }

    public static void Destroy(Identifier<World> id)
    {
        lock (s_worlds)
        {
            if (id.value < 0 || id.value >= s_worlds.Count)
            {
                return;
            }

            var world = s_worlds[id.value];
            world?.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<World, ErrorStatus> GetWorld(Identifier<World> id)
    {
        if (id.value < 0 || id.value >= s_worlds.Count)
        {
            return ErrorStatus.InvalidArgument;
        }

        var world = s_worlds[id.value];
        if (world is null)
        {
            return ErrorStatus.NotFound;
        }

        return world;
    }
}

public partial class World : IIdentifierType, IDisposable, IEquatable<World>
{
    private readonly Identifier<World> _id;
    private readonly JobScheduler _jobScheduler;

    private readonly EntityManager _entityManager;
    private readonly EntityCommandBuffer _entityCommandBuffer;
    private readonly EntityCommandBuffer[] _threadLocalECBs;

    private UnsafeList<Archetype> _archetypes;
    private UnsafeList<EntityQuery> _entityQueries;

    private UnsafeHashMap<int, Identifier<Archetype>> _archetypeLookup; // Signature Hash to Archetype ID
    private UnsafeHashMap<int, Identifier<EntityQuery>> _querieLookup; // Query Mask Hash to Query ID

    private bool _disposed = false;

    internal int ArchetypeCount => _archetypes.Count;

    /// <summary>
    /// Gets the unique identifier of this world.
    /// </summary>
    public Identifier<World> ID => _id;

    /// <summary>
    /// Gets the job scheduler associated with this world.
    /// </summary>
    public JobScheduler JobScheduler => _jobScheduler;

    /// <summary>
    /// Gets the publicntity manager for this world.
    /// </summary>
    public EntityManager EntityManager => _entityManager;

    /// <summary>
    /// Gets the main entity command buffer for this world.
    /// </summary>
    /// <remarks>
    /// Use <see cref="GetThreadLocalEntityCommandBuffer(int)"/> to get thread-local command buffers for multi-threaded jobs.
    /// </remarks>
    public EntityCommandBuffer EntityCommandBuffer => _entityCommandBuffer;

    private World(Identifier<World> id, int entityCapacity, JobScheduler jobScheduler)
    {
        _id = id;
        _jobScheduler = jobScheduler;

        _archetypes = new UnsafeList<Archetype>(16, Allocator.Persistent);
        _entityQueries = new UnsafeList<EntityQuery>(16, Allocator.Persistent);

        _archetypeLookup = new UnsafeHashMap<int, Identifier<Archetype>>(16, Allocator.Persistent);
        _querieLookup = new UnsafeHashMap<int, Identifier<EntityQuery>>(16, Allocator.Persistent);

        _entityManager = new EntityManager(this, entityCapacity);
        _entityCommandBuffer = new EntityCommandBuffer(_entityManager);
        _threadLocalECBs = new EntityCommandBuffer[jobScheduler.WorkerCount];

        for (var i = 0; i < jobScheduler.WorkerCount; i++)
        {
            _threadLocalECBs[i] = new EntityCommandBuffer(_entityManager);
        }

        // Create the empty archetype
        CreateArchetype(ReadOnlySpan<Identifier<IComponent>>.Empty, 0);
    }

    ~World()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Identifier<Archetype> CreateArchetype(ReadOnlySpan<Identifier<IComponent>> componentTypeIDs, int signatureHash)
    {
        var arcID = new Identifier<Archetype>(_archetypes.Count);
        _archetypes.Add(new Archetype(arcID, _id, componentTypeIDs));
        _archetypeLookup.Add(signatureHash, arcID);

        for (int i = 0; i < _entityQueries.Count; i++)
        {
            ref var query = ref _entityQueries[i];
            query.AddArchetypeIfMatch(in _archetypes[arcID.value]);
        }

        return arcID;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Identifier<Archetype> GetArchetypeIDBySignatureHash(int signatureHash)
    {
        if (_archetypeLookup.TryGetValue(signatureHash, out var arcID))
        {
            return arcID;
        }

        return Identifier<Archetype>.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref Archetype GetArchetypeReference(Identifier<Archetype> id)
    {
        return ref _archetypes[id.value];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Identifier<EntityQuery> CreateEntityQuery(EntityQueryMask mask, int maskHash)
    {
        var queryID = new Identifier<EntityQuery>(_entityQueries.Count);
        _entityQueries.Add(new EntityQuery(queryID, _id, mask));
        _querieLookup.Add(maskHash, queryID);

        ref var query = ref _entityQueries[queryID.value];
        for (var i = 0; i < _archetypes.Count; i++)
        {
            query.AddArchetypeIfMatch(in _archetypes[i]);
        }

        return queryID;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Identifier<EntityQuery> GetEntityQueryIDByMaskHash(int maskHash)
    {
        if (_querieLookup.TryGetValue(maskHash, out var queryID))
        {
            return queryID;
        }

        return Identifier<EntityQuery>.Invalid;
    }

    internal void PlaybackEntityCommandBuffers()
    {
        _entityCommandBuffer.Playback();

        for (var i = 0; i < _threadLocalECBs.Length; i++)
        {
            _threadLocalECBs[i].Playback();
        }
    }

    /// <summary>
    /// Gets a reference to the entity query with the specified identifier.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref EntityQuery GetEntityQueryReference(Identifier<EntityQuery> id)
    {
        return ref _entityQueries[id.value];
    }

    /// <summary>
    /// Gets the thread-local entity command buffer for the specified thread index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityCommandBuffer GetThreadLocalEntityCommandBuffer(int threadIndex)
    {
        return _threadLocalECBs[threadIndex];
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

        foreach (var archetype in _archetypes)
        {
            archetype.Dispose();
        }

        foreach (var query in _entityQueries)
        {
            query.Dispose();
        }

        _entityManager.Dispose();
        _entityCommandBuffer.Dispose();
        for (var i = 0; i < _threadLocalECBs.Length; i++)
        {
            _threadLocalECBs[i].Dispose();
        }

        _archetypes.Dispose();
        _entityQueries.Dispose();
        _archetypeLookup.Dispose();
        _querieLookup.Dispose();

        s_freeWorldSlots.Enqueue(_id);
        s_worlds[_id] = null;

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

