using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// Base class for D3D12 descriptor implementations.
/// </summary>
internal abstract class Descriptor
{
    protected readonly uint index;
    protected readonly bool isShaderVisible;

    protected Descriptor(uint index, bool isShaderVisible)
    {
        this.index = index;
        this.isShaderVisible = isShaderVisible;
    }

    public bool IsValid => index != uint.MaxValue;
    public uint Index => index;
    public bool IsShaderVisible => isShaderVisible;

    /// <summary>
    /// Gets the CPU descriptor handle.
    /// </summary>
    public abstract CpuDescriptorHandle CpuHandle
    {
        get;
    }

    /// <summary>
    /// Gets the GPU descriptor handle (only valid for shader-visible descriptors).
    /// </summary>
    public abstract GpuDescriptorHandle GpuHandle
    {
        get;
    }
}

/// <summary>
/// D3D12 implementation of render target view (RTV) descriptor.
/// </summary>
internal sealed class RenderTargetDescriptor : Descriptor
{
    private readonly CpuDescriptorHandle _cpuHandle;

    public RenderTargetDescriptor(uint index, CpuDescriptorHandle cpuHandle)
        : base(index, false)
    {
        _cpuHandle = cpuHandle;
    }

    public override CpuDescriptorHandle CpuHandle => _cpuHandle;
    public override GpuDescriptorHandle GpuHandle => default;
}

/// <summary>
/// D3D12 implementation of depth stencil view (DSV) descriptor.
/// </summary>
internal sealed class DepthStencilDescriptor : Descriptor
{
    private readonly CpuDescriptorHandle _cpuHandle;

    public DepthStencilDescriptor(uint index, CpuDescriptorHandle cpuHandle)
        : base(index, false) // DSVs are not shader visible
    {
        _cpuHandle = cpuHandle;
    }

    public override CpuDescriptorHandle CpuHandle => _cpuHandle;
    public override GpuDescriptorHandle GpuHandle => default;
}

/// <summary>
/// D3D12 implementation of shader resource view (SRV) descriptor.
/// </summary>
internal sealed class ShaderResourceDescriptor : Descriptor
{
    private readonly CpuDescriptorHandle _cpuHandle;
    private readonly GpuDescriptorHandle _gpuHandle;

    public ShaderResourceDescriptor(uint index, CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle)
        : base(index, true)
    {
        _cpuHandle = cpuHandle;
        _gpuHandle = gpuHandle;
    }

    public override CpuDescriptorHandle CpuHandle => _cpuHandle;
    public override GpuDescriptorHandle GpuHandle => _gpuHandle;
}

/// <summary>
/// D3D12 implementation of sampler descriptor.
/// </summary>
internal sealed class SamplerDescriptor : Descriptor
{
    private readonly CpuDescriptorHandle _cpuHandle;
    private readonly GpuDescriptorHandle _gpuHandle;

    public SamplerDescriptor(uint index, CpuDescriptorHandle cpuHandle, GpuDescriptorHandle gpuHandle)
        : base(index, true)
    {
        _cpuHandle = cpuHandle;
        _gpuHandle = gpuHandle;
    }

    public override CpuDescriptorHandle CpuHandle => _cpuHandle;
    public override GpuDescriptorHandle GpuHandle => _gpuHandle;
}