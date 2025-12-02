using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.ArcEntities;

public unsafe struct Archetype : IDisposable
{
    private UnsafeList<Chuck> _chunks;
    private UnsafeArray<int> _offsets;
    private UnsafeArray<int> _componentIDToOffset;
    private int _entityCapacity;
    private int _maxComponentID;
    private int _entityIdsOffset;

    public int EntityCapacity => _entityCapacity;
    public int ChunkCount => _chunks.Count;

    public Archetype(ReadOnlySpan<ComponentInfo> components)
    {
        _chunks = new UnsafeList<Chuck>(4, Allocator.Persistent);
        CalculateLayout(components);
    }

    private void CalculateLayout(ReadOnlySpan<ComponentInfo> components)
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
        _offsets = new UnsafeArray<int>(components.Length, Allocator.Persistent);
        _componentIDToOffset = new UnsafeArray<int>(_maxComponentID + 1, Allocator.Persistent);

        _componentIDToOffset.AsSpan().Fill(-1);

        var sortedComponents = new ComponentInfo[components.Length];
        components.CopyTo(sortedComponents);
        Array.Sort(sortedComponents, (a, b) => b.alignment.CompareTo(a.alignment));

        while (_entityCapacity > 0)
        {
            var currentOffset = 0;
            var fits = true;

            currentOffset = (currentOffset + entityAlign - 1) & ~(entityAlign - 1);
            currentOffset += _entityCapacity * entitySize;

            _entityIdsOffset = currentOffset;
            var tempOffsets = stackalloc int[components.Length];

            for (var i = 0; i < sortedComponents.Length; i++)
            {
                var size = sortedComponents[i].size;
                var align = sortedComponents[i].alignment;

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
                for (var i = 0; i < sortedComponents.Length; i++)
                {
                    _offsets[i] = tempOffsets[i];
                    _componentIDToOffset[sortedComponents[i].id] = tempOffsets[i];
                }

                return;
            }

            _entityCapacity--;
        }
    }

    internal ref Chuck GetChunkReference(int index)
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

    public void RemoveEntity(int chunkIndex, int rowIndex)
    {
        var chunk = _chunks[chunkIndex];
        int lastIndex = chunk.Count - 1;

        // 1. If we are NOT removing the very last entity, we must swap.
        if (rowIndex != lastIndex)
        {
            // A. We are moving the 'last' entity into the 'row' spot.
            // We need to know WHO that last entity is so we can update the lookup map.

            var pLastEntity = chunk.GetUnsafePtr() + _entityIdsOffset + (sizeof(Entity) * lastIndex);
            var lastEntity = *(Entity*)pLastEntity;

            // B. Now we can update the map
            // World.UpdateLocation(lastEntity.ID, newIndex: rowIndex);

            // C. Perform the memory copy (Swap components)
            for (var i = 0; i <= _offsets.Count; i++)
            {
                var offset = _offsets[i];
                var compSize = ComponentRegister.GetComponentInfo(i).size;

                var pRow = chunk.GetUnsafePtr() + offset + (compSize * rowIndex);
                var pLast = chunk.GetUnsafePtr() + offset + (compSize * lastIndex);

                MemoryUtility.MemCpy(pLast, pRow, (nuint)compSize);
            }
        }

        chunk.Count--;
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
        _componentIDToOffset.Dispose();

        foreach (var chunk in _chunks)
        {
            chunk.Dispose();
        }

        _chunks.Dispose();
    }
}
