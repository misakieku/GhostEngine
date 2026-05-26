using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.D3D12_Alias;
using static TerraFX.Aliases.D3D12MA_Alias;
using static TerraFX.Interop.DirectX.D3D12MemAlloc;

namespace Ghost.Graphics.D3D12;

internal sealed unsafe partial class D3D12ResourceAllocator
{
    // NOTE: MAX_BYTES may not be accurate, we need to verify it with feature level checks.
    private const uint MAX_BYTES = D3D12_REQ_RESOURCE_SIZE_IN_MEGABYTES_EXPRESSION_A_TERM * 1024u * 1024u;
    private const uint MAX_TEXTURE2D_DIMENSION = 16384u;
    private const uint MAX_TEXTURE3D_DIMENSION = 2048u;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckBufferSize(ulong sizeInBytes)
    {
        if (sizeInBytes > MAX_BYTES)
        {
            throw new InvalidOperationException($"ERROR: Resource size too large for DirectX 12 (size {sizeInBytes})");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckTexture2DSize(uint width, uint height)
    {
        if (width > MAX_TEXTURE2D_DIMENSION || height > MAX_TEXTURE2D_DIMENSION)
        {
            throw new InvalidOperationException($"ERROR: Texture size too large for DirectX 12 (width {width}, height {height})");
        }
    }
}

internal sealed unsafe partial class D3D12ResourceAllocator : IResourceAllocator
{
    private const uint UPLOAD_BATCH_SIZE = 64 * 1024 * 1024; // 64 MB
    private const uint MAX_RESOURCE_SIZE_TO_FIT_IN_UPLOAD_BATCH = 16 * 1024 * 1024; // 16 MB

    private UniquePtr<D3D12MA_Allocator> _d3d12MA;

    private readonly D3D12RenderDevice _device;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12PipelineLibrary _pipelineLibrary;

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
            pDevice = (ID3D12Device*)device.NativeObject.Get(),
            Flags = D3D12MA_ALLOCATOR_FLAG_DEFAULT_POOLS_NOT_ZEROED | D3D12MA_ALLOCATOR_FLAG_MSAA_TEXTURES_ALWAYS_COMMITTED,
        };

        D3D12MA_Allocator* pAllocator = default;
        ThrowIfFailed(D3D12MA_CreateAllocator(&desc, &pAllocator));
        _d3d12MA.Attach(pAllocator);

        _device = device;
        _descriptorAllocator = descriptorAllocator;
        _resourceDatabase = resourceDatabase;
        _pipelineLibrary = pipelineLibrary;
    }

    ~D3D12ResourceAllocator()
    {
        Dispose();
    }

    private HRESULT CreateResource(D3D12MA_ALLOCATION_DESC* pAllocationDesc, D3D12_RESOURCE_DESC1* pResourceDesc, D3D12_BARRIER_LAYOUT initialLayout, CreationOptions options, uint numCapatableFormats, DXGI_FORMAT* pCastableFormats, Guid* riid, void** ppv)
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

            hr = _d3d12MA.Get()->CreateAliasingResource2(result.Value.resource.allocation.Get(), options.Offset, pResourceDesc, initialLayout, null, numCapatableFormats, pCastableFormats, riid, ppv);
        }
        else
        {
            Logger.DebugAssert(*riid == __uuidof<D3D12MA_Allocation>());

            var iid_null = IID.IID_NULL;
            hr = _d3d12MA.Get()->CreateResource3(pAllocationDesc, pResourceDesc, initialLayout, null, numCapatableFormats, pCastableFormats, (D3D12MA_Allocation**)ppv, &iid_null, null);
        }

        return hr;
    }

    // TODO: Should we move this to device?
    public ResourceSizeInfo GetSizeInfo(ResourceDesc desc)
    {
        D3D12_RESOURCE_DESC1 d3d12Desc;
        if (desc.Type == ResourceType.Texture)
        {
            d3d12Desc = desc.TextureDescriptor.ToD3D12ResourceDesc1();
        }
        else
        {
            d3d12Desc = desc.BufferDescriptor.ToD3D12ResourceDesc1();
        }

        D3D12_RESOURCE_ALLOCATION_INFO1 info1;
        var info = _device.NativeObject.Get()->GetResourceAllocationInfo2(0, 1, &d3d12Desc, &info1);
        return new ResourceSizeInfo
        {
            Size = info.SizeInBytes,
            Alignment = info.Alignment,
            Offset = info1.Offset,
        };
    }

    public Handle<GPUResource> Allocate(ref readonly AllocationDesc desc, string? name = null)
    {
        var allocDesc = new D3D12MA_ALLOCATION_DESC
        {
            HeapType = desc.HeapType.ToD3D12HeapType(),
            Flags = D3D12MA_ALLOCATION_FLAG_COMMITTED,
            ExtraHeapFlags = desc.HeapFlags.ToD3D12HeapFlags()
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

        return _resourceDatabase.AddAllocation(alloc, barrierData, ResourceViewGroup.Invalid, default, name);
    }

    public Handle<GPUTexture> CreateTexture(ref readonly TextureDesc desc, string? name = null, CreationOptions options = default, AdditionalTextureDesc additionalDesc = default)
    {
        Logger.DebugAssert(!_disposed);

        CheckTexture2DSize(desc.Width, desc.Height);

        var resourceDesc = desc.ToD3D12ResourceDesc1();
        var allocationDesc = new D3D12MA_ALLOCATION_DESC
        {
            HeapType = D3D12_HEAP_TYPE_DEFAULT,
            Flags = D3D12MA_ALLOCATION_FLAG_NONE
        };

        var isSubAllocation = options.AllocationType == ResourceAllocationType.Suballocation;
        D3D12MA_Allocation* pAllocation = default;
        ID3D12Resource* pResource = default;
        HRESULT hr;

        var pCastableFormats = stackalloc DXGI_FORMAT[additionalDesc.CastableFormat.Length];
        for (var i = 0; i < additionalDesc.CastableFormat.Length; i++)
        {
            pCastableFormats[i] = additionalDesc.CastableFormat[i].ToDXGIFormat();
        }

        if (isSubAllocation)
        {
            hr = CreateResource(&allocationDesc, &resourceDesc, D3D12_BARRIER_LAYOUT_COMMON, options,
                (uint)additionalDesc.CastableFormat.Length, pCastableFormats,
                __uuidof(pResource), (void**)&pResource);
        }
        else
        {
            hr = CreateResource(&allocationDesc, &resourceDesc, D3D12_BARRIER_LAYOUT_COMMON, options,
                (uint)additionalDesc.CastableFormat.Length, pCastableFormats,
                __uuidof(pAllocation), (void**)&pAllocation);

            if (hr.SUCCEEDED)
            {
                pResource = pAllocation->GetResource();
            }
        }

        if (hr.FAILED)
        {
#if DEBUG
            ThrowIfFailed(hr);
#endif
            return Handle<GPUTexture>.Invalid;
        }

        var resourceDescriptor = D3D12Utility.CreateResourceDescriptor(_device, _descriptorAllocator, ResourceDesc.Texture(desc), pResource);
        var barrierData = new ResourceBarrierData
        {
            layout = BarrierLayout.Common,
            access = BarrierAccess.Common,
            sync = BarrierSync.None
        };

        Handle<GPUResource> resource;
        if (isSubAllocation)
        {
            resource = _resourceDatabase.ImportExternalResource(pResource, barrierData, resourceDescriptor, ResourceDesc.Texture(desc), name);
        }
        else
        {
            resource = _resourceDatabase.AddAllocation(pAllocation, barrierData, resourceDescriptor, ResourceDesc.Texture(desc), name);
        }

        return resource.AsTexture();
    }

    public Handle<GPUBuffer> CreateBuffer(ref readonly BufferDesc desc, string? name = null, CreationOptions options = default)
    {
        Logger.DebugAssert(!_disposed);
        CheckBufferSize(desc.Size);

        var resourceDesc = desc.ToD3D12ResourceDesc1();
        var isRaw = desc.Usage.HasFlag(BufferUsage.Raw);

        var allocationDesc = new D3D12MA_ALLOCATION_DESC
        {
            HeapType = desc.HeapType.ToD3D12HeapType(),
            Flags = D3D12MA_ALLOCATION_FLAG_NONE,
        };

        var isSubAllocation = options.Heap.IsValid;
        D3D12MA_Allocation* pAllocation = default;
        ID3D12Resource* pResource = default;
        HRESULT hr;

        if (isSubAllocation)
        {
            hr = CreateResource(&allocationDesc, &resourceDesc, D3D12_BARRIER_LAYOUT_UNDEFINED, options,
                0u, null,
                __uuidof(pResource), (void**)&pResource);
        }
        else
        {
            hr = CreateResource(&allocationDesc, &resourceDesc, D3D12_BARRIER_LAYOUT_UNDEFINED, options,
                0u, null,
                __uuidof(pAllocation), (void**)&pAllocation);

            if (hr.SUCCEEDED)
            {
                pResource = pAllocation->GetResource();
            }
        }

        if (hr.FAILED)
        {
#if DEBUG
            ThrowIfFailed(hr);
#endif
            return Handle<GPUBuffer>.Invalid;
        }

        var resourceDescriptor = D3D12Utility.CreateResourceDescriptor(_device, _descriptorAllocator, ResourceDesc.Buffer(desc), pResource);
        var barrierData = new ResourceBarrierData
        {
            layout = BarrierLayout.Undefined,
            access = BarrierAccess.Common,
            sync = BarrierSync.None
        };

        Handle<GPUResource> resource;
        if (isSubAllocation)
        {
            resource = _resourceDatabase.ImportExternalResource(pResource, barrierData, resourceDescriptor, ResourceDesc.Buffer(desc), name);
        }
        else
        {
            resource = _resourceDatabase.AddAllocation(pAllocation, barrierData, resourceDescriptor, ResourceDesc.Buffer(desc), name);
        }

        return resource.AsBuffer();
    }

    public Identifier<Sampler> CreateSampler(ref readonly SamplerDesc desc)
    {
        Logger.DebugAssert(!_disposed);

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
        _device.NativeObject.Get()->CreateSampler(&samplerDesc, cpuHandle);
        _descriptorAllocator.CopyToShaderVisible(samplerDescriptor);

        return _resourceDatabase.AddSampler(in desc, samplerDescriptor.Value);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _d3d12MA.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
