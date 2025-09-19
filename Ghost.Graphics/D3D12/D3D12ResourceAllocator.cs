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
    private ResourceHandle TrackResource(ref readonly Allocation allocation, ResourceStates state, bool isTemp)
    {
        var handle = _resourceDatabase.AddResource(in allocation, _renderSystem.CPUFenceValue, D3D12StatesToRHIState(state));

        if (isTemp)
        {
            _temResources.Enqueue(handle);
        }

        return handle;
    }

    public TextureHandle CreateTexture(ref readonly TextureDesc desc, bool tempResource = false)
    {
        CheckTexture2DSize(desc.Width, desc.Height);

        var d3d12Format = ConvertTextureFormat(desc.Format);
        var mipLevels = desc.MipLevels == 0 ? (ushort)(1 + Math.Floor(Math.Log2(Math.Max(desc.Width, desc.Height)))) : (ushort)desc.MipLevels;

        var resourceDesc = desc.Dimension switch
        {
            TextureDimension.Texture2D => ResourceDescription.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                flags: ConvertTextureUsage(desc.Usage)),
            TextureDimension.Texture3D => ResourceDescription.Tex3D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                (ushort)desc.Slice,
                                flags: ConvertTextureUsage(desc.Usage)),
            //case TextureDimension.TextureCube:
            //    break;
            TextureDimension.Texture2DArray => ResourceDescription.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: (ushort)desc.Slice,
                                flags: ConvertTextureUsage(desc.Usage)),
            //case TextureDimension.TextureCubeArray:
            //    break;
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

        var handle = TrackResource(in allocation, initialState, tempResource);

        if (desc.CreationFlags.HasFlag(TextureCreationFlags.Bindless))
        {
            var descriptorHandle = _descriptorAllocator.AllocateBindless();
            var srvDesc = new ShaderResourceViewDescription
            {
                Format = d3d12Format,
                Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
            };

            switch (desc.Dimension)
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
                        ArraySize = desc.Slice,
                    };
                    break;
                default:
                    throw new ArgumentException($"Unsupported texture dimension for SRV: {desc.Dimension}");
            }

            _device->CreateShaderResourceView(allocation.Resource, &srvDesc, _descriptorAllocator.GetCpuHandle(descriptorHandle));
        }

        return new(handle);
    }

    public TextureHandle CreateRenderTarget(ref readonly RenderTargetDesc desc, bool tempResource = false)
    {
        var textureDesc = RenderTargetDesc.ToTextureDescriptor(desc);
        return CreateTexture(ref textureDesc, tempResource);
    }

    public BufferHandle CreateBuffer(ref readonly BufferDesc desc, bool tempResource = false)
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

        var handle = TrackResource(in allocation, initialState, tempResource);

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

    public BufferHandle CreateUploadBuffer(ulong size, bool temp = true)
    {
        var desc = new BufferDesc
        {
            Size = size,
            Usage = BufferUsage.Upload,
            MemoryType = MemoryType.Upload,
        };

        return CreateBuffer(in desc, temp);
    }

    public Identifier<Mesh> CreateMesh(ReadOnlySpan<Vertex> vertices, ReadOnlySpan<uint> indices)
    {
        var vertexBufferDesc = new BufferDesc
        {
            Size = (ulong)(vertices.Length * Unsafe.SizeOf<Vertex>()),
            Stride = (uint)Unsafe.SizeOf<Vertex>(),
            Usage = BufferUsage.Vertex | BufferUsage.ShaderResource,
            MemoryType = MemoryType.Default,
            CreationFlags = BufferCreationFlags.Bindless
        };

        var indexBufferDesc = new BufferDesc
        {
            Size = (ulong)(indices.Length * sizeof(uint)),
            Stride = sizeof(uint),
            Usage = BufferUsage.Index | BufferUsage.ShaderResource,
            MemoryType = MemoryType.Default,
            CreationFlags = BufferCreationFlags.Bindless
        };

        var vertexBuffer = CreateBuffer(ref vertexBufferDesc, true);
        var indexBuffer = CreateBuffer(ref indexBufferDesc, true);

        var data = new Mesh(vertices, indices, vertexBuffer, indexBuffer);
        return _resourceDatabase.AddMesh(in data);
    }

    public Identifier<Material> CreateMaterial(Identifier<Shader> shader)
    {
        var materialData = new Material
        {
            Shader = shader,
        };

        var shaderResource = _resourceDatabase.GetShader(shader);

        if (shaderResource.ConstantBuffers.Count > 0)
        {
            var maxSlot = shaderResource.ConstantBuffers.Max(cb => cb.RegisterSlot);
            materialData._cBufferCaches = new UnsafeArray<CBufferCache>((int)maxSlot + 1, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);

            foreach (var cbufferInfo in shaderResource.ConstantBuffers)
            {
                var desc = new BufferDesc
                {
                    Size = cbufferInfo.Size,
                    Usage = BufferUsage.Constant,
                    MemoryType = MemoryType.Default,
                };

                var buffer = CreateBuffer(in desc);

                materialData._cBufferCaches[cbufferInfo.RegisterSlot] = new CBufferCache(buffer, cbufferInfo.Size);
            }
        }

        return _resourceDatabase.AddMaterial(in materialData);
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

    public void ReleaseTempResource()
    {
        while (_temResources.Count > 0)
        {
            var handle = _temResources.Peek();
            ref var info = ref _resourceDatabase.GetResourceInfo(handle, out var exist);
            if (exist && info.Allocated && info.cpuFenceValue > _renderSystem.GPUFenceValue)
            {
                break;
            }

            ReleaseResource(handle);
            _temResources.Dequeue();
        }
    }

    public void ReleaseResource(ResourceHandle handle)
    {
        if (!handle.IsValid)
        {
            return;
        }

        ref var info = ref _resourceDatabase.GetResourceInfo(handle, out var exist);

        if (!exist || !info.Allocated)
        {
            return;
        }

        info.Dispose();
        _resourceDatabase.RemoveResource(handle);
    }

    public void Dispose()
    {
#if DEBUG
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