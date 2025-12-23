using Ghost.Core;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;

using static TerraFX.Aliases.D3D12_Alias;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of viewGroup allocator that manages different types of viewGroup heaps.
/// </summary>
internal unsafe class D3D12DescriptorAllocator : IDisposable
{
    private readonly D3D12DescriptorHeap _rtvHeap;
    private readonly D3D12DescriptorHeap _dsvHeap;
    private readonly D3D12DescriptorHeap _cbvSrvUavHeap;
    private readonly D3D12DescriptorHeap _samplerHeap;

    private bool _disposed;

    public D3D12DescriptorAllocator(D3D12RenderDevice device, int initialRtvCount = 256, int initialDsvCount = 256, int initialSrvCount = 200_000, int initialSamplerCount = 256)
    {
        _rtvHeap = new D3D12DescriptorHeap("rtv", device, D3D12_DESCRIPTOR_HEAP_TYPE_RTV, initialRtvCount, initialRtvCount / 2);
        _dsvHeap = new D3D12DescriptorHeap("dsv", device, D3D12_DESCRIPTOR_HEAP_TYPE_DSV, initialDsvCount, initialDsvCount / 2);
        _cbvSrvUavHeap = new D3D12DescriptorHeap("srv", device, D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV, initialSrvCount, initialSrvCount / 2);
        _samplerHeap = new D3D12DescriptorHeap("sampler", device, D3D12_DESCRIPTOR_HEAP_TYPE_SAMPLER, initialSamplerCount, initialSamplerCount);
    }

    ~D3D12DescriptorAllocator()
    {
        Dispose();
    }

    #region RTV Methods

    public Identifier<RTVDescriptor> AllocateRTV(bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = dynamic ? _rtvHeap.AllocateDescriptorDynamic() : _rtvHeap.AllocateDescriptor();
        if (index == -1)
        {
            throw new InvalidOperationException("Failed to allocate RTV descriptor");
        }

        return new Identifier<RTVDescriptor>(index);
    }

    public Identifier<RTVDescriptor>[] AllocateRTVs(int count, bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = dynamic ? _rtvHeap.AllocateDescriptorsDynamic(count) : _rtvHeap.AllocateDescriptors(count);
        if (baseIndex == -1)
        {
            throw new InvalidOperationException($"Failed to allocate {count} RTV descriptors");
        }

        var descriptors = new Identifier<RTVDescriptor>[count];
        for (var i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new Identifier<RTVDescriptor>(index);
        }

        return descriptors;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandle(Identifier<RTVDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _rtvHeap.GetCpuHandle(descriptor.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release(Identifier<RTVDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _rtvHeap.ReleaseDescriptor(descriptor.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release(ReadOnlySpan<Identifier<RTVDescriptor>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(Identifier<RTVDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _rtvHeap.CopyToPersistentHeap(descriptor.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(ReadOnlySpan<Identifier<RTVDescriptor>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        foreach (var descriptor in descriptors)
        {
            MakePersistent(descriptor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetRTVDynamicHeap()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _rtvHeap.ResetDynamicHeap();
    }

    #endregion

    #region DSV Methods

    public Identifier<DSVDescriptor> AllocateDSV(bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = dynamic ? _dsvHeap.AllocateDescriptorDynamic() : _dsvHeap.AllocateDescriptor();
        if (index == -1)
        {
            throw new InvalidOperationException("Failed to allocate DSV descriptor");
        }

        return new Identifier<DSVDescriptor>(index);
    }

    public Identifier<DSVDescriptor>[] AllocateDSVs(int count, bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = dynamic ? _dsvHeap.AllocateDescriptorsDynamic(count) : _dsvHeap.AllocateDescriptors(count);
        if (baseIndex == -1)
        {
            throw new InvalidOperationException($"Failed to allocate {count} DSV descriptors");
        }

        var descriptors = new Identifier<DSVDescriptor>[count];
        for (var i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new Identifier<DSVDescriptor>(index);
        }

        return descriptors;
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandle(Identifier<DSVDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _dsvHeap.GetCpuHandle(descriptor.Value);
    }

    public void Release(Identifier<DSVDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _dsvHeap.ReleaseDescriptor(descriptor.Value);
    }

    public void Release(ReadOnlySpan<Identifier<DSVDescriptor>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(Identifier<DSVDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _dsvHeap.CopyToPersistentHeap(descriptor.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(ReadOnlySpan<Identifier<DSVDescriptor>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        foreach (var descriptor in descriptors)
        {
            MakePersistent(descriptor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetDSVDynamicHeap()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _dsvHeap.ResetDynamicHeap();
    }

    #endregion

    #region CBV_SRV_UAV Methods

    public Identifier<CbvSrvUavDescriptor> AllocateCbvSrvUav(bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = dynamic ? _cbvSrvUavHeap.AllocateDescriptorDynamic() : _cbvSrvUavHeap.AllocateDescriptor();
        if (index == -1)
        {
            throw new InvalidOperationException("Failed to allocate CBV/SRV/UAV descriptor");
        }

        _cbvSrvUavHeap.CopyToShaderVisibleHeap(index);
        return new Identifier<CbvSrvUavDescriptor>(index);
    }

    public Identifier<CbvSrvUavDescriptor>[] AllocateSRVs(int count, bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = dynamic ? _cbvSrvUavHeap.AllocateDescriptorsDynamic(count) : _cbvSrvUavHeap.AllocateDescriptors(count);
        if (baseIndex == -1)
        {
            throw new InvalidOperationException($"Failed to allocate {count} CBV/SRV/UAV descriptors");
        }

        var descriptors = new Identifier<CbvSrvUavDescriptor>[count];
        for (var i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new Identifier<CbvSrvUavDescriptor>(index);
        }

        _cbvSrvUavHeap.CopyToShaderVisibleHeap(baseIndex, count);
        return descriptors;
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandle(Identifier<CbvSrvUavDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _cbvSrvUavHeap.GetCpuHandle(descriptor.Value);
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandleShaderVisible(Identifier<CbvSrvUavDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _cbvSrvUavHeap.GetCpuHandleShaderVisible(descriptor.Value);
    }

    public D3D12_GPU_DESCRIPTOR_HANDLE GetGpuHandle(Identifier<CbvSrvUavDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _cbvSrvUavHeap.GetGpuHandle(descriptor.Value);
    }

    public void Release(Identifier<CbvSrvUavDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cbvSrvUavHeap.ReleaseDescriptor(descriptor.Value);
    }

    public void Release(ReadOnlySpan<Identifier<CbvSrvUavDescriptor>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(Identifier<CbvSrvUavDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cbvSrvUavHeap.CopyToPersistentHeap(descriptor.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(ReadOnlySpan<Identifier<CbvSrvUavDescriptor>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        foreach (var descriptor in descriptors)
        {
            MakePersistent(descriptor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetCbvSrvUavDynamicHeap()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cbvSrvUavHeap.ResetDynamicHeap();
    }

    #endregion

    #region Sampler Methods

    public Identifier<SamplerDescriptor> AllocateSampler()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = _samplerHeap.AllocateDescriptor();
        if (index == -1)
        {
            throw new InvalidOperationException("Failed to allocate Sampler descriptor");
        }

        _samplerHeap.CopyToShaderVisibleHeap(index);
        return new Identifier<SamplerDescriptor>(index);
    }

    public Identifier<SamplerDescriptor>[] AllocateSamplers(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = _samplerHeap.AllocateDescriptors(count);
        if (baseIndex == -1)
        {
            throw new InvalidOperationException($"Failed to allocate {count} Sampler descriptors");
        }

        var descriptors = new Identifier<SamplerDescriptor>[count];
        for (var i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new Identifier<SamplerDescriptor>(index);
        }

        _samplerHeap.CopyToShaderVisibleHeap(baseIndex, count);
        return descriptors;
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandle(Identifier<SamplerDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samplerHeap.GetCpuHandle(descriptor.Value);
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandleShaderVisible(Identifier<SamplerDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samplerHeap.GetCpuHandleShaderVisible(descriptor.Value);
    }

    public D3D12_GPU_DESCRIPTOR_HANDLE GetGpuHandle(Identifier<SamplerDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samplerHeap.GetGpuHandle(descriptor.Value);
    }

    public void Release(Identifier<SamplerDescriptor> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _samplerHeap.ReleaseDescriptor(descriptor.Value);
    }

    public void Release(ReadOnlySpan<Identifier<SamplerDescriptor>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release(ResourceViewGroup descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Release(descriptor.rtv);
        Release(descriptor.dsv);
        Release(descriptor.srv);
        Release(descriptor.cbv);
        Release(descriptor.uav);
        Release(descriptor.sampler);
    }

    #region Utility Methods

    /// <summary>
    /// Gets the RTV heap for binding to the command list.
    /// </summary>
    public ID3D12DescriptorHeap* GetRTVHeap() => _rtvHeap.Heap;

    /// <summary>
    /// Gets the DSV heap for binding to the command list.
    /// </summary>
    public ID3D12DescriptorHeap* GetDSVHeap() => _dsvHeap.Heap;

    /// <summary>
    /// Gets the CBV/SRV/UAV heap for binding to the command list.
    /// </summary>
    public ID3D12DescriptorHeap* GetCbvSrvUavHeap() => _cbvSrvUavHeap.ShaderVisibleHeap;

    /// <summary>
    /// Gets the sampler heap for binding to the command list.
    /// </summary>
    public ID3D12DescriptorHeap* GetSamplerHeap() => _samplerHeap.ShaderVisibleHeap;

    /// <summary>
    /// Gets the shader visible heaps that need to be bound to the command list.
    /// </summary>
    /// <param name="ppHeap">An array of two ID3D12DescriptorHeap pointers to receive the CBV/SRV/UAV and Sampler heaps.</param>
    public void GetShaderVisibleHeaps(ID3D12DescriptorHeap** ppHeap)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (ppHeap == null)
        {
            throw new ArgumentNullException(nameof(ppHeap));
        }

        ppHeap[0] = _cbvSrvUavHeap.ShaderVisibleHeap;
        ppHeap[1] = _samplerHeap.ShaderVisibleHeap;
    }

    #endregion

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _rtvHeap.Dispose();
        _dsvHeap.Dispose();
        _cbvSrvUavHeap.Dispose();
        _samplerHeap.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
