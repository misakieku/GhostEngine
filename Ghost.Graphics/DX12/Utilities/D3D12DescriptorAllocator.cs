using System.Diagnostics;
using System.Numerics;
using Vortice.Direct3D12;
using DescriptorIndex = System.UInt32;

namespace Ghost.Graphics.DX12.Utilities;

internal class D3D12DescriptorAllocator : IDisposable
{
    private const DescriptorIndex _INVALID_DESCRIPTOR_INDEX = ~0u;

    private readonly ID3D12Device _device;
    private readonly Lock _lock = new();

    private ID3D12DescriptorHeap? _heap;
    private ID3D12DescriptorHeap? _shaderVisibleHeap;
    private CpuDescriptorHandle _startCpuHandle = default;
    private CpuDescriptorHandle _startCpuHandleShaderVisible = default;
    private GpuDescriptorHandle _startGpuHandleShaderVisible = default;
    private DescriptorIndex _searchStart;
    private bool[] _allocatedDescriptors = [];

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

    public ID3D12DescriptorHeap Heap => _heap!;
    public ID3D12DescriptorHeap? ShaderVisibleHeap => _shaderVisibleHeap;

    public D3D12DescriptorAllocator(ID3D12Device device, DescriptorHeapType type, uint numDescriptors)
    {
        _device = device;
        HeapType = type;
        NumDescriptors = numDescriptors;
        ShaderVisible = type == DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView || type == DescriptorHeapType.Sampler;
        Stride = device.GetDescriptorHandleIncrementSize(type);

        var success = AllocateResources(numDescriptors);
        Debug.Assert(success);
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
        _device.CopyDescriptorsSimple(count, GetCpuHandleShaderVisible(index), GetCpuHandle(index), HeapType);
    }

    private bool AllocateResources(uint numDescriptors)
    {
        NumDescriptors = numDescriptors;
        _heap?.Dispose();
        _shaderVisibleHeap?.Dispose();

        DescriptorHeapDescription heapDesc = new()
        {
            Type = HeapType,
            DescriptorCount = numDescriptors,
            Flags = DescriptorHeapFlags.None,
            NodeMask = 0
        };

        var hr = _device.CreateDescriptorHeap(in heapDesc, out _heap);
        if (hr.Failure)
        {
            return false;
        }

        _startCpuHandle = _heap!.GetCPUDescriptorHandleForHeapStart();
        Array.Resize(ref _allocatedDescriptors, (int)numDescriptors);

        if (ShaderVisible)
        {
            heapDesc.Flags = DescriptorHeapFlags.ShaderVisible;

            hr = _device.CreateDescriptorHeap(in heapDesc, out _shaderVisibleHeap);

            if (hr.Failure)
            {
                return false;
            }

            _startCpuHandleShaderVisible = _shaderVisibleHeap!.GetCPUDescriptorHandleForHeapStart();
            _startGpuHandleShaderVisible = _shaderVisibleHeap!.GetGPUDescriptorHandleForHeapStart();
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

        _device.CopyDescriptorsSimple(oldSize, _startCpuHandle, oldHeap!.GetCPUDescriptorHandleForHeapStart(), HeapType);

        if (_shaderVisibleHeap is not null)
        {
            _device.CopyDescriptorsSimple(oldSize, _startCpuHandleShaderVisible, oldHeap.GetCPUDescriptorHandleForHeapStart(), HeapType);
        }

        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _heap?.Dispose();
        _shaderVisibleHeap?.Dispose();
    }
}