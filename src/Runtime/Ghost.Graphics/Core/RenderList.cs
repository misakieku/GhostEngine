using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Graphics.Core;

public record struct RenderRecord
{
    public float4x4 localToWorld;
    public Handle<Material> material;
    public Handle<Mesh> mesh;
    public RenderingLayerMask renderingLayerMask;
    public byte subMeshIndex;
}

public struct RenderList : IDisposable
{
    public unsafe ref struct Enumerator
    {
        private readonly UnsafeList<RenderRecord>* pList;
        private readonly int length;

        private int _listIndex;
        private int _itemIndex;

        internal Enumerator(RenderList List)
        {
            pList = (UnsafeList<RenderRecord>*)List._threadLocalRecords.GetUnsafePtr();
            length = List._threadLocalRecords.Length;

            _listIndex = 0;
            _itemIndex = -1;
        }

        public RenderRecord Current => pList[_listIndex][_itemIndex];

        public bool MoveNext()
        {
            while (_listIndex < length)
            {
                if (_itemIndex < pList[_listIndex].Count)
                {
                    _itemIndex++;
                    return true;
                }
                else
                {
                    _listIndex++;
                    _itemIndex = 0;
                }
            }

            return false;
        }

        public void Reset()
        {
            _listIndex = 0;
            _itemIndex = -1;
        }
    }

    private UnsafeArray<UnsafeList<RenderRecord>> _threadLocalRecords;

    public readonly int ThreadLocalCount => _threadLocalRecords.Length;
    public readonly bool IsCreated => _threadLocalRecords.IsCreated;

    public RenderList(int maxLevelOfConcurrency, int capacity, AllocationHandle allocationHandle)
    {
        if (maxLevelOfConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLevelOfConcurrency), "Max level of concurrency must be greater than zero.");
        }

        _threadLocalRecords = new UnsafeArray<UnsafeList<RenderRecord>>(maxLevelOfConcurrency, allocationHandle);

        for (int i = 0; i < maxLevelOfConcurrency; i++)
        {
            _threadLocalRecords[i] = new UnsafeList<RenderRecord>(capacity, allocationHandle);
        }
    }

    public RenderList(int maxLevelOfConcurrency, int capacity, Allocator allocator)
        : this(maxLevelOfConcurrency, capacity, AllocationManager.GetAllocationHandle(allocator))
    {
    }

    private readonly void ThrowIfNotCreated()
    {
        if (!IsCreated)
        {
            throw new InvalidOperationException("RenderList is not created.");
        }
    }

    public readonly Enumerator GetEnumerator()
    {
        ThrowIfNotCreated();
        return new Enumerator(this);
    }

    public readonly void Add(RenderRecord record, int threadIndex)
    {
        ThrowIfNotCreated();
        _threadLocalRecords[threadIndex].Add(record);
    }

    public readonly ReadOnlyUnsafeCollection<RenderRecord> GetThreadLocalRecords(int threadIndex)
    {
        ThrowIfNotCreated();
        return _threadLocalRecords[threadIndex].AsReadOnly();
    }

    public readonly void Clear()
    {
        ThrowIfNotCreated();
        for (int i = 0; i < _threadLocalRecords.Length; i++)
        {
            _threadLocalRecords[i].Clear();
        }
    }

    public readonly void ClearThreadLocal(int threadIndex)
    {
        ThrowIfNotCreated();
        _threadLocalRecords[threadIndex].Clear();
    }

    public readonly void Append(RenderList other)
    {
        if (!IsCreated || !other.IsCreated)
        {
            throw new InvalidOperationException("Both RenderLists must be created before appending.");
        }

        var maxConcurrency = Math.Min(_threadLocalRecords.Length, other._threadLocalRecords.Length);
        for (int i = 0; i < maxConcurrency; i++)
        {
            _threadLocalRecords[i].AddRange(other._threadLocalRecords[i].AsSpan());
        }

        if (other._threadLocalRecords.Length > _threadLocalRecords.Length)
        {
            // Add remaining records from other lists to the first list if other has more thread local lists than this
            for (int i = _threadLocalRecords.Length; i < other._threadLocalRecords.Length; i++)
            {
                _threadLocalRecords[0].AddRange(other._threadLocalRecords[i].AsSpan());
            }
        }
    }

    public void Dispose()
    {
        if (!IsCreated)
        {
            return;
        }

        for (int i = 0; i < _threadLocalRecords.Length; i++)
        {
            _threadLocalRecords[i].Dispose();
        }

        _threadLocalRecords.Dispose();
    }
}
