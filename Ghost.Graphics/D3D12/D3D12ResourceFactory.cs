using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;

namespace Ghost.Graphics.D3D12;
internal class D3D12ResourceFactory : IResourceFactory
{
    private readonly D3D12ResourceAllocator _allocator;

    public unsafe D3D12ResourceFactory(D3D12RenderDevice device, RenderSystem renderSystem)
    {
        _allocator = new D3D12ResourceAllocator((IDXGIAdapter*)device.Adapter, (ID3D12Device*)device.NativeDevice, renderSystem);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IRenderTarget CreateRenderTarget(ref readonly RenderTargetDesc desc)
    {
        var usage = desc.Type == RenderTargetType.Color ? TextureUsage.RenderTarget : TextureUsage.DepthStencil;
        if (desc.CreationFlags.HasFlag(RenderTargetCreationFlags.AllowUAV))
        {
            usage |= TextureUsage.UnorderedAccess;
        }

        var textureDesc = new TextureDesc(desc.Width, desc.Height, desc.Format, desc.Dimension, 1, usage);
        return new D3D12RenderTarget(CreateTextureHandle(in textureDesc), in desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TextureHandle CreateTextureHandle(ref readonly TextureDesc desc)
    {
        return _allocator.CreateTexture2D(in desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ITexture CreateTexture(ref readonly TextureDesc desc)
    {
        return new D3D12Texture(CreateTextureHandle(in desc), in desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferHandle CreateBufferHandle(ref readonly BufferDesc desc)
    {
        return _allocator.CreateBuffer(in desc);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IBuffer CreateBuffer(ref readonly BufferDesc desc)
    {
        throw new NotImplementedException();
    }
}