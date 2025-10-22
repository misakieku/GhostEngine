using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;

using static TerraFX.Aliases.D3D12_Alias;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of descriptor allocator that manages different types of descriptor heaps.
/// </summary>
internal unsafe class D3D12DescriptorAllocator : IDisposable
{
    private readonly D3D12DescriptorHeap _rtvHeap;
    private readonly D3D12DescriptorHeap _dsvHeap;
    private readonly D3D12DescriptorHeap _cbvSrvUavHeap;
    private readonly D3D12DescriptorHeap _samplerHeap;

    private bool _disposed;

    public unsafe D3D12DescriptorAllocator(D3D12RenderDevice device, int initialRtvCount = 256, int initialDsvCount = 256, int initialSrvCount = 200_000, int initialSamplerCount = 256)
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

    public Identifier<RTVDesc> AllocateRTV(bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = dynamic ? _rtvHeap.AllocateDescriptorDynamic() : _rtvHeap.AllocateDescriptor();
        if (index == -1)
        {
            throw new InvalidOperationException("Failed to allocate RTV descriptor");
        }

        return new Identifier<RTVDesc>(index);
    }

    public Identifier<RTVDesc>[] AllocateRTVs(int count, bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = dynamic ? _rtvHeap.AllocateDescriptorsDynamic(count) : _rtvHeap.AllocateDescriptors(count);
        if (baseIndex == -1)
        {
            throw new InvalidOperationException($"Failed to allocate {count} RTV descriptors");
        }

        var descriptors = new Identifier<RTVDesc>[count];
        for (var i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new Identifier<RTVDesc>(index);
        }

        return descriptors;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandle(Identifier<RTVDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _rtvHeap.GetCpuHandle(descriptor.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release(Identifier<RTVDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _rtvHeap.ReleaseDescriptor(descriptor.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release(ReadOnlySpan<Identifier<RTVDesc>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(Identifier<RTVDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _rtvHeap.CopyToPersistentHeap(descriptor.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(ReadOnlySpan<Identifier<RTVDesc>> descriptors)
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

    public Identifier<DSVDesc> AllocateDSV(bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = dynamic ? _dsvHeap.AllocateDescriptorDynamic() : _dsvHeap.AllocateDescriptor();
        if (index == -1)
        {
            throw new InvalidOperationException("Failed to allocate DSV descriptor");
        }

        return new Identifier<DSVDesc>(index);
    }

    public Identifier<DSVDesc>[] AllocateDSVs(int count, bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = dynamic ? _dsvHeap.AllocateDescriptorsDynamic(count) : _dsvHeap.AllocateDescriptors(count);
        if (baseIndex == -1)
        {
            throw new InvalidOperationException($"Failed to allocate {count} DSV descriptors");
        }

        var descriptors = new Identifier<DSVDesc>[count];
        for (var i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new Identifier<DSVDesc>(index);
        }

        return descriptors;
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandle(Identifier<DSVDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _dsvHeap.GetCpuHandle(descriptor.value);
    }

    public void Release(Identifier<DSVDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _dsvHeap.ReleaseDescriptor(descriptor.value);
    }

    public void Release(ReadOnlySpan<Identifier<DSVDesc>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(Identifier<DSVDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _dsvHeap.CopyToPersistentHeap(descriptor.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(ReadOnlySpan<Identifier<DSVDesc>> descriptors)
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

    public Identifier<CbvSrvUavDesc> AllocateCbvSrvUav(bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = dynamic ? _cbvSrvUavHeap.AllocateDescriptorDynamic() : _cbvSrvUavHeap.AllocateDescriptor();
        if (index == -1)
        {
            throw new InvalidOperationException("Failed to allocate CBV/SRV/UAV descriptor");
        }

        _cbvSrvUavHeap.CopyToShaderVisibleHeap(index);
        return new Identifier<CbvSrvUavDesc>(index);
    }

    public Identifier<CbvSrvUavDesc>[] AllocateSRVs(int count, bool dynamic = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = dynamic ? _cbvSrvUavHeap.AllocateDescriptorsDynamic(count) : _cbvSrvUavHeap.AllocateDescriptors(count);
        if (baseIndex == -1)
        {
            throw new InvalidOperationException($"Failed to allocate {count} CBV/SRV/UAV descriptors");
        }

        var descriptors = new Identifier<CbvSrvUavDesc>[count];
        for (var i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new Identifier<CbvSrvUavDesc>(index);
        }

        _cbvSrvUavHeap.CopyToShaderVisibleHeap(baseIndex, count);
        return descriptors;
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandle(Identifier<CbvSrvUavDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _cbvSrvUavHeap.GetCpuHandle(descriptor.value);
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandleShaderVisible(Identifier<CbvSrvUavDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _cbvSrvUavHeap.GetCpuHandleShaderVisible(descriptor.value);
    }

    public D3D12_GPU_DESCRIPTOR_HANDLE GetGpuHandle(Identifier<CbvSrvUavDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _cbvSrvUavHeap.GetGpuHandle(descriptor.value);
    }

    public void Release(Identifier<CbvSrvUavDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cbvSrvUavHeap.ReleaseDescriptor(descriptor.value);
    }

    public void Release(ReadOnlySpan<Identifier<CbvSrvUavDesc>> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(Identifier<CbvSrvUavDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _cbvSrvUavHeap.CopyToPersistentHeap(descriptor.value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MakePersistent(ReadOnlySpan<Identifier<CbvSrvUavDesc>> descriptors)
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

    public Identifier<SamplerDesc> AllocateSampler()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = _samplerHeap.AllocateDescriptor();
        if (index == -1)
        {
            throw new InvalidOperationException("Failed to allocate Sampler descriptor");
        }

        _samplerHeap.CopyToShaderVisibleHeap(index);
        return new Identifier<SamplerDesc>(index);
    }

    public Identifier<SamplerDesc>[] AllocateSamplers(int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = _samplerHeap.AllocateDescriptors(count);
        if (baseIndex == -1)
        {
            throw new InvalidOperationException($"Failed to allocate {count} Sampler descriptors");
        }

        var descriptors = new Identifier<SamplerDesc>[count];
        for (var i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new Identifier<SamplerDesc>(index);
        }

        _samplerHeap.CopyToShaderVisibleHeap(baseIndex, count);
        return descriptors;
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandle(Identifier<SamplerDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samplerHeap.GetCpuHandle(descriptor.value);
    }

    public D3D12_CPU_DESCRIPTOR_HANDLE GetCpuHandleShaderVisible(Identifier<SamplerDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samplerHeap.GetCpuHandleShaderVisible(descriptor.value);
    }

    public D3D12_GPU_DESCRIPTOR_HANDLE GetGpuHandle(Identifier<SamplerDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samplerHeap.GetGpuHandle(descriptor.value);
    }

    public void Release(Identifier<SamplerDesc> descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _samplerHeap.ReleaseDescriptor(descriptor.value);
    }

    public void Release(ReadOnlySpan<Identifier<SamplerDesc>> descriptors)
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