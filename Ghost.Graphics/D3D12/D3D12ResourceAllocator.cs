using Ghost.Core;
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

internal unsafe class D3D12ResourceAllocator : IResourceAllocator, IDisposable
{
    private const uint _MAX_BYTES = D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_A_TERM * 1024u * 1024u;
    private const uint _MAX_TEXTURE2D_DIMENSION = 16384u;
    private const uint _MAX_TEXTURE3D_DIMENSION = 2048u;

    private readonly ID3D12Device14* _device;

    private readonly Allocator _allocator;
    private readonly RenderSystem _renderSystem;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private UnsafeQueue<Handle<GPUResource>> _temResources = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);

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

    public D3D12ResourceAllocator(RenderSystem renderSystem, D3D12RenderDevice device, D3D12DescriptorAllocator descriptorAllocator, D3D12ResourceDatabase resourceDatabase)
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
        _resourceDatabase = resourceDatabase;
    }

    ~D3D12ResourceAllocator()
    {
        Dispose();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Handle<GPUResource> TrackResource(ref readonly Allocation allocation, ResourceStates state, D3D12ResourceDescriptor resourceDescriptor, ResourceDesc desc, bool isTemp)
    {
        var handle = _resourceDatabase.AddResource(in allocation, _renderSystem.CPUFenceValue, D3D12StatesToRHIState(state), resourceDescriptor, desc);

        if (isTemp)
        {
            _temResources.Enqueue(handle);
        }

        return handle;
    }

    private void CreateSRV(ID3D12Resource* pResource, CpuDescriptorHandle descriptorHandle, Format format, TextureDimension dimension, uint mipLevels, uint arraySize)
    {
        var srvDesc = new ShaderResourceViewDescription
        {
            Format = format,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
        };

        switch (dimension)
        {
            case TextureDimension.Texture2D:
                srvDesc.ViewDimension = SrvDimension.Texture2D;
                srvDesc.Texture2D = new Texture2DSrv
                {
                    MipLevels = mipLevels,
                };
                break;
            case TextureDimension.Texture3D:
                srvDesc.ViewDimension = SrvDimension.Texture3D;
                srvDesc.Texture3D = new Texture3DSrv
                {
                    MipLevels = 0,
                };
                break;
            case TextureDimension.Texture2DArray:
                srvDesc.ViewDimension = SrvDimension.Texture2DArray;
                srvDesc.Texture2DArray = new Texture2DArraySrv
                {
                    MipLevels = mipLevels,
                    ArraySize = arraySize,
                };
                break;
            case TextureDimension.TextureCube:
                srvDesc.ViewDimension = SrvDimension.TextureCube;
                srvDesc.TextureCube = new TexureCubeSrv
                {
                    MipLevels = mipLevels,
                };
                break;
            case TextureDimension.TextureCubeArray:
                srvDesc.ViewDimension = SrvDimension.TextureCubeArray;
                srvDesc.TextureCubeArray = new TexureCubeArraySrv
                {
                    MipLevels = mipLevels,
                    NumCubes = arraySize / 6,
                };
                break;
            default:
                throw new ArgumentException($"Unsupported texture dimension for SRV: {dimension}");
        }

        _device->CreateShaderResourceView(pResource, &srvDesc, descriptorHandle);
    }

    public Handle<Texture> CreateTexture(ref readonly TextureDesc desc, bool isTemp = false)
    {
        CheckTexture2DSize(desc.Width, desc.Height);

        var d3d12Format = ConvertTextureFormat(desc.Format);
        var mipLevels = desc.MipLevels == 0 ? (ushort)(1 + Math.Floor(Math.Log2(Math.Max(desc.Width, desc.Height)))) : (ushort)desc.MipLevels;

        var resourceFlags = ConvertTextureUsage(desc.Usage);
        var resourceDesc = desc.Dimension switch
        {
            TextureDimension.Texture2D => ResourceDescription.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                flags: resourceFlags),
            TextureDimension.Texture3D => ResourceDescription.Tex3D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                (ushort)desc.Slice,
                                flags: resourceFlags),
            TextureDimension.TextureCube => ResourceDescription.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: 6,
                                flags: resourceFlags),
            TextureDimension.Texture2DArray => ResourceDescription.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: (ushort)desc.Slice,
                                flags: resourceFlags),
            TextureDimension.TextureCubeArray => ResourceDescription.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: (ushort)(desc.Slice * 6),
                                flags: resourceFlags),
            _ => throw new ArgumentException($"Unsupported texture dimension: {desc.Dimension}"),
        };

        var allocationDesc = new AllocationDesc
        {
            HeapType = HeapType.Default,
            Flags = AllocationFlags.None
        };

        var initialState = DetermineInitialTextureState(desc.Usage);

        Allocation allocation = default;
        ThrowIfFailed(_allocator.CreateResource(&allocationDesc, in resourceDesc, initialState, null, &allocation, IID_NULL, null));

        var resourceDescriptor = D3D12ResourceDescriptor.Invalid;
        if (desc.Usage.HasFlag(TextureUsage.ShaderResource))
        {
            resourceDescriptor.srv = _descriptorAllocator.AllocateCbvSrvUav(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.srv);

            CreateSRV(allocation.Resource, cpuHandle, d3d12Format, desc.Dimension, mipLevels, desc.Slice);
        }

        if (desc.Usage.HasFlag(TextureUsage.RenderTarget))
        {
            resourceDescriptor.rtv = _descriptorAllocator.AllocateRTV(isTemp);
            var rtvDesc = new RenderTargetViewDescription(allocation.Resource);
            _device->CreateRenderTargetView(allocation.Resource, &rtvDesc, _descriptorAllocator.GetCpuHandle(resourceDescriptor.rtv));
        }

        if (desc.Usage.HasFlag(TextureUsage.DepthStencil))
        {
            resourceDescriptor.dsv = _descriptorAllocator.AllocateDSV(isTemp);
            var dsvDesc = new DepthStencilViewDescription(allocation.Resource);
            _device->CreateDepthStencilView(allocation.Resource, &dsvDesc, _descriptorAllocator.GetCpuHandle(resourceDescriptor.dsv));
        }

        if (desc.Usage.HasFlag(TextureUsage.UnorderedAccess))
        {
            resourceDescriptor.uav = _descriptorAllocator.AllocateCbvSrvUav(isTemp);
            var uavDesc = new UnorderedAccessViewDescription
            {
                ViewDimension = UavDimension.Texture2D,
                Format = d3d12Format,
                Texture2D = new Texture2DUav
                {
                    MipSlice = 0,
                    PlaneSlice = 0,
                }
            };
            _device->CreateUnorderedAccessView(allocation.Resource, null, &uavDesc, _descriptorAllocator.GetCpuHandle(resourceDescriptor.uav));
        }

        var handle = TrackResource(ref allocation, initialState, resourceDescriptor, ResourceDesc.Texture(desc), isTemp);

        return handle.AsTexture();
    }

    public Handle<Texture> CreateRenderTarget(ref readonly RenderTargetDesc desc, bool isTemp = false)
    {
        var textureDesc = RenderTargetDesc.ToTextureDescripton(desc);
        return CreateTexture(ref textureDesc, isTemp);
    }

    public Handle<GraphicsBuffer> CreateBuffer(ref readonly BufferDesc desc, bool isTemp = false)
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

        var resourceDescriptor = D3D12ResourceDescriptor.Invalid;
        if (desc.Usage.HasFlag(BufferUsage.ShaderResource) && desc.CreationFlags.HasFlag(BufferCreationFlags.Bindless))
        {
            resourceDescriptor.srv = _descriptorAllocator.AllocateCbvSrvUav(isTemp);

            var srvDesc = new ShaderResourceViewDescription
            {
                ViewDimension = SrvDimension.Buffer,
                Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
            };

            if (desc.Usage.HasFlag(BufferUsage.Raw))
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

            _device->CreateShaderResourceView(allocation.Resource, &srvDesc, _descriptorAllocator.GetCpuHandle(resourceDescriptor.srv));
        }

        var handle = TrackResource(ref allocation, initialState, resourceDescriptor, ResourceDesc.Buffer(desc), isTemp);
        return handle.AsGraphicsBuffer();
    }

    public Handle<GraphicsBuffer> CreateUploadBuffer(ulong size, bool isTemp = true)
    {
        var desc = new BufferDesc
        {
            Size = size,
            Usage = BufferUsage.Upload,
            MemoryType = MemoryType.Upload,
        };

        return CreateBuffer(ref desc, isTemp);
    }

    public Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices)
    {
        var vertexBufferDesc = new BufferDesc
        {
            Size = (ulong)(vertices.Count * Unsafe.SizeOf<Vertex>()),
            Stride = (uint)Unsafe.SizeOf<Vertex>(),
            Usage = BufferUsage.Vertex | BufferUsage.ShaderResource,
            MemoryType = MemoryType.Default,
            CreationFlags = BufferCreationFlags.Bindless
        };

        var indexBufferDesc = new BufferDesc
        {
            Size = (ulong)(indices.Count * sizeof(uint)),
            Stride = sizeof(uint),
            Usage = BufferUsage.Index | BufferUsage.ShaderResource,
            MemoryType = MemoryType.Default,
            CreationFlags = BufferCreationFlags.Bindless
        };

        var vertexBuffer = CreateBuffer(ref vertexBufferDesc);
        var indexBuffer = CreateBuffer(ref indexBufferDesc);

        var data = new Mesh
        {
            vertices = vertices,
            indices = indices,
            vertexBuffer = vertexBuffer,
            indexBuffer = indexBuffer,
        };

        return _resourceDatabase.AddMesh(ref data);
    }

    public Handle<Material> CreateMaterial(Identifier<Shader> shader)
    {
        var materialData = new Material
        {
            Shader = shader,
        };

        ref var shaderRef = ref _resourceDatabase.GetShaderReference(shader);

        if (shaderRef.ConstantBuffers.Count > 0)
        {
            var maxSlot = shaderRef.ConstantBuffers.Max(cb => cb.RegisterSlot);
            materialData._cBufferCaches = new UnsafeArray<CBufferCache>((int)maxSlot + 1, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);

            foreach (var cbufferInfo in shaderRef.ConstantBuffers)
            {
                var desc = new BufferDesc
                {
                    Size = cbufferInfo.Size,
                    Usage = BufferUsage.Constant,
                    MemoryType = MemoryType.Default,
                };

                var buffer = CreateBuffer(ref desc);

                materialData._cBufferCaches[cbufferInfo.RegisterSlot] = new CBufferCache(buffer, cbufferInfo.Size);
            }
        }

        return _resourceDatabase.AddMaterial(ref materialData);
    }

    public Identifier<Shader> CreateShader()
    {
        var shaderData = new Shader();
        return _resourceDatabase.AddShader(ref shaderData);
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

    private static ResourceState D3D12StatesToRHIState(ResourceStates states)
    {
        switch (states)
        {
            //case ResourceStates.None:
            //case ResourceStates.Present:
            case ResourceStates.Common:
                return ResourceState.Common;
            case ResourceStates.VertexAndConstantBuffer:
                return ResourceState.VertexAndConstantBuffer;
            case ResourceStates.IndexBuffer:
                return ResourceState.IndexBuffer;
            case ResourceStates.RenderTarget:
                return ResourceState.RenderTarget;
            case ResourceStates.UnorderedAccess:
                return ResourceState.UnorderedAccess;
            case ResourceStates.DepthWrite:
                return ResourceState.DepthWrite;
            case ResourceStates.DepthRead:
                return ResourceState.DepthRead;
            case ResourceStates.PixelShaderResource:
                return ResourceState.PixelShaderResource;
            //case ResourceStates.Predication:
            case ResourceStates.IndirectArgument:
                return ResourceState.IndirectArgument;
            case ResourceStates.CopyDest:
                return ResourceState.CopyDest;
            case ResourceStates.CopySource:
                return ResourceState.CopySource;
            case ResourceStates.GenericRead:
                return ResourceState.GenericRead;
            default:
                return ResourceState.Common;
        }
    }

    #endregion

    public void ReleaseTempResources()
    {
        while (_temResources.Count > 0)
        {
            var handle = _temResources.Peek();
            ref var info = ref _resourceDatabase.GetResourceInfo(handle, out var exist);
            if (!exist || !info.Allocated)
            {
                // Resource already released or invalid, just dequeue
                _temResources.Dequeue();
                continue;
            }

            if (info.cpuFenceValue > _renderSystem.GPUFenceValue)
            {
                // Resource still in use by GPU, stop processing.
                // Since resources are enqueued in order, we can break here.
                break;
            }

            _resourceDatabase.ReleaseResource(handle);
            _temResources.Dequeue();
        }
    }

    public void ReleaseResource(Handle<GPUResource> handle)
    {
        _resourceDatabase.ReleaseResource(handle);
    }

    public void Dispose()
    {
#if DEBUG || GHOST_EDITOR
        if (_temResources.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_temResources.Count} temp allocations still registered. Ensure all resources are released before disposing.");
        }
#endif

        foreach (var handle in _temResources)
        {
            ReleaseResource(handle);
        }

        _temResources.Dispose();
        _allocator.Release();

        GC.SuppressFinalize(this);
    }
}