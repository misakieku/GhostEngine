using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Entities;

public struct EntityQueryMask : IDisposable
{
    public UnsafeBitSet all;
    public UnsafeBitSet any;
    public UnsafeBitSet absent;

    public bool Matches(UnsafeBitSet archetypeSignature)
    {
        // 1. Check All: Archetype must have ALL bits set
        if (all.IsCreated && !archetypeSignature.All(all))
        {
            return false;
        }

        // 2. Check None: Archetype must have NONE of these bits set
        if (absent.IsCreated && archetypeSignature.Any(absent))
        {
            return false;
        }

        // 3. Check Any: Archetype must have AT LEAST ONE of these bits (if Any is defined)
        if (any.IsCreated && any.Count != 0 && !archetypeSignature.Any(any))
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hash = 17;
        if (all.IsCreated) hash = hash * 23 + all.GetHashCode();
        if (absent.IsCreated) hash = hash * 23 + absent.GetHashCode();
        if (any.IsCreated) hash = hash * 23 + any.GetHashCode();

        return hash;
    }

    public void Dispose()
    {
        all.Dispose();
        any.Dispose();
        absent.Dispose();
    }
}

public unsafe struct EntityQuery : IIdentifierType, IDisposable
{
    public readonly ref struct QueryItem
    {
        private readonly ref Archetype _archetype;
        private readonly ref Chunk _chunk;

        public readonly int Count => _chunk.Count;

        internal QueryItem(ref Archetype archetype, int chunkIndex)
        {
            _archetype = ref archetype;
            _chunk = ref archetype.GetChunkReference(chunkIndex);
        }

        public readonly Span<T> GetComponentData<T>()
            where T : unmanaged, IComponent
        {
            var offset = _archetype.GetOffset(ComponentTypeID<T>.value);
            if (offset < 0)
            {
                throw new InvalidOperationException($"Archetype does not contain component of type {typeof(T)}");
            }

            var ptr = (byte*)_chunk.GetUnsafePtr() + offset;
            return new Span<T>(ptr, _chunk.Count);
        }
    }

    public ref struct ChunkEnumerator
    {
        private ReadOnlyUnsafeCollection<Identifier<Archetype>> _matchingArchetypes;
        private World _world;
        private int _archetypeIndex;
        private int _chunkIndex;

        internal ChunkEnumerator(ReadOnlyUnsafeCollection<Identifier<Archetype>> matchingArchetypes, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _world = world;
            _archetypeIndex = 0;
            _chunkIndex = -1;
        }

        public QueryItem Current
        {
            get
            {
                ref var archetype = ref _world.GetArchetypeReference(_matchingArchetypes[_archetypeIndex]);
                return new QueryItem(ref archetype, _chunkIndex);
            }
        }

        public bool MoveNext()
        {
            _chunkIndex++;

            while (_archetypeIndex < _matchingArchetypes.Count)
            {
                ref var archetype = ref _world.GetArchetypeReference(_matchingArchetypes[_archetypeIndex]);
                if (_chunkIndex < archetype.ChunkCount)
                {
                    return true;
                }

                _chunkIndex = 0;
                _archetypeIndex++;
            }

            return false;
        }

        public void Reset()
        {
            _archetypeIndex = 0;
            _chunkIndex = -1;
        }

        public void Dispose()
        {
        }
    }

    private Identifier<World> _worldID;
    private EntityQueryMask _mask;
    private UnsafeList<Identifier<Archetype>> _matchingArchetypes;

    internal EntityQuery(Identifier<World> worldID, EntityQueryMask mask)
    {
        _worldID = worldID;
        _mask = mask;
        _matchingArchetypes = new UnsafeList<Identifier<Archetype>>(8, Allocator.Persistent);
    }

    // Called by World when a new archetype is created
    internal void AddArchetypeIfMatch(Archetype archetype)
    {
        if (_mask.Matches(archetype._signature))
        {
            _matchingArchetypes.Add(archetype.ID);
        }
    }

    public ChunkEnumerator GetEnumerator()
    {
        var world = World.GetWorld(_worldID).Value;
        return new ChunkEnumerator(_matchingArchetypes.AsReadOnly(), world);
    }

    public void Dispose()
    {
        _mask.Dispose();
        _matchingArchetypes.Dispose();
    }
}

public ref struct QueryBuilder
{
    private Stack.Scope _scope;

    private UnsafeList<Identifier<IComponent>> _all;
    private UnsafeList<Identifier<IComponent>> _any;
    private UnsafeList<Identifier<IComponent>> _absent;

    public QueryBuilder()
    {
        _scope = AllocationManager.CreateStackScope();

        _all = new UnsafeList<Identifier<IComponent>>(4, Allocator.Stack);
        _any = new UnsafeList<Identifier<IComponent>>(4, Allocator.Stack);
        _absent = new UnsafeList<Identifier<IComponent>>(4, Allocator.Stack);
    }

    public QueryBuilder WithAll<T>()
        where T : unmanaged, IComponent
    {
        _all.Add(ComponentTypeID<T>.value);
        return this;
    }

    public QueryBuilder WithAny<T>()
        where T : unmanaged, IComponent
    {
        _any.Add(ComponentTypeID<T>.value);
        return this;
    }

    public QueryBuilder WithNone<T>()
        where T : unmanaged, IComponent
    {
        _absent.Add(ComponentTypeID<T>.value);
        return this;
    }

    public Identifier<EntityQuery> Build(World world)
    {
        // 1. Calculate max component ID to size the BitSets
        int maxID = 0;
        FindMax(_all, ref maxID);
        FindMax(_any, ref maxID);
        FindMax(_absent, ref maxID);

        // 2. Create the Mask
        using var mask = new EntityQueryMask
        {
            all = new UnsafeBitSet(maxID + 1, Allocator.Stack),
            any = new UnsafeBitSet(maxID + 1, Allocator.Stack),
            absent = new UnsafeBitSet(maxID + 1, Allocator.Stack)
        };

        // 3. Fill BitSets
        foreach (var id in _all) mask.all.SetBit(id);
        foreach (var id in _any) mask.any.SetBit(id);
        foreach (var id in _absent) mask.absent.SetBit(id);

        // 4. Ask World for the Query (Cached)
        var queryID = world.GetEntityQueryIDByMaskHash(mask.GetHashCode());
        if (queryID.IsNotValid)
        {
            queryID = world.CreateEntityQuery(mask);
            ref var query = ref world.GetEntityQueryReference(queryID);
            for (var i = 0; i < world.ArchetypeCount; i++)
            {
                ref var archetype = ref world.GetArchetypeReference(i);
                query.AddArchetypeIfMatch(archetype);
            }
        }

        Dispose();

        return queryID;
    }

    private void FindMax(UnsafeList<Identifier<IComponent>> list, ref int max)
    {
        foreach (var id in list)
        {
            if (id.value > max) max = id.value;
        }
    }

    private void Dispose()
    {
        _all.Dispose();
        _any.Dispose();
        _absent.Dispose();

        _scope.Dispose();
    }
}
