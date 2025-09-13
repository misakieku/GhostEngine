using Ghost.Graphics.Data;
using System.Runtime.CompilerServices;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.RHI;

public interface IDescriptorAllocator
{
    public RenderTargetDescriptor AllocateRTV();

    public RenderTargetDescriptor[] AllocateRTVs(uint count);

    public DepthStencilDescriptor AllocateDSV();

    public DepthStencilDescriptor[] AllocateDSVs(uint count);

    public ShaderResourceDescriptor AllocateSRV();

    public ShaderResourceDescriptor[] AllocateSRVs(uint count);

    public SamplerDescriptor AllocateSampler();

    public SamplerDescriptor[] AllocateSamplers(uint count);

    public BindlessDescriptor AllocateBindless();

    public BindlessDescriptor[] AllocateBindless(uint count);

    public CpuDescriptorHandle GetCpuHandle(RenderTargetDescriptor descriptor);

    public CpuDescriptorHandle GetCpuHandle(DepthStencilDescriptor descriptor);

    public CpuDescriptorHandle GetCpuHandle(ShaderResourceDescriptor descriptor);

    public GpuDescriptorHandle GetGpuHandle(ShaderResourceDescriptor descriptor);

    public CpuDescriptorHandle GetCpuHandle(SamplerDescriptor descriptor);

    public GpuDescriptorHandle GetGpuHandle(SamplerDescriptor descriptor);

    public CpuDescriptorHandle GetCpuHandle(BindlessDescriptor descriptor);

    public GpuDescriptorHandle GetGpuHandle(BindlessDescriptor descriptor);

    public void Release(RenderTargetDescriptor descriptor);

    public void Release(ReadOnlySpan<RenderTargetDescriptor> descriptors);

    public void Release(DepthStencilDescriptor descriptor);

    public void Release(ReadOnlySpan<DepthStencilDescriptor> descriptors);

    public void Release(ShaderResourceDescriptor descriptor);

    public void Release(ReadOnlySpan<ShaderResourceDescriptor> descriptors);

    public void Release(SamplerDescriptor descriptor);

    public void Release(ReadOnlySpan<SamplerDescriptor> descriptors);

    public void Release(BindlessDescriptor descriptor);

    public void Release(ReadOnlySpan<BindlessDescriptor> descriptors);
}