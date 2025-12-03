using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.ArcEntities;

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
    public int componentID;
    public Identifier<Archetype> targetArchetype;
}

internal unsafe struct Archetype : IIdentifierType, IDisposable
{
    private struct ComponentMemoryLayout
    {
        public int offset;
        public int size;
    }

    private readonly Identifier<Archetype> _id;
    private readonly Identifier<World> _worldID;

    private UnsafeBitSet _signature;
    private UnsafeList<Chuck> _chunks;
    private UnsafeArray<ComponentMemoryLayout> _layouts;
    private UnsafeArray<int> _componentIDToOffset;

    private UnsafeList<Edge> _edgesAdd;
    private UnsafeList<Edge> _edgesRemove;

    private int _entityCapacity;
    private int _maxComponentID;
    private int _entityIdsOffset;

    public UnsafeBitSet Signature => _signature;
    public int EntityCapacity => _entityCapacity;
    public int ChunkCount => _chunks.Count;

    public Archetype(Identifier<Archetype> id, Identifier<World> worldID, ReadOnlySpan<int> componentIds)
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

        _signature = new UnsafeBitSet(highestComponentID, Allocator.Persistent, AllocationOption.Clear);
        _chunks = new UnsafeList<Chuck>(4, Allocator.Persistent);

        _edgesAdd = new UnsafeList<Edge>(4, Allocator.Persistent);
        _edgesRemove = new UnsafeList<Edge>(4, Allocator.Persistent);

        var pComponents = stackalloc ComponentInfo[componentIds.Length];
        for (var i = 0; i < componentIds.Length; i++)
        {
            _signature.SetBit(componentIds[i]);
            pComponents[i] = ComponentRegister.s_registeredComponents[componentIds[i]];
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
            bytesPerEntity += components[i].size;
            if (components[i].id > maxComponentID)
            {
                maxComponentID = components[i].id;
            }
        }

        _maxComponentID = maxComponentID;
        _entityCapacity = Chuck.CHUNK_SIZE / bytesPerEntity;
        _layouts = new UnsafeArray<ComponentMemoryLayout>(components.Length, Allocator.Persistent);
        _componentIDToOffset = new UnsafeArray<int>(_maxComponentID + 1, Allocator.Persistent);

        _componentIDToOffset.AsSpan().Fill(-1);

        components.Sort((a, b) => b.alignment.CompareTo(a.alignment));

        while (_entityCapacity > 0)
        {
            var currentOffset = 0;
            var fits = true;

            currentOffset = (currentOffset + entityAlign - 1) & ~(entityAlign - 1);

            _entityIdsOffset = currentOffset;
            currentOffset += _entityCapacity * entitySize;

            var tempOffsets = stackalloc int[components.Length];

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
                        size = components[i].size
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
        _chunks.Add(newChunk);

        rowIndex = 0;
        newChunk.Count++;
        chunkIndex = _chunks.Count - 1;
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
        int lastIndex = chunk.Count - 1;

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
    public void AddEdgeAdd(int componentID, Identifier<Archetype> targetArchetype)
    {
        _edgesAdd.Add(new Edge
        {
            componentID = componentID,
            targetArchetype = targetArchetype
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<int, ResultStatus> GetEdgeAdd(int componentID)
    {
        for (var i = 0; i < _edgesAdd.Count; i++)
        {
            if (_edgesAdd[i].componentID == componentID)
            {
                return Result.Create(_edgesAdd[i].targetArchetype.value, ResultStatus.Success);
            }
        }

        return Result.Create(-1, ResultStatus.NotFound);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdgeRemove(int componentID, Identifier<Archetype> targetArchetype)
    {
        _edgesRemove.Add(new Edge
        {
            componentID = componentID,
            targetArchetype = targetArchetype
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Result<int, ResultStatus> GetEdgeRemove(int componentID)
    {
        for (var i = 0; i < _edgesRemove.Count; i++)
        {
            if (_edgesRemove[i].componentID == componentID)
            {
                return Result.Create(_edgesRemove[i].targetArchetype.value, ResultStatus.Success);
            }
        }

        return Result.Create(-1, ResultStatus.NotFound);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<T> GetComponentArray<T>(int chunkIndex)
        where T : unmanaged
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
        return new UnsafeArray<T>((T*)((byte*)chunk.GetUnsafePtr() + offset), _entityCapacity);
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
