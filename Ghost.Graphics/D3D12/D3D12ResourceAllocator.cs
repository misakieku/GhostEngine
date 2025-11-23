using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.Mathematics;
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
            Format = resourceDesc.Format,
            ViewDimension = D3D12_SRV_DIMENSION_BUFFER,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
        };

        if (isRaw)
        {
            srvDesc.Buffer.FirstElement = 0;
            srvDesc.Buffer.NumElements = (uint)(resourceDesc.Width / 4u);
            srvDesc.Buffer.StructureByteStride = 0;
            srvDesc.Buffer.Flags = D3D12_BUFFER_SRV_FLAG_RAW;
        }
        else // Assumes Structured
        {
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
            Format = resourceDesc.Format,
            ViewDimension = D3D12_UAV_DIMENSION_BUFFER,
        };

        if (isRaw)
        {
            uavDesc.Buffer.FirstElement = 0;
            uavDesc.Buffer.NumElements = (uint)(resourceDesc.Width / 4u);
            uavDesc.Buffer.StructureByteStride = 0;
            uavDesc.Buffer.Flags = D3D12_BUFFER_UAV_FLAG_RAW;
        }
        else // Assumes Structured
        {
            uavDesc.Buffer.FirstElement = 0;
            uavDesc.Buffer.NumElements = (uint)(resourceDesc.Width / stride);
            uavDesc.Buffer.StructureByteStride = stride;
            uavDesc.Buffer.Flags = D3D12_BUFFER_UAV_FLAG_NONE;
        }

        return uavDesc;
    }

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

        if (usage.HasFlag(BufferUsage.Raw) || usage.HasFlag(BufferUsage.UnorderedAccess))
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

        // Default to Common, but check for specific roles
        var state = D3D12_RESOURCE_STATE_COMMON;

        if (usage.HasFlag(BufferUsage.Vertex) || usage.HasFlag(BufferUsage.Constant))
        {
            // Vertex and Constant buffers can share this state
            state |= D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;
        }

        if (usage.HasFlag(BufferUsage.Index))
        {
            state |= D3D12_RESOURCE_STATE_INDEX_BUFFER;
        }

        if (usage.HasFlag(BufferUsage.UnorderedAccess))
        {
            state |= D3D12_RESOURCE_STATE_UNORDERED_ACCESS;
        }

        if (usage.HasFlag(BufferUsage.IndirectArgument))
        {
            state |= D3D12_RESOURCE_STATE_INDIRECT_ARGUMENT;
        }

        // If only one state is set (e.g., just IndexBuffer), return it directly
        // This is a common optimization to avoid an initial barrier
        if (math.ispow2((int)state))
        {
            return state;
        }

        // If multiple roles (e.g., Vertex and Index), start in COMMON
        // or return the combined state if they are compatible
        // For simplicity, we'll just check for the common "Vertex/Constant" combo
        if (state == D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER)
        {
            return D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;
        }

        // If it's a mix, start in common and let the user barrier
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
}

// TODO: Dedicated pool for copy, render graph, and persistent resources

// TODO: Thread safety for resource allocator
// A common solution is to use ticket. Each pAllocation request create a ticket and put it into a thread-safe queue. A dedicated thread process the queue and fulfill the requests.

internal sealed unsafe partial class D3D12ResourceAllocator : IUnknownObject<D3D12MA_Allocator>, IResourceAllocator
{
    private readonly IFenceSynchronizer _fenceSynchronizer;
    private readonly D3D12RenderDevice _device;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12PipelineLibrary _pipelineLibrary;

    private UnsafeQueue<Handle<GPUResource>> _temResources;

    public D3D12ResourceAllocator(
        IFenceSynchronizer fenceSynchronizer,
        D3D12RenderDevice device,
        D3D12DescriptorAllocator descriptorAllocator,
        D3D12ResourceDatabase resourceDatabase,
        D3D12PipelineLibrary pipelineLibrary)
    {
        var desc = new D3D12MA_ALLOCATOR_DESC
        {
            pAdapter = (IDXGIAdapter*)device.Adapter,
            pDevice = (ID3D12Device*)device.NativeDevice,
            Flags = D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED | D3D12MA_ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED,
        };

        D3D12MA_Allocator* pAllocator = default;
        ThrowIfFailed(D3D12MA_CreateAllocator(&desc, &pAllocator));
        nativeObject.Attach(pAllocator);

        _fenceSynchronizer = fenceSynchronizer;
        _device = device;
        _descriptorAllocator = descriptorAllocator;
        _resourceDatabase = resourceDatabase;
        _pipelineLibrary = pipelineLibrary;

        _temResources = new(64, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
    }

    ~D3D12ResourceAllocator()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Handle<GPUResource> TrackResource(D3D12MA_Allocation* allocation, D3D12_RESOURCE_STATES state, ResourceViewGroup resourceDescriptor, ResourceDesc desc, bool isTemp)
    {
        var handle = _resourceDatabase.AddResource(allocation, _fenceSynchronizer.CPUFenceValue, D3D12StatesToRHIState(state), resourceDescriptor, desc);

        if (isTemp)
        {
            _temResources.Enqueue(handle);
        }

        return handle;
    }

    public Handle<Texture> CreateTexture(ref readonly TextureDesc desc, bool isTemp = false)
    {
        ThrowIfDisposed();

        CheckTexture2DSize(desc.Width, desc.Height);

        var d3d12Format = ConvertTextureFormat(desc.Format);
        var maxDimension = Math.Max(desc.Width, Math.Max(desc.Height, desc.Slice));
        var mipLevels = desc.MipLevels == 0
            ? (ushort)(1 + Math.Floor(Math.Log2(maxDimension)))
            : (ushort)desc.MipLevels;

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

        D3D12MA_Allocation* pAllocation = default;
        ThrowIfFailed(nativeObject.Get()->CreateResource(&allocationDesc, &resourceDesc, initialState, null, &pAllocation, Win32Utility.IID_NULL, null));

        var resourceDescriptor = ResourceViewGroup.Invalid;
        if (desc.Usage.HasFlag(TextureUsage.ShaderResource))
        {
            resourceDescriptor.srv = _descriptorAllocator.AllocateCbvSrvUav(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.srv);

            var isCubeMap = desc.Dimension == TextureDimension.TextureCube || desc.Dimension == TextureDimension.TextureCubeArray;
            var srvDesc = CreateTextureSrvDesc(pAllocation->GetResource(), mipLevels, desc.Slice, isCubeMap);

            _device.NativeDevice->CreateShaderResourceView(pAllocation->GetResource(), &srvDesc, cpuHandle);
        }

        if (desc.Usage.HasFlag(TextureUsage.RenderTarget))
        {
            resourceDescriptor.rtv = _descriptorAllocator.AllocateRTV(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.rtv);
            var rtvDesc = CreateRtvDesc(pAllocation->GetResource());

            _device.NativeDevice->CreateRenderTargetView(pAllocation->GetResource(), &rtvDesc, cpuHandle);
        }

        if (desc.Usage.HasFlag(TextureUsage.DepthStencil))
        {
            resourceDescriptor.dsv = _descriptorAllocator.AllocateDSV(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.dsv);
            var dsvDesc = CreateDsvDesc(pAllocation->GetResource());

            _device.NativeDevice->CreateDepthStencilView(pAllocation->GetResource(), &dsvDesc, cpuHandle);
        }

        if (desc.Usage.HasFlag(TextureUsage.UnorderedAccess))
        {
            resourceDescriptor.uav = _descriptorAllocator.AllocateCbvSrvUav(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.uav);
            var uavDesc = CreateTextureUavDesc(pAllocation->GetResource());

            _device.NativeDevice->CreateUnorderedAccessView(pAllocation->GetResource(), null, &uavDesc, cpuHandle);
        }

        var handle = TrackResource(pAllocation, initialState, resourceDescriptor, ResourceDesc.Texture(desc), isTemp);

        return handle.AsTexture();
    }

    public Handle<Texture> CreateRenderTarget(ref readonly RenderTargetDesc desc, bool isTemp = false)
    {
        ThrowIfDisposed();

        var textureDesc = desc.ToTextureDescripton();
        return CreateTexture(ref textureDesc, isTemp);
    }

    public Handle<GraphicsBuffer> CreateBuffer(ref readonly BufferDesc desc, bool isTemp = false)
    {
        ThrowIfDisposed();
        CheckBufferSize(desc.Size);

        var resourceDescription = D3D12_RESOURCE_DESC.Buffer(desc.Size, ConvertBufferUsage(desc.Usage));
        var isRaw = desc.Usage.HasFlag(BufferUsage.Raw);
        if (isRaw)
        {
            resourceDescription.Format = DXGI_FORMAT_R32_TYPELESS;
        }

        var allocationDesc = new D3D12MA_ALLOCATION_DESC
        {
            HeapType = ConvertMemoryType(desc.MemoryType),
            Flags = D3D12MA_ALLOCATION_FLAG_NONE
        };

        var initialState = DetermineInitialBufferState(desc.Usage, desc.MemoryType);

        D3D12MA_Allocation* pAllocation = default;
        ThrowIfFailed(nativeObject.Get()->CreateResource(&allocationDesc, &resourceDescription, initialState, null, &pAllocation, Win32Utility.IID_NULL, null));

        var resourceDescriptor = ResourceViewGroup.Invalid;
        var pResource = pAllocation->GetResource();

        if (desc.Usage.HasFlag(BufferUsage.Constant))
        {
            // D3D12 CBV size must be 256-byte aligned
            var alignedSize = (uint)(desc.Size + 255) & ~255u;

            resourceDescriptor.cbv = _descriptorAllocator.AllocateCbvSrvUav(isTemp);
            var cbvDesc = new D3D12_CONSTANT_BUFFER_VIEW_DESC
            {
                BufferLocation = pResource->GetGPUVirtualAddress(),
                SizeInBytes = alignedSize
            };

            _device.NativeDevice->CreateConstantBufferView(&cbvDesc, _descriptorAllocator.GetCpuHandle(resourceDescriptor.cbv));
        }

        if (desc.Usage.HasFlag(BufferUsage.ShaderResource))
        {
            resourceDescriptor.srv = _descriptorAllocator.AllocateCbvSrvUav(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.srv);
            var srvDesc = CreateBufferSrvDesc(pAllocation->GetResource(), desc.Stride, isRaw);

            _device.NativeDevice->CreateShaderResourceView(pResource, &srvDesc, cpuHandle);
        }

        if (desc.Usage.HasFlag(BufferUsage.UnorderedAccess))
        {
            resourceDescriptor.uav = _descriptorAllocator.AllocateCbvSrvUav(isTemp);
            var cpuHandle = _descriptorAllocator.GetCpuHandle(resourceDescriptor.uav);
            var uavDesc = CreateBufferUavDesc(pAllocation->GetResource(), desc.Stride, isRaw);

            _device.NativeDevice->CreateUnorderedAccessView(pResource, null, &uavDesc, cpuHandle);
        }

        var handle = TrackResource(pAllocation, initialState, resourceDescriptor, ResourceDesc.Buffer(desc), isTemp);
        return handle.AsGraphicsBuffer();
    }

    public Handle<GraphicsBuffer> CreateUploadBuffer(ulong size, bool isTemp = true)
    {
        ThrowIfDisposed();

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
        ThrowIfDisposed();

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
        ThrowIfDisposed();

        var material = new Material();
        material.SetShader(shader, this, _resourceDatabase);

        return _resourceDatabase.AddMaterial(ref material);
    }

    public Identifier<Shader> CreateGraphicsShader(ShaderDescriptor descriptor)
    {
        ThrowIfDisposed();

        var shader = new Shader(descriptor);
        foreach (var pass in descriptor.passes)
        {
            if (pass is not FullPassDescriptor fullPass)
            {
                continue;
            }

            var passKey = new ShaderPassKey(fullPass.Identifier);
            var cbr = _pipelineLibrary.GetCBufferInfo(passKey);
            if (cbr.Status != ResultStatus.Success)
            {
                return Identifier<Shader>.Invalid;
            }

            _resourceDatabase.AddShaderPass(passKey, new ShaderPass(cbr.Value));
        }

        return _resourceDatabase.AddShader(shader);
    }

    public void ReleaseTempResources()
    {
        ThrowIfDisposed();

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

            if (info.cpuFenceValue > _fenceSynchronizer.GPUFenceValue)
            {
                // Resource still in use by GPU, stop processing.
                // Since resources are enqueued in order, we can break here.
                break;
            }

            _resourceDatabase.ReleaseResource(handle);
            _temResources.Dequeue();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

#if DEBUG || GHOST_EDITOR
        if (_temResources.Count > 0)
        {
            throw new InvalidOperationException($"ResourceAllocator is being disposed with {_temResources.Count} temp allocations still registered. Ensure all resources are released before disposing.");
        }
#endif

        foreach (var handle in _temResources)
        {
            _resourceDatabase.ReleaseResource(handle);
        }

        _temResources.Dispose();

        base.Dispose(disposing);
    }
}
