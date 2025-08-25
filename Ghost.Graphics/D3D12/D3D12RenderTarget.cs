using Ghost.Graphics.RHI;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of render target interface
/// Supports either color OR depth rendering, not both
/// </summary>
internal unsafe class D3D12RenderTarget : IRenderTarget
{
    private readonly D3D12Texture _target;
    private bool _disposed;

    public uint Width { get; }
    public uint Height { get; }
    public RenderTargetType Type { get; }
    public ITexture Target => _target;

    public D3D12RenderTarget(ComPtr<ID3D12Device14> device, D3D12DescriptorAllocator descriptorAllocator, RenderTargetDesc desc)
    {
        Width = desc.Width;
        Height = desc.Height;
        Type = desc.Type;

        // Create the target texture based on type
        var usage = Type == RenderTargetType.Color ? TextureUsage.RenderTarget : TextureUsage.DepthStencil;
        var textureDesc = new TextureDesc(desc.Width, desc.Height, desc.Format, 1, usage);
        _target = new D3D12Texture(device, textureDesc);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _target?.Dispose();
        _disposed = true;
    }
}
