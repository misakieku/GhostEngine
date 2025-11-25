using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;
using System.Numerics;
using TerraFX.Interop.DirectX;

using static TerraFX.Aliases.D3D12_Alias;

namespace Ghost.Graphics.D3D12.Utilities;

internal unsafe struct D3D12DescriptorHeap : IDisposable
{
    private const int _INVALID_DESCRIPTOR_INDEX = -1;

    private readonly D3D12RenderDevice _device;

    private UniquePtr<ID3D12DescriptorHeap> _heap;
    private UniquePtr<ID3D12DescriptorHeap> _shaderVisibleHeap;
    private D3D12_CPU_DESCRIPTOR_HANDLE _startCpuHandle;
    private D3D12_CPU_DESCRIPTOR_HANDLE _startCpuHandleShaderVisible;
    private D3D12_GPU_DESCRIPTOR_HANDLE _startGpuHandleShaderVisible;
    private int _searchStart;
    private UnsafeArray<bool> _allocatedDescriptors;

    private readonly int _dynamicHeapStart;
    private int _currentDynamicOffset;

    private readonly Lock _lock = new();

    public D3D12_DESCRIPTOR_HEAP_TYPE HeapType
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

    public D3D12DescriptorHeap(string name, D3D12RenderDevice device, D3D12_DESCRIPTOR_HEAP_TYPE type, int numDescriptors, int dynamicHeapStart)
    {
        numDescriptors = Math.Max(64, numDescriptors);

        _device = device;

        HeapType = type;
        NumDescriptors = numDescriptors;
        ShaderVisible = type == D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV || type == D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER;
        Stride = device.NativeDevice.Get()->GetDescriptorHandleIncrementSize(type);

        _dynamicHeapStart = Math.Clamp(dynamicHeapStart, 0, numDescriptors);
        _currentDynamicOffset = 0;

        var success = AllocateResources(numDescriptors);
        Debug.Assert(success);

        _heap.Get()->SetName(name.AsSpan().GetUnsafePtr());
        if (ShaderVisible)
        {
            _shaderVisibleHeap.Get()->SetName($"{name} Shader Visible".AsSpan().GetUnsafePtr());
        }
    }

    public int AllocateDescriptor() => AllocateDescriptors(1);

    public int AllocateDescriptors(int count)
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

    public int AllocateDescriptorDynamic() => AllocateDescriptorsDynamic(1);

    public int AllocateDescriptorsDynamic(int count)
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

    public void ReleaseDescriptor(int index) => ReleaseDescriptors(index, 1);

    public void ReleaseDescriptors(int baseIndex, int count = 1)
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

    public readonly D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandle(int index)
    {
        if (index < 0 || index >= NumDescriptors)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Descriptor index is out of range.");
        }

        return _startCpuHandle.Offset(index, Stride);
    }

    public readonly D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandleShaderVisible(int index)
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

    public readonly D3D12_GPU_DESCRIPTOR_HANDLE GetGpuHandle(int index)
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

    public int CopyToPersistentHeap(int index, int count = 1)
    {
        if (index < _dynamicHeapStart)
        {
            return index;
        }

        var newLocation = AllocateDescriptors(count);
        _device.NativeDevice.Get()->CopyDescriptorsSimple((uint)count, GetCpuHandle(index), GetCpuHandle(newLocation), HeapType);

        return newLocation;
    }

    public readonly void CopyToShaderVisibleHeap(int index, int count = 1)
    {
        _device.NativeDevice.Get()->CopyDescriptorsSimple((uint)count, GetCpuHandleShaderVisible(index), GetCpuHandle(index), HeapType);
    }

    private bool AllocateResources(int numDescriptors)
    {
        NumDescriptors = numDescriptors;
        _heap.Dispose();
        _shaderVisibleHeap.Dispose();

        D3D12_DESCRIPTOR_HEAP_DESC heapDesc = new()
        {
            Type = HeapType,
            NumDescriptors = (uint)numDescriptors,
            Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
            NodeMask = 0
        };

        ID3D12DescriptorHeap* pHeap = default;
        var hr = _device.NativeDevice.Get()->CreateDescriptorHeap(&heapDesc, __uuidof(pHeap), (void**)&pHeap);
        if (hr.FAILED)
        {
            return false;
        }

        _heap.Attach(pHeap);

        _startCpuHandle = _heap.Get()->GetCPUDescriptorHandleForHeapStart();

        if (!_allocatedDescriptors.IsCreated)
        {
            _allocatedDescriptors = new UnsafeArray<bool>(numDescriptors, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent, Misaki.HighPerformance.LowLevel.Buffer.AllocationOption.Clear);
        }
        else
        {
            _allocatedDescriptors.Resize(numDescriptors, Misaki.HighPerformance.LowLevel.Buffer.AllocationOption.Clear);
        }

        if (ShaderVisible)
        {
            ID3D12DescriptorHeap* pShaderVisibleHeap = default;

            heapDesc.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;
            hr = _device.NativeDevice.Get()->CreateDescriptorHeap(&heapDesc, __uuidof(pShaderVisibleHeap), (void**)&pShaderVisibleHeap);
            if (hr.FAILED)
            {
                return false;
            }

            _startCpuHandleShaderVisible = pShaderVisibleHeap->GetCPUDescriptorHandleForHeapStart();
            _startGpuHandleShaderVisible = pShaderVisibleHeap->GetGPUDescriptorHandleForHeapStart();

            _shaderVisibleHeap.Attach(pShaderVisibleHeap);
        }

        return true;
    }

    private bool Grow(int minRequiredSize)
    {
        var oldSize = NumDescriptors;
        var newSize = (int)BitOperations.RoundUpToPowerOf2((uint)minRequiredSize);

        var oldHeap = _heap.Detach();

        try
        {
            if (!AllocateResources(newSize))
            {
                return false;
            }

            _device.NativeDevice.Get()->CopyDescriptorsSimple((uint)oldSize, _startCpuHandle, oldHeap->GetCPUDescriptorHandleForHeapStart(), HeapType);

            if (_shaderVisibleHeap.Get() != null)
            {
                _device.NativeDevice.Get()->CopyDescriptorsSimple((uint)oldSize, _startCpuHandleShaderVisible, oldHeap->GetCPUDescriptorHandleForHeapStart(), HeapType);
            }
        }
        finally
        {
            oldHeap->Release();
        }

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Debug.Assert(NumAllocatedDescriptors == 0);

        _heap.Dispose();
        _shaderVisibleHeap.Dispose();
        _allocatedDescriptors.Dispose();
    }
}