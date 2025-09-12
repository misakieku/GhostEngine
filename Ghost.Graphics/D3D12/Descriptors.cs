using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// Implementation of render target view (RTV) descriptor.
/// </summary>
public readonly struct RenderTargetDescriptor
{
    public uint Index
    {
        get;
    }

    public CpuDescriptorHandle CpuHandle
    {
        get;
    }

    public RenderTargetDescriptor(uint index, CpuDescriptorHandle cpuHandle)
    {
        Index = index;
        CpuHandle = cpuHandle;
    }
}

/// <summary>
/// Implementation of depth stencil view (DSV) descriptor.
/// </summary>
public readonly struct DepthStencilDescriptor
{
    public uint Index
    {
        get;
    }

    public CpuDescriptorHandle CpuHandle
    {
        get;
    }

    public DepthStencilDescriptor(uint index, CpuDescriptorHandle cpuHandle)
    {
        Index = index;
        CpuHandle = cpuHandle;
    }
}

/// <summary>
/// Implementation of shader resource view (SRV) descriptor.
/// </summary>
public sealed class ShaderResourceDescriptor
{
    public uint Index
    {
        get;
    }

    public CpuDescriptorHandle CpuHandle
    {
        get;
    }

    public GpuDescriptorHandle GpuHandle
    {
        get;
    }

    public ShaderResourceDescriptor(uint index, CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle)
    {
        Index = index;
        CpuHandle = cpuHandle;
        GpuHandle = gpuHandle;
    }
}

/// <summary>
/// Implementation of sampler descriptor.
/// </summary>
public sealed class SamplerDescriptor
{
    public uint Index
    {
        get;
    }

    public CpuDescriptorHandle CpuHandle
    {
        get;
    }

    public GpuDescriptorHandle GpuHandle
    {
        get;
    }

    public SamplerDescriptor(uint index, CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle)
    {
        Index = index;
        CpuHandle = cpuHandle;
        GpuHandle = gpuHandle;
    }
}

/// <summary>
/// Implementation of bindless descriptor for SM 6.6 rendering.
/// This descriptor maintains a 1:1 relationship between allocation indices and shader indices.
/// </summary>
public sealed class BindlessDescriptor
{
    public uint Index
    {
        get;
    }

    public CpuDescriptorHandle CpuHandle
    {
        get;
    }

    public GpuDescriptorHandle GpuHandle
    {
        get;
    }

    public BindlessDescriptor(uint index, CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle)
    {
        Index = index;
        CpuHandle = cpuHandle;
        GpuHandle = gpuHandle;
    }
}