using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using Win32.Graphics.D3D12MemoryAllocator;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;
using Win32.Graphics.Dxgi.Common;
using static Win32.Graphics.D3D12MemoryAllocator.Apis;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12ResourceAllocator : IResourceAllocator<ID3D12Resource>, IDisposable
{
    private readonly struct AllocationInfo : IDisposable
    {
        public readonly Allocation allocation;
        public readonly uint cpuFenceValue;

        public bool Allocated => allocation.IsNotNull;

        public AllocationInfo(in Allocation allocation, uint cpuFenceValue)
        {
            this.allocation = allocation;
            this.cpuFenceValue = cpuFenceValue;
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

    private readonly ID3D12Device14* _device;

    private readonly Allocator _allocator;
    private readonly RenderSystem _renderSystem;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private UnsafeSlotMap<AllocationInfo> _allocations = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
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

    public D3D12ResourceAllocator(RenderSystem renderSystem, D3D12RenderDevice device, D3D12DescriptorAllocator descriptorAllocator)
    {
        var desc = new AllocatorDesc
        {
            pAdapter = (IDXGIAdapter*)device.Adapter,
            pDevice = (ID3D12Device*)device.NativeDevice,
            Flags = AllocatorFlags.DefaultPoolsNotZeroed | AllocatorFlags.MSAATexturesAlwaysCommitted
        };

        CreateAllocator(in desc, out _allocator);

        _device = device.NativeDevice;
        _renderSystem = renderSystem;
        _descriptorAllocator = descriptorAllocator;
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

    private ResourceHandle TrackResource(ref readonly Allocation allocation, bool isTemp)
    {
        var id = _allocations.Add(new(in allocation, _renderSystem.CPUFenceValue), out var generation);

        var handle = new ResourceHandle(id, generation);
        if (isTemp)
        {
            _temResources.Enqueue(handle);
        }

        return handle;
    }

    public TextureHandle CreateTextureHandle(ref readonly TextureDesc desc, bool tempResource = false)
    {
        CheckTexture2DSize(desc.Width, desc.Height);

        var resourceDesc = ResourceDescription.Tex2D(
            ConvertTextureFormat(desc.Format),
            desc.Width,
            desc.Height,
            mipLevels: (ushort)desc.MipLevels,
            arraySize: (ushort)desc.Slice,
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

    public BufferHandle CreateBufferHandle(ref readonly BufferDesc desc, bool tempResource = false)
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

        var handle = TrackResource(in allocation, tempResource);

        if (desc.Usage.HasFlag(BufferUsage.ShaderResource) && desc.CreationFlags.HasFlag(BufferCreationFlags.Bindless))
        {
            var isRaw = desc.Usage.HasFlag(BufferUsage.Raw);

            var descriptorHandle = _descriptorAllocator.AllocateBindless();

            var srvDesc = new ShaderResourceViewDescription
            {
                ViewDimension = SrvDimension.Buffer,
                Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
            };

            if (isRaw)
            {
                srvDesc.Format = Format.R32Typeless;
                srvDesc.Buffer.FirstElement = 0;
                srvDesc.Buffer.NumElements = (uint)(desc.Size / 4);
                srvDesc.Buffer.StructureByteStride = 0;
                srvDesc.Buffer.Flags = BufferSrvFlags.Raw;
            }
            else
            {
                srvDesc.Format = Format.Unknown;
                srvDesc.Buffer.FirstElement = 0;
                srvDesc.Buffer.NumElements = (uint)(desc.Size / desc.Stride);
                srvDesc.Buffer.StructureByteStride = desc.Stride;
                srvDesc.Buffer.Flags = BufferSrvFlags.None;
            }

            _device->CreateShaderResourceView(allocation.Resource, &srvDesc, _descriptorAllocator.GetCpuHandle(descriptorHandle));

            return new(handle, descriptorHandle);
        }

        return new(handle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IRenderTarget CreateRenderTarget(ref readonly RenderTargetDesc desc, bool tempResource = false)
    {
        var textureDesc = RenderTargetDesc.ToTextureDescriptor(desc);
        return D3D12RenderTarget.Create(CreateTextureHandle(in textureDesc), in desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ITexture CreateTexture(ref readonly TextureDesc desc, bool tempResource = false)
    {
        return new D3D12Texture(CreateTextureHandle(in desc, tempResource), in desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBuffer CreateBuffer(ref readonly BufferDesc desc, bool tempResource = false)
    {
        return new D3D12Buffer(CreateBufferHandle(in desc, tempResource), in desc, this);
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
        {
            flags |= ResourceFlags.AllowRenderTarget;
        }

        if (usage.HasFlag(TextureUsage.DepthStencil))
        {
            flags |= ResourceFlags.AllowDepthStencil;
        }

        if (usage.HasFlag(TextureUsage.UnorderedAccess))
        {
            flags |= ResourceFlags.AllowUnorderedAccess;
        }

        return flags;
    }

    private static ResourceFlags ConvertBufferUsage(BufferUsage usage)
    {
        var flags = ResourceFlags.None;

        if (usage.HasFlag(BufferUsage.Raw))
        {
            flags |= ResourceFlags.AllowUnorderedAccess;
        }

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
        {
            return ResourceStates.RenderTarget;
        }

        if (usage.HasFlag(TextureUsage.DepthStencil))
        {
            return ResourceStates.DepthWrite;
        }

        if (usage.HasFlag(TextureUsage.UnorderedAccess))
        {
            return ResourceStates.UnorderedAccess;
        }

        return ResourceStates.Common;
    }

    private static ResourceStates DetermineInitialBufferState(BufferUsage usage, MemoryType memoryType)
    {
        if (memoryType == MemoryType.Upload)
        {
            return ResourceStates.GenericRead;
        }

        if (memoryType == MemoryType.Readback)
        {
            return ResourceStates.CopyDest;
        }

        if (usage.HasFlag(BufferUsage.Vertex))
        {
            return ResourceStates.VertexAndConstantBuffer;
        }

        if (usage.HasFlag(BufferUsage.Index))
        {
            return ResourceStates.IndexBuffer;
        }

        if (usage.HasFlag(BufferUsage.Constant))
        {
            return ResourceStates.VertexAndConstantBuffer;
        }

        return ResourceStates.Common;
    }

    #endregion

    public void ReleaseTempResource()
    {
        while (_temResources.Count > 0)
        {
            var handle = _temResources.Peek();

            if (_allocations.TryGetElementAt(handle.id, handle.generation, out var info)
                && info.Allocated)
            {
                break;
            }

            ReleaseResource(handle);
            _temResources.Dequeue();
        }
    }

    public ID3D12Resource* GetResource(ResourceHandle handle)
    {
        if (!handle.IsValid)
        {
            throw new InvalidOperationException("Invalid resource handle.");
        }

        var info = _allocations.GetElementAt(handle.id, handle.generation);
        if (!info.Allocated)
        {
            throw new InvalidOperationException($"Resource with ID {handle.id} and generation {handle.generation} is not allocated or has been released.");
        }

        return info.allocation.Resource;
    }

    public void ReleaseResource(ResourceHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        ref var info = ref _allocations.GetElementReferenceAt(handle.id, handle.generation, out var exist);

        if (!exist || !info.Allocated)
        {
            return;
        }

        info.Dispose();
        _allocations.Remove(handle.id, handle.generation);
    }

    public void Dispose()
    {
#if DEBUG
        if (_allocations.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_allocations.Count} allocations still registered. Ensure all resources are released before disposing.");
        }
#endif
        foreach (var info in _allocations)
        {
            info.Dispose();
        }

        _allocations.Dispose();
        _temResources.Dispose();
        _allocator.Release();
    }
}