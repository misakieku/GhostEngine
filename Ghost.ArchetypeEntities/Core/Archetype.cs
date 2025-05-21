using Ghost.Entities.Helpers;
using Misaki.HighPerformance.Unsafe.Collections;
using Misaki.HighPerformance.Unsafe.Helpers;

namespace Ghost.Entities.Core;

internal partial struct Archetype : IDisposable
{
    private const int _BUCKET_SIZE = 16;

    // The component ID to array index lookup array.
    private UnsafeArray<int> _lookupArray;

    public readonly int sizeOfBaseChunk;
    public readonly int sizeOfChunk;

    public readonly int entitiesPerChunk;
    public int totalEntityCount;

    public Signature signature;
    // For fast lookups
    public BitSet bitSet;
    public ChunkCollection chunks;


    private UnsafeHashSet<Archetype> _insertionEdges;
    private UnsafeHashSet<Archetype> _deletionEdges;

    public readonly UnsafeArray<int> LookupArray => _lookupArray;

    internal Archetype(Signature signature, int sizeOfBaseChunk, int minimalEntityCountPerChunk)
    {
        this.signature = signature;
        this.sizeOfBaseChunk = sizeOfBaseChunk;

        var sizeOfTotalData = Component.GetTotalSize(signature.componentDatas.AsSpan());
        sizeOfChunk = GetSizeOfChunk(sizeOfBaseChunk, minimalEntityCountPerChunk, sizeOfTotalData);
        entitiesPerChunk = GetEntityCount(sizeOfChunk, sizeOfTotalData);

        _lookupArray = Component.ToLookupArray(signature.componentDatas.AsSpan(), Allocator.Persistent);
        bitSet = new BitSet();
        for (var i = 0; i < signature.componentDatas.Count; i++)
        {
            bitSet.SetBit(signature.componentDatas[i].id);
        }

        chunks = new ChunkCollection(1);
        AddNewChunk();
    }

    private unsafe static int GetEntityCount(int sizeOfChunk, int sizeOfTotalData)
    {
        return sizeOfChunk / (sizeof(Entity) + sizeOfTotalData);
    }

    private static unsafe int GetSizeOfChunk(int sizeOfBaseChunk, int entityCount, int sizeOfTotalData)
    {
        var entityBytes = (sizeof(Entity) + sizeOfTotalData) * entityCount;
        return (int)Math.Ceiling((float)entityBytes / sizeOfBaseChunk) * sizeOfBaseChunk;  // Calculates and rounds to a multiple of BaseSize to store the number of entities
    }

    public ref Chunk AddNewChunk()
    {
        chunks.EnsureCapacity(chunks.Count + 1);
        chunks.Add(new Chunk(entitiesPerChunk, signature.componentDatas.AsSpan(), _lookupArray));
        return ref chunks[^1];
    }

    public void Dispose()
    {
        _lookupArray.Dispose();
        signature.Dispose();
        bitSet.Dispose();
        chunks.Dispose();
    }
}
