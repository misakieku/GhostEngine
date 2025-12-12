using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

internal unsafe sealed class ChunkDebugView
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
#if !(DEBUG || GHOST_EDITOR)
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
        var r = World.GetWorld(worldID);
        if (!r)
        {
            return [];
        }

        ref var archetype = ref r.Value.GetArchetypeReference(archetypeID);
        var it = archetype._signature.GetIterator();
        while (it.Next(out var index))
        {
            var type = ComponentRegister.s_runtimeIDToType[index];
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
    private UnsafeArray<int> _versions;

    internal int _count;
    internal readonly int _capacity;

#if DEBUG || GHOST_EDITOR
    // For debugging purpose
    internal int _worldID;
    internal int _archetypeID;
#endif

    public Chunk(int bufferSize, int capacity, int componentCount, int globalVersion)
    {
        _data = new UnsafeArray<byte>(bufferSize, Allocator.Persistent, AllocationOption.Clear);
        _versions = new UnsafeArray<int>(componentCount, Allocator.Persistent);
        _capacity = capacity;
        _count = 0;

        _versions.AsSpan().Fill(globalVersion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte* GetUnsafePtr()
    {
        return (byte*)_data.GetUnsafePtr();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int* GetVersionUnsafePtr()
    {
        return (int*)_versions.GetUnsafePtr();
    }

    public void Dispose()
    {
        _data.Dispose();
        Console.WriteLine($"Disposing chunk data");
        _versions.Dispose();
        Console.WriteLine($"Disposing chunk versions");
    }
}

internal unsafe struct Archetype : IIdentifierType, IDisposable
{
    internal struct ComponentMemoryLayout
    {
        public int componentID;
        public int size;
        public int offset;
        public int enableBitsOffset;
        public int versionIndex;
    }

    private struct Edge
    {
        public int componentID;
        public int targetArchetype; // can't use Identifier<Archetype> because cycle causer
    }

    internal UnsafeBitSet _signature;
    internal UnsafeList<Chunk> _chunks;
    internal UnsafeArray<ComponentMemoryLayout> _layouts;

    private UnsafeArray<int> _componentIDToLayoutIndex;
    // TODO: Is hash map better?
    private UnsafeList<Edge> _edgesAdd;
    private UnsafeList<Edge> _edgesRemove;

    private readonly Identifier<Archetype> _id;
    private readonly Identifier<World> _worldID;

    private readonly int _hash;
    private int _entityCapacity;
    private int _maxComponentID;
    private int _entityIdsOffset;

    public readonly Identifier<Archetype> ID => _id;
    public readonly Identifier<World> WorldID => _worldID;

    public readonly int EntityCapacity => _entityCapacity;
    public readonly int ChunkCount => _chunks.Count;
    public readonly int EntityIDsOffset => _entityIdsOffset;

    public Archetype(Identifier<Archetype> id, Identifier<World> worldID, ReadOnlySpan<Identifier<IComponent>> componentIds)
    {
        _id = id;
        _worldID = worldID;

        _chunks = new UnsafeList<Chunk>(4, Allocator.Persistent);
        _edgesAdd = new UnsafeList<Edge>(4, Allocator.Persistent);
        _edgesRemove = new UnsafeList<Edge>(4, Allocator.Persistent);

        if (componentIds.IsEmpty)
        {
            _signature = new UnsafeBitSet(1, Allocator.Persistent, AllocationOption.Clear);
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

        _signature = new UnsafeBitSet(highestComponentID + 1, Allocator.Persistent, AllocationOption.Clear);
        _hash = _signature.GetHashCode();

        CalculateLayout(componentIds);
    }

    private void CalculateLayout(ReadOnlySpan<Identifier<IComponent>> componentIds)
    {
        var entitySize = sizeof(Entity);
        var entityAlign = (int)MemoryUtility.AlignOf<Entity>();

        var components = (Span<ComponentInfo>)stackalloc ComponentInfo[componentIds.Length];
        for (var i = 0; i < componentIds.Length; i++)
        {
            _signature.SetBit(componentIds[i]);
            components[i] = ComponentRegister.GetComponentInfo(componentIds[i]);
        }

        // Calculate total size per entity to get an initial capacity estimate
        var bytesPerEntity = entitySize;
        var maxComponentID = 0;
        for (var i = 0; i < components.Length; i++)
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
        _layouts = new UnsafeArray<ComponentMemoryLayout>(components.Length, Allocator.Persistent);
        _componentIDToLayoutIndex = new UnsafeArray<int>(_maxComponentID + 1, Allocator.Persistent);

        _componentIDToLayoutIndex.AsSpan().Fill(-1);

        components.Sort((a, b) => b.alignment.CompareTo(a.alignment));
        var tempOffsets = stackalloc int[components.Length];
        var tempBitmaskOffsets = stackalloc int[components.Length];

        while (_entityCapacity > 0)
        {
            var currentOffset = 0;
            var fits = true;

            currentOffset = (currentOffset + entityAlign - 1) & ~(entityAlign - 1);

            _entityIdsOffset = currentOffset;
            currentOffset += _entityCapacity * entitySize;

            for (var i = 0; i < components.Length; i++)
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
                for (var i = 0; i < components.Length; i++)
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

    public void AllocateEntity(out int chunkIndex, out int rowIndex)
    {
        for (var i = 0; i < _chunks.Count; i++)
        {
            ref var chunk = ref _chunks[i];
            if (chunk._count < _entityCapacity)
            {
                rowIndex = chunk._count;
                chunk._count++;
                chunkIndex = i;

                return;
            }
        }

        var world = World.GetWorldUncheck(_worldID);

        // Need to allocate a new chunk
        var newChunk = new Chunk(Chunk.CHUNK_BUFFER_SIZE, _entityCapacity, _layouts.Count, world.Version);
#if DEBUG || GHOST_EDITOR
        newChunk._worldID = _worldID;
        newChunk._archetypeID = _id;
#endif

        // Set all enable to true by default for enableable components
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

        rowIndex = 0;
        newChunk._count++;
        chunkIndex = _chunks.Count;

        _chunks.Add(newChunk);
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
    public readonly ErrorStatus SetComponentData(int chunkIndex, int rowIndex, Identifier<IComponent> componentID, void* pComponent)
    {
        var r = GetLayout(componentID);
        if (r.Error != ErrorStatus.None)
        {
            return r.Error;
        }

        var offset = r.Value.offset;
        ref var chunk = ref _chunks[chunkIndex];

        var chunkBase = chunk.GetUnsafePtr();
        var size = ComponentRegister.GetComponentInfo(componentID).size;
        var dst = chunkBase + offset + (size * rowIndex);

        MemoryUtility.MemCpy(dst, pComponent, (nuint)size);

        var world = World.GetWorldUncheck(_worldID);
        MarkChanged(chunkIndex, componentID, world.Version);

        return ErrorStatus.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void* GetComponentData(int chunkIndex, int rowIndex, Identifier<IComponent> componentID)
    {
        var r = GetLayout(componentID);
        if (r.Error != ErrorStatus.None)
        {
            return null;
        }

        var offset = r.Value.offset;
        var chunk = _chunks[chunkIndex];

        var chunkBase = chunk.GetUnsafePtr();
        var size = ComponentRegister.GetComponentInfo(componentID).size;
        return chunkBase + offset + (size * rowIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Chunk GetChunkReference(int index)
    {
        return ref _chunks[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Result<ComponentMemoryLayout, ErrorStatus> GetLayout(int componentID)
    {
        if (componentID >= _componentIDToLayoutIndex.Count)
        {
            return ErrorStatus.InvalidArgument;
        }

        var layoutIndex = _componentIDToLayoutIndex[componentID];
        if (layoutIndex == -1)
        {
            return ErrorStatus.NotFound;
        }

        return _layouts[layoutIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ErrorStatus MarkChanged(int chunkIndex, int componentTypeId, int globalVersion)
    {
        var layoutResult = GetLayout(componentTypeId);
        if (layoutResult.IsFailure)
        {
            return layoutResult.Error;
        }

        ref var chunk = ref _chunks[chunkIndex];
        chunk.GetVersionUnsafePtr()[layoutResult.Value.versionIndex] = globalVersion;

        return ErrorStatus.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Result<int, ErrorStatus> GetVersion(int chunkIndex, int componentTypeId)
    {
        var layoutResult = GetLayout(componentTypeId);
        if (layoutResult.Error != ErrorStatus.None)
        {
            return layoutResult.Error;
        }

        ref var chunk = ref _chunks[chunkIndex];
        return chunk.GetVersionUnsafePtr()[layoutResult.Value.versionIndex];
    }

    public ErrorStatus RemoveEntity(int chunkIndex, int rowIndex)
    {
        if (chunkIndex < 0 || chunkIndex >= _chunks.Count)
        {
            return ErrorStatus.InvalidArgument;
        }

        ref var chunk = ref _chunks[chunkIndex];
        var lastIndex = chunk._count - 1;

        // If we are NOT removing the very last entity, we must swap.
        if (rowIndex != lastIndex)
        {
            var chunkBase = chunk.GetUnsafePtr();
            var pLastEntity = chunkBase + _entityIdsOffset + (sizeof(Entity) * lastIndex);
            var pRowEntity = chunkBase + _entityIdsOffset + (sizeof(Entity) * rowIndex);

            var wrold = World.GetWorldUncheck(_worldID);
            var result = wrold.EntityManager.UpdateEntityLocation(*(Entity*)pLastEntity, _id, chunkIndex, rowIndex);
            if (result != ErrorStatus.None)
            {
                return result;
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
        return ErrorStatus.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasComponent(Identifier<IComponent> componentID)
    {
        return _signature.IsSet(componentID);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdgeAdd(Identifier<IComponent> componentID, Identifier<Archetype> targetArchetype)
    {
        _edgesAdd.Add(new Edge
        {
            componentID = componentID,
            targetArchetype = targetArchetype
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Identifier<Archetype> GetEdgeAdd(Identifier<IComponent> componentID)
    {
        for (var i = 0; i < _edgesAdd.Count; i++)
        {
            var edge = _edgesAdd[i];
            if (edge.componentID == componentID)
            {
                return edge.targetArchetype;
            }
        }

        return Identifier<Archetype>.Invalid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdgeRemove(Identifier<IComponent> componentID, Identifier<Archetype> targetArchetype)
    {
        _edgesRemove.Add(new Edge
        {
            componentID = componentID,
            targetArchetype = targetArchetype
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Identifier<Archetype> GetEdgeRemove(Identifier<IComponent> componentID)
    {
        for (var i = 0; i < _edgesRemove.Count; i++)
        {
            var edge = _edgesRemove[i];
            if (edge.componentID == componentID)
            {
                return edge.targetArchetype;
            }
        }

        return Identifier<Archetype>.Invalid;
    }

    public override readonly int GetHashCode()
    {
        return _hash;
    }

    public void Dispose()
    {
        if (_chunks.IsCreated)
        {
            var i= 0;
            foreach (ref var chunk in _chunks)
            {
                Console.WriteLine($"Disposing chunk {i} of archetype {_id}");
                chunk.Dispose();
                i++;
            }
        }

        _signature.Dispose();
        _chunks.Dispose();
        _componentIDToLayoutIndex.Dispose();
        _layouts.Dispose();
        _edgesAdd.Dispose();
        _edgesRemove.Dispose();
    }
}
