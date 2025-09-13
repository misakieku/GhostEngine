using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of descriptor allocator that manages different types of descriptor heaps.
/// </summary>
internal unsafe class D3D12DescriptorAllocator : IDescriptorAllocator, IDisposable
{
    private readonly D3D12DescriptorHeap _rtvHeap;
    private readonly D3D12DescriptorHeap _dsvHeap;
    private readonly D3D12DescriptorHeap _srvHeap;
    private readonly D3D12DescriptorHeap _samplerHeap;
    private readonly BindlessDescriptorHeap _bindlessHeap;

    private bool _disposed;

    public unsafe D3D12DescriptorAllocator(D3D12RenderDevice device, uint initialRtvCount = 256, uint initialDsvCount = 256, uint initialSrvCount = 1024, uint initialSamplerCount = 256, uint initialBindlessCount = 10000)
    {
        var pDevice = device.NativeDevice;

        _rtvHeap = new D3D12DescriptorHeap("rtv", pDevice, DescriptorHeapType.Rtv, initialRtvCount);
        _dsvHeap = new D3D12DescriptorHeap("dsv", pDevice, DescriptorHeapType.Dsv, initialDsvCount);
        _srvHeap = new D3D12DescriptorHeap("srv", pDevice, DescriptorHeapType.CbvSrvUav, initialSrvCount);
        _samplerHeap = new D3D12DescriptorHeap("sampler", pDevice, DescriptorHeapType.Sampler, initialSamplerCount);
        _bindlessHeap = new BindlessDescriptorHeap(pDevice, initialBindlessCount);
    }

    ~D3D12DescriptorAllocator()
    {
        Dispose();
    }

    #region RTV Methods

    public RenderTargetDescriptor AllocateRTV()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = _rtvHeap.AllocateDescriptor();
        if (index == uint.MaxValue)
        {
            throw new InvalidOperationException("Failed to allocate RTV descriptor");
        }

        return new RenderTargetDescriptor { Index = index };
    }

    public RenderTargetDescriptor[] AllocateRTVs(uint count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = _rtvHeap.AllocateDescriptors(count);
        if (baseIndex == uint.MaxValue)
        {
            throw new InvalidOperationException($"Failed to allocate {count} RTV descriptors");
        }

        var descriptors = new RenderTargetDescriptor[count];
        for (uint i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new RenderTargetDescriptor { Index = index };
        }

        return descriptors;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CpuDescriptorHandle GetCpuHandle(RenderTargetDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _rtvHeap.GetCpuHandle(descriptor.Index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Release(RenderTargetDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _rtvHeap.ReleaseDescriptor(descriptor.Index);
    }

    public void Release(ReadOnlySpan<RenderTargetDescriptor> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    #endregion

    #region DSV Methods

    public DepthStencilDescriptor AllocateDSV()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = _dsvHeap.AllocateDescriptor();
        if (index == uint.MaxValue)
        {
            throw new InvalidOperationException("Failed to allocate DSV descriptor");
        }

        return new DepthStencilDescriptor { Index = index };
    }

    public DepthStencilDescriptor[] AllocateDSVs(uint count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = _dsvHeap.AllocateDescriptors(count);
        if (baseIndex == uint.MaxValue)
        {
            throw new InvalidOperationException($"Failed to allocate {count} DSV descriptors");
        }

        var descriptors = new DepthStencilDescriptor[count];
        for (uint i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new DepthStencilDescriptor { Index = index };
        }

        return descriptors;
    }

    public CpuDescriptorHandle GetCpuHandle(DepthStencilDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _dsvHeap.GetCpuHandle(descriptor.Index);
    }

    public void Release(DepthStencilDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _dsvHeap.ReleaseDescriptor(descriptor.Index);
    }

    public void Release(ReadOnlySpan<DepthStencilDescriptor> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    #endregion

    #region SRV Methods

    public ShaderResourceDescriptor AllocateSRV()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = _srvHeap.AllocateDescriptor();
        if (index == uint.MaxValue)
        {
            throw new InvalidOperationException("Failed to allocate SRV descriptor");
        }

        _srvHeap.CopyToShaderVisibleHeap(index);
        return new ShaderResourceDescriptor { Index = index };
    }

    public ShaderResourceDescriptor[] AllocateSRVs(uint count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = _srvHeap.AllocateDescriptors(count);
        if (baseIndex == uint.MaxValue)
        {
            throw new InvalidOperationException($"Failed to allocate {count} SRV descriptors");
        }

        var descriptors = new ShaderResourceDescriptor[count];
        for (uint i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new ShaderResourceDescriptor { Index = index };
        }

        _srvHeap.CopyToShaderVisibleHeap(baseIndex, count);
        return descriptors;
    }

    public CpuDescriptorHandle GetCpuHandle(ShaderResourceDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _srvHeap.GetCpuHandle(descriptor.Index);
    }

    public GpuDescriptorHandle GetGpuHandle(ShaderResourceDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _srvHeap.GetGpuHandle(descriptor.Index);
    }

    public void Release(ShaderResourceDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _srvHeap.ReleaseDescriptor(descriptor.Index);
    }

    public void Release(ReadOnlySpan<ShaderResourceDescriptor> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    #endregion

    #region Sampler Methods

    public SamplerDescriptor AllocateSampler()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = _samplerHeap.AllocateDescriptor();
        if (index == uint.MaxValue)
        {
            throw new InvalidOperationException("Failed to allocate Sampler descriptor");
        }

        _samplerHeap.CopyToShaderVisibleHeap(index);
        return new SamplerDescriptor { Index = index };
    }

    public SamplerDescriptor[] AllocateSamplers(uint count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = _samplerHeap.AllocateDescriptors(count);
        if (baseIndex == uint.MaxValue)
        {
            throw new InvalidOperationException($"Failed to allocate {count} Sampler descriptors");
        }

        var descriptors = new SamplerDescriptor[count];
        for (uint i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new SamplerDescriptor { Index = index };
        }

        _samplerHeap.CopyToShaderVisibleHeap(baseIndex, count);
        return descriptors;
    }

    public CpuDescriptorHandle GetCpuHandle(SamplerDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samplerHeap.GetCpuHandle(descriptor.Index);
    }

    public GpuDescriptorHandle GetGpuHandle(SamplerDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _samplerHeap.GetGpuHandle(descriptor.Index);
    }

    public void Release(SamplerDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _samplerHeap.ReleaseDescriptor(descriptor.Index);
    }

    public void Release(ReadOnlySpan<SamplerDescriptor> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    #endregion

    #region Bindless Methods

    /// <summary>
    /// Allocates a bindless descriptor for SM 6.6 rendering.
    /// The returned descriptor maintains a 1:1 relationship between allocation index and shader index.
    /// </summary>
    public BindlessDescriptor AllocateBindless()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var index = _bindlessHeap.AllocateDescriptor();
        if (index == uint.MaxValue)
        {
            throw new InvalidOperationException("Failed to allocate bindless descriptor");
        }

        return new BindlessDescriptor { Index = index };
    }

    /// <summary>
    /// Allocates multiple bindless descriptors for SM 6.6 rendering.
    /// </summary>
    public BindlessDescriptor[] AllocateBindless(uint count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var baseIndex = _bindlessHeap.AllocateDescriptors(count);
        if (baseIndex == uint.MaxValue)
        {
            throw new InvalidOperationException($"Failed to allocate {count} bindless descriptors");
        }

        var descriptors = new BindlessDescriptor[count];
        for (uint i = 0; i < count; i++)
        {
            var index = baseIndex + i;
            descriptors[i] = new BindlessDescriptor { Index = index };
        }

        return descriptors;
    }

    public CpuDescriptorHandle GetCpuHandle(BindlessDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _bindlessHeap.GetCpuHandle(descriptor.Index);
    }

    public GpuDescriptorHandle GetGpuHandle(BindlessDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _bindlessHeap.GetGpuHandle(descriptor.Index);
    }

    /// <summary>
    /// Releases a bindless descriptor.
    /// </summary>
    public void Release(BindlessDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _bindlessHeap.ReleaseDescriptor(descriptor.Index);
    }

    /// <summary>
    /// Releases multiple bindless descriptors.
    /// </summary>
    public void Release(ReadOnlySpan<BindlessDescriptor> descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            Release(descriptor);
        }
    }

    #endregion

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
    /// Gets the SRV heap for binding to the command list.
    /// </summary>
    public ID3D12DescriptorHeap* GetSRVHeap() => _srvHeap.ShaderVisibleHeap;

    /// <summary>
    /// Gets the sampler heap for binding to the command list.
    /// </summary>
    public ID3D12DescriptorHeap* GetSamplerHeap() => _samplerHeap.ShaderVisibleHeap;

    /// <summary>
    /// Gets the bindless heap for SM 6.6 bindless rendering.
    /// </summary>
    public ID3D12DescriptorHeap* GetBindlessHeap() => _bindlessHeap.BindlessHeap;

    /// <summary>
    /// Gets the shader visible heaps that need to be bound to the command list.
    /// </summary>
    public ID3D12DescriptorHeap*[] GetShaderVisibleHeaps()
    {
        return [_srvHeap.ShaderVisibleHeap, _samplerHeap.ShaderVisibleHeap];
    }

    /// <summary>
    /// Gets the shader visible heaps including bindless heap for SM 6.6 rendering.
    /// </summary>
    public ConstPtr<ID3D12DescriptorHeap>[] GetShaderVisibleHeapsWithBindless()
    {
        return [_bindlessHeap.BindlessHeap, _srvHeap.ShaderVisibleHeap, _samplerHeap.ShaderVisibleHeap];
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
        _srvHeap.Dispose();
        _samplerHeap.Dispose();
        _bindlessHeap.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}