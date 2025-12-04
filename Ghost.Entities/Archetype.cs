using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

internal unsafe struct Chuck : IDisposable
{
    public const int CHUNK_SIZE = 16384; // 16 KB

    private UnsafeArray<byte> _data;
    private int _count;
    private int _capacity;

    public int Count
    {
        get => _count;
        set => _count = value;
    }

    public int Capacity => _capacity;

    public Chuck(int size, int capacity)
    {
        _data = new UnsafeArray<byte>(size, Allocator.Persistent);
        _capacity = capacity;
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte* GetUnsafePtr()
    {
        return (byte*)_data.GetUnsafePtr();
    }

    public void Dispose()
    {
        _data.Dispose();
    }
}

internal struct Edge
{
    public Identifier<IComponent> componentID;
    public int targetArchetype; // can't use Identifier<Archetype> because cycle causer
}

internal struct ComponentMemoryLayout
{
    public int offset;
    public int size;
    public Identifier<IComponent> componentID;
}

internal unsafe struct Archetype : IIdentifierType, IDisposable
{
    private readonly Identifier<Archetype> _id;
    private readonly Identifier<World> _worldID;

    private UnsafeBitSet _signature;
    private UnsafeList<Chuck> _chunks;
    private UnsafeArray<ComponentMemoryLayout> _layouts;
    private UnsafeArray<int> _componentIDToOffset;

    // TODO: Is hash map better?
    private UnsafeList<Edge> _edgesAdd;
    private UnsafeList<Edge> _edgesRemove;

    private int _hash;
    private int _entityCapacity;
    private int _maxComponentID;
    private int _entityIdsOffset;

    public Identifier<Archetype> ID => _id;

    public UnsafeBitSet Signature => _signature;
    public UnsafeList<Chuck> Chunks => _chunks;
    public UnsafeArray<ComponentMemoryLayout> Layouts => _layouts;

    public int EntityCapacity => _entityCapacity;
    public int ChunkCount => _chunks.Count;

    public Archetype(Identifier<Archetype> id, Identifier<World> worldID, ReadOnlySpan<Identifier<IComponent>> componentIds)
    {
        _id = id;
        _worldID = worldID;

        if (componentIds.IsEmpty)
        {
            _signature = new UnsafeBitSet(1, Allocator.Persistent, AllocationOption.Clear);
            _chunks = new UnsafeList<Chuck>(4, Allocator.Persistent);

            _edgesAdd = new UnsafeList<Edge>(4, Allocator.Persistent);
            _edgesRemove = new UnsafeList<Edge>(4, Allocator.Persistent);

            _signature.ClearAll();

            _entityCapacity = Chuck.CHUNK_SIZE / sizeof(Entity);

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
        _chunks = new UnsafeList<Chuck>(4, Allocator.Persistent);

        _edgesAdd = new UnsafeList<Edge>(4, Allocator.Persistent);
        _edgesRemove = new UnsafeList<Edge>(4, Allocator.Persistent);

        _hash = _signature.GetHashCode();

        var pComponents = stackalloc ComponentInfo[componentIds.Length];
        for (var i = 0; i < componentIds.Length; i++)
        {
            _signature.SetBit(componentIds[i]);
            pComponents[i] = ComponentRegister.GetComponentInfo(componentIds[i]);
        }

        CalculateLayout(new Span<ComponentInfo>(pComponents, componentIds.Length));
    }

    private void CalculateLayout(Span<ComponentInfo> components)
    {
        var entitySize = sizeof(Entity);
        var entityAlign = (int)MemoryUtility.AlignOf<Entity>();

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
        _entityCapacity = Chuck.CHUNK_SIZE / bytesPerEntity;
        _layouts = new UnsafeArray<ComponentMemoryLayout>(components.Length, Allocator.Persistent);
        _componentIDToOffset = new UnsafeArray<int>(_maxComponentID + 1, Allocator.Persistent);

        _componentIDToOffset.AsSpan().Fill(-1);

        components.Sort((a, b) => b.alignment.CompareTo(a.alignment));
        var tempOffsets = stackalloc int[components.Length];

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

                if (currentOffset > Chuck.CHUNK_SIZE)
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
                        offset = tempOffsets[i],
                        size = components[i].size,
                        componentID = components[i].id
                    };

                    _componentIDToOffset[components[i].id] = tempOffsets[i];
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
            var chunk = _chunks[i];
            if (chunk.Count < _entityCapacity)
            {
                rowIndex = chunk.Count;
                chunk.Count++;
                chunkIndex = i;

                return;
            }
        }

        // Need to allocate a new chunk
        var newChunk = new Chuck(Chuck.CHUNK_SIZE, _entityCapacity);

        rowIndex = 0;
        newChunk.Count++;
        chunkIndex = _chunks.Count;

        _chunks.Add(newChunk);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetEntity(int chunkIndex, int rowIndex, Entity entity)
    {
        var chunk = _chunks[chunkIndex];
        var chunkBase = chunk.GetUnsafePtr();
        var pEntity = chunkBase + _entityIdsOffset + (sizeof(Entity) * rowIndex);

        MemoryUtility.MemCpy(&entity, pEntity, (nuint)sizeof(Entity));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetComponentData(int chunkIndex, int rowIndex, Identifier<IComponent> componentID, void* pComponent)
    {
        var offset = _componentIDToOffset[componentID];
        var chunk = _chunks[chunkIndex];

        var chunkBase = chunk.GetUnsafePtr();
        var size = ComponentRegister.GetComponentInfo(componentID).size;
        var dst = chunkBase + offset + (size * rowIndex);

        MemoryUtility.MemCpy(pComponent, dst, (nuint)size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref Chuck GetChunkReference(int index)
    {
        return ref _chunks[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOffset(int componentId)
    {
        if (componentId >= _componentIDToOffset.Count)
        {
            return -1;
        }

        return _componentIDToOffset[componentId];
    }

    public ResultStatus RemoveEntity(int chunkIndex, int rowIndex)
    {
        if (chunkIndex < 0 || chunkIndex >= _chunks.Count)
        {
            return ResultStatus.InvalidArgument;
        }

        ref var chunk = ref _chunks[chunkIndex];
        var lastIndex = chunk.Count - 1;

        // If we are NOT removing the very last entity, we must swap.
        if (rowIndex != lastIndex)
        {
            var chunkBase = chunk.GetUnsafePtr();
            var pLastEntity = chunkBase + _entityIdsOffset + (sizeof(Entity) * lastIndex);
            var pRowEntity = chunkBase + _entityIdsOffset + (sizeof(Entity) * rowIndex);

            var wroldResult = World.GetWorld(_worldID);
            if (wroldResult.Status != ResultStatus.Success)
            {
                return wroldResult.Status;
            }

            var result = wroldResult.Value.EntityManager.UpdateEntityLocation(*(Entity*)pLastEntity, _id, chunkIndex, rowIndex);
            if (result != ResultStatus.Success)
            {
                return result;
            }

            // Only operate the swap back after the update is succeed.
            MemoryUtility.MemCpy(pLastEntity, pRowEntity, (nuint)sizeof(Entity));

            for (var i = 0; i <= _layouts.Count; i++)
            {
                var layout = _layouts[i];

                var pRow = chunk.GetUnsafePtr() + layout.offset + (layout.size * rowIndex);
                var pLast = chunk.GetUnsafePtr() + layout.offset + (layout.size * lastIndex);

                MemoryUtility.MemCpy(pLast, pRow, (nuint)layout.size);
            }
        }

        chunk.Count--;
        return ResultStatus.Success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(Identifier<IComponent> componentID)
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
    public Identifier<Archetype> GetEdgeAdd(Identifier<IComponent> componentID)
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
    public Identifier<Archetype> GetEdgeRemove(Identifier<IComponent> componentID)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetComponentArray<T>(int chunkIndex)
        where T : unmanaged, IComponent
    {
        var id = ComponentTypeID<T>.value;
        if (id >= _componentIDToOffset.Count)
        {
            return default;
        }

        var offset = _componentIDToOffset[id];
        if (offset == -1)
        {
            return default;
        }

        var chunk = _chunks[chunkIndex];
        return new Span<T>((T*)((byte*)chunk.GetUnsafePtr() + offset), chunk.Count);
    }

    public override int GetHashCode()
    {
        return _hash;
    }

    public void Dispose()
    {
        if (_chunks.IsCreated)
        {
            foreach (ref var chunk in _chunks)
            {
                chunk.Dispose();
            }
        }

        _signature.Dispose();
        _chunks.Dispose();
        _componentIDToOffset.Dispose();
        _layouts.Dispose();
        _edgesAdd.Dispose();
        _edgesRemove.Dispose();
    }
}
