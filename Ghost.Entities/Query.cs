using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public struct EntityQueryMask : IDisposable
{
    public UnsafeBitSet all;
    public UnsafeBitSet any;
    public UnsafeBitSet absent;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Matches(UnsafeBitSet archetypeSignature)
    {
        return (!all.IsCreated || all.All(archetypeSignature))
            && (!absent.IsCreated || absent.None(archetypeSignature))
            && (!any.IsCreated || any.Count == 0 || any.Any(archetypeSignature));
    }

    public readonly override int GetHashCode()
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

public unsafe partial struct EntityQuery : IIdentifierType, IDisposable
{
    public readonly ref struct ChunkIterator
    {
        public readonly ref struct ChunkView
        {
            private readonly ref Archetype _archetype;
            private readonly ref Chunk _chunk;

            public readonly int Count => _chunk.Count;

            internal ChunkView(ref Archetype archetype, int chunkIndex)
            {
                _archetype = ref archetype;
                _chunk = ref archetype.GetChunkReference(chunkIndex);
            }

            public readonly ReadOnlySpan<Entity> GetEntities()
            {
                var ptr = _chunk.GetUnsafePtr();
                var pEntity = (Entity*)(ptr + _archetype.EntityIDsOffset);
                return new ReadOnlySpan<Entity>(pEntity, _chunk.Count);
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

        public ref struct Enumerator
        {
            private readonly ReadOnlyUnsafeCollection<Identifier<Archetype>> _matchingArchetypes;
            private readonly World _world;
            private int _archetypeIndex;
            private int _chunkIndex;

            internal Enumerator(ReadOnlyUnsafeCollection<Identifier<Archetype>> matchingArchetypes, World world)
            {
                _matchingArchetypes = matchingArchetypes;
                _world = world;
                _archetypeIndex = 0;
                _chunkIndex = -1;
            }

            public readonly ChunkView Current
            {
                get
                {
                    ref var archetype = ref _world.GetArchetypeReference(_matchingArchetypes[_archetypeIndex]);
                    return new ChunkView(ref archetype, _chunkIndex);
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

            public readonly void Dispose()
            {
            }
        }

        private readonly ReadOnlyUnsafeCollection<Identifier<Archetype>> _matchingArchetypes;
        private readonly World _world;

        internal ChunkIterator(ReadOnlyUnsafeCollection<Identifier<Archetype>> matchingArchetypes, World world)
        {
            _matchingArchetypes = matchingArchetypes;
            _world = world;
        }

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(_matchingArchetypes, _world);
        }
    }

    private readonly Identifier<World> _worldID;
    private EntityQueryMask _mask;
    private UnsafeList<Identifier<Archetype>> _matchingArchetypes;

    internal EntityQuery(Identifier<World> worldID, EntityQueryMask mask)
    {
        _worldID = worldID;
        _mask = mask;
        _matchingArchetypes = new UnsafeList<Identifier<Archetype>>(8, Allocator.Persistent);
    }

    internal void AddArchetypeIfMatch(Archetype archetype)
    {
        if (_mask.Matches(archetype._signature))
        {
            _matchingArchetypes.Add(archetype.ID);
        }
    }

    public readonly ChunkIterator GetChunkIterator()
    {
        var world = World.GetWorld(_worldID).Value;
        return new ChunkIterator(_matchingArchetypes.AsReadOnly(), world);
    }

    public void Dispose()
    {
        _mask.Dispose();
        _matchingArchetypes.Dispose();
    }
}

public ref struct QueryBuilder
{
    private readonly Stack.Scope _scope;

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

    private static void FindMax(UnsafeList<Identifier<IComponent>> list, ref int max)
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
