using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

internal struct EntityQueryMask : IDisposable, IEquatable<EntityQueryMask>
{
    public UnsafeBitSet structuralAll;
    public UnsafeBitSet structuralAny;
    public UnsafeBitSet structuralAbsent;

    public UnsafeBitSet requireEnabled;
    public UnsafeBitSet requireDisabled;
    public UnsafeBitSet rejectIfEnabled;

    public UnsafeBitSet writeAccess;

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
        if (structuralAll.IsCreated)
        {
            hash = hash * 23 + structuralAll.GetHashCode();
        }

        if (structuralAbsent.IsCreated)
        {
            hash = hash * 23 + structuralAbsent.GetHashCode();
        }

        if (structuralAny.IsCreated)
        {
            hash = hash * 23 + structuralAny.GetHashCode();
        }

        if (requireEnabled.IsCreated)
        {
            hash = hash * 23 + requireEnabled.GetHashCode();
        }

        if (requireDisabled.IsCreated)
        {
            hash = hash * 23 + requireDisabled.GetHashCode();
        }

        if (rejectIfEnabled.IsCreated)
        {
            hash = hash * 23 + rejectIfEnabled.GetHashCode();
        }

        if (writeAccess.IsCreated)
        {
            hash = hash * 23 + writeAccess.GetHashCode();
        }

        return hash;
    }

    public readonly bool Equals(EntityQueryMask other)
    {
        return structuralAll.Equals(other.structuralAll)
            && structuralAny.Equals(other.structuralAny)
            && structuralAbsent.Equals(other.structuralAbsent)
            && requireEnabled.Equals(other.requireEnabled)
            && requireDisabled.Equals(other.requireDisabled)
            && rejectIfEnabled.Equals(other.rejectIfEnabled)
            && writeAccess.Equals(other.writeAccess);
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

        writeAccess.Dispose();
    }
}

/// <summary>
/// Provides a read-only view over a chunk of entities and their component data within an archetype.
/// </summary>
/// <remarks>This does not filter disabled/enabled components. You must handle that manually.</remarks>
public readonly unsafe ref struct ChunkView
{
    // We flatten all the information we need for fast access.
    private readonly ReadOnlyView<Archetype.ComponentMemoryLayout> _layouts;
    private readonly ReadOnlyView<int> _layoutIndexLookup;
    private readonly ReadOnlyView<Archetype.SharedComponentLayout> _sharedLayouts;

    private readonly byte* _pChunkData;
    private readonly uint* _pVersion;
    private readonly byte* _pSharedData;

    private readonly int _entityOffset;
    private readonly int _entityCount;
    private readonly uint _structuralVersion;
    private readonly uint _currentVersion;

    public readonly int EntityCount => _entityCount;

    internal ChunkView(ref readonly Archetype archetype, ref readonly Chunk chunk)
    {
        _layouts = archetype._layouts.AsReadOnly();
        _layoutIndexLookup = archetype._componentIDToLayoutIndex.AsReadOnly();
        _sharedLayouts = archetype._sharedLayouts.AsReadOnly();

        _pChunkData = chunk.GetUnsafePtr();
        _pVersion = chunk.GetVersionUnsafePtr();
        _pSharedData = archetype._chunkGroups[chunk._groupIndex].sharedData.IsCreated ? (byte*)archetype._chunkGroups[chunk._groupIndex].sharedData.GetUnsafePtr() : null;

        _entityOffset = archetype.EntityIDsOffset;
        _entityCount = chunk._count;

        _structuralVersion = chunk._structuralVersion;
        _currentVersion = World.GetWorldUncheck(archetype.WorldID).Version;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Archetype.ComponentMemoryLayout GetLayout(Identifier<IComponent> id)
    {
        var index = _layoutIndexLookup[id.Value];
        if (index == -1)
        {
            throw new InvalidOperationException($"Component {id} is not exist in the archetype.");
        }

        return _layouts[index];
    }

    /// <summary>
    /// Determines whether the specified component has changed since the given version.
    /// </summary>
    /// <param name="id">The identifier of the component to check for changes.</param>
    /// <param name="version">The version number to compare against the component's current version. Must be greater than or equal to zero.</param>
    /// <returns>true if the component's current version is less than or equal to the specified version; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasChanged(Identifier<IComponent> id, uint version)
    {
        var layout = GetLayout(id);
        return version < _pVersion[layout.versionIndex];
    }

    /// <summary>
    /// Determines whether the specified version indicates that the component of space <typeparamref name="T"/> has
    /// changed since the last recorded version.
    /// </summary>
    /// <typeparam name="T">The space of component to check for changes. Must be an unmanaged space that implements <see cref="IComponent"/>.</typeparam>
    /// <param name="version">The version number to compare against the current version of the component.</param>
    /// <returns>true if the component of space T has changed since the specified version; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasChanged<T>(uint version)
        where T : unmanaged, IComponentData
    {
        var layout = GetLayout(ComponentTypeID<T>.Value);
        return version < _pVersion[layout.versionIndex];
    }

    /// <summary>
    /// Determines whether the chunk's structure has changed since the specified version.
    /// </summary>
    /// <param name="version">The version number to compare against the chunk's structural version.</param>
    /// <returns>true if the chunk's structure has changed since the specified version; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasStructuralChanged(uint version)
    {
        return version < _structuralVersion;
    }

    /// <summary>
    /// Gets the current version number associated with the specified component identifier.
    /// </summary>
    /// <param name="id">The identifier of the component for which to retrieve the version number. Must reference a valid component.</param>
    /// <returns>The version number of the specified component.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint GetComponentVersion(Identifier<IComponent> id)
    {
        return _pVersion[id];
    }

    /// <summary>
    /// Gets the current version number associated with the specified component space.
    /// </summary>
    /// <typeparam name="T">The component space for which to retrieve the version. Must be an unmanaged space that implements <see cref="IComponent"/>.</typeparam>
    /// <returns>The version number of the component space <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint GetComponentVersion<T>()
        where T : unmanaged, IComponentData
    {
        return _pVersion[ComponentTypeID<T>.Value];
    }

    /// <summary>
    /// Returns a read-only span containing structuralAll entities stored in the current chunk.
    /// </summary>
    /// <returns>A read-only span of <see cref="Entity"/> values representing the entities in the chunk.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<Entity> GetEntities()
    {
        var pEntity = (Entity*)(_pChunkData + _entityOffset);
        return new ReadOnlySpan<Entity>(pEntity, _entityCount);
    }

    /// <summary>
    /// Gets a readonly span providing direct access to the component data of space T0 for structuralAll entities in the chunk.
    /// </summary>
    /// <typeparam name="T">The space of component to access. Must be an unmanaged space that implements <see cref="IComponent"/>.</typeparam>
    /// <returns>A readonly span of space <see cref="{T}"/> containing the component data for each entity in the chunk.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified component space is not present in the archetype.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> GetComponentData<T>()
        where T : unmanaged, IComponentData
    {
        var layout = GetLayout(ComponentTypeID<T>.Value);
        var pComponentData = _pChunkData + layout.offset;
        return new ReadOnlySpan<T>(pComponentData, _entityCount);
    }

    /// <summary>
    /// Gets a span providing direct access to the component data of space T0 for structuralAll entities in the chunk.
    /// </summary>
    /// <typeparam name="T">The space of component to access. Must be an unmanaged space that implements <see cref="IComponent"/>.</typeparam>
    /// <returns>A span of space <see cref="{T}"/> containing the component data for each entity in the chunk.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified component space is not present in the archetype.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetComponentDataRW<T>()
        where T : unmanaged, IComponentData
    {
        var compId = ComponentTypeID<T>.Value;
        var layout = GetLayout(compId);

        _pVersion[layout.versionIndex] = _currentVersion;

        var pComponentData = _pChunkData + layout.offset;
        return new Span<T>(pComponentData, _entityCount);
    }

    /// <summary>
    /// Gets a bit set representing the enabled state of each instance of the specified enableable component
    /// space within the current chunk.
    /// </summary>
    /// <typeparam name="T">The component space for which to retrieve enablement bits. Must be unmanaged and implement <see cref="IEnableableComponent"/>.</typeparam>
    /// <returns>A <see cref="SpanBitSet"/> that provides access to the enablement bits for all instances of the specified component space in the chunk.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified component space does not support enablement.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanBitSet GetEnableBits<T>()
        where T : unmanaged, IEnableableComponent
    {
        var layout = _layouts[ComponentTypeID<T>.Value];
        var maskBase = _pChunkData + layout.enableBitsOffset;
        return new SpanBitSet(new Span<uint>(maskBase, (_entityCount + 31) / 32));
    }

    /// <summary>
    /// Determines whether the specified component of space <typeparamref name="T"/> at the given index is currently enabled.
    /// </summary>
    /// <typeparam name="T">The space of the component to check. Must be an unmanaged space that implements <see cref="IEnableableComponent"/>.</typeparam>
    /// <param name="index">The zero-based index of the component instance to check within the chunk.</param>
    /// <returns>true if the component at the specified index is enabled; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified component space <typeparamref name="T"/> was not found in current chunk.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsComponentEnabled<T>(int index)
        where T : unmanaged, IEnableableComponent
    {
        var layout = GetLayout(ComponentTypeID<T>.Value);
        var pMask = _pChunkData + layout.enableBitsOffset;
        return EntityQuery.CheckBit(pMask, index);
    }

    /// <summary>
    /// Get the value of the shared data stored in current chunk.
    /// </summary>
    /// <typeparam name="T">The space of the component to check. Must be an unmanaged space that implements <see cref="ISharedComponent"/>.</typeparam>
    /// <returns> The reference to the shared data.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified component space <typeparamref name="T"/> was not found in current chunk.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T GetSharedComponent<T>()
        where T : unmanaged, ISharedComponent
    {
#if GHOST_SAFETY_CHECKS
        if (_pSharedData == null)
        {
            throw new InvalidOperationException($"Shared component type {typeof(T).Name} does not exist in current chunk.");
        }
#endif

        var compID = ComponentTypeID<T>.Value;
        var layoutIndex = -1;
        for (var i = 0; i < _sharedLayouts.Length; i++)
        {
            if (_sharedLayouts[i].componentID == compID.Value)
            {
                layoutIndex = i;
                break;
            }
        }

        if (layoutIndex == -1)
        {
            throw new InvalidOperationException($"Shared component type {typeof(T).Name} does not exist in current chunk.");
        }

        return ref *(T*)(_pSharedData + _sharedLayouts[layoutIndex].offset);
    }
}

public unsafe partial struct EntityQuery : IDisposable
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
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ref var archetype = ref _iterator._world.ComponentManager.GetArchetypeReference(_iterator._matchingArchetypes[_archetypeIndex]);
                    ref var chunk = ref archetype.GetChunkReference(_chunkIndex);
                    return new ChunkView(in archetype, in chunk);
                }
            }

            public bool MoveNext()
            {
            Start:
                _chunkIndex++;

                while (_archetypeIndex < _iterator._matchingArchetypes.Count)
                {
                    ref var archetype = ref _iterator._world.ComponentManager.GetArchetypeReference(_iterator._matchingArchetypes[_archetypeIndex]);
                    if (_chunkIndex < archetype.ChunkCount)
                    {
                        if (archetype.GetChunkReference(_chunkIndex)._count == 0)
                        {
                            goto Start;
                        }

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

        private readonly ReadOnlyView<Identifier<Archetype>> _matchingArchetypes;
        private readonly World _world;

        internal ChunkIterator(ReadOnlyView<Identifier<Archetype>> matchingArchetypes, World world)
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

    internal EntityQuery(Identifier<EntityQuery> id, Identifier<World> worldID, ref readonly EntityQueryMask mask)
    {
        _id = id;
        _worldID = worldID;
        _mask = mask;
        _matchingArchetypes = new UnsafeList<Identifier<Archetype>>(8, AllocationHandle.Persistent);
    }

    // TODO: Fetching layout every time is not optimal. Cache them?
    private static bool IsEntityValid(byte* chunkBase, int entityIndex, ref readonly Archetype archetype, ref readonly EntityQueryMask mask)
    {
        // Check "Require Enabled" (WithAll)
        var it = mask.requireEnabled.GetIterator();
        while (it.Next(out var id))
        {
            // Get the EnableBitmask for this component in this chunk
            var layoutResult = archetype.GetLayout(id);
            if (layoutResult.Error != Error.None
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

        // Check "Require Disabled" (WithDisabled)
        it = mask.requireDisabled.GetIterator();
        while (it.Next(out var id))
        {
            var layoutResult = archetype.GetLayout(id);
            if (layoutResult.Error != Error.None)
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

        // Check "Reject if Enabled" (The "Soft WithNone")
        it = mask.rejectIfEnabled.GetIterator();
        while (it.Next(out var id))
        {
            var layoutResult = archetype.GetLayout(id);
            if (layoutResult.Error != Error.None)
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

    private static bool RequiresEnableableFiltering(ref readonly Archetype archetype, ref readonly EntityQueryMask mask)
    {
        // If the query asks to filter by enabled/disabled state AND the archetype 
        // actually has that component marked as enableable (enableBitsOffset != -1), we must filter.
        var it = mask.requireEnabled.GetIterator();
        while (it.Next(out var id))
        {
            var layoutResult = archetype.GetLayout(id);
            if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
            {
                return true;
            }
        }

        it = mask.requireDisabled.GetIterator();
        while (it.Next(out var id))
        {
            var layoutResult = archetype.GetLayout(id);
            if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
            {
                return true;
            }
        }

        it = mask.rejectIfEnabled.GetIterator();
        while (it.Next(out var id))
        {
            var layoutResult = archetype.GetLayout(id);
            if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
            {
                return true;
            }
        }

        return false;
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
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return default;
        }

        return new ChunkIterator(_matchingArchetypes.AsReadOnly(), world);
    }

    /// <summary>
    /// Calculate total entity count in this query
    /// </summary>
    /// <returns> Total entity count</returns>
    public readonly int CalculateEntityCount()
    {
        var total = 0;
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return 0;
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            var archetypeID = _matchingArchetypes[i];
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(archetypeID);

            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);

            for (var j = 0; j < archetype.ChunkCount; j++)
            {
                ref var chunk = ref archetype.GetChunkReference(j);
                if (chunk._count == 0)
                {
                    continue;
                }

                if (!requiresFiltering)
                {
                    // Fast path: all entities match
                    total += chunk._count;
                }
                else
                {
                    // Slow path: check individual bits
                    for (var k = 0; k < chunk._count; k++)
                    {
                        if (IsEntityValid(chunk.GetUnsafePtr(), k, in archetype, in _mask))
                        {
                            total++;
                        }
                    }
                }
            }
        }

        return total;
    }

    public readonly bool HasMatchingEntity()
    {
        var world = World.GetWorld(_worldID);
        if (world is null)
        {
            return false;
        }

        for (var i = 0; i < _matchingArchetypes.Count; i++)
        {
            var archetypeID = _matchingArchetypes[i];
            ref var archetype = ref world.ComponentManager.GetArchetypeReference(archetypeID);

            // Check ONCE per archetype if we actually need to loop entities
            var requiresFiltering = RequiresEnableableFiltering(in archetype, in _mask);

            for (var j = 0; j < archetype.ChunkCount; j++)
            {
                ref var chunk = ref archetype.GetChunkReference(j);
                if (chunk._count == 0)
                {
                    continue;
                }

                if (!requiresFiltering)
                {
                    return true;
                }

                var ulongCount = (chunk._count + 63) / 64;
                var chunkHasMatch = false;

                // Loop through 64 entities at a time
                for (var block = 0; block < ulongCount; block++)
                {
                    // Start assuming all 64 entities in this block are valid (ulong.MaxValue = 1111...)
                    // For the last block, mask out the garbage bits beyond chunk._count
                    var validMask = ulong.MaxValue;
                    var remaining = chunk._count - (block * 64);
                    if (remaining < 64)
                    {
                        validMask = (1UL << remaining) - 1UL;
                    }

                    // Intersect requireEnabled (Bits MUST be 1)
                    var it = _mask.requireEnabled.GetIterator();
                    while (it.Next(out var id) && validMask != 0)
                    {
                        var layoutResult = archetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        {
                            var pMask = (ulong*)(chunk.GetUnsafePtr() + layoutResult.Value.enableBitsOffset);
                            validMask &= pMask[block];
                        }
                    }

                    // Intersect requireDisabled / rejectIfEnabled (Bits MUST be 0)
                    it = _mask.requireDisabled.GetIterator();
                    while (it.Next(out var id) && validMask != 0)
                    {
                        var layoutResult = archetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        {
                            var pMask = (ulong*)(chunk.GetUnsafePtr() + layoutResult.Value.enableBitsOffset);
                            validMask &= ~pMask[block]; // Invert memory, because it must be 0!
                        }
                    }

                    // rejectIfEnabled behaves identically to requireDisabled
                    it = _mask.rejectIfEnabled.GetIterator();
                    while (it.Next(out var id) && validMask != 0)
                    {
                        var layoutResult = archetype.GetLayout(id);
                        if (layoutResult.Error == Error.None && layoutResult.Value.enableBitsOffset != -1)
                        {
                            var pMask = (ulong*)(chunk.GetUnsafePtr() + layoutResult.Value.enableBitsOffset);
                            validMask &= ~pMask[block];
                        }
                    }

                    // If validMask still has ANY bits set to 1, we found at least one matching entity.
                    if (validMask != 0)
                    {
                        chunkHasMatch = true;
                        break;
                    }
                }

                if (chunkHasMatch)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void Dispose()
    {
        _mask.Dispose();
        _matchingArchetypes.Dispose();
    }
}

public ref partial struct QueryBuilder : IDisposable
{
    private readonly VirtualStack.Scope _scope;

    private UnsafeList<Identifier<IComponent>> _all;
    private UnsafeList<Identifier<IComponent>> _any;
    private UnsafeList<Identifier<IComponent>> _absent;
    private UnsafeList<Identifier<IComponent>> _none;
    private UnsafeList<Identifier<IComponent>> _disabled;
    private UnsafeList<Identifier<IComponent>> _present;

    private UnsafeList<Identifier<IComponent>> _rw;

    public QueryBuilder()
    {
        _scope = AllocationManager.CreateStackScope();

        _all = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _any = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _absent = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _none = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _disabled = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
        _present = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);

        _rw = new UnsafeList<Identifier<IComponent>>(4, _scope.AllocationHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static QueryBuilder New()
    {
        return new QueryBuilder();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void FindMax(UnsafeList<Identifier<IComponent>> list, ref int max)
    {
        foreach (var id in list)
        {
            max = Math.Max(max, id.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WithAll(params Span<Identifier<IComponent>> componentIDs)
    {
        _all.AddRange(componentIDs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WithAny(params Span<Identifier<IComponent>> componentIDs)
    {
        _any.AddRange(componentIDs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WithAbsent(params Span<Identifier<IComponent>> componentIDs)
    {
        _absent.AddRange(componentIDs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WithNone(params Span<Identifier<IComponent>> componentIDs)
    {
        _none.AddRange(componentIDs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WithDisabled(params Span<Identifier<IComponent>> componentIDs)
    {
        _disabled.AddRange(componentIDs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WithPresent(params Span<Identifier<IComponent>> componentIDs)
    {
        _present.AddRange(componentIDs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WithPresentRW(params Span<Identifier<IComponent>> componentIDs)
    {
        _present.AddRange(componentIDs);
        _rw.AddRange(componentIDs);
    }

    private void BuildQueryMask(AllocationHandle allocationHandle, out EntityQueryMask mask)
    {
        // Calculate max component ID to size the BitSets
        var maxID = 0;
        FindMax(_all, ref maxID);
        FindMax(_any, ref maxID);
        FindMax(_absent, ref maxID);
        FindMax(_none, ref maxID);
        FindMax(_disabled, ref maxID);
        FindMax(_present, ref maxID);

        // Create the Mask
        mask = new EntityQueryMask
        {
            structuralAll = new UnsafeBitSet(maxID + 1, allocationHandle, AllocationOption.Clear),
            structuralAny = new UnsafeBitSet(maxID + 1, allocationHandle, AllocationOption.Clear),
            structuralAbsent = new UnsafeBitSet(maxID + 1, allocationHandle, AllocationOption.Clear),
            requireEnabled = new UnsafeBitSet(maxID + 1, allocationHandle, AllocationOption.Clear),
            requireDisabled = new UnsafeBitSet(maxID + 1, allocationHandle, AllocationOption.Clear),
            rejectIfEnabled = new UnsafeBitSet(maxID + 1, allocationHandle, AllocationOption.Clear),

            writeAccess = new UnsafeBitSet(maxID + 1, allocationHandle, AllocationOption.Clear),
        };

        // Fill BitSets
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
            if (ComponentRegistry.GetComponentInfo(id).isEnableable)
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

        foreach (var id in _rw)
        {
            mask.writeAccess.SetBit(id);
        }
    }

    public EntityQuery BuildWithoutCache(World world, AllocationHandle allocationHandle, bool dispose = true)
    {
        BuildQueryMask(allocationHandle, out var mask);
        var query = new EntityQuery(Identifier<EntityQuery>.Invalid, world.ID, ref mask);

        for (var i = 0; i < world.ComponentManager.ArchetypeCount; i++)
        {
            query.AddArchetypeIfMatch(in world.ComponentManager.GetArchetypeReference(i));
        }

        if (dispose)
        {
            Dispose();
        }

        return query;
    }

    public Identifier<EntityQuery> Build(World world, bool dispose = true)
    {
        BuildQueryMask(AllocationHandle.Persistent, out var mask);

        var maskHash = mask.GetHashCode();
        var queryID = world.ComponentManager.GetEntityQueryIDByMaskHash(maskHash);
        if (queryID.IsValid)
        {
            // Check if the masks are actually equal (Hash collision?).
            // Really worth it? It's unlikely to have collisions here.
            if (world.ComponentManager.GetEntityQueryReference(queryID)._mask.Equals(mask))
            {
                mask.Dispose();
                goto Return;
            }
        }

        // NOTE: We do not dispose the mask here, as it is now owned by the EntityQuery.
        queryID = world.ComponentManager.CreateEntityQuery(in mask, maskHash);

    Return:
        if (dispose)
        {
            Dispose();
        }

        return queryID;
    }

    public void Clear()
    {
        _all.Clear();
        _any.Clear();
        _absent.Clear();
        _none.Clear();
        _disabled.Clear();
        _present.Clear();
        _rw.Clear();
    }

    public readonly void Dispose()
    {
        _all.Dispose();
        _any.Dispose();
        _absent.Dispose();
        _none.Dispose();
        _disabled.Dispose();
        _present.Dispose();
        _rw.Dispose();

        _scope.Dispose();
    }
}
