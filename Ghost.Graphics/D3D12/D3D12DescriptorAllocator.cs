using Ghost.Graphics.RHI;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of descriptor allocator interface
/// </summary>
internal unsafe class D3D12DescriptorAllocator : IDescriptorAllocator
{
    private readonly DescriptorAllocator _internalAllocator;
    private bool _disposed;

    public D3D12DescriptorAllocator(ComPtr<ID3D12Device14> device)
    {
        _internalAllocator = new DescriptorAllocator();
    }

    public DescriptorHandle AllocateRTV()
    {
        var rtvDescriptor = _internalAllocator.AllocateRTV();
        return new DescriptorHandle(rtvDescriptor.Index);
    }

    public DescriptorHandle[] AllocateRTVs(uint count)
    {
        var rtvDescriptors = _internalAllocator.AllocateRTVs(count);
        return rtvDescriptors.Select(desc => new DescriptorHandle(desc.Index)).ToArray();
    }

    public DescriptorHandle AllocateDSV()
    {
        var dsvDescriptor = _internalAllocator.AllocateDSV();
        return new DescriptorHandle(dsvDescriptor.Index);
    }

    public DescriptorHandle AllocateSRV()
    {
        var srvDescriptor = _internalAllocator.AllocateSRV();
        return new DescriptorHandle(srvDescriptor.Index);
    }

    public DescriptorHandle AllocateSampler()
    {
        var samplerDescriptor = _internalAllocator.AllocateSampler();
        return new DescriptorHandle(samplerDescriptor.Index);
    }

    public DescriptorHandle AllocateBindless()
    {
        var bindlessDescriptor = _internalAllocator.AllocateBindless();
        return new DescriptorHandle(bindlessDescriptor.Index);
    }

    public void ReleaseRTV(DescriptorHandle handle)
    {
        // TODO: Convert back to internal descriptor and release
    }

    public void ReleaseDSV(DescriptorHandle handle)
    {
        // TODO: Convert back to internal descriptor and release
    }

    public void ReleaseSRV(DescriptorHandle handle)
    {
        // TODO: Convert back to internal descriptor and release
    }

    public void ReleaseSampler(DescriptorHandle handle)
    {
        // TODO: Convert back to internal descriptor and release
    }

    public void ReleaseBindless(DescriptorHandle handle)
    {
        // TODO: Convert back to internal descriptor and release
    }

    public void Dispose()
    {
        if (_disposed) return;

        _internalAllocator?.Dispose();
        _disposed = true;
    }
}
