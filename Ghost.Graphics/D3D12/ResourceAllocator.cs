using Ghost.Graphics.Data;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using Win32.Graphics.D3D12MemoryAllocator;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;
using Win32.Graphics.Dxgi.Common;
using static Win32.Graphics.D3D12MemoryAllocator.Apis;
using ResourceHandle = Ghost.Graphics.Data.ResourceHandle;

namespace Ghost.Graphics.D3D12;

internal unsafe class ResourceAllocator
{
    private readonly struct AllocationInfo : IDisposable
    {
        public readonly Allocation allocation;
        public readonly uint cpuFenceValue;
        public readonly uint generation;

        public bool Allocated => allocation.IsNotNull;

        public AllocationInfo(in Allocation allocation, uint cpuFenceValue, uint generation)
        {
            this.allocation = allocation;
            this.cpuFenceValue = cpuFenceValue;
            this.generation = generation;
        }

        public AllocationInfo(in Allocation allocation, uint generation)
            : this(allocation, GraphicsPipeline.CPUFenceValue + 1, generation)
        {
        }

        public void Dispose()
        {
            if (allocation.IsNull)
            {
                return;
            }

            allocation.Release();
        }
    }

    private const uint _MAX_BYTES = D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_A_TERM * 1024u * 1024u;
    private const uint _MAX_TEXTURE2D_DIMENSION = 16384u;
    private const uint _MAX_TEXTURE3D_DIMENSION = 2048u;

    private readonly Allocator _allocator;
    private UnsafeList<AllocationInfo> _allocations = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
    private UnsafeQueue<int> _freeSlots = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
    private UnsafeQueue<ResourceHandle> _temResources = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);

    private readonly Lock _lock = new();

    private Guid* IID_NULL
    {
        get
        {
            fixed (Guid* pGuid = &Guid.Empty)
            {
                return pGuid;
            }
        }
    }

    public ResourceAllocator()
    {
        var desc = new AllocatorDesc
        {
            pAdapter = (IDXGIAdapter*)GraphicsPipeline.GraphicsDevice.Adapter.Ptr,
            pDevice = (ID3D12Device*)GraphicsPipeline.GraphicsDevice.NativeDevice.Ptr,
            Flags = AllocatorFlags.DefaultPoolsNotZeroed | AllocatorFlags.MSAATexturesAlwaysCommitted
        };

        CreateAllocator(in desc, out _allocator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckBufferSize(uint sizeInBytes)
    {
        if (sizeInBytes > _MAX_BYTES)
        {
            throw new InvalidOperationException($"ERROR: Resource size too large for DirectX 12 (size {sizeInBytes})");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckTexture2DSize(uint width, uint height)
    {
        if (width > _MAX_TEXTURE2D_DIMENSION || height > _MAX_TEXTURE2D_DIMENSION)
        {
            throw new InvalidOperationException($"ERROR: Texture size too large for DirectX 12 (width {width}, height {height})");
        }
    }

    private ResourceHandle TrackResource(in Allocation allocation, bool isTemp)
    {
        int id;
        uint generation;
        AllocationInfo allocInfo;

        lock (_lock)
        {
            if (_freeSlots.Count > 0)
            {
                id = _freeSlots.Dequeue();
                var info = _allocations[id];
                if (info.Allocated)
                {
                    throw new InvalidOperationException($"ERROR: Resource ID {id} registered as free but still allocated.");
                }

                generation = info.generation + 1;
                allocInfo = new AllocationInfo(in allocation, generation);

                _allocations[id] = allocInfo;
            }
            else
            {
                id = _allocations.Count;
                generation = 0u;
                allocInfo = new AllocationInfo(in allocation, generation);

                _allocations.Add(allocInfo);
            }

            var handle = new ResourceHandle(id, generation);
            if (isTemp)
            {
                _temResources.Enqueue(handle);
            }

            return handle;
        }
    }

    public TextureHandle CreateTexture2D(uint width, uint height, ushort mipLevels, Format format = Format.R8G8B8A8Unorm, ResourceFlags resFlags = ResourceFlags.None, AllocationFlags allocFlags = AllocationFlags.None, ResourceStates state = ResourceStates.Common, bool tempResource = false)
    {
        CheckTexture2DSize(width, height);

        var resourceDesc = ResourceDescription.Tex2D(format, width, height, mipLevels: mipLevels, arraySize: 1, flags: resFlags);
        var allocationDesc = new AllocationDesc
        {
            HeapType = HeapType.Default,
            Flags = allocFlags
        };

        Allocation allocation = default;
        ThrowIfFailed(_allocator.CreateResource(&allocationDesc, in resourceDesc, state, null, &allocation, IID_NULL, null));

        return new(TrackResource(in allocation, tempResource));
    }

    public BufferHandle CreateBuffer(uint sizeInBytes, HeapType heapType = HeapType.Default, ResourceFlags resFlags = ResourceFlags.None, AllocationFlags allocFlags = AllocationFlags.None, ResourceStates initialState = ResourceStates.Common, bool tempResource = false)
    {
        CheckBufferSize(sizeInBytes);

        var resourceDescription = ResourceDescription.Buffer(sizeInBytes, resFlags);
        var allocationDesc = new AllocationDesc
        {
            HeapType = heapType,
            Flags = allocFlags
        };

        Allocation allocation = default;
        ThrowIfFailed(_allocator.CreateResource(&allocationDesc, in resourceDescription, initialState, null, &allocation, IID_NULL, null));

        return new(TrackResource(in allocation, tempResource));
    }

    public BufferHandle CreateUploadBuffer(uint sizeInBytes, bool tempResource = false)
    {
        return CreateBuffer(sizeInBytes, HeapType.Upload, ResourceFlags.None, AllocationFlags.None, ResourceStates.GenericRead, tempResource);
    }

    public void ReleaseTempResource()
    {
        while (_temResources.Count > 0)
        {
            ref var handle = ref _temResources.Peek();
            ref var info = ref _allocations[handle.id];
            if (info.cpuFenceValue > GraphicsPipeline.GPUFenceValue)
            {
                break;
            }

            ReleaseAllocation(in handle);
            _temResources.Dequeue();
        }
    }

    public Allocation GetAllocation(in ResourceHandle handle)
    {
        if (!handle.IsValid)
        {
            throw new InvalidOperationException("Invalid resource handle.");
        }

        lock (_lock)
        {
            ref var allocationInfo = ref _allocations[handle.id];
            if (!allocationInfo.Allocated || allocationInfo.generation != handle.generation)
            {
                throw new InvalidOperationException($"Resource with ID {handle.id} and generation {handle.generation} is not allocated or has been released.");
            }

            return allocationInfo.allocation;
        }
    }

    public void ReleaseAllocation(in ResourceHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        lock (_lock)
        {
            ref var allocationInfo = ref _allocations[handle.id];

            if (!allocationInfo.Allocated || allocationInfo.generation != handle.generation)
            {
                return;
            }

            allocationInfo.Dispose();
            _freeSlots.Enqueue(handle.id);
        }
    }

    public void Dispose()
    {
#if DEBUG
        if (_allocations.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_allocations.Count} allocations still registered. Ensure all resources are released before disposing.");
        }
#endif
        for (var i = 0; i < _allocations.Count; i++)
        {
            _allocations[i].Dispose();
        }

        _allocations.Dispose();
        _temResources.Dispose();
        _allocator.Release();
    }
}