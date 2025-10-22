using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.D3D12MA_Alias;
using static TerraFX.Aliases.DXGI_Alias;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12ResourceAllocator : IResourceAllocator, IDisposable
{
    private const uint _MAX_BYTES = D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_A_TERM * 1024u * 1024u;
    private const uint _MAX_TEXTURE2D_DIMENSION = 16384u;
    private const uint _MAX_TEXTURE3D_DIMENSION = 2048u;

    private ComPtr<D3D12MA_Allocator> _allocator;

    private readonly D3D12RenderDevice _device;
    private readonly RenderSystem _renderSystem;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private UnsafeQueue<Handle<GPUResource>> _temResources = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);

    public D3D12ResourceAllocator(RenderSystem renderSystem, D3D12RenderDevice device, D3D12DescriptorAllocator descriptorAllocator, D3D12ResourceDatabase resourceDatabase)
    {
        var desc = new D3D12MA_ALLOCATOR_DESC
        {
            pAdapter = (IDXGIAdapter*)device.Adapter,
            pDevice = (ID3D12Device*)device.NativeDevice,
            Flags = D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED | D3D12MA_ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED,
        };

        D3D12MA_CreateAllocator(&desc, _allocator.GetAddressOf());

        _device = device;
        _renderSystem = renderSystem;
        _descriptorAllocator = descriptorAllocator;
        _resourceDatabase = resourceDatabase;
    }

    ~D3D12ResourceAllocator()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckBufferSize(ulong sizeInBytes)
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
    private Handle<GPUResource> TrackResource(ComPtr<D3D12MA_Allocation> allocation, D3D12_RESOURCE_STATES state, ResourceViewGroup resourceDescriptor, ResourceDesc desc, bool isTemp)
    {
        var handle = _resourceDatabase.AddResource(allocation, _renderSystem.CPUFenceValue, D3D12StatesToRHIState(state), resourceDescriptor, desc);

        if (isTemp)
        {
            _temResources.Enqueue(handle);
        }

        return handle;
    }

    private D3D12_SHADER_RESOURCE_VIEW_DESC CreateSrvDesc(ID3D12Resource* pResource, bool isCubeMap, uint mipLevels, uint arraySize)
    {
        var resourceDesc = pResource->GetDesc();
        var srvDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC
        {
            Format = resourceDesc.Format,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
        };

        switch (resourceDesc.Dimension)
        {
            case D3D12_RESOURCE_DIMENSION_BUFFER:
                srvDesc.ViewDimension = D3D12_SRV_DIMENSION_BUFFER;
                srvDesc.Buffer = new D3D12_BUFFER_SRV
                {
                    FirstElement = 0,
                    NumElements = (uint)(resourceDesc.Width / 4),
                    StructureByteStride = 0,
                    Flags = D3D12_BUFFER_SRV_FLAG_RAW,
                };
                break;

            case D3D12_RESOURCE_DIMENSION_TEXTURE1D:
                if (resourceDesc.DepthOrArraySize > 1)
                {
                    srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE1DARRAY;
                    srvDesc.Texture1DArray = new D3D12_TEX1D_ARRAY_SRV
                    {
                        MipLevels = mipLevels,
                        ArraySize = arraySize,
                    };
                }
                else
                {
                    srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE1D;
                    srvDesc.Texture1D = new D3D12_TEX1D_SRV
                    {
                        MipLevels = mipLevels,
                    };
                }
                break;

            case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                if (resourceDesc.DepthOrArraySize > 1)
                {
                    if (isCubeMap)
                    {
                        srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURECUBEARRAY;
                        srvDesc.TextureCubeArray = new D3D12_TEXCUBE_ARRAY_SRV
                        {
                            MipLevels = mipLevels,
                            NumCubes = arraySize / 6,
                        };
                    }
                    else
                    {
                        srvDesc.ViewDimension = resourceDesc.SampleDesc.Count > 1 ? D3D12_SRV_DIMENSION_TEXTURE2DMSARRAY : D3D12_SRV_DIMENSION_TEXTURE2DARRAY;
                        srvDesc.Texture2DArray = new D3D12_TEX2D_ARRAY_SRV
                        {
                            MipLevels = mipLevels,
                            ArraySize = arraySize,
                        };
                    }
                }
                else
                {
                    if (isCubeMap)
                    {
                        srvDesc.ViewDimension = D3D12_SRV_DIMENSION_TEXTURECUBE;
                        srvDesc.TextureCube = new D3D12_TEXCUBE_SRV
                        {
                            MipLevels = mipLevels,
                        };
                    }
                    else
                    {
                        srvDesc.ViewDimension = resourceDesc.SampleDesc.Count > 1 ? D3D12_SRV_DIMENSION_TEXTURE2DMS : D3D12_SRV_DIMENSION_TEXTURE2D;
                        srvDesc.Texture2D = new D3D12_TEX2D_SRV
                        {
                            MipLevels = mipLevels,
                        };
                    }
                }
                break;

            case D3D12_RESOURCE_DIMENSION_TEXTURE3D:
                srvDesc.Texture3D = new D3D12_TEX3D_SRV
                {
                    MipLevels = mipLevels,
                };
                break;
            default:
                throw new ArgumentException($"Unsupported texture dimension for SRV: {resourceDesc.Dimension}");
        }

        return srvDesc;
    }

    private D3D12_RENDER_TARGET_VIEW_DESC CreateRtvDesc(ID3D12Resource* pResource, uint mipSlice = 0, uint firstArraySlice = 0, uint planeSlice = 0)
    {
        var resourceDesc = pResource->GetDesc();
        var rtvDesc = new D3D12_RENDER_TARGET_VIEW_DESC();

        switch (resourceDesc.Dimension)
        {
            case D3D12_RESOURCE_DIMENSION_BUFFER:
                rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_BUFFER;
                break;

            case D3D12_RESOURCE_DIMENSION_TEXTURE1D:
                rtvDesc.ViewDimension = resourceDesc.DepthOrArraySize > 1 ? D3D12_RTV_DIMENSION_TEXTURE1DARRAY : D3D12_RTV_DIMENSION_TEXTURE1D;
                break;

            case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                if (resourceDesc.SampleDesc.Count > 1)
                {
                    rtvDesc.ViewDimension = resourceDesc.DepthOrArraySize > 1 ? D3D12_RTV_DIMENSION_TEXTURE2DMSARRAY : D3D12_RTV_DIMENSION_TEXTURE2DMS;
                }
                else
                {
                    rtvDesc.ViewDimension = resourceDesc.DepthOrArraySize > 1 ? D3D12_RTV_DIMENSION_TEXTURE2DARRAY : D3D12_RTV_DIMENSION_TEXTURE2D;
                }
                break;

            case D3D12_RESOURCE_DIMENSION_TEXTURE3D:
                rtvDesc.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE3D;
                break;

            default:
                throw new ArgumentException($"Unsupported texture dimension for SRV: {resourceDesc.Dimension}");
        }

        rtvDesc.Format = resourceDesc.Format;

        var isArray =
            rtvDesc.ViewDimension == D3D12_RTV_DIMENSION_TEXTURE2DARRAY ||
            rtvDesc.ViewDimension == D3D12_RTV_DIMENSION_TEXTURE2DMSARRAY;

        var arraySize = 1u;
        if (isArray)
        {
            arraySize = resourceDesc.ArraySize() - firstArraySlice;
        }

        switch (rtvDesc.ViewDimension)
        {
            case D3D12_RTV_DIMENSION_BUFFER:
                rtvDesc.Buffer.FirstElement = firstArraySlice;
                rtvDesc.Buffer.NumElements = arraySize;
                break;

            case D3D12_RTV_DIMENSION_TEXTURE1D:
                rtvDesc.Texture1D.MipSlice = mipSlice;
                break;

            case D3D12_RTV_DIMENSION_TEXTURE1DARRAY:
                rtvDesc.Texture1DArray.MipSlice = mipSlice;
                rtvDesc.Texture1DArray.FirstArraySlice = firstArraySlice;
                rtvDesc.Texture1DArray.ArraySize = arraySize;
                break;

            case D3D12_RTV_DIMENSION_TEXTURE2D:
                rtvDesc.Texture2D.MipSlice = mipSlice;
                rtvDesc.Texture2D.PlaneSlice = planeSlice;
                break;

            case D3D12_RTV_DIMENSION_TEXTURE2DARRAY:
                rtvDesc.Texture2DArray.MipSlice = mipSlice;
                rtvDesc.Texture2DArray.FirstArraySlice = firstArraySlice;
                rtvDesc.Texture2DArray.ArraySize = arraySize;
                rtvDesc.Texture2DArray.PlaneSlice = planeSlice;
                break;

            case D3D12_RTV_DIMENSION_TEXTURE2DMS:
                break;

            case D3D12_RTV_DIMENSION_TEXTURE2DMSARRAY:
                rtvDesc.Texture2DMSArray.FirstArraySlice = firstArraySlice;
                rtvDesc.Texture2DMSArray.ArraySize = arraySize;
                break;

            case D3D12_RTV_DIMENSION_TEXTURE3D:
                rtvDesc.Texture3D.MipSlice = mipSlice;
                rtvDesc.Texture3D.FirstWSlice = firstArraySlice;
                rtvDesc.Texture3D.WSize = arraySize;
                break;

            default:
                throw new ArgumentException($"Unsupported RTV dimension: {rtvDesc.ViewDimension}");
        }

        return rtvDesc;
    }

    private D3D12_DEPTH_STENCIL_VIEW_DESC CreateDsvDesc(ID3D12Resource* pResource, uint mipSlice = 0, uint firstArraySlice = 0, D3D12_DSV_FLAGS flags = D3D12_DSV_FLAG_NONE)
    {
        var resourceDesc = pResource->GetDesc();
        var dsvDesc = new D3D12_DEPTH_STENCIL_VIEW_DESC
        {
            Flags = flags,
        };

        switch (resourceDesc.Dimension)
        {
            case D3D12_RESOURCE_DIMENSION_TEXTURE1D:
                dsvDesc.ViewDimension = resourceDesc.DepthOrArraySize > 1 ? D3D12_DSV_DIMENSION_TEXTURE1DARRAY : D3D12_DSV_DIMENSION_TEXTURE1D;
                break;
            case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                if (resourceDesc.SampleDesc.Count > 1)
                {
                    dsvDesc.ViewDimension = resourceDesc.DepthOrArraySize > 1 ? D3D12_DSV_DIMENSION_TEXTURE2DMSARRAY : D3D12_DSV_DIMENSION_TEXTURE2DMS;
                }
                else
                {
                    dsvDesc.ViewDimension = resourceDesc.DepthOrArraySize > 1 ? D3D12_DSV_DIMENSION_TEXTURE2DARRAY : D3D12_DSV_DIMENSION_TEXTURE2D;
                }
                break;
        }

        dsvDesc.Format = resourceDesc.Format;

        var isArray =
            dsvDesc.ViewDimension == D3D12_DSV_DIMENSION_TEXTURE2DARRAY ||
            dsvDesc.ViewDimension == D3D12_DSV_DIMENSION_TEXTURE2DMSARRAY;

        var arraySize = 1u;
        if (isArray)
        {
            arraySize = resourceDesc.ArraySize() - firstArraySlice;
        }

        switch (dsvDesc.ViewDimension)
        {
            case D3D12_DSV_DIMENSION_TEXTURE1D:
                dsvDesc.Texture1D.MipSlice = mipSlice;
                break;
            case D3D12_DSV_DIMENSION_TEXTURE1DARRAY:
                dsvDesc.Texture1DArray.MipSlice = mipSlice;
                dsvDesc.Texture1DArray.FirstArraySlice = firstArraySlice;
                dsvDesc.Texture1DArray.ArraySize = arraySize;
                break;
            case D3D12_DSV_DIMENSION_TEXTURE2D:
                dsvDesc.Texture2D.MipSlice = mipSlice;
                break;
            case D3D12_DSV_DIMENSION_TEXTURE2DARRAY:
                dsvDesc.Texture2DArray.MipSlice = mipSlice;
                dsvDesc.Texture2DArray.FirstArraySlice = firstArraySlice;
                dsvDesc.Texture2DArray.ArraySize = arraySize;
                break;
            case D3D12_DSV_DIMENSION_TEXTURE2DMS:
                break;
            case D3D12_DSV_DIMENSION_TEXTURE2DMSARRAY:
                dsvDesc.Texture2DMSArray.FirstArraySlice = firstArraySlice;
                dsvDesc.Texture2DMSArray.ArraySize = arraySize;
                break;
            default:
                break;
        }

        return dsvDesc;
    }

    public Handle<Texture> CreateTexture(ref readonly TextureDesc desc, bool isTemp = false)
    {
        CheckTexture2DSize(desc.Width, desc.Height);

        var d3d12Format = ConvertTextureFormat(desc.Format);
        var mipLevels = desc.MipLevels == 0 ? (ushort)(1 + Math.Floor(Math.Log2(Math.Max(desc.Width, desc.Height)))) : (ushort)desc.MipLevels;

        var resourceFlags = ConvertTextureUsage(desc.Usage);
        var resourceDesc = desc.Dimension switch
        {
            TextureDimension.Texture2D => D3D12_RESOURCE_DESC.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                flags: resourceFlags),
            TextureDimension.Texture3D => D3D12_RESOURCE_DESC.Tex3D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                (ushort)desc.Slice,
                                flags: resourceFlags),
            TextureDimension.TextureCube => D3D12_RESOURCE_DESC.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: 6,
                                flags: resourceFlags),
            TextureDimension.Texture2DArray => D3D12_RESOURCE_DESC.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: (ushort)desc.Slice,
                                flags: resourceFlags),
            TextureDimension.TextureCubeArray => D3D12_RESOURCE_DESC.Tex2D(
                                d3d12Format,
                                desc.Width,
                                desc.Height,
                                mipLevels: mipLevels,
                                arraySize: (ushort)(desc.Slice * 6),
                                flags: resourceFlags),
            _ => throw new ArgumentException($"Unsupported texture dimension: {desc.Dimension}"),
        };

        var allocationDesc = new D3D12MA_ALLOCATION_DESC
        {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
            Flags = D3D12MA_ALLOCATION_FLAG_NONE
        };

        var initialState = DetermineInitialTextureState(desc.Usage);

        ComPtr<D3D12MA_Allocation> allocation = default;
        ThrowIfFailed(_allocator.Get()->CreateResource(&allocationDesc, &resourceDesc, initialState, null, allocation.GetAddressOf(), Win32Utility.IID_NULL, null));

        var resourceDescriptor = ResourceViewGroup.Invalid;
        if (desc.Usage.HasFlag(TextureUsage.ShaderResource))
        {
            resourceDescriptor.srv = _descriptorAllocator.AllocateCbvSrvUav(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.srv);

            var isCubeMap = desc.Dimension == TextureDimension.TextureCube || desc.Dimension == TextureDimension.TextureCubeArray;
            var srvDesc = CreateSrvDesc(allocation.Get()->GetResource(), isCubeMap, mipLevels, desc.Slice);

            _device.NativeDevice->CreateShaderResourceView(allocation.Get()->GetResource(), &srvDesc, cpuHandle);
        }

        if (desc.Usage.HasFlag(TextureUsage.RenderTarget))
        {
            resourceDescriptor.rtv = _descriptorAllocator.AllocateRTV(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.rtv);
            var rtvDesc = CreateRtvDesc(allocation.Get()->GetResource());

            _device.NativeDevice->CreateRenderTargetView(allocation.Get()->GetResource(), &rtvDesc, cpuHandle);
        }

        if (desc.Usage.HasFlag(TextureUsage.DepthStencil))
        {
            resourceDescriptor.dsv = _descriptorAllocator.AllocateDSV(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.dsv);
            var dsvDesc = CreateDsvDesc(allocation.Get()->GetResource());

            _device.NativeDevice->CreateDepthStencilView(allocation.Get()->GetResource(), &dsvDesc, cpuHandle);
        }

        if (desc.Usage.HasFlag(TextureUsage.UnorderedAccess))
        {
            resourceDescriptor.uav = _descriptorAllocator.AllocateCbvSrvUav(isTemp);
            var uavDesc = new D3D12_UNORDERED_ACCESS_VIEW_DESC
            {
                ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D,
                Format = d3d12Format,
                Texture2D = new D3D12_TEX2D_UAV
                {
                    MipSlice = 0,
                    PlaneSlice = 0,
                }
            };
            _device.NativeDevice->CreateUnorderedAccessView(allocation.Get()->GetResource(), null, &uavDesc, _descriptorAllocator.GetCpuHandle(resourceDescriptor.uav));
        }

        var handle = TrackResource(allocation, initialState, resourceDescriptor, ResourceDesc.Texture(desc), isTemp);

        return handle.AsTexture();
    }

    public Handle<Texture> CreateRenderTarget(ref readonly RenderTargetDesc desc, bool isTemp = false)
    {
        var textureDesc = RenderTargetDesc.ToTextureDescripton(desc);
        return CreateTexture(ref textureDesc, isTemp);
    }

    public Handle<GraphicsBuffer> CreateBuffer(ref readonly BufferDesc desc, bool isTemp = false)
    {
        CheckBufferSize(desc.Size);

        var resourceDescription = D3D12_RESOURCE_DESC.Buffer(desc.Size, ConvertBufferUsage(desc.Usage));
        var allocationDesc = new D3D12MA_ALLOCATION_DESC
        {
            HeapType = ConvertMemoryType(desc.MemoryType),
            Flags = D3D12MA_ALLOCATION_FLAG_NONE
        };

        var initialState = DetermineInitialBufferState(desc.Usage, desc.MemoryType);

        ComPtr<D3D12MA_Allocation> allocation = default;
        ThrowIfFailed(_allocator.Get()->CreateResource(&allocationDesc, &resourceDescription, initialState, null, allocation.GetAddressOf(), Win32Utility.IID_NULL, null));
        
        var resourceDescriptor = ResourceViewGroup.Invalid;
        if (desc.Usage.HasFlag(BufferUsage.ShaderResource))
        {
            resourceDescriptor.srv = _descriptorAllocator.AllocateCbvSrvUav(isTemp);

            var srvDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC
            {
                ViewDimension = D3D12_SRV_DIMENSION_BUFFER,
                Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
            };

            if (desc.Usage.HasFlag(BufferUsage.Raw))
            {
                srvDesc.Format = DXGI_FORMAT_R32_TYPELESS;
                srvDesc.Buffer.FirstElement = 0;
                srvDesc.Buffer.NumElements = (uint)(desc.Size / 4u);
                srvDesc.Buffer.StructureByteStride = 0;
                srvDesc.Buffer.Flags = D3D12_BUFFER_SRV_FLAG_RAW;
            }
            else
            {
                srvDesc.Format = DXGI_FORMAT_UNKNOWN;
                srvDesc.Buffer.FirstElement = 0;
                srvDesc.Buffer.NumElements = (uint)(desc.Size / desc.Stride);
                srvDesc.Buffer.StructureByteStride = desc.Stride;
                srvDesc.Buffer.Flags = D3D12_BUFFER_SRV_FLAG_NONE;
            }

            _device.NativeDevice->CreateShaderResourceView(allocation.Get()->GetResource(), &srvDesc, _descriptorAllocator.GetCpuHandle(resourceDescriptor.srv));
        }

        var handle = TrackResource(allocation, initialState, resourceDescriptor, ResourceDesc.Buffer(desc), isTemp);
        return handle.AsGraphicsBuffer();
    }

    public Handle<GraphicsBuffer> CreateUploadBuffer(ulong size, bool isTemp = true)
    {
        var desc = new BufferDesc
        {
            Size = size,
            Usage = BufferUsage.Upload,
            MemoryType = ResourceMemoryType.Upload,
        };

        return CreateBuffer(ref desc, isTemp);
    }

    public Handle<Mesh> CreateMesh(UnsafeList<Vertex> vertices, UnsafeList<uint> indices)
    {
        var vertexBufferDesc = new BufferDesc
        {
            Size = (uint)(vertices.Count * Unsafe.SizeOf<Vertex>()),
            Stride = (uint)Unsafe.SizeOf<Vertex>(),
            Usage = BufferUsage.Vertex | BufferUsage.ShaderResource,
            MemoryType = ResourceMemoryType.Default,
        };

        var indexBufferDesc = new BufferDesc
        {
            Size = (uint)(indices.Count * sizeof(uint)),
            Stride = sizeof(uint),
            Usage = BufferUsage.Index | BufferUsage.ShaderResource,
            MemoryType = ResourceMemoryType.Default,
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

        // TODO: Get per-material constant buffer size from database

        var desc = new BufferDesc
        {
            Size = shaderRef.PerMaterialBufferInfo.Size,
            Usage = BufferUsage.Constant,
            MemoryType = ResourceMemoryType.Default,
        };

        var buffer = CreateBuffer(ref desc);
        materialData._materialPropertiesCache = new CBufferCache(buffer, shaderRef.PerMaterialBufferInfo.Size);

        return _resourceDatabase.AddMaterial(ref materialData);
    }

    public Identifier<Shader> CreateShader(ShaderDescriptor descriptor)
    {
        var shaderData = new Shader();
        return _resourceDatabase.AddShader(ref shaderData);
    }

    #region Conversion Methods

    private static DXGI_FORMAT ConvertTextureFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8G8B8A8_UNorm => DXGI_FORMAT_R8G8B8A8_UNORM,
            TextureFormat.B8G8R8A8_UNorm => DXGI_FORMAT_B8G8R8A8_UNORM,
            TextureFormat.R16G16B16A16_Float => DXGI_FORMAT_R16G16B16A16_FLOAT,
            TextureFormat.R32G32B32A32_Float => DXGI_FORMAT_R32G32B32A32_FLOAT,
            TextureFormat.D24_UNorm_S8_UInt => DXGI_FORMAT_D24_UNORM_S8_UINT,
            TextureFormat.D32_Float => DXGI_FORMAT_D32_FLOAT,
            _ => throw new ArgumentException($"Unsupported texture format: {format}")
        };
    }

    private static D3D12_RESOURCE_FLAGS ConvertTextureUsage(TextureUsage usage)
    {
        var flags = D3D12_RESOURCE_FLAG_NONE;

        if (usage.HasFlag(TextureUsage.RenderTarget))
        {
            flags |= D3D12_RESOURCE_FLAG_ALLOW_RENDER_TARGET;
        }

        if (usage.HasFlag(TextureUsage.DepthStencil))
        {
            flags |= D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;
        }

        if (usage.HasFlag(TextureUsage.UnorderedAccess))
        {
            flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
        }

        return flags;
    }

    private static D3D12_RESOURCE_FLAGS ConvertBufferUsage(BufferUsage usage)
    {
        var flags = D3D12_RESOURCE_FLAG_NONE;

        if (usage.HasFlag(BufferUsage.Raw))
        {
            flags |= D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS;
        }

        return flags;
    }

    private static D3D12_HEAP_TYPE ConvertMemoryType(ResourceMemoryType memoryType)
    {
        return memoryType switch
        {
            ResourceMemoryType.Default => D3D12_HEAP_TYPE_DEFAULT,
            ResourceMemoryType.Upload => D3D12_HEAP_TYPE_UPLOAD,
            ResourceMemoryType.Readback => D3D12_HEAP_TYPE_READBACK,
            _ => throw new ArgumentException($"Unsupported memory type: {memoryType}")
        };
    }

    private static D3D12_RESOURCE_STATES DetermineInitialTextureState(TextureUsage usage)
    {
        if (usage.HasFlag(TextureUsage.RenderTarget))
        {
            return D3D12_RESOURCE_STATE_RENDER_TARGET;
        }

        if (usage.HasFlag(TextureUsage.DepthStencil))
        {
            return D3D12_RESOURCE_STATE_DEPTH_WRITE;
        }

        if (usage.HasFlag(TextureUsage.UnorderedAccess))
        {
            return D3D12_RESOURCE_STATE_UNORDERED_ACCESS;
        }

        return D3D12_RESOURCE_STATE_COMMON;
    }

    private static D3D12_RESOURCE_STATES DetermineInitialBufferState(BufferUsage usage, ResourceMemoryType memoryType)
    {
        if (memoryType == ResourceMemoryType.Upload)
        {
            return D3D12_RESOURCE_STATE_GENERIC_READ;
        }

        if (memoryType == ResourceMemoryType.Readback)
        {
            return D3D12_RESOURCE_STATE_COPY_DEST;
        }

        if (usage.HasFlag(BufferUsage.Vertex))
        {
            return D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;
        }

        if (usage.HasFlag(BufferUsage.Index))
        {
            return D3D12_RESOURCE_STATE_INDEX_BUFFER;
        }

        if (usage.HasFlag(BufferUsage.Constant))
        {
            return D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;
        }

        return D3D12_RESOURCE_STATE_COMMON;
    }

    private static ResourceState D3D12StatesToRHIState(D3D12_RESOURCE_STATES states)
    {
        return states switch
        {
            //case ResourceStates.None:
            //case ResourceStates.Present:
            D3D12_RESOURCE_STATE_COMMON => ResourceState.Common,
            D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER => ResourceState.VertexAndConstantBuffer,
            D3D12_RESOURCE_STATE_INDEX_BUFFER => ResourceState.IndexBuffer,
            D3D12_RESOURCE_STATE_RENDER_TARGET => ResourceState.RenderTarget,
            D3D12_RESOURCE_STATE_UNORDERED_ACCESS => ResourceState.UnorderedAccess,
            D3D12_RESOURCE_STATE_DEPTH_WRITE => ResourceState.DepthWrite,
            D3D12_RESOURCE_STATE_DEPTH_READ => ResourceState.DepthRead,
            D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE => ResourceState.PixelShaderResource,
            //case ResourceStates.Predication:
            D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT => ResourceState.IndirectArgument,
            D3D12_RESOURCE_STATE_COPY_DEST => ResourceState.CopyDest,
            D3D12_RESOURCE_STATE_COPY_SOURCE => ResourceState.CopySource,
            D3D12_RESOURCE_STATE_GENERIC_READ => ResourceState.GenericRead,
            _ => ResourceState.Common,
        };
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
        _allocator.Dispose();

        GC.SuppressFinalize(this);
    }
}