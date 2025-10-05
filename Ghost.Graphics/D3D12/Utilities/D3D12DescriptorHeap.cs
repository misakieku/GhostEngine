using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Numerics;
using Win32;
using Win32.Graphics.Direct3D12;
using DescriptorIndex = System.Int32;

namespace Ghost.Graphics.D3D12.Utilities;

internal unsafe struct D3D12DescriptorHeap : IDisposable
{
    private const DescriptorIndex _INVALID_DESCRIPTOR_INDEX = -1;

    private readonly D3D12RenderDevice _device;

    private ComPtr<ID3D12DescriptorHeap> _heap;
    private ComPtr<ID3D12DescriptorHeap> _shaderVisibleHeap;
    private CpuDescriptorHandle _startCpuHandle;
    private CpuDescriptorHandle _startCpuHandleShaderVisible;
    private GpuDescriptorHandle _startGpuHandleShaderVisible;
    private DescriptorIndex _searchStart;
    private UnsafeArray<bool> _allocatedDescriptors;

    private readonly DescriptorIndex _dynamicHeapStart;
    private DescriptorIndex _currentDynamicOffset;

    private readonly Lock _lock = new();

    public DescriptorHeapType HeapType
    {
        get;
    }

    public int NumDescriptors
    {
        get; private set;
    }

    public int NumAllocatedDescriptors
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

    public D3D12DescriptorHeap(string name, D3D12RenderDevice device, DescriptorHeapType type, int numDescriptors, int dynamicHeapStart)
    {
        numDescriptors = Math.Max(64, numDescriptors);

        _device = device;

        HeapType = type;
        NumDescriptors = numDescriptors;
        ShaderVisible = type == DescriptorHeapType.CbvSrvUav || type == DescriptorHeapType.Sampler;
        Stride = device.NativeDevice->GetDescriptorHandleIncrementSize(type);

        _dynamicHeapStart = Math.Clamp(dynamicHeapStart, 0, numDescriptors);
        _currentDynamicOffset = 0;

        var success = AllocateResources(numDescriptors);
        Debug.Assert(success);

        _heap.Get()->SetName(name);
        if (ShaderVisible)
        {
            _shaderVisibleHeap.Get()->SetName($"{name} Shader Visible");
        }
    }

    public DescriptorIndex AllocateDescriptor() => AllocateDescriptors(1);

    public DescriptorIndex AllocateDescriptors(int count)
    {
        lock (_lock)
        {
            var foundIndex = 0;
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

            if (!found || foundIndex >= _dynamicHeapStart)
            {
                Debug.Assert(false, "ERROR: Descriptor heap is full!");
                return _INVALID_DESCRIPTOR_INDEX;
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

    public DescriptorIndex AllocateDescriptorDynamic() => AllocateDescriptorsDynamic(1);

    public DescriptorIndex AllocateDescriptorsDynamic(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than zero.");
        }

        // NOTE: In dynamic allocation, we use arena-style allocation without freeing.
        // We reset the offset at the beginning of each frame instead.

        lock (_lock)
        {
            var baseIndex = _currentDynamicOffset + _dynamicHeapStart;
            _currentDynamicOffset += count;

            var requiredSize = baseIndex + count;
            if (requiredSize > NumDescriptors)
            {
                if (!Grow(requiredSize))
                {
                    Debug.Assert(false, "ERROR: Failed to grow a descriptor heap!");
                    return _INVALID_DESCRIPTOR_INDEX;
                }
            }

            NumAllocatedDescriptors += count;
            return baseIndex;
        }
    }

    public void ReleaseDescriptor(DescriptorIndex index) => ReleaseDescriptors(index, 1);

    public void ReleaseDescriptors(DescriptorIndex baseIndex, int count = 1)
    {
        if (count == 0)
        {
            return;
        }

        if (baseIndex < _dynamicHeapStart)
        {
            // Dynamic allocations are not released individually.
            return;
        }

        lock (_lock)
        {
            for (var index = baseIndex; index < baseIndex + count; index++)
            {
#if DEBUG || GHOST_EDITOR
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

    public void ResetDynamicHeap()
    {
        lock (_lock)
        {
            _currentDynamicOffset = 0;
        }
    }

    public readonly CpuDescriptorHandle GetCpuHandle(DescriptorIndex index)
    {
        if (index < 0 || index >= NumDescriptors)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Descriptor index is out of range.");
        }

        return _startCpuHandle.Offset(index, Stride);
    }

    public readonly CpuDescriptorHandle GetCpuHandleShaderVisible(DescriptorIndex index)
    {
        if (index < 0 || index >= NumDescriptors)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Descriptor index is out of range.");
        }

        if (!ShaderVisible)
        {
            throw new InvalidOperationException("Descriptor heap is not shader visible.");
        }

        return _startCpuHandleShaderVisible.Offset(index, Stride);
    }

    public readonly GpuDescriptorHandle GetGpuHandle(DescriptorIndex index)
    {
        if (index < 0 || index >= NumDescriptors)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Descriptor index is out of range.");
        }

        if (!ShaderVisible)
        {
            throw new InvalidOperationException("Descriptor heap is not shader visible.");
        }

        return _startGpuHandleShaderVisible.Offset(index, Stride);
    }

    public DescriptorIndex CopyToPersistentHeap(DescriptorIndex index, int count = 1)
    {
        if (index < _dynamicHeapStart)
        {
            return index;
        }

        var newLocation = AllocateDescriptors(count);
        _device.NativeDevice->CopyDescriptorsSimple((uint)count, GetCpuHandle(index), GetCpuHandle(newLocation), HeapType);

        return newLocation;
    }

    public readonly void CopyToShaderVisibleHeap(DescriptorIndex index, int count = 1)
    {
        _device.NativeDevice->CopyDescriptorsSimple((uint)count, GetCpuHandleShaderVisible(index), GetCpuHandle(index), HeapType);
    }

    private bool AllocateResources(int numDescriptors)
    {
        NumDescriptors = numDescriptors;
        _heap.Dispose();
        _shaderVisibleHeap.Dispose();

        DescriptorHeapDescription heapDesc = new()
        {
            Type = HeapType,
            NumDescriptors = (uint)numDescriptors,
            Flags = DescriptorHeapFlags.None,
            NodeMask = 0
        };

        fixed (void* heapPtr = &_heap)
        {
            var hr = _device.NativeDevice->CreateDescriptorHeap(&heapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)heapPtr);
            if (hr.Failure)
            {
                return false;
            }
        }

        _startCpuHandle = _heap.Get()->GetCPUDescriptorHandleForHeapStart();
        _allocatedDescriptors.Resize(numDescriptors);

        if (ShaderVisible)
        {
            heapDesc.Flags = DescriptorHeapFlags.ShaderVisible;

            fixed (void* heapPtr = &_shaderVisibleHeap)
            {
                var hr = _device.NativeDevice->CreateDescriptorHeap(&heapDesc, __uuidof<ID3D12DescriptorHeap>(), (void**)heapPtr);
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

    private bool Grow(int minRequiredSize)
    {
        var oldSize = NumDescriptors;
        var newSize = (int)BitOperations.RoundUpToPowerOf2((uint)minRequiredSize);

        using var oldHeap = _heap;

        if (!AllocateResources(newSize))
        {
            return false;
        }

        _device.NativeDevice->CopyDescriptorsSimple((uint)oldSize, _startCpuHandle, oldHeap.Get()->GetCPUDescriptorHandleForHeapStart(), HeapType);

        if (_shaderVisibleHeap.Get() != null)
        {
            _device.NativeDevice->CopyDescriptorsSimple((uint)oldSize, _startCpuHandleShaderVisible, oldHeap.Get()->GetCPUDescriptorHandleForHeapStart(), HeapType);
        }

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
#if DEBUG
        if (NumAllocatedDescriptors > 0)
        {
            Debug.WriteLine($"Warning: Descriptor heap of type {HeapType} is being disposed with {NumAllocatedDescriptors} allocated descriptors.");
        }
#endif

        _heap.Dispose();
        _shaderVisibleHeap.Dispose();
        _allocatedDescriptors.Dispose();
    }
}