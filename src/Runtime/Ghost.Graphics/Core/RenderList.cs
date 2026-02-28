using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Core;

public record struct RenderRecord
{
    public Handle<Material> material;
    public Handle<Mesh> mesh;
}

public struct RenderList : IDisposable
{
    public unsafe ref struct Reader
    {
        private readonly UnsafeList<RenderRecord>* pList;
        private readonly int length;

        private int _listIndex;
        private int _itemIndex;

        internal Reader(RenderList List)
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

    public RenderList(int maxLevelOfConcurrency, int capacity, AllocationHandle allocationHandle)
    {
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

    public readonly Reader GetEnumerator()
    {
        return new Reader(this);
    }

    public readonly void Add(RenderRecord record, int threadIndex)
    {
        _threadLocalRecords[threadIndex].Add(record);
    }

    public void Dispose()
    {
        for (int i = 0; i < _threadLocalRecords.Length; i++)
        {
            _threadLocalRecords[i].Dispose();
        }

        _threadLocalRecords.Dispose();
    }
}
