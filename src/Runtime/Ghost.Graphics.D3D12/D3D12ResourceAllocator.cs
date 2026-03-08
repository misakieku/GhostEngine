using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.D3D12MA_Alias;
using static TerraFX.Aliases.DXGI_Alias;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace Ghost.Graphics.D3D12;

internal sealed unsafe partial class D3D12ResourceAllocator
{
    // NOTE: _MAX_BYTES may not be accurate, we need to verify it with feature level checks.
    private const uint _MAX_BYTES = D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_A_TERM * 1024u * 1024u;
    private const uint _MAX_TEXTURE2D_DIMENSION = 16384u;
    private const uint _MAX_TEXTURE3D_DIMENSION = 2048u;

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

    private static D3D12_SHADER_RESOURCE_VIEW_DESC CreateTextureSrvDesc(ID3D12Resource* pResource, uint mipLevels, uint arraySize, bool isCubeMap)
    {
        var resourceDesc = pResource->GetDesc();
        var srvDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC
        {
            Format = resourceDesc.Format,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
        };

        switch (resourceDesc.Dimension)
        {
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

    private static D3D12_SHADER_RESOURCE_VIEW_DESC CreateBufferSrvDesc(ID3D12Resource* pResource, uint stride, bool isRaw)
    {
        var resourceDesc = pResource->GetDesc();
        var srvDesc = new D3D12_SHADER_RESOURCE_VIEW_DESC
        {
            ViewDimension = D3D12_SRV_DIMENSION_BUFFER,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
        };

        if (isRaw)
        {
            srvDesc.Format = DXGI_FORMAT_R32_TYPELESS;
            srvDesc.Buffer.FirstElement = 0;
            srvDesc.Buffer.NumElements = (uint)(resourceDesc.Width / 4u);
            srvDesc.Buffer.StructureByteStride = 0;
            srvDesc.Buffer.Flags = D3D12_BUFFER_SRV_FLAG_RAW;
        }
        else // Assumes Structured
        {
            srvDesc.Format = resourceDesc.Format;
            srvDesc.Buffer.FirstElement = 0;
            srvDesc.Buffer.NumElements = (uint)(resourceDesc.Width / stride);
            srvDesc.Buffer.StructureByteStride = stride;
            srvDesc.Buffer.Flags = D3D12_BUFFER_SRV_FLAG_NONE;
        }

        return srvDesc;
    }

    private static D3D12_RENDER_TARGET_VIEW_DESC CreateRtvDesc(ID3D12Resource* pResource, uint mipSlice = 0, uint firstArraySlice = 0, uint planeSlice = 0)
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

    private static D3D12_DEPTH_STENCIL_VIEW_DESC CreateDsvDesc(ID3D12Resource* pResource, uint mipSlice = 0, uint firstArraySlice = 0, D3D12_DSV_FLAGS flags = D3D12_DSV_FLAG_NONE)
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

    private static D3D12_UNORDERED_ACCESS_VIEW_DESC CreateTextureUavDesc(ID3D12Resource* pResource, uint mipSlice = 0, uint firstArraySlice = 0, uint planeSlice = 0)
    {
        var resourceDesc = pResource->GetDesc();
        var uavDesc = new D3D12_UNORDERED_ACCESS_VIEW_DESC
        {
            Format = resourceDesc.Format
        };

        switch (resourceDesc.Dimension)
        {
            case D3D12_RESOURCE_DIMENSION_TEXTURE1D:
                if (resourceDesc.DepthOrArraySize > 1)
                {
                    uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE1DARRAY;
                    uavDesc.Texture1DArray = new D3D12_TEX1D_ARRAY_UAV
                    {
                        MipSlice = mipSlice,
                        FirstArraySlice = firstArraySlice,
                        ArraySize = resourceDesc.ArraySize() - firstArraySlice
                    };
                }
                else
                {
                    uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE1D;
                    uavDesc.Texture1D = new D3D12_TEX1D_UAV
                    {
                        MipSlice = mipSlice
                    };
                }
                break;

            case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                if (resourceDesc.DepthOrArraySize > 1)
                {
                    uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2DARRAY;
                    uavDesc.Texture2DArray = new D3D12_TEX2D_ARRAY_UAV
                    {
                        MipSlice = mipSlice,
                        FirstArraySlice = firstArraySlice,
                        ArraySize = resourceDesc.ArraySize() - firstArraySlice,
                        PlaneSlice = planeSlice
                    };
                }
                else
                {
                    uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE2D;
                    uavDesc.Texture2D = new D3D12_TEX2D_UAV
                    {
                        MipSlice = mipSlice,
                        PlaneSlice = planeSlice
                    };
                }
                break;

            case D3D12_RESOURCE_DIMENSION_TEXTURE3D:
                uavDesc.ViewDimension = D3D12_UAV_DIMENSION_TEXTURE3D;
                uavDesc.Texture3D = new D3D12_TEX3D_UAV
                {
                    MipSlice = mipSlice,
                    FirstWSlice = firstArraySlice,
                    WSize = resourceDesc.Depth() - firstArraySlice
                };
                break;

            case D3D12_RESOURCE_DIMENSION_BUFFER:
                uavDesc.ViewDimension = D3D12_UAV_DIMENSION_BUFFER;
                uavDesc.Buffer = new D3D12_BUFFER_UAV
                {
                    FirstElement = 0,
                    NumElements = (uint)(resourceDesc.Width / 4), // Assuming R32_TYPELESS RAW
                    StructureByteStride = 0,
                    Flags = D3D12_BUFFER_UAV_FLAG_RAW
                };
                break;

            default:
                throw new ArgumentException($"Unsupported texture dimension for UAV: {resourceDesc.Dimension}");
        }

        return uavDesc;
    }

    private static D3D12_UNORDERED_ACCESS_VIEW_DESC CreateBufferUavDesc(ID3D12Resource* pResource, uint stride, bool isRaw)
    {
        var resourceDesc = pResource->GetDesc();
        var uavDesc = new D3D12_UNORDERED_ACCESS_VIEW_DESC
        {
            ViewDimension = D3D12_UAV_DIMENSION_BUFFER,
        };

        if (isRaw)
        {
            uavDesc.Format = DXGI_FORMAT_R32_TYPELESS;
            uavDesc.Buffer.FirstElement = 0;
            uavDesc.Buffer.NumElements = (uint)(resourceDesc.Width / 4u);
            uavDesc.Buffer.StructureByteStride = 0;
            uavDesc.Buffer.Flags = D3D12_BUFFER_UAV_FLAG_RAW;
        }
        else // Assumes Structured
        {
            uavDesc.Format = resourceDesc.Format;
            uavDesc.Buffer.FirstElement = 0;
            uavDesc.Buffer.NumElements = (uint)(resourceDesc.Width / stride);
            uavDesc.Buffer.StructureByteStride = stride;
            uavDesc.Buffer.Flags = D3D12_BUFFER_UAV_FLAG_NONE;
        }

        return uavDesc;
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
}

internal sealed unsafe partial class D3D12ResourceAllocator : IResourceAllocator
{
    private const uint _UPLOAD_BATCH_SIZE = 64 * 1024 * 1024; // 64 MB
    private const uint _MAX_RESOURCE_SIZE_TO_FIT_IN_UPLOAD_BATCH = 16 * 1024 * 1024; // 16 MB

    private UniquePtr<D3D12MA_Allocator> _d3d12MA;

    private readonly D3D12RenderDevice _device;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12PipelineLibrary _pipelineLibrary;

    // TODO: We should use ring buffer pool in d3d12ma for upload buffer.
    private readonly Handle<GraphicsBuffer> _uploadBatch;
    private ulong _uploadBatchOffset;

    private bool _disposed;

    public D3D12ResourceAllocator(
        D3D12RenderDevice device,
        D3D12DescriptorAllocator descriptorAllocator,
        D3D12ResourceDatabase resourceDatabase,
        D3D12PipelineLibrary pipelineLibrary)
    {
        var desc = new D3D12MA_ALLOCATOR_DESC
        {
            pAdapter = (IDXGIAdapter*)device.Adapter.Get(),
            pDevice = (ID3D12Device*)device.NativeDevice.Get(),
            Flags = D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED | D3D12MA_ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED,
        };

        D3D12MA_Allocator* pAllocator = default;
        ThrowIfFailed(D3D12MA_CreateAllocator(&desc, &pAllocator));
        _d3d12MA.Attach(pAllocator);

        _device = device;
        _descriptorAllocator = descriptorAllocator;
        _resourceDatabase = resourceDatabase;
        _pipelineLibrary = pipelineLibrary;

        // Create an upload batch
        var uploadDesc = new BufferDesc
        {
            Size = _UPLOAD_BATCH_SIZE,
            Usage = BufferUsage.Upload,
            MemoryType = ResourceMemoryType.Upload,
        };

        _uploadBatch = CreateBuffer(in uploadDesc, "D3D12ResourceAllocator_UploadBatch");
        _uploadBatchOffset = 0;
    }

    ~D3D12ResourceAllocator()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Handle<GPUResource> TrackAllocation(D3D12MA_Allocation* allocation, ResourceBarrierData barrierData, ResourceViewGroup resourceDescriptor, ResourceDesc desc, string name, bool isTemp)
    {
        var handle = _resourceDatabase.AddAllocation(allocation, barrierData, resourceDescriptor, desc, name);
        if (isTemp)
        {
            _resourceDatabase.ReleaseResource(handle);
        }

        return handle;
    }

    private HRESULT CreateResource(D3D12MA_ALLOCATION_DESC* pAllocationDesc, D3D12_RESOURCE_DESC* pResourceDesc, D3D12_RESOURCE_STATES initialState, CreationOptions options, Guid* riid, void** ppv)
    {
        HRESULT hr;

        if (options.AllocationType == ResourceAllocationType.Suballocation)
        {
            // pAllocation should be the render graph Heap. ppvResource should be the out resource.
            var result = _resourceDatabase.GetResourceRecord(options.Heap);
            if (result.IsFailure)
            {
                return E.E_NOTFOUND;
            }

            hr = _d3d12MA.Get()->CreateAliasingResource(result.Value.resource.allocation.Get(), options.Offset, pResourceDesc, initialState, null, riid, ppv);
        }
        else
        {
            var iid_null = IID.IID_NULL;
            hr = _d3d12MA.Get()->CreateResource(pAllocationDesc, pResourceDesc, initialState, null, (D3D12MA_Allocation**)ppv, &iid_null, null);
        }

        return hr;
    }

    public ResourceSizeInfo GetSizeInfo(ResourceDesc desc)
    {
        D3D12_RESOURCE_DESC d3d12Desc;
        if (desc.Type == ResourceType.Texture)
        {
            d3d12Desc = desc.TextureDescription.ToD3D12ResourceDesc();
        }
        else
        {
            d3d12Desc = desc.BufferDescription.ToD3D12ResourceDesc();
        }

        var info = _device.NativeDevice.Get()->GetResourceAllocationInfo(0, 1, &d3d12Desc);
        return new ResourceSizeInfo
        {
            Size = info.SizeInBytes,
            Alignment = info.Alignment
        };
    }

    public Handle<GPUResource> Allocate(ref readonly AllocationDesc desc, string name)
    {
        var allocDesc = new D3D12MA_ALLOCATION_DESC
        {
            HeapType = desc.HeapType switch
            {
                HeapType.Default => D3D12_HEAP_TYPE_DEFAULT,
                HeapType.Upload => D3D12_HEAP_TYPE_UPLOAD,
                HeapType.Readback => D3D12_HEAP_TYPE_READBACK,
                _ => D3D12_HEAP_TYPE_DEFAULT
            },
            Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED,
            ExtraHeapFlags = desc.HeapFlags switch
            {
                HeapFlags.None => D3D12_HEAP_FLAG_NONE,
                HeapFlags.AllowBuffers => D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS,
                HeapFlags.AllowTextures => D3D12_HEAP_FLAG_ALLOW_ONLY_NON_RT_DS_TEXTURES,
                HeapFlags.AllowRTAndDS => D3D12_HEAP_FLAG_ALLOW_ONLY_RT_DS_TEXTURES,
                HeapFlags.AlowBufferAndTexture => D3D12_HEAP_FLAG_ALLOW_ALL_BUFFERS_AND_TEXTURES,
                _ => D3D12_HEAP_FLAG_NONE
            }
        };

        // SizeInBytes must be aligned to 64KB for committed resources
        var allocInfo = new D3D12_RESOURCE_ALLOCATION_INFO
        {
            SizeInBytes = desc.Size + 65535 & ~65535u,
            Alignment = desc.Alignment
        };

        D3D12MA_Allocation* alloc = default;
        if (_d3d12MA.Get()->AllocateMemory(&allocDesc, &allocInfo, &alloc).FAILED)
        {
            return Handle<GPUResource>.Invalid;
        }

        var barrierData = new ResourceBarrierData
        {
            access = BarrierAccess.NoAccess,
            layout = BarrierLayout.Common,
            sync = BarrierSync.None
        };

        return TrackAllocation(alloc, barrierData, ResourceViewGroup.Invalid, default, name, false);
    }

    public Handle<Texture> CreateTexture(ref readonly TextureDesc desc, string name, CreationOptions options = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        CheckTexture2DSize(desc.Width, desc.Height);

        var resourceDesc = desc.ToD3D12ResourceDesc();
        var allocationDesc = new D3D12MA_ALLOCATION_DESC
        {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
            Flags = D3D12MA_ALLOCATION_FLAG_NONE
        };

        var isSubAllocation = options.AllocationType == ResourceAllocationType.Suballocation;
        D3D12MA_Allocation* pAllocation = default;
        ID3D12Resource* pResource = default;
        HRESULT hr;

        if (isSubAllocation)
        {
            hr = CreateResource(&allocationDesc, &resourceDesc, D3D12_RESOURCE_STATE_COMMON, options, __uuidof(pResource), (void**)&pResource);
        }
        else
        {
            hr = CreateResource(&allocationDesc, &resourceDesc, D3D12_RESOURCE_STATE_COMMON, options, null, (void**)&pAllocation);
            pResource = pAllocation->GetResource();
        }

        if (hr.FAILED)
        {
#if DEBUG
            ThrowIfFailed(hr);
#endif
            return Handle<Texture>.Invalid;
        }

        var isTemp = options.AllocationType == ResourceAllocationType.Temporary;
        var resourceDescriptor = ResourceViewGroup.Invalid;
        if (desc.Usage.HasFlag(TextureUsage.ShaderResource))
        {
            resourceDescriptor.srv = _descriptorAllocator.AllocateCbvSrvUav();
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.srv);

            var isCubeMap = desc.Dimension == TextureDimension.TextureCube || desc.Dimension == TextureDimension.TextureCubeArray;
            var srvDesc = CreateTextureSrvDesc(pResource, resourceDesc.MipLevels, resourceDesc.DepthOrArraySize, isCubeMap);

            _device.NativeDevice.Get()->CreateShaderResourceView(pResource, &srvDesc, cpuHandle);
            _descriptorAllocator.CopyToShaderVisible(resourceDescriptor.srv);
        }

        if (desc.Usage.HasFlag(TextureUsage.RenderTarget))
        {
            resourceDescriptor.rtv = _descriptorAllocator.AllocateRTV();
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.rtv);
            var rtvDesc = CreateRtvDesc(pResource);

            _device.NativeDevice.Get()->CreateRenderTargetView(pResource, &rtvDesc, cpuHandle);
        }

        if (desc.Usage.HasFlag(TextureUsage.DepthStencil))
        {
            resourceDescriptor.dsv = _descriptorAllocator.AllocateDSV();
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.dsv);
            var dsvDesc = CreateDsvDesc(pResource);

            _device.NativeDevice.Get()->CreateDepthStencilView(pResource, &dsvDesc, cpuHandle);
        }

        if (desc.Usage.HasFlag(TextureUsage.UnorderedAccess))
        {
            resourceDescriptor.uav = _descriptorAllocator.AllocateCbvSrvUav();
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.uav);
            var uavDesc = CreateTextureUavDesc(pResource);

            _device.NativeDevice.Get()->CreateUnorderedAccessView(pResource, null, &uavDesc, cpuHandle);
            _descriptorAllocator.CopyToShaderVisible(resourceDescriptor.uav);
        }

        var barrierData = new ResourceBarrierData
        {
            access = BarrierAccess.NoAccess,
            layout = BarrierLayout.Common,
            sync = BarrierSync.None
        };

        Handle<GPUResource> resource;
        if (isSubAllocation)
        {
            resource = _resourceDatabase.ImportExternalResource(pResource, barrierData, resourceDescriptor, name);
        }
        else
        {
            resource = TrackAllocation(pAllocation, barrierData, resourceDescriptor, ResourceDesc.Texture(desc), name, isTemp);
        }

        return resource.AsTexture();
    }

    public Handle<Texture> CreateRenderTarget(ref readonly RenderTargetDesc desc, string name, CreationOptions options = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var textureDesc = desc.ToTextureDescription();
        return CreateTexture(in textureDesc, name, options);
    }

    public Handle<GraphicsBuffer> CreateBuffer(ref readonly BufferDesc desc, string name, CreationOptions options = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        CheckBufferSize(desc.Size);

        var resourceDesc = desc.ToD3D12ResourceDesc();
        var isRaw = desc.Usage.HasFlag(BufferUsage.Raw);

        var allocationDesc = new D3D12MA_ALLOCATION_DESC
        {
            HeapType = ConvertMemoryType(desc.MemoryType),
            Flags = D3D12MA_ALLOCATION_FLAG_NONE,
        };

        var isSubAllocation = options.Heap.IsValid;
        D3D12MA_Allocation* pAllocation = default;
        ID3D12Resource* pResource = default;
        HRESULT hr;

        var initialState = desc.MemoryType switch
        {
            ResourceMemoryType.Default => D3D12_RESOURCE_STATE_COMMON,
            ResourceMemoryType.Upload => D3D12_RESOURCE_STATE_GENERIC_READ,
            ResourceMemoryType.Readback => D3D12_RESOURCE_STATE_COPY_DEST,
            _ => D3D12_RESOURCE_STATE_COMMON
        };

        if (isSubAllocation)
        {
            hr = CreateResource(&allocationDesc, &resourceDesc, initialState, options, __uuidof(pResource), (void**)&pResource);
        }
        else
        {
            hr = CreateResource(&allocationDesc, &resourceDesc, initialState, options, null, (void**)&pAllocation);
            pResource = pAllocation->GetResource();
        }

        if (hr.FAILED)
        {
#if DEBUG
            ThrowIfFailed(hr);
#endif
            return Handle<GraphicsBuffer>.Invalid;
        }

        var isTemp = options.AllocationType == ResourceAllocationType.Temporary;
        var resourceDescriptor = ResourceViewGroup.Invalid;

        if (desc.Usage.HasFlag(BufferUsage.Constant))
        {
            resourceDescriptor.cbv = _descriptorAllocator.AllocateCbvSrvUav();
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.cbv);
            var cbvDesc = new D3D12_CONSTANT_BUFFER_VIEW_DESC
            {
                BufferLocation = pResource->GetGPUVirtualAddress(),
                SizeInBytes = (uint)resourceDesc.Width,
            };

            _device.NativeDevice.Get()->CreateConstantBufferView(&cbvDesc, cpuHandle);
            _descriptorAllocator.CopyToShaderVisible(resourceDescriptor.cbv);
        }

        if (desc.Usage.HasFlag(BufferUsage.ShaderResource))
        {
            resourceDescriptor.srv = _descriptorAllocator.AllocateCbvSrvUav();
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.srv);
            var srvDesc = CreateBufferSrvDesc(pResource, desc.Stride, isRaw);

            _device.NativeDevice.Get()->CreateShaderResourceView(pResource, &srvDesc, cpuHandle);
            _descriptorAllocator.CopyToShaderVisible(resourceDescriptor.srv);
        }

        if (desc.Usage.HasFlag(BufferUsage.UnorderedAccess))
        {
            resourceDescriptor.uav = _descriptorAllocator.AllocateCbvSrvUav();
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.uav);
            var uavDesc = CreateBufferUavDesc(pResource, desc.Stride, isRaw);

            _device.NativeDevice.Get()->CreateUnorderedAccessView(pResource, null, &uavDesc, cpuHandle);
            _descriptorAllocator.CopyToShaderVisible(resourceDescriptor.uav);
        }

        var barrierData = new ResourceBarrierData
        {
            access = BarrierAccess.NoAccess,
            layout = BarrierLayout.Undefined,
            sync = BarrierSync.None
        };

        Handle<GPUResource> resource;
        if (isSubAllocation)
        {
            resource = _resourceDatabase.ImportExternalResource(pResource, barrierData, resourceDescriptor, name);
        }
        else
        {
            resource = TrackAllocation(pAllocation, barrierData, resourceDescriptor, ResourceDesc.Buffer(desc), name, isTemp);
        }

        return resource.AsGraphicsBuffer();
    }

    public Handle<GraphicsBuffer> CreateTempUploadBuffer(ulong sizeInBytes, out ulong offset)
    {
        if (sizeInBytes <= _MAX_RESOURCE_SIZE_TO_FIT_IN_UPLOAD_BATCH && sizeInBytes + _uploadBatchOffset <= _UPLOAD_BATCH_SIZE)
        {
            offset = _uploadBatchOffset;
            _uploadBatchOffset += sizeInBytes;
            return _uploadBatch;
        }
        else
        {
            var bufferDesc = new BufferDesc
            {
                Size = (uint)sizeInBytes,
                Usage = BufferUsage.Upload,
                MemoryType = ResourceMemoryType.Upload,
            };

            var options = new CreationOptions
            {
                AllocationType = ResourceAllocationType.Temporary,
            };

            offset = 0;
            var handle = CreateBuffer(in bufferDesc, "TempUploadBuffer", options);

            _resourceDatabase.ReleaseResource(handle.AsResource());
            return handle;
        }
    }

    public Identifier<Sampler> CreateSampler(ref readonly SamplerDesc desc)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_resourceDatabase.TryGetSampler(in desc, out var id))
        {
            return id;
        }

        var samplerDesc = new D3D12_SAMPLER_DESC
        {
            Filter = desc.FilterMode.ToD3D12Filter(),
            AddressU = desc.AddressU.ToD3D12TextureAddressMode(),
            AddressV = desc.AddressV.ToD3D12TextureAddressMode(),
            AddressW = desc.AddressW.ToD3D12TextureAddressMode(),
            MipLODBias = desc.MipLODBias,
            MaxAnisotropy = desc.MaxAnisotropy,
            ComparisonFunc = desc.ComparisonFunc.ToD3D12ComparisonFunc(),
            MinLOD = desc.MinLOD,
            MaxLOD = desc.MaxLOD,
        };

        var samplerDescriptor = _descriptorAllocator.AllocateSampler();
        var cpuHandle = _descriptorAllocator.GetCpuHandle(samplerDescriptor);
        _device.NativeDevice.Get()->CreateSampler(&samplerDesc, cpuHandle);
        _descriptorAllocator.CopyToShaderVisible(samplerDescriptor);

        return _resourceDatabase.AddSampler(in desc, samplerDescriptor.Value);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _resourceDatabase.ReleaseResourceImmediately(_uploadBatch.AsResource());
        _d3d12MA.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
