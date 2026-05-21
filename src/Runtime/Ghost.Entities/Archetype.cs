using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

internal sealed unsafe class ChunkDebugView
{
    [DebuggerDisplay("{Name,nq}: {Data}")]
    internal class ComponentArrayView
    {
        public string Name { get; }
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Data { get; }

        public ComponentArrayView(string name, object data)
        {
            Name = name;
            Data = data;
        }
    }

    private Chunk _chunk;

    public ChunkDebugView(Chunk chunk)
    {
        _chunk = chunk;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public object[] Items => GetItems(in _chunk);

    private static T[] ReadComponentArray<T>(long pData, int offsetInChunk, int count)
        where T : unmanaged
    {
        var result = new T[count];
        unsafe
        {
            var basePtr = (byte*)pData + offsetInChunk;
            var span = new Span<T>(basePtr, count);
            span.CopyTo(result);
        }

        return result;
    }

    private static object[] GetItems(ref readonly Chunk chunk)
    {
#if !DEBUG
        return [];
#else
        var pData = chunk.GetUnsafePtr();
        var count = chunk._count;
        var capacity = chunk._capacity;
        var worldID = chunk._worldID;
        var archetypeID = chunk._archetypeID;

        if (count == 0)
        {
            return [];
        }

        var views = new List<object>();
        var world = World.GetWorld(worldID);
        if (world is null)
        {
            return [];
        }

        ref var archetype = ref world.ComponentManager.GetArchetypeReference(archetypeID);
        var it = archetype._signature.GetIterator();
        while (it.Next(out var index))
        {
            var type = ComponentRegistry.s_runtimeIDToType[index];
            if (type == null)
            {
                continue;
            }
            var layout = archetype.GetLayout(index).Value;
            var readMethod = typeof(ChunkDebugView)
                .GetMethod(nameof(ReadComponentArray), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .MakeGenericMethod(type);

            // 3. Invoke it to get a Position[] or Velocity[]
            var array = readMethod.Invoke(null, [(long)pData, layout.offset, count]);
            if (array == null)
            {
                continue;
            }

            // 4. Wrap it in a nice label so the debugger shows "Position[]"
            views.Add(new ComponentArrayView(type.Name, array));
        }

        return [.. views];
#endif
    }
}

[DebuggerTypeProxy(typeof(ChunkDebugView))]
internal unsafe struct Chunk : IDisposable
{
    public const int CHUNK_BUFFER_SIZE = 16384; // 16 KB
    public const int BIT_ALIGNMENT = 8;
    public const int BIT_SHIFT = 3; // log2(BIT_ALIGNMENT)
    public const int BIT_ALIGNMENT_MINUS_ONE = BIT_ALIGNMENT - 1;

    private UnsafeArray<byte> _data;
    private UnsafeArray<uint> _versions;

    internal uint _structuralVersion;

    internal int _count;
    internal readonly int _capacity;

    internal readonly int _worldID;
    internal readonly int _archetypeID;
    internal readonly int _groupIndex;

    public Chunk(int capacity, int componentCount, uint globalVersion, int worldID, int archetypeID, int groupIndex)
    {
        _data = new UnsafeArray<byte>(CHUNK_BUFFER_SIZE, AllocationHandle.Persistent, AllocationOption.Clear);
        _capacity = capacity;
        _count = 0;

        if (componentCount > 0)
        {
            _versions = new UnsafeArray<uint>(componentCount, AllocationHandle.Persistent);
            _versions.AsSpan().Fill(globalVersion);
        }

        _structuralVersion = globalVersion;
        _worldID = worldID;
        _archetypeID = archetypeID;
        _groupIndex = groupIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)_data.GetUnsafePtr();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly uint* GetVersionUnsafePtr()
    {
        return _versions.IsCreated ? (uint*)_versions.GetUnsafePtr() : null;
    }

    public void Dispose()
    {
        _data.Dispose();
        _versions.Dispose();
    }
}

internal unsafe struct Archetype : IDisposable
{
    internal struct ComponentMemoryLayout
    {
        public int componentID;
        public int size;
        public int offset;
        public int enableBitsOffset;
        public int versionIndex;
    }

    internal struct SharedComponentLayout
    {
        public int componentID;
        public int offset;  // offset into ChunkGroup.sharedData
        public int size;
    }

    internal struct ChunkGroup : IDisposable
    {
        public int sharedDataHash;
        public int activeChunkIndex;  // last chunk with room, -1 if none
        public UnsafeArray<byte> sharedData;  // the shared values for this group
        public int refCount;

        public void Dispose()
        {
            sharedData.Dispose();
        }
    }

    //private struct Edge
    //{
    //    public int componentID;
    //    public int targetArchetype; // can't use Identifier<Archetype> because cycle causer
    //}

    internal UnsafeBitSet _signature;
    internal UnsafeList<Chunk> _chunks;
    internal UnsafeArray<ComponentMemoryLayout> _layouts;
    internal UnsafeArray<int> _componentIDToLayoutIndex;

    internal UnsafeArray<SharedComponentLayout> _sharedLayouts;
    internal UnsafeList<ChunkGroup> _chunkGroups;

    private UnsafeHashMap<int, int> _edgesAdd;
    private UnsafeHashMap<int, int> _edgesRemove;

    // 0 means no cleanup component (since 0 is the empty archetype), -1 means haven't computed yet, positive value means the archetype id of the cleanup edge.
    internal int _cleanupEdge;

    private readonly Identifier<Archetype> _id;
    private readonly Identifier<World> _worldID;

    private readonly int _hash;
    private int _entityCapacity;
    private int _maxComponentID;
    private int _entityIdsOffset;
    internal int _sharedDataSize;

    public readonly Identifier<Archetype> ID => _id;
    public readonly Identifier<World> WorldID => _worldID;

    public readonly int EntityCapacity => _entityCapacity;
    public readonly int ChunkCount => _chunks.Count;
    public readonly int EntityIDsOffset => _entityIdsOffset;

    public Archetype(Identifier<Archetype> id, Identifier<World> worldID, ReadOnlySpan<Identifier<IComponent>> componentIds)
    {
        _id = id;
        _worldID = worldID;

        _chunks = new UnsafeList<Chunk>(4, AllocationHandle.Persistent);
        _edgesAdd = new UnsafeHashMap<int, int>(4, AllocationHandle.Persistent);
        _edgesRemove = new UnsafeHashMap<int, int>(4, AllocationHandle.Persistent);

        if (componentIds.IsEmpty)
        {
            _signature = new UnsafeBitSet(1, AllocationHandle.Persistent, AllocationOption.Clear);
            _chunkGroups = new UnsafeList<ChunkGroup>(1, AllocationHandle.Persistent);
            _hash = 0;

            _signature.ClearAll();
            _entityCapacity = Chunk.CHUNK_BUFFER_SIZE / sizeof(Entity);

            return;
        }

        var highestComponentID = 0;
        for (var i = 0; i < componentIds.Length; i++)
        {
            if (componentIds[i] > highestComponentID)
            {
                highestComponentID = componentIds[i];
            }
        }

        _signature = new UnsafeBitSet(highestComponentID + 1, AllocationHandle.Persistent, AllocationOption.Clear);
        _hash = _signature.GetHashCode();

        CalculateLayout(componentIds);
    }

    private void CalculateLayout(ReadOnlySpan<Identifier<IComponent>> componentIds)
    {
        var entitySize = sizeof(Entity);
        var entityAlign = (int)MemoryUtility.AlignOf<Entity>();


        using var scope = AllocationManager.CreateStackScope();
        using var components = new UnsafeList<ComponentInfo>(componentIds.Length, scope.AllocationHandle);
        using var sharedInfos = new UnsafeList<ComponentInfo>(componentIds.Length, scope.AllocationHandle);

        var cleanupCount = 0;
        for (var i = 0; i < componentIds.Length; i++)
        {
            _signature.SetBit(componentIds[i]);

            var info = ComponentRegistry.GetComponentInfo(componentIds[i]);

            if (info.isShared)
            {
                sharedInfos.Add(info);  // store info directly — components list skips shared
                continue;
            }

            if (info.isCleanup)
            {
                cleanupCount++;
            }

            components.Add(info);
        }

        if (sharedInfos.Count > 0)
        {
            var offset = 0;

            _sharedLayouts = new UnsafeArray<SharedComponentLayout>(sharedInfos.Count, AllocationHandle.Persistent);
            for (var i = 0; i < sharedInfos.Count; i++)
            {
                var info = sharedInfos[i];
                _sharedLayouts[i] = new SharedComponentLayout
                {
                    componentID = info.id.Value,
                    size = info.size,
                    offset = offset
                };

                offset += info.size;
            }

            _sharedDataSize = offset;
        }

        if (cleanupCount > 0)
        {
            _cleanupEdge = -1;
        }

        // Calculate total size per entity to get an initial capacity estimate
        var bytesPerEntity = entitySize;
        var maxComponentID = 0;
        for (var i = 0; i < components.Count; i++)
        {
            var comp = components[i];
            bytesPerEntity += comp.size;
            if (comp.id > maxComponentID)
            {
                maxComponentID = comp.id;
            }
        }

        _maxComponentID = maxComponentID;
        _entityCapacity = Chunk.CHUNK_BUFFER_SIZE / bytesPerEntity;
        _layouts = new UnsafeArray<ComponentMemoryLayout>(components.Count, AllocationHandle.Persistent);
        _componentIDToLayoutIndex = new UnsafeArray<int>(_maxComponentID + 1, AllocationHandle.Persistent);
        _chunkGroups = new UnsafeList<ChunkGroup>(4, AllocationHandle.Persistent);

        _componentIDToLayoutIndex.AsSpan().Fill(-1);

        components.AsSpan().Sort(static (a, b) => b.alignment.CompareTo(a.alignment));
        using var tempOffsets = new UnsafeArray<int>(components.Count, scope.AllocationHandle);
        using var tempBitmaskOffsets = new UnsafeArray<int>(components.Count, scope.AllocationHandle);

        while (_entityCapacity > 0)
        {
            var currentOffset = 0;
            var fits = true;

            currentOffset = (currentOffset + entityAlign - 1) & ~(entityAlign - 1);

            _entityIdsOffset = currentOffset;
            currentOffset += _entityCapacity * entitySize;

            for (var i = 0; i < components.Count; i++)
            {
                var size = components[i].size;
                var align = components[i].alignment;

                currentOffset = (currentOffset + align - 1) & ~(align - 1);
                tempOffsets[i] = currentOffset;
                currentOffset += _entityCapacity * size;

                var bitmaskOffset = -1;
                if (components[i].isEnableable)
                {
                    var bitmaskSize = (_entityCapacity + Chunk.BIT_ALIGNMENT_MINUS_ONE) / Chunk.BIT_ALIGNMENT;
                    // Reserve space for the bitmask (1 bit per entity)

                    currentOffset = (currentOffset + Chunk.BIT_ALIGNMENT_MINUS_ONE) & ~Chunk.BIT_ALIGNMENT_MINUS_ONE; // Align
                    bitmaskOffset = currentOffset;
                    currentOffset += bitmaskSize;
                }

                tempBitmaskOffsets[i] = bitmaskOffset;

                if (currentOffset > Chunk.CHUNK_BUFFER_SIZE)
                {
                    fits = false;
                    break;
                }
            }

            if (fits)
            {
                for (var i = 0; i < components.Count; i++)
                {
                    _layouts[i] = new ComponentMemoryLayout
                    {
                        componentID = components[i].id,
                        offset = tempOffsets[i],
                        size = components[i].size,
                        enableBitsOffset = tempBitmaskOffsets[i],
                        versionIndex = i
                    };

                    _componentIDToLayoutIndex[components[i].id] = i;
                }

                return;
            }

            _entityCapacity--;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly Chunk CreateNewChunk(uint version, int groupIndex)
    {
        var newChunk = new Chunk(_entityCapacity, _layouts.Count, version, _worldID, _id, groupIndex);

        for (var i = 0; i < _layouts.Count; i++)
        {
            var layout = _layouts[i];
            if (layout.enableBitsOffset != -1)
            {
                var pChunk = newChunk.GetUnsafePtr();
                var pBits = pChunk + layout.enableBitsOffset;
                MemoryUtility.MemSet(pBits, 0xFF, (nuint)((_entityCapacity + Chunk.BIT_ALIGNMENT_MINUS_ONE) / Chunk.BIT_ALIGNMENT));
            }
        }

        return newChunk;
    }

    public void AllocateEntity(ReadOnlySpan<byte> sharedData, int sharedDataHash, out int chunkIndex, out int rowIndex)
    {
        var world = World.GetWorldUncheck(_worldID);
        var groupIndex = -1;

        for (var i = 0; i < _chunkGroups.Count; i++)
        {
            var group = _chunkGroups[i];
            if (group.sharedDataHash == sharedDataHash)
            {
                groupIndex = i;
                group.refCount++;

                if (group.activeChunkIndex < 0)
                {
                    break;
                }

                ref var chunk = ref _chunks[group.activeChunkIndex];
                if (chunk._count < _entityCapacity)
                {
                    rowIndex = chunk._count;
                    chunkIndex = group.activeChunkIndex;

                    chunk._count++;
                    chunk._structuralVersion = world.Version;

                    return;
                }
            }
        }

        if (groupIndex == -1)
        {
            var data = sharedData.IsEmpty ? default : new UnsafeArray<byte>(sharedData.Length, AllocationHandle.Persistent);
            if (!sharedData.IsEmpty)
            {
                data.CopyFrom(sharedData);
            }
            groupIndex = _chunkGroups.Count;

            _chunkGroups.Add(new ChunkGroup
            {
                sharedDataHash = sharedDataHash,
                sharedData = data,
                refCount = 1
            });
        }

        var newChunk = CreateNewChunk(world.Version, groupIndex);

        rowIndex = 0;
        newChunk._count++;
        chunkIndex = _chunks.Count;

        _chunkGroups[groupIndex].activeChunkIndex = chunkIndex;
        _chunks.Add(newChunk);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AllocateEntity(out int chunkIndex, out int rowIndex)
    {
        AllocateEntity(ReadOnlySpan<byte>.Empty, 0, out chunkIndex, out rowIndex);
    }

    public void AllocateEntities(ReadOnlySpan<byte> sharedData, int sharedDataHash, Span<int> chunkIndex, Span<int> rowIndex)
    {
        Logger.DebugAssert(chunkIndex.Length == rowIndex.Length, "chunkIndex and rowIndex spans must have the same length");

        var world = World.GetWorldUncheck(_worldID);
        var groupIndex = -1;

        var idx = 0;
        for (var i = 0; i < _chunkGroups.Count; i++)
        {
            var group = _chunkGroups[i];
            if (group.sharedDataHash == sharedDataHash)
            {
                groupIndex = i;
                group.refCount++;

                if (group.activeChunkIndex < 0)
                {
                    break;
                }

                ref var chunk = ref _chunks[group.activeChunkIndex];
                while (chunk._count < _entityCapacity && idx < rowIndex.Length)
                {
                    rowIndex[idx] = chunk._count;
                    chunkIndex[idx] = group.activeChunkIndex;

                    chunk._count++;
                    idx++;
                }

                if (idx != 0)
                {
                    chunk._structuralVersion = world.Version;
                }

                if (idx == rowIndex.Length - 1)
                {
                    return;
                }
            }
        }

        if (groupIndex == -1)
        {
            var data = sharedData.IsEmpty ? default : new UnsafeArray<byte>(sharedData.Length, AllocationHandle.Persistent);
            if (!sharedData.IsEmpty)
            {
                data.CopyFrom(sharedData);
            }
            groupIndex = _chunkGroups.Count;

            _chunkGroups.Add(new ChunkGroup
            {
                sharedDataHash = sharedDataHash,
                sharedData = data,
                refCount = 1
            });
        }


        while (idx < rowIndex.Length)
        {
            var newChunk = CreateNewChunk(world.Version, groupIndex);

            while (newChunk._count < _entityCapacity && idx < rowIndex.Length)
            {
                rowIndex[idx] = newChunk._count;
                chunkIndex[idx] = _chunks.Count;

                newChunk._count++;
                idx++;
            }

            _chunkGroups[groupIndex].activeChunkIndex = _chunks.Count;
            _chunks.Add(newChunk);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AllocateEntities(Span<int> chunkIndex, Span<int> rowIndex)
    {
        AllocateEntities(ReadOnlySpan<byte>.Empty, 0, chunkIndex, rowIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Entity GetEntity(int chunkIndex, int rowIndex)
    {
        var chunk = _chunks[chunkIndex];
        var chunkBase = chunk.GetUnsafePtr();
        var src = chunkBase + _entityIdsOffset + (sizeof(Entity) * rowIndex);

        return *(Entity*)src;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetEntity(int chunkIndex, int rowIndex, Entity entity)
    {
        var chunk = _chunks[chunkIndex];
        var chunkBase = chunk.GetUnsafePtr();
        var dst = chunkBase + _entityIdsOffset + (sizeof(Entity) * rowIndex);

        MemoryUtility.MemCpy(dst, &entity, (nuint)sizeof(Entity));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Error SetComponentData(int chunkIndex, int rowIndex, Identifier<IComponent> componentID, void* pComponent)
    {
#if GHOST_SAFETY_CHECKS
        if (ComponentRegistry.GetComponentInfo(componentID).isShared)
        {
            return Error.InvalidArgument;
        }
#endif

        var r = GetLayout(componentID);
        if (r.Error != Error.None)
        {
            return r.Error;
        }

        var offset = r.Value.offset;
        ref var chunk = ref _chunks[chunkIndex];

        var chunkBase = chunk.GetUnsafePtr();
        var size = ComponentRegistry.GetComponentInfo(componentID).size;
        var dst = chunkBase + offset + (size * rowIndex);

        MemoryUtility.MemCpy(dst, pComponent, (nuint)size);

        var world = World.GetWorldUncheck(_worldID);
        MarkChanged(chunkIndex, componentID, world.Version);

        return Error.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetComponentData(int chunkIndex, int rowIndex, Identifier<IComponent> componentID)
    {
#if GHOST_SAFETY_CHECKS
        if (ComponentRegistry.GetComponentInfo(componentID).isShared)
        {
            return null;
        }
#endif

        var r = GetLayout(componentID);
        if (r.Error != Error.None)
        {
            return null;
        }

        var offset = r.Value.offset;
        var chunk = _chunks[chunkIndex];

        var chunkBase = chunk.GetUnsafePtr();
        var size = ComponentRegistry.GetComponentInfo(componentID).size;
        return chunkBase + offset + (size * rowIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Chunk GetChunkReference(int index)
    {
        return ref _chunks[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Result<ComponentMemoryLayout, Error> GetLayout(int componentID)
    {
        if (componentID >= _componentIDToLayoutIndex.Count)
        {
            return Error.InvalidArgument;
        }

        var layoutIndex = _componentIDToLayoutIndex[componentID];
        if (layoutIndex == -1)
        {
            return Error.NotFound;
        }

        return _layouts[layoutIndex];
    }

    /// <summary>Returns the shared component layout for the given component ID, or an error if not found.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Result<SharedComponentLayout, Error> GetSharedLayout(int componentID)
    {
        for (var i = 0; i < _sharedLayouts.Count; i++)
        {
            if (_sharedLayouts[i].componentID == componentID)
            {
                return _sharedLayouts[i];
            }
        }

        return Error.NotFound;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Error MarkChanged(int chunkIndex, int componentTypeId, uint globalVersion)
    {
        var layoutResult = GetLayout(componentTypeId);
        if (layoutResult.IsFailure)
        {
            return layoutResult.Error;
        }

        ref var chunk = ref _chunks[chunkIndex];
        chunk.GetVersionUnsafePtr()[layoutResult.Value.versionIndex] = globalVersion;

        return Error.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Result<uint, Error> GetVersion(int chunkIndex, int componentTypeId)
    {
        var layoutResult = GetLayout(componentTypeId);
        if (layoutResult.Error != Error.None)
        {
            return layoutResult.Error;
        }

        ref var chunk = ref _chunks[chunkIndex];
        return chunk.GetVersionUnsafePtr()[layoutResult.Value.versionIndex];
    }

    public Error RemoveEntity(int chunkIndex, int rowIndex)
    {
        if (chunkIndex < 0 || chunkIndex >= _chunks.Count)
        {
            return Error.InvalidArgument;
        }

        var world = World.GetWorldUncheck(_worldID);
        ref var chunk = ref _chunks[chunkIndex];
        var lastIndex = chunk._count - 1;

        // If we are NOT removing the very last entity, we must swap.
        if (rowIndex != lastIndex)
        {
            var chunkBase = chunk.GetUnsafePtr();
            var pLastEntity = chunkBase + _entityIdsOffset + (sizeof(Entity) * lastIndex);
            var pRowEntity = chunkBase + _entityIdsOffset + (sizeof(Entity) * rowIndex);

            var error = world.EntityManager.UpdateEntityLocation(*(Entity*)pLastEntity, _id, chunkIndex, rowIndex);
            if (error != Error.None)
            {
                return error;
            }

            // Only operate the swap back after the update is succeed.
            MemoryUtility.MemCpy(pRowEntity, pLastEntity, (nuint)sizeof(Entity));

            for (var i = 0; i < _layouts.Count; i++)
            {
                var layout = _layouts[i];

                var pRow = chunk.GetUnsafePtr() + layout.offset + (layout.size * rowIndex);
                var pLast = chunk.GetUnsafePtr() + layout.offset + (layout.size * lastIndex);

                MemoryUtility.MemCpy(pRow, pLast, (nuint)layout.size);
            }
        }

        chunk._count--;
        chunk._structuralVersion = world.Version;

        return Error.None;
    }

    public Error RemoveEntities(int chunkIndex, ReadOnlySpan<int> sortedIndicesToRemove)
    {
        if (chunkIndex < 0 || chunkIndex >= _chunks.Count)
        {
            return Error.InvalidArgument;
        }

        if (sortedIndicesToRemove.Length == 0)
        {
            return Error.None;
        }

        ref var chunk = ref _chunks[chunkIndex];

        var oldCount = chunk._count;
        var removeCount = sortedIndicesToRemove.Length;
        var newCount = oldCount - removeCount; // The boundary between "Keep" and "Drop"

        var chunkBase = chunk.GetUnsafePtr();
        var world = World.GetWorldUncheck(_worldID); // Typo fixed from 'wrold'

        // Pointers for the swap logic
        // 'holePtr' tracks which index in the sorted list we are processing
        var holePtr = 0;

        // 'candidateIndex' starts at the end of the OLD array and moves backward
        var candidateIndex = oldCount - 1;

        // 'removalTailPtr' tracks removals at the end of the array to skip them
        var removalTailPtr = sortedIndicesToRemove.Length - 1;

        // Iterate through the holes that are strictly INSIDE the new valid range
        while (holePtr < removeCount)
        {
            var holeIndex = sortedIndicesToRemove[holePtr];

            // If the current hole is beyond the new count, it's in the "Drop Zone".
            // Since the list is sorted, all subsequent holes are also in the drop zone.
            // We are done filling holes.
            if (holeIndex >= newCount)
                break;

            // --- Find a Valid Filler ---
            // We look for an entity at the end of the array that IS NOT scheduled for removal.
            while (candidateIndex >= newCount)
            {
                // Check if the current candidate is actually marked for removal
                var isCandidateRemoved = false;

                // Because sortedIndices is sorted, we check the end of the list 
                // to see if the candidateIndex matches a removal request.
                if (removalTailPtr >= 0 && sortedIndicesToRemove[removalTailPtr] == candidateIndex)
                {
                    isCandidateRemoved = true;
                    removalTailPtr--; // Consume this removal
                }

                if (!isCandidateRemoved)
                {
                    break;
                }

                // This candidate was also removed, so skip it and keep looking left
                candidateIndex--;
            }

            // Move 'candidateIndex' (Filler) into 'holeIndex' (Hole)

            var pFillerEntity = chunkBase + _entityIdsOffset + (sizeof(Entity) * candidateIndex);
            var pHoleEntity = chunkBase + _entityIdsOffset + (sizeof(Entity) * holeIndex);

            // Update the Map
            // We tell the world: "The entity that WAS at 'candidateIndex' is now at 'holeIndex'"
            var result = world.EntityManager.UpdateEntityLocation(*(Entity*)pFillerEntity, _id, chunkIndex, holeIndex);
            if (result != Error.None)
            {
                return result;
            }

            // Overwrite entity id and components
            MemoryUtility.MemCpy(pHoleEntity, pFillerEntity, (nuint)sizeof(Entity));

            for (var i = 0; i < _layouts.Count; i++)
            {
                var layout = _layouts[i];
                var pRow = chunkBase + layout.offset + (layout.size * holeIndex);
                var pLast = chunkBase + layout.offset + (layout.size * candidateIndex);
                MemoryUtility.MemCpy(pRow, pLast, (nuint)layout.size);
            }

            // Prepare for next hole
            holePtr++;
            candidateIndex--;
        }

        chunk._count = newCount;
        chunk._structuralVersion = world.Version;

        return Error.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasComponent(Identifier<IComponent> componentID)
    {
        return _signature.IsSet(componentID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdgeAdd(Identifier<IComponent> componentID, Identifier<Archetype> targetArchetype)
    {
        _edgesAdd.TryAdd(componentID, targetArchetype);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Identifier<Archetype> GetEdgeAdd(Identifier<IComponent> componentID)
    {
        return _edgesAdd.GetValueOrDefault(componentID, -1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdgeRemove(Identifier<IComponent> componentID, Identifier<Archetype> targetArchetype)
    {
        _edgesRemove.TryAdd(componentID, targetArchetype);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Identifier<Archetype> GetEdgeRemove(Identifier<IComponent> componentID)
    {
        return _edgesRemove.GetValueOrDefault(componentID, -1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Collect()
    {
        for (var i = 0; i < _chunks.Count; i++)
        {
            if (_chunks[i]._count == 0)
            {
                ref var chunk = ref _chunks[i];
                ref var group = ref _chunkGroups[chunk._groupIndex];
                // How can we set the activeChunkIndex?
                group.refCount--;
                if (group.activeChunkIndex == i)
                {
                    group.activeChunkIndex = -1;
                }

                chunk.Dispose();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override readonly int GetHashCode()
    {
        return _hash;
    }

    public void Dispose()
    {
        for (var i = 0; i < _chunks.Count; i++)
        {
            _chunks[i].Dispose();
        }

        for (var i = 0; i < _chunkGroups.Count; i++)
        {
            _chunkGroups[i].Dispose();
        }

        _signature.Dispose();
        _chunks.Dispose();
        _componentIDToLayoutIndex.Dispose();
        _layouts.Dispose();
        _sharedLayouts.Dispose();
        _chunkGroups.Dispose();

        _edgesAdd.Dispose();
        _edgesRemove.Dispose();
    }
}
