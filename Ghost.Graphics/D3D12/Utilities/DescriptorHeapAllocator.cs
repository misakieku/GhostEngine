using System.Diagnostics;
using System.Numerics;
using Win32;
using Win32.Graphics.Direct3D12;
using DescriptorIndex = System.UInt32;

namespace Ghost.Graphics.D3D12.Utilities;

internal unsafe struct DescriptorHeapAllocator : IDisposable
{
    private const DescriptorIndex _INVALID_DESCRIPTOR_INDEX = ~0u;

    private ComPtr<ID3D12Device14> _device;

    private ComPtr<ID3D12DescriptorHeap> _heap;
    private ComPtr<ID3D12DescriptorHeap> _shaderVisibleHeap;
    private CpuDescriptorHandle _startCpuHandle;
    private CpuDescriptorHandle _startCpuHandleShaderVisible;
    private GpuDescriptorHandle _startGpuHandleShaderVisible;
    private DescriptorIndex _searchStart;
    private bool[] _allocatedDescriptors = [];

    private readonly Lock _lock = new();

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

    public bool ShaderVisible
    {
        get;
    }

    public uint Stride
    {
        get;
    }

    public readonly ID3D12DescriptorHeap* Heap => _heap.Get();
    public readonly ID3D12DescriptorHeap* ShaderVisibleHeap => _shaderVisibleHeap.Get();

    public DescriptorHeapAllocator(string name, ComPtr<ID3D12Device14> device, DescriptorHeapType type, uint numDescriptors)
    {
        _device = device;
        device.Get()->AddRef();

        HeapType = type;
        NumDescriptors = numDescriptors;
        ShaderVisible = type == DescriptorHeapType.CbvSrvUav || type == DescriptorHeapType.Sampler;
        Stride = device.Get()->GetDescriptorHandleIncrementSize(type);

        var success = AllocateResources(numDescriptors);
        Debug.Assert(success);

        _heap.Get()->SetName(name);
        if (ShaderVisible)
        {
            _shaderVisibleHeap.Get()->SetName($"{name} Shader Visible");
        }
    }

    public DescriptorIndex AllocateDescriptor() => AllocateDescriptors(1);

    public DescriptorIndex AllocateDescriptors(uint count)
    {
        lock (_lock)
        {
            DescriptorIndex foundIndex = 0;
            uint freeCount = 0;
            var found = false;

            // Find a contiguous range of 'count' indices for which _allocatedDescriptors[index] is false
            for (var index = _searchStart; index < NumDescriptors; index++)
            {
                if (_allocatedDescriptors[index])
                {
                    freeCount = 0;
                }
                else
                {
                    freeCount += 1;
                }

                if (freeCount >= count)
                {
                    foundIndex = index > 0 ? index - count + 1 : 0;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                foundIndex = NumDescriptors;

                if (!Grow(NumDescriptors + count))
                {
                    Debug.WriteLine("ERROR: Failed to grow a descriptor heap!");
                    return _INVALID_DESCRIPTOR_INDEX;
                }
            }

            for (var index = foundIndex; index < foundIndex + count; index++)
            {
                _allocatedDescriptors[index] = true;
            }

            NumAllocatedDescriptors += count;
            _searchStart = foundIndex + count;
            return foundIndex;
        }
    }

    public void ReleaseDescriptor(DescriptorIndex index) => ReleaseDescriptors(index, 1);

    public void ReleaseDescriptors(DescriptorIndex baseIndex, uint count = 1)
    {
        if (count == 0)
        {
            return;
        }

        lock (_lock)
        {
            for (var index = baseIndex; index < baseIndex + count; index++)
            {
#if DEBUG
                if (!_allocatedDescriptors[index])
                {
                    Debug.WriteLine("Error: Attempted to release an un-allocated descriptor");
                }
#endif

                _allocatedDescriptors[index] = false;
            }

            NumAllocatedDescriptors -= count;

            if (_searchStart > baseIndex)
            {
                _searchStart = baseIndex;
            }
        }
    }

    public CpuDescriptorHandle GetCpuHandle(DescriptorIndex index)
    {
        var handle = _startCpuHandle;
        return handle.Offset((int)index, Stride);
    }

    public CpuDescriptorHandle GetCpuHandleShaderVisible(DescriptorIndex index)
    {
        var handle = _startCpuHandleShaderVisible;
        return handle.Offset((int)index, Stride);
    }

    public GpuDescriptorHandle GetGpuHandle(DescriptorIndex index)
    {
        var handle = _startGpuHandleShaderVisible;
        return handle.Offset((int)index, Stride);
    }

    public void CopyToShaderVisibleHeap(DescriptorIndex index, uint count = 1)
    {
        _device.Get()->CopyDescriptorsSimple(count, GetCpuHandleShaderVisible(index), GetCpuHandle(index), HeapType);
    }

    private bool AllocateResources(uint numDescriptors)
    {
        NumDescriptors = numDescriptors;
        _heap.Dispose();
        _shaderVisibleHeap.Dispose();

        DescriptorHeapDescription heapDesc = new()
        {
            Type = HeapType,
            NumDescriptors = numDescriptors,
            Flags = DescriptorHeapFlags.None,
            NodeMask = 0
        };

        fixed (void* heapPtr = &_heap)
        {
            var hr = _device.Get()->CreateDescriptorHeap(&heapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)heapPtr);
            if (hr.Failure)
            {
                return false;
            }
        }

        _startCpuHandle = _heap.Get()->GetCPUDescriptorHandleForHeapStart();
        Array.Resize(ref _allocatedDescriptors, (int)numDescriptors);

        if (ShaderVisible)
        {
            heapDesc.Flags = DescriptorHeapFlags.ShaderVisible;

            fixed (void* heapPtr = &_shaderVisibleHeap)
            {
                var hr = _device.Get()->CreateDescriptorHeap(&heapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)heapPtr);
                if (hr.Failure)
                {
                    return false;
                }
            }

            _startCpuHandleShaderVisible = _shaderVisibleHeap.Get()->GetCPUDescriptorHandleForHeapStart();
            _startGpuHandleShaderVisible = _shaderVisibleHeap.Get()->GetGPUDescriptorHandleForHeapStart();
        }

        return true;
    }

    private bool Grow(uint minRequiredSize)
    {
        var oldSize = NumDescriptors;
        var newSize = BitOperations.RoundUpToPowerOf2(minRequiredSize);

        var oldHeap = _heap;

        if (!AllocateResources(newSize))
        {
            return false;
        }

        _device.Get()->CopyDescriptorsSimple(oldSize, _startCpuHandle, oldHeap.Get()->GetCPUDescriptorHandleForHeapStart(), HeapType);

        if (_shaderVisibleHeap.Get() is not null)
        {
            _device.Get()->CopyDescriptorsSimple(oldSize, _startCpuHandleShaderVisible, oldHeap.Get()->GetCPUDescriptorHandleForHeapStart(), HeapType);
        }

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _device.Dispose();

        _heap.Dispose();
        _shaderVisibleHeap.Dispose();
    }
}