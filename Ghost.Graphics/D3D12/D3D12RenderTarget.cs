using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of render target interface
/// Supports either color OR depth rendering, not both
/// </summary>
internal unsafe class D3D12RenderTarget : D3D12Texture, IRenderTarget
{
    public RenderTargetType Type
    {
        get;
    }

    private D3D12RenderTarget(ComPtr<ID3D12Resource> resource, uint width, uint height, uint slice, TextureFormat format, RenderTargetType type, uint mipLevels = 1)
        : base(resource, width, height, slice, format, mipLevels)
    {
        Type = type;
    }

    private D3D12RenderTarget(TextureHandle handle, ref readonly RenderTargetDesc desc, ref readonly TextureDesc texDesc)
        : base(handle, in texDesc)
    {
        Type = desc.Type;
    }

    /// <summary>
    /// Create a new render target with its own texture
    /// </summary>
    /// <param name="handle">The handle to the texture resource</param>
    /// <param name="desc">The descriptor to describe the render target</param>
    /// <returns>New render target instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static D3D12RenderTarget Create(TextureHandle handle, ref readonly RenderTargetDesc desc)
    {
        var texDesc = RenderTargetDesc.ToTextureDescriptor(desc);
        return new D3D12RenderTarget(handle, in desc, in texDesc);
    }

    /// <summary>
    /// Create a new render target from an existing D3D12 resource
    /// </summary>
    /// <param name="resource">The existing D3D12 resource</param>
    /// <param name="width">The width of the render target</param>
    /// <param name="height">The height of the render target</param>
    /// <param name="format">The format of the render target</param>
    /// <param name="type">The type of the render target</param>
    /// <param name="mipLevels">The number of mip levels</param>
    /// <returns>New render target instance</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static D3D12RenderTarget Create(ComPtr<ID3D12Resource> resource, uint width, uint height, uint slice, TextureFormat format, RenderTargetType type, uint mipLevels = 1)
    {
        return new D3D12RenderTarget(resource, width, height, slice, format, type, mipLevels);
    }
}
