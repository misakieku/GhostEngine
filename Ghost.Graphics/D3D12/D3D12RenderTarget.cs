using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of render target interface
/// Supports either color OR depth rendering, not both
/// </summary>
internal unsafe class D3D12RenderTarget : IRenderTarget
{
    private readonly D3D12Texture _target;
    private bool _disposed;

    public uint Width
    {
        get;
    }

    public uint Height
    {
        get;
    }

    public RenderTargetType Type
    {
        get;
    }

    public ITexture Target => _target;

    /// <summary>
    /// Create a new render target with its own texture
    /// </summary>
    public D3D12RenderTarget(TextureHandle handle, ref readonly RenderTargetDesc desc)
    {
        Width = desc.Width;
        Height = desc.Height;
        Type = desc.Type;

        // Create the target texture based on type
        var usage = Type == RenderTargetType.Color ? TextureUsage.RenderTarget : TextureUsage.DepthStencil;
        var textureDesc = new TextureDesc(desc.Width, desc.Height, desc.Format, desc.Dimension, desc.MipLevels, usage);
        _target = new D3D12Texture(handle, in textureDesc);
    }

    /// <summary>
    /// Wrap an existing texture as a render target (for swap chain back buffers)
    /// </summary>
    public D3D12RenderTarget(D3D12Texture existingTexture, RenderTargetType type)
    {
        _target = existingTexture;
        Width = existingTexture.Width;
        Height = existingTexture.Height;
        Type = type;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _target?.Dispose();
        _disposed = true;
    }
}
