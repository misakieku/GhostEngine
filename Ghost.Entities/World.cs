using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public partial class World
{
    private static List<World?> s_worlds = new(4);
    private static Queue<Identifier<World>> s_freeWorldSlots = new();

    internal static Identifier<Archetype> EmptyArchetypeID => new Identifier<Archetype>(0);

    public static int WorldCount => s_worlds.Count - s_freeWorldSlots.Count;

    public static World Create(int entityCapacity = 16)
    {
        lock (s_worlds)
        {
            if (s_freeWorldSlots.TryDequeue(out var index))
            {
                s_worlds[index.value] = new World(index, entityCapacity);
            }
            else
            {
                index = new Identifier<World>(s_worlds.Count);
                s_worlds.Add(new World(index, entityCapacity));
            }

            return s_worlds[index.value]!;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result<World, ResultStatus> GetWorld(Identifier<World> id)
    {
        if (id.value < 0 || id.value >= s_worlds.Count)
        {
            return Result.Create(default(World)!, ResultStatus.NotFound);
        }

        var world = s_worlds[id.value];
        if (world is null)
        {
            return Result.Create(default(World)!, ResultStatus.NotFound);
        }

        return Result.Create(world, ResultStatus.Success);
    }
}

public partial class World : IIdentifierType, IDisposable, IEquatable<World>
{
    private readonly Identifier<World> _id;

    private UnsafeList<Archetype> _archetypes;
    private UnsafeHashMap<int, Identifier<Archetype>> _archetypeLookup; // Signature Hash to Archetype ID
    private EntityManager _entityManager;
    private EntityCommandBuffer _entityCommandBuffer;

    private bool _disposed = false;

    public Identifier<World> ID => _id;
    public EntityManager EntityManager => _entityManager;
    public EntityCommandBuffer EntityCommandBuffer => _entityCommandBuffer;

    private World(Identifier<World> id, int entityCapacity)
    {
        _id = id;

        _archetypes = new UnsafeList<Archetype>(16, Allocator.Persistent);
        _archetypeLookup = new UnsafeHashMap<int, Identifier<Archetype>>(16, Allocator.Persistent);

        _entityManager = new EntityManager(this, entityCapacity);
        _entityCommandBuffer = new EntityCommandBuffer(_entityManager);

        // Create the empty archetype
        CreateArchetype(ReadOnlySpan<Identifier<IComponent>>.Empty, 0);
    }

    ~World()
    {
        Dispose();
    }

    internal Identifier<Archetype> CreateArchetype(ReadOnlySpan<Identifier<IComponent>> componentTypeIDs, int signatureHash)
    {
        var arcID = new Identifier<Archetype>(_archetypes.Count);
        _archetypes.Add(new Archetype(arcID, _id, componentTypeIDs));
        _archetypeLookup.Add(signatureHash, arcID);

        return arcID;
    }

    internal ref Archetype GetArchetypeReference(Identifier<Archetype> id)
    {
        return ref _archetypes[id.value];
    }

    internal Identifier<Archetype> GetArchetypeIDBySignatureHash(int signatureHash)
    {
        if (_archetypeLookup.TryGetValue(signatureHash, out var arcID))
        {
            return arcID;
        }

        return Identifier<Archetype>.Invalid;
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

        _archetypes.Dispose();
        _archetypeLookup.Dispose();

        s_freeWorldSlots.Enqueue(_id);

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}

