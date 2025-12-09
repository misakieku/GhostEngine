using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public struct EntityQueryMask : IDisposable, IEquatable<EntityQueryMask>
{
    public UnsafeBitSet structuralAll;
    public UnsafeBitSet structuralAny;
    public UnsafeBitSet structuralAbsent;

    public UnsafeBitSet requireEnabled;
    public UnsafeBitSet requireDisabled;
    public UnsafeBitSet rejectIfEnabled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Matches(ref readonly UnsafeBitSet archetypeSignature)
    {
        return (!structuralAll.IsCreated || structuralAll.All(archetypeSignature))
            && (!structuralAbsent.IsCreated || structuralAbsent.None(archetypeSignature))
            && (!structuralAny.IsCreated || structuralAny.Count == 0 || structuralAny.Any(archetypeSignature));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        var hash = 17;
        if (structuralAll.IsCreated) hash = hash * 23 + structuralAll.GetHashCode();
        if (structuralAbsent.IsCreated) hash = hash * 23 + structuralAbsent.GetHashCode();
        if (structuralAny.IsCreated) hash = hash * 23 + structuralAny.GetHashCode();
        if (requireEnabled.IsCreated) hash = hash * 23 + requireEnabled.GetHashCode();
        if (requireDisabled.IsCreated) hash = hash * 23 + requireDisabled.GetHashCode();
        if (rejectIfEnabled.IsCreated) hash = hash * 23 + rejectIfEnabled.GetHashCode();

        return hash;
    }

    public readonly bool Equals(EntityQueryMask other)
    {
        return structuralAll.Equals(other.structuralAll)
            && structuralAny.Equals(other.structuralAny)
            && structuralAbsent.Equals(other.structuralAbsent)
            && requireEnabled.Equals(other.requireEnabled)
            && requireDisabled.Equals(other.requireDisabled)
            && rejectIfEnabled.Equals(other.rejectIfEnabled);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is EntityQueryMask mask && Equals(mask);
    }

    public static bool operator ==(EntityQueryMask left, EntityQueryMask right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EntityQueryMask left, EntityQueryMask right)
    {
        return !(left == right);
    }

    public void Dispose()
    {
        structuralAll.Dispose();
        structuralAny.Dispose();
        structuralAbsent.Dispose();

        requireEnabled.Dispose();
        requireDisabled.Dispose();
        rejectIfEnabled.Dispose();
    }
}

/// <summary>
/// Provides a read-only view over a chunk of entities and their component data within an archetype.
/// </summary>
/// <remarks>This does not filter disabled/enabled components. You must handle that manually.</remarks>
public readonly unsafe ref struct ChunkView
{
    private readonly ReadOnlyUnsafeCollection<Archetype.ComponentMemoryLayout> _layouts;
    private readonly byte* _pChunkData;
    private readonly int _entityOffset;
    private readonly int _entityCount;

    public readonly int Count => _entityCount;

    internal ChunkView(ReadOnlyUnsafeCollection<Archetype.ComponentMemoryLayout> layouts, byte* pChunkData, int entityOffset, int entityCount)
    {
        _layouts = layouts;
        _pChunkData = pChunkData;
        _entityOffset = entityOffset;
        _entityCount = entityCount;
    }

    internal ChunkView(ref readonly Archetype archetype, ref readonly Chunk chunk)
    {
        _layouts = archetype._layouts.AsReadOnly();
        _pChunkData = chunk.GetUnsafePtr();
        _entityOffset = archetype.EntityIDsOffset;
        _entityCount = chunk.Count;
    }

    /// <summary>
    /// Returns a read-only span containing structuralAll entities stored in the current chunk.
    /// </summary>
    /// <returns>A read-only span of <see cref="Entity"/> values representing the entities in the chunk.</returns>
    public readonly ReadOnlySpan<Entity> GetEntities()
    {
        var pEntity = (Entity*)(_pChunkData + _entityOffset);
        return new ReadOnlySpan<Entity>(pEntity, _entityCount);
    }

    /// <summary>
    /// Gets a span providing direct access to the component data of type T0 for structuralAll entities in the chunk.
    /// </summary>
    /// <typeparam name="T">The type of component to access. Must be an unmanaged type that implements <see cref="Component"/>.</typeparam>
    /// <returns>A span of type <see cref="{T}"/> containing the component data for each entity in the chunk.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified component type is not present in the archetype.</exception>
    public readonly Span<T> GetComponentData<T>()
        where T : unmanaged, IComponent
    {
        var layout = _layouts[ComponentTypeID<T>.value];
        var pComponentData = _pChunkData + layout.offset;
        return new Span<T>(pComponentData, _entityCount);
    }

    /// <summary>
    /// Gets a bit set representing the enabled state of each instance of the specified enableable component
    /// type within the current chunk.
    /// </summary>
    /// <typeparam name="T">The component type for which to retrieve enablement bits. Must be unmanaged and implement <see cref="IEnableableComponent"/>.</typeparam>
    /// <returns>A <see cref="SpanBitSet"/> that provides access to the enablement bits for all instances of the specified component type in the chunk.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified component type does not support enablement.</exception>
    public readonly SpanBitSet GetEnableBits<T>()
        where T : unmanaged, IEnableableComponent
    {
        var layout = _layouts[ComponentTypeID<T>.value];
        if (layout.enableBitsOffset == -1)
        {
            throw new InvalidOperationException($"Component {typeof(T).FullName} is not enableable.");
        }

        var maskBase = _pChunkData + layout.enableBitsOffset;
        return new SpanBitSet(new Span<uint>(maskBase, (_entityCount + 31) / 32));
    }

    /// <summary>
    /// Determines whether the specified component of type <typeparamref name="T"/> at the given index is currently enabled.
    /// </summary>
    /// <typeparam name="T">The type of the component to check. Must be an unmanaged type that implements <see cref="IEnableableComponent"/>.</typeparam>
    /// <param name="index">The zero-based index of the component instance to check within the chunk.</param>
    /// <returns>true if the component at the specified index is enabled; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified component type <typeparamref name="T"/> does not support enable/disable functionality.</exception>
    public readonly bool IsComponentEnabled<T>(int index)
        where T : unmanaged, IEnableableComponent
    {
        var layout = _layouts[ComponentTypeID<T>.value];
        if (layout.enableBitsOffset == -1)
        {
            throw new InvalidOperationException($"Component {typeof(T).FullName} is not enableable.");
        }

        var pMask = _pChunkData + layout.enableBitsOffset;
        return EntityQuery.CheckBit(pMask, index);
    }
}

public unsafe partial struct EntityQuery : IIdentifierType, IDisposable
{
    /// <summary>
    /// Provides an enumerator for iterating over chunks of entities and their component data that match a set of archetypes within a world.
    /// </summary>
    public readonly ref struct ChunkIterator
    {
        public ref struct Enumerator
        {
            private readonly ChunkIterator _iterator;
            private int _archetypeIndex;
            private int _chunkIndex;

            internal Enumerator(ChunkIterator iterator)
            {
                _iterator = iterator;
                _archetypeIndex = 0;
                _chunkIndex = -1;
            }

            public readonly ChunkView Current
            {
                get
                {
                    ref var archetype = ref _iterator._world.GetArchetypeReference(_iterator._matchingArchetypes[_archetypeIndex]);
                    ref var chunk = ref archetype.GetChunkReference(_chunkIndex);
                    return new ChunkView(in archetype, in chunk);
                }
            }

            public bool MoveNext()
            {
                _chunkIndex++;

                while (_archetypeIndex < _iterator._matchingArchetypes.Count)
                {
                    ref var archetype = ref _iterator._world.GetArchetypeReference(_iterator._matchingArchetypes[_archetypeIndex]);
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
            return new Enumerator(this);
        }
    }

    internal EntityQueryMask _mask;

    private UnsafeList<Identifier<Archetype>> _matchingArchetypes;
    private readonly Identifier<EntityQuery> _id;
    private readonly Identifier<World> _worldID;

    internal EntityQuery(Identifier<EntityQuery> id, Identifier<World> worldID, EntityQueryMask mask)
    {
        _id = id;
        _worldID = worldID;
        _mask = mask;
        _matchingArchetypes = new UnsafeList<Identifier<Archetype>>(8, Allocator.Persistent);
    }

    // TODO: Fetching layout every time is not optimal. Cache them?
    private static bool IsEntityValid(byte* chunkBase, int entityIndex, ref readonly Archetype archetype, ref readonly EntityQueryMask mask)
    {
        // 1. Check "Require Enabled" (WithAll)
        var it = mask.requireEnabled.GetIterator();
        while (it.Next(out var id))
        {
            // Get the EnableBitmask for this component in this chunk
            var layoutResult = archetype.GetLayout(id);
            if (layoutResult.Error != ErrorStatus.None
                // Not enableable, always true
                || layoutResult.Value.enableBitsOffset == -1)
            {
                continue;
            }

            // Check bit
            if (!CheckBit(chunkBase + layoutResult.Value.enableBitsOffset, entityIndex))
            {
                return false;
            }
        }

        // 2. Check "Require Disabled" (WithDisabled)
        it = mask.requireDisabled.GetIterator();
        while (it.Next(out var id))
        {
            var layoutResult = archetype.GetLayout(id);
            if (layoutResult.Error != ErrorStatus.None)
            {
                continue;
            }

            // If component is not enableable, it is technically "Always Enabled", 
            // so it cannot satisfy "WithDisabled".

            // Check bit (Must be 0)
            if (layoutResult.Value.enableBitsOffset == -1
                || CheckBit(chunkBase + layoutResult.Value.enableBitsOffset, entityIndex))
            {
                return false;
            }
        }

        // 3. Check "Reject if Enabled" (The "Soft WithNone")
        it = mask.rejectIfEnabled.GetIterator();
        while (it.Next(out var id))
        {
            var layoutResult = archetype.GetLayout(id);
            if (layoutResult.Error != ErrorStatus.None)
            {
                continue;
            }

            // If component is not enableable, it is technically "Always Enabled", 
            // so it cannot satisfy "Reject if Enabled".

            // Check bit (Must be 0)
            if (layoutResult.Value.enableBitsOffset == -1
                || CheckBit(chunkBase + layoutResult.Value.enableBitsOffset, entityIndex))
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CheckBit(byte* maskBase, int index)
    {
        var byteIndex = index >> Chunk.BIT_SHIFT;
        var bitIndex = index & Chunk.BIT_ALIGNMENT_MINUS_ONE;
        return (maskBase[byteIndex] & (1 << bitIndex)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddArchetypeIfMatch(ref readonly Archetype archetype)
    {
        if (_mask.Matches(in archetype._signature))
        {
            _matchingArchetypes.Add(archetype.ID);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

public ref partial struct QueryBuilder
{
    private readonly Stack.Scope _scope;

    private UnsafeList<Identifier<IComponent>> _all;
    private UnsafeList<Identifier<IComponent>> _any;
    private UnsafeList<Identifier<IComponent>> _absent;
    private UnsafeList<Identifier<IComponent>> _none;
    private UnsafeList<Identifier<IComponent>> _disabled;
    private UnsafeList<Identifier<IComponent>> _present;

    public QueryBuilder()
    {
        _scope = AllocationManager.CreateStackScope();

        _all = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _any = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _absent = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _none = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _disabled = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _present = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FindMax(UnsafeList<Identifier<IComponent>> list, ref int max)
    {
        foreach (var id in list)
        {
            if (id.value > max) max = id.value;
        }
    }

    public Identifier<EntityQuery> Build(World world)
    {
        // 1. Calculate max component ID to size the BitSets
        var maxID = 0;
        FindMax(_all, ref maxID);
        FindMax(_any, ref maxID);
        FindMax(_absent, ref maxID);
        FindMax(_none, ref maxID);
        FindMax(_disabled, ref maxID);
        FindMax(_present, ref maxID);

        // 2. Create the Mask
        var mask = new EntityQueryMask
        {
            structuralAll = new UnsafeBitSet(maxID + 1, Allocator.Persistent, AllocationOption.Clear),
            structuralAny = new UnsafeBitSet(maxID + 1, Allocator.Persistent, AllocationOption.Clear),
            structuralAbsent = new UnsafeBitSet(maxID + 1, Allocator.Persistent, AllocationOption.Clear),
            requireEnabled = new UnsafeBitSet(maxID + 1, Allocator.Persistent, AllocationOption.Clear),
            requireDisabled = new UnsafeBitSet(maxID + 1, Allocator.Persistent, AllocationOption.Clear),
            rejectIfEnabled = new UnsafeBitSet(maxID + 1, Allocator.Persistent, AllocationOption.Clear),
        };

        // 3. Fill BitSets
        foreach (var id in _all)
        {
            mask.structuralAll.SetBit(id);  // Structure: Must Exist
            mask.requireEnabled.SetBit(id); // Filter: Must be Enabled
        }

        foreach (var id in _disabled)
        {
            mask.structuralAll.SetBit(id);   // Structure: Must Exist
            mask.requireDisabled.SetBit(id); // Filter: Must be Disabled
        }

        foreach (var id in _none)
        {
            if (ComponentRegister.GetComponentInfo(id).isEnableable)
            {
                mask.rejectIfEnabled.SetBit(id); // Filter: Must Not be Enabled (Can be Absent or Disabled)
            }
            else
            {
                mask.structuralAbsent.SetBit(id); // Structure: Must Not Exist
            }
        }

        foreach (var id in _present)
        {
            mask.structuralAll.SetBit(id);
        }

        foreach (var id in _absent)
        {
            mask.structuralAbsent.SetBit(id);
        }

        foreach (var id in _any)
        {
            mask.structuralAny.SetBit(id);
        }

        // 4. Ask World for the Query (Cached)
        var maskHash = mask.GetHashCode();
        var queryID = world.GetEntityQueryIDByMaskHash(maskHash);
        if (queryID.IsValid)
        {
            // Check if the masks are actually equal (Hash collision?).
            // Really worth it? It's unlikely to have collisions here.
            if (world.GetEntityQueryReference(queryID)._mask.Equals(mask))
            {
                mask.Dispose();
                goto Return;
            }
        }

        // NOTE: We do not dispose the mask here, as it is now owned by the EntityQuery.
        queryID = world.CreateEntityQuery(mask, maskHash);

    Return:
        Dispose();
        return queryID;
    }

    private void Dispose()
    {
        _scope.Dispose();
    }
}
