using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.ArcEntities;

internal unsafe struct Archetype : IDisposable
{
    private UnsafeArray<int> _offsets;
    private UnsafeArray<int> _componentIDs;
    private UnsafeArray<int> _componentIDToOffset;
    private int _entityCapacity;
    private int _maxComponentID;

    private UnsafeList<Chuck> _chunks;

    public int EntityCapacity => _entityCapacity;

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
        _maxComponentID = 0;
        for (var i = 0; i < components.Length; i++)
        {
            bytesPerEntity += components[i].size;
            if (components[i].id > _maxComponentID)
            {
                _maxComponentID = components[i].id;
            }
        }

        _entityCapacity = Chuck.CHUNK_SIZE / bytesPerEntity;
        _componentIDToOffset = new UnsafeArray<int>(_maxComponentID + 1, Allocator.Persistent);
        for (var i = 0; i < _componentIDToOffset.Count; ++i) _componentIDToOffset[i] = -1;

        while (_entityCapacity > 0)
        {
            var currentOffset = 0;
            var fits = true;

            currentOffset = (currentOffset + entityAlign - 1) & ~(entityAlign - 1);
            currentOffset += _entityCapacity * entitySize;

            var entityOffset = currentOffset;
            var tempOffsets = new int[components.Length];

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
                    _componentIDToOffset[components[i].id] = tempOffsets[i];
                }

                return;
            }

            _entityCapacity--;
        }
    }

    public Chuck CreateChunk()
    {
        var chunk = new Chuck(Chuck.CHUNK_SIZE, _entityCapacity);
        _chunks.Add(chunk);

        return chunk;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnsafeArray<T> GetComponentArray<T>(int chunkIndex)
        where T : unmanaged
    {
        var id = ComponentType<T>.id;
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
