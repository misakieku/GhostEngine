using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.Data;

/// <summary>
/// Render target view (RTV) descriptor.
/// </summary>
public readonly struct RenderTargetDescriptor
{
    public uint Index
    {
        get; init;
    }

    public static RenderTargetDescriptor Invalid => new() { Index = ~0u };

    public bool IsValid => Index != ~0u;
}

/// <summary>
/// Depth stencil view (DSV) descriptor.
/// </summary>
public readonly struct DepthStencilDescriptor
{
    public uint Index
    {
        get; init;
    }

    public static DepthStencilDescriptor Invalid => new() { Index = ~0u };

    public bool IsValid => Index != ~0u;
}

/// <summary>
/// Shader resource view (SRV) descriptor.
/// </summary>
public readonly struct ShaderResourceDescriptor
{
    public uint Index
    {
        get; init;
    }

    public static ShaderResourceDescriptor Invalid => new() { Index = ~0u };

    public bool IsValid => Index != ~0u;
}

/// <summary>
/// Sampler descriptor.
/// </summary>
public readonly struct SamplerDescriptor
{
    public uint Index
    {
        get; init;
    }

    public static SamplerDescriptor Invalid => new() { Index = ~0u };

    public bool IsValid => Index != ~0u;
}

/// <summary>
/// Bindless descriptor
/// </summary>
public readonly struct BindlessDescriptor
{
    public uint Index
    {
        get; init;
    }

    public static BindlessDescriptor Invalid => new() { Index = ~0u };

    public bool IsValid => Index != ~0u;
}