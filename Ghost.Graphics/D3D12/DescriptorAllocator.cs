using Ghost.Core;
using Ghost.Graphics.D3D12.Utilities;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of descriptor allocator that manages different types of descriptor heaps.
/// </summary>
internal class DescriptorAllocator : IDisposable
{
    private readonly DescriptorHeapAllocator _rtvHeap;
    private readonly DescriptorHeapAllocator _dsvHeap;
    private readonly DescriptorHeapAllocator _srvHeap;
    private readonly DescriptorHeapAllocator _samplerHeap;

    private bool _disposed;

    public DescriptorAllocator(uint initialRtvCount = 256, uint initialDsvCount = 256, uint initialSrvCount = 1024, uint initialSamplerCount = 256)
    {
        var device = GraphicsPipeline.GraphicsDevice;

        _rtvHeap = new DescriptorHeapAllocator(device.NativeDevice, DescriptorHeapType.Rtv, initialRtvCount);
        _dsvHeap = new DescriptorHeapAllocator(device.NativeDevice, DescriptorHeapType.Dsv, initialDsvCount);
        _srvHeap = new DescriptorHeapAllocator(device.NativeDevice, DescriptorHeapType.CbvSrvUav, initialSrvCount);
        _samplerHeap = new DescriptorHeapAllocator(device.NativeDevice, DescriptorHeapType.Sampler, initialSamplerCount);
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

        var cpuHandle = _rtvHeap.GetCpuHandle(index);
        return new RenderTargetDescriptor(index, cpuHandle);
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
            var cpuHandle = _rtvHeap.GetCpuHandle(index);
            descriptors[i] = new RenderTargetDescriptor(index, cpuHandle);
        }

        return descriptors;
    }

    public void ReleaseRTV(RenderTargetDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (descriptor is RenderTargetDescriptor d3d12Descriptor)
        {
            _rtvHeap.ReleaseDescriptor(d3d12Descriptor.Index);
        }
    }

    public void ReleaseRTVs(RenderTargetDescriptor[] descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            ReleaseRTV(descriptor);
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

        var cpuHandle = _dsvHeap.GetCpuHandle(index);
        return new DepthStencilDescriptor(index, cpuHandle);
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
            var cpuHandle = _dsvHeap.GetCpuHandle(index);
            descriptors[i] = new DepthStencilDescriptor(index, cpuHandle);
        }

        return descriptors;
    }

    public void ReleaseDSV(DepthStencilDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (descriptor is DepthStencilDescriptor d3d12Descriptor)
        {
            _dsvHeap.ReleaseDescriptor(d3d12Descriptor.Index);
        }
    }

    public void ReleaseDSVs(DepthStencilDescriptor[] descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            ReleaseDSV(descriptor);
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

        var cpuHandle = _srvHeap.GetCpuHandle(index);
        var gpuHandle = _srvHeap.GetGpuHandle(index);

        // Copy to shader visible heap
        _srvHeap.CopyToShaderVisibleHeap(index);

        return new ShaderResourceDescriptor(index, cpuHandle, gpuHandle);
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
            var cpuHandle = _srvHeap.GetCpuHandle(index);
            var gpuHandle = _srvHeap.GetGpuHandle(index);
            descriptors[i] = new ShaderResourceDescriptor(index, cpuHandle, gpuHandle);
        }

        // Copy all descriptors to shader visible heap
        _srvHeap.CopyToShaderVisibleHeap(baseIndex, count);

        return descriptors;
    }

    public void ReleaseSRV(ShaderResourceDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (descriptor is ShaderResourceDescriptor d3d12Descriptor)
        {
            _srvHeap.ReleaseDescriptor(d3d12Descriptor.Index);
        }
    }

    public void ReleaseSRVs(ShaderResourceDescriptor[] descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            ReleaseSRV(descriptor);
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

        var cpuHandle = _samplerHeap.GetCpuHandle(index);
        var gpuHandle = _samplerHeap.GetGpuHandle(index);

        // Copy to shader visible heap
        _samplerHeap.CopyToShaderVisibleHeap(index);

        return new SamplerDescriptor(index, cpuHandle, gpuHandle);
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
            var cpuHandle = _samplerHeap.GetCpuHandle(index);
            var gpuHandle = _samplerHeap.GetGpuHandle(index);
            descriptors[i] = new SamplerDescriptor(index, cpuHandle, gpuHandle);
        }

        // Copy all descriptors to shader visible heap
        _samplerHeap.CopyToShaderVisibleHeap(baseIndex, count);

        return descriptors;
    }

    public void ReleaseSampler(SamplerDescriptor descriptor)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (descriptor is SamplerDescriptor d3d12Descriptor)
        {
            _samplerHeap.ReleaseDescriptor(d3d12Descriptor.Index);
        }
    }

    public void ReleaseSamplers(SamplerDescriptor[] descriptors)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var descriptor in descriptors)
        {
            ReleaseSampler(descriptor);
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Gets the RTV heap for binding to the command list.
    /// </summary>
    public ConstPtr<ID3D12DescriptorHeap> GetRTVHeap() => _rtvHeap.Heap;

    /// <summary>
    /// Gets the DSV heap for binding to the command list.
    /// </summary>
    public ConstPtr<ID3D12DescriptorHeap> GetDSVHeap() => _dsvHeap.Heap;

    /// <summary>
    /// Gets the SRV heap for binding to the command list.
    /// </summary>
    public ConstPtr<ID3D12DescriptorHeap> GetSRVHeap() => _srvHeap.ShaderVisibleHeap;

    /// <summary>
    /// Gets the sampler heap for binding to the command list.
    /// </summary>
    public ConstPtr<ID3D12DescriptorHeap> GetSamplerHeap() => _samplerHeap.ShaderVisibleHeap;

    /// <summary>
    /// Gets the shader visible heaps that need to be bound to the command list.
    /// </summary>
    public ConstPtr<ID3D12DescriptorHeap>[] GetShaderVisibleHeaps()
    {
        return [_srvHeap.ShaderVisibleHeap, _samplerHeap.ShaderVisibleHeap];
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

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}