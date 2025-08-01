using Ghost.Core;
using System.Diagnostics;
using Win32;
using Win32.Graphics.Direct3D12;
using DescriptorIndex = System.UInt32;

namespace Ghost.Graphics.D3D12.Utilities;

/// <summary>
/// Specialized descriptor heap allocator for SM 6.6 bindless rendering with ResourceDescriptorHeap[index].
/// This allocator maintains a 1:1 relationship between allocation indices and shader indices.
/// </summary>
internal unsafe struct BindlessDescriptorHeapAllocator : IDisposable
{
    private const DescriptorIndex _INVALID_DESCRIPTOR_INDEX = ~0u;

    private readonly ConstPtr<ID3D12Device14> _device;
    private readonly Lock _lock = new();

    private ComPtr<ID3D12DescriptorHeap> _bindlessHeap;
    private CpuDescriptorHandle _startCpuHandle;
    private GpuDescriptorHandle _startGpuHandle;
    private Queue<uint> _freeDescriptors;
    private uint _stride;

    public DescriptorHeapType HeapType
    {
        get;
    }

    public uint NumDescriptors
    {
        get; private set;
    }

    public uint NumAllocatedDescriptors
    {
        get; private set;
    }

    public uint Stride => _stride;

    public readonly ConstPtr<ID3D12DescriptorHeap> BindlessHeap => new(_bindlessHeap.Get());

    public BindlessDescriptorHeapAllocator(ConstPtr<ID3D12Device14> device, uint numDescriptors = 10000)
    {
        _device = device;
        HeapType = DescriptorHeapType.CbvSrvUav;
        NumDescriptors = numDescriptors;
        _stride = device.Ptr->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
        _freeDescriptors = new Queue<uint>();

        var success = AllocateResources(numDescriptors);
        Debug.Assert(success);

        _bindlessHeap.Get()->SetName("bindless");
    }

    public DescriptorIndex AllocateDescriptor()
    {
        lock (_lock)
        {
            if (_freeDescriptors.Count == 0)
            {
                // Try to grow the heap
                if (!Grow(NumDescriptors * 2))
                {
                    Debug.WriteLine("ERROR: Failed to grow bindless descriptor heap!");
                    return _INVALID_DESCRIPTOR_INDEX;
                }
            }

            var index = _freeDescriptors.Dequeue();
            NumAllocatedDescriptors++;
            return index;
        }
    }

    public DescriptorIndex AllocateDescriptors(uint count)
    {
        lock (_lock)
        {
            if (_freeDescriptors.Count < count)
            {
                // Try to grow the heap
                var newSize = Math.Max(NumDescriptors * 2, NumDescriptors + count);
                if (!Grow(newSize))
                {
                    Debug.WriteLine("ERROR: Failed to grow bindless descriptor heap!");
                    return _INVALID_DESCRIPTOR_INDEX;
                }
            }

            var baseIndex = _freeDescriptors.Dequeue();
            for (uint i = 1; i < count; i++)
            {
                _freeDescriptors.Dequeue();
            }

            NumAllocatedDescriptors += count;
            return baseIndex;
        }
    }

    public void ReleaseDescriptor(DescriptorIndex index)
    {
        lock (_lock)
        {
            if (index >= NumDescriptors)
            {
                Debug.WriteLine("Error: Attempted to release an invalid descriptor index");
                return;
            }

            _freeDescriptors.Enqueue(index);
            NumAllocatedDescriptors--;
        }
    }

    public void ReleaseDescriptors(DescriptorIndex baseIndex, uint count = 1)
    {
        lock (_lock)
        {
            for (uint i = 0; i < count; i++)
            {
                var index = baseIndex + i;
                if (index >= NumDescriptors)
                {
                    Debug.WriteLine("Error: Attempted to release an invalid descriptor index");
                    continue;
                }

                _freeDescriptors.Enqueue(index);
            }

            NumAllocatedDescriptors -= count;
        }
    }

    public CpuDescriptorHandle GetCpuHandle(DescriptorIndex index)
    {
        var handle = _startCpuHandle;
        return handle.Offset((int)index, _stride);
    }

    public GpuDescriptorHandle GetGpuHandle(DescriptorIndex index)
    {
        var handle = _startGpuHandle;
        return handle.Offset((int)index, _stride);
    }

    public GpuDescriptorHandle GetGpuHandleStart()
    {
        return _startGpuHandle;
    }

    private bool AllocateResources(uint numDescriptors)
    {
        NumDescriptors = numDescriptors;
        _bindlessHeap.Dispose();

        var heapDesc = new DescriptorHeapDescription
        {
            Type = HeapType,
            NumDescriptors = numDescriptors,
            Flags = DescriptorHeapFlags.ShaderVisible, // Must be shader visible for SM 6.6
            NodeMask = 0
        };

        fixed (void* heapPtr = &_bindlessHeap)
        {
            var hr = _device.Ptr->CreateDescriptorHeap(&heapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)heapPtr);
            if (hr.Failure)
            {
                return false;
            }
        }

        _startCpuHandle = _bindlessHeap.Get()->GetCPUDescriptorHandleForHeapStart();
        _startGpuHandle = _bindlessHeap.Get()->GetGPUDescriptorHandleForHeapStart();

        // Initialize free descriptor queue
        _freeDescriptors.Clear();
        for (uint i = 0; i < numDescriptors; i++)
        {
            _freeDescriptors.Enqueue(i);
        }

        return true;
    }

    private bool Grow(uint minRequiredSize)
    {
        var oldSize = NumDescriptors;
        var newSize = Math.Max(minRequiredSize, oldSize * 2);

        var oldHeap = _bindlessHeap;

        if (!AllocateResources(newSize))
        {
            return false;
        }

        // Copy old descriptors to new heap
        if (oldHeap.Get() is not null)
        {
            _device.Ptr->CopyDescriptorsSimple(oldSize, _startCpuHandle, oldHeap.Get()->GetCPUDescriptorHandleForHeapStart(), HeapType);
        }

        return true;
    }

    public void Dispose()
    {
        _bindlessHeap.Dispose();
    }
}