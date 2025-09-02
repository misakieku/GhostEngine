using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using Win32.Graphics.D3D12MemoryAllocator;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;
using Win32.Graphics.Dxgi.Common;
using static Win32.Graphics.D3D12MemoryAllocator.Apis;
using ResourceHandle = Ghost.Graphics.Data.ResourceHandle;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12ResourceAllocator
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

    private readonly RenderSystem _renderSystem;
    private readonly Allocator _allocator;
    private UnsafeList<AllocationInfo> _allocations = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
    private UnsafeQueue<int> _freeSlots = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
    private UnsafeQueue<ResourceHandle> _temResources = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);

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

    public D3D12ResourceAllocator(IDXGIAdapter* pAdapter, ID3D12Device* pDevice, RenderSystem renderSystem)
    {
        _renderSystem = renderSystem;

        var desc = new AllocatorDesc
        {
            pAdapter = pAdapter,
            pDevice = pDevice,
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

        if (_freeSlots.Count > 0)
        {
            id = _freeSlots.Dequeue();
            var info = _allocations[id];
            if (info.Allocated)
            {
                throw new InvalidOperationException($"ERROR: Resource ID {id} registered as free but still allocated.");
            }

            generation = info.generation + 1;
            allocInfo = new AllocationInfo(in allocation, _renderSystem.CPUFenceValue, generation);

            _allocations[id] = allocInfo;
        }
        else
        {
            id = _allocations.Count;
            generation = 0u;
            allocInfo = new AllocationInfo(in allocation, _renderSystem.CPUFenceValue, generation);

            _allocations.Add(allocInfo);
        }

        var handle = new ResourceHandle(id, generation);
        if (isTemp)
        {
            _temResources.Enqueue(handle);
        }

        return handle;
    }

    public TextureHandle CreateTexture2D(in TextureDesc desc, bool tempResource = false)
    {
        CheckTexture2DSize(desc.Width, desc.Height);

        var resourceDesc = ResourceDescription.Tex2D(
            ConvertTextureFormat(desc.Format),
            desc.Width,
            desc.Height,
            mipLevels: (ushort)desc.MipLevels,
            arraySize: 1,
            flags: ConvertTextureUsage(desc.Usage)
        );

        var allocationDesc = new AllocationDesc
        {
            HeapType = HeapType.Default,
            Flags = AllocationFlags.None
        };

        var initialState = DetermineInitialTextureState(desc.Usage);

        Allocation allocation = default;
        ThrowIfFailed(_allocator.CreateResource(&allocationDesc, in resourceDesc, initialState, null, &allocation, IID_NULL, null));

        return new(TrackResource(in allocation, tempResource));
    }

    public BufferHandle CreateBuffer(in BufferDesc desc, bool tempResource = false)
    {
        CheckBufferSize((uint)desc.Size);

        var resourceDescription = ResourceDescription.Buffer(desc.Size, ConvertBufferUsage(desc.Usage));
        var allocationDesc = new AllocationDesc
        {
            HeapType = ConvertMemoryType(desc.MemoryType),
            Flags = AllocationFlags.None
        };

        var initialState = DetermineInitialBufferState(desc.Usage, desc.MemoryType);

        Allocation allocation = default;
        ThrowIfFailed(_allocator.CreateResource(&allocationDesc, in resourceDescription, initialState, null, &allocation, IID_NULL, null));

        return new(TrackResource(in allocation, tempResource));
    }

    public BufferHandle CreateUploadBuffer(uint sizeInBytes, bool tempResource = false)
    {
        var desc = new BufferDesc(sizeInBytes, BufferUsage.Upload, MemoryType.Upload);
        return CreateBuffer(in desc, tempResource);
    }

    #region Conversion Methods

    private static Format ConvertTextureFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8G8B8A8_UNorm => Format.R8G8B8A8Unorm,
            TextureFormat.B8G8R8A8_UNorm => Format.B8G8R8A8Unorm,
            TextureFormat.R16G16B16A16_Float => Format.R16G16B16A16Float,
            TextureFormat.R32G32B32A32_Float => Format.R32G32B32A32Float,
            TextureFormat.D24_UNorm_S8_UInt => Format.D24UnormS8Uint,
            TextureFormat.D32_Float => Format.D32Float,
            _ => throw new ArgumentException($"Unsupported texture format: {format}")
        };
    }

    private static ResourceFlags ConvertTextureUsage(TextureUsage usage)
    {
        var flags = ResourceFlags.None;

        if (usage.HasFlag(TextureUsage.RenderTarget))
            flags |= ResourceFlags.AllowRenderTarget;

        if (usage.HasFlag(TextureUsage.DepthStencil))
            flags |= ResourceFlags.AllowDepthStencil;

        if (usage.HasFlag(TextureUsage.UnorderedAccess))
            flags |= ResourceFlags.AllowUnorderedAccess;

        return flags;
    }

    private static ResourceFlags ConvertBufferUsage(BufferUsage usage)
    {
        var flags = ResourceFlags.None;

        if (usage.HasFlag(BufferUsage.Raw))
            flags |= ResourceFlags.AllowUnorderedAccess;

        return flags;
    }

    private static HeapType ConvertMemoryType(MemoryType memoryType)
    {
        return memoryType switch
        {
            MemoryType.Default => HeapType.Default,
            MemoryType.Upload => HeapType.Upload,
            MemoryType.Readback => HeapType.Readback,
            _ => throw new ArgumentException($"Unsupported memory type: {memoryType}")
        };
    }

    private static ResourceStates DetermineInitialTextureState(TextureUsage usage)
    {
        if (usage.HasFlag(TextureUsage.RenderTarget))
            return ResourceStates.RenderTarget;

        if (usage.HasFlag(TextureUsage.DepthStencil))
            return ResourceStates.DepthWrite;

        if (usage.HasFlag(TextureUsage.UnorderedAccess))
            return ResourceStates.UnorderedAccess;

        return ResourceStates.Common;
    }

    private static ResourceStates DetermineInitialBufferState(BufferUsage usage, MemoryType memoryType)
    {
        if (memoryType == MemoryType.Upload)
            return ResourceStates.GenericRead;

        if (memoryType == MemoryType.Readback)
            return ResourceStates.CopyDest;

        if (usage.HasFlag(BufferUsage.Vertex))
            return ResourceStates.VertexAndConstantBuffer;

        if (usage.HasFlag(BufferUsage.Index))
            return ResourceStates.IndexBuffer;

        if (usage.HasFlag(BufferUsage.Constant))
            return ResourceStates.VertexAndConstantBuffer;

        return ResourceStates.Common;
    }

    #endregion

    public void ReleaseTempResource()
    {
        while (_temResources.Count > 0)
        {
            ref var handle = ref _temResources.Peek();
            ref var info = ref _allocations[handle.id];

            if (info.Allocated && info.cpuFenceValue > _renderSystem.CPUFenceValue)
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

        ref var allocationInfo = ref _allocations[handle.id];
        if (!allocationInfo.Allocated || allocationInfo.generation != handle.generation)
        {
            throw new InvalidOperationException($"Resource with ID {handle.id} and generation {handle.generation} is not allocated or has been released.");
        }

        return allocationInfo.allocation;
    }

    public void ReleaseAllocation(in ResourceHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        ref var allocationInfo = ref _allocations[handle.id];

        if (!allocationInfo.Allocated || allocationInfo.generation != handle.generation)
        {
            return;
        }

        allocationInfo.Dispose();
        _freeSlots.Enqueue(handle.id);
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