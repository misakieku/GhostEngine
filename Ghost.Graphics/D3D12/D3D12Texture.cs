using Ghost.Graphics.RHI;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of texture interface
/// </summary>
internal unsafe class D3D12Texture : ITexture
{
    private ComPtr<ID3D12Resource> _resource;
    private ResourceState _currentState;
    private bool _disposed;

    public uint Width { get; }
    public uint Height { get; }
    public TextureFormat Format { get; }
    public uint MipLevels { get; }
    public string Name { get; set; } = string.Empty;
    public ulong Size { get; }
    public ResourceState CurrentState => _currentState;

    public ID3D12Resource* NativeResource => _resource.Get();

    public D3D12Texture(ComPtr<ID3D12Resource> resource, uint width, uint height, TextureFormat format, uint mipLevels = 1)
    {
        _resource = resource.Move();
        Width = width;
        Height = height;
        Format = format;
        MipLevels = mipLevels;
        _currentState = ResourceState.Common;

        var desc = _resource.Get()->GetDesc();
        Size = (ulong)(desc.Width * desc.Height * GetBytesPerPixel(format));
    }

    public D3D12Texture(ComPtr<ID3D12Device14> device, TextureDesc desc)
    {
        Width = desc.Width;
        Height = desc.Height;
        Format = desc.Format;
        MipLevels = desc.MipLevels;
        _currentState = ResourceState.Common;

        CreateTexture(device, desc);
        Size = (ulong)(Width * Height * GetBytesPerPixel(Format));
    }

    private void CreateTexture(ComPtr<ID3D12Device14> device, TextureDesc desc)
    {
        var resourceDesc = new ResourceDescription
        {
            Dimension = ResourceDimension.Texture2D,
            Alignment = 0,
            Width = desc.Width,
            Height = desc.Height,
            DepthOrArraySize = 1,
            MipLevels = (ushort)desc.MipLevels,
            Format = ConvertTextureFormat(desc.Format),
            SampleDesc = new Win32.Graphics.Dxgi.Common.SampleDescription(1, 0),
            Layout = TextureLayout.Unknown,
            Flags = ConvertTextureUsage(desc.Usage)
        };

        var heapProps = new HeapProperties
        {
            Type = HeapType.Default,
            CPUPageProperty = CpuPageProperty.Unknown,
            MemoryPoolPreference = MemoryPool.Unknown,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        };

        device.Get()->CreateCommittedResource(
            &heapProps,
            HeapFlags.None,
            &resourceDesc,
            Win32.Graphics.Direct3D12.ResourceStates.Common,
            null,
            __uuidof<ID3D12Resource>(),
            _resource.GetVoidAddressOf());
    }

    private static Win32.Graphics.Dxgi.Common.Format ConvertTextureFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8G8B8A8_UNorm => Win32.Graphics.Dxgi.Common.Format.R8G8B8A8Unorm,
            TextureFormat.B8G8R8A8_UNorm => Win32.Graphics.Dxgi.Common.Format.B8G8R8A8Unorm,
            TextureFormat.R16G16B16A16_Float => Win32.Graphics.Dxgi.Common.Format.R16G16B16A16Float,
            TextureFormat.R32G32B32A32_Float => Win32.Graphics.Dxgi.Common.Format.R32G32B32A32Float,
            TextureFormat.D24_UNorm_S8_UInt => Win32.Graphics.Dxgi.Common.Format.D24UnormS8Uint,
            TextureFormat.D32_Float => Win32.Graphics.Dxgi.Common.Format.D32Float,
            _ => throw new ArgumentException($"Unsupported texture format: {format}")
        };
    }

    private static ResourceFlags ConvertTextureUsage(TextureUsage usage)
    {
        var flags = ResourceFlags.None;

        if ((usage & TextureUsage.RenderTarget) != 0)
            flags |= ResourceFlags.AllowRenderTarget;
        if ((usage & TextureUsage.DepthStencil) != 0)
            flags |= ResourceFlags.AllowDepthStencil;
        if ((usage & TextureUsage.UnorderedAccess) != 0)
            flags |= ResourceFlags.AllowUnorderedAccess;

        return flags;
    }

    private static uint GetBytesPerPixel(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8G8B8A8_UNorm => 4,
            TextureFormat.B8G8R8A8_UNorm => 4,
            TextureFormat.R16G16B16A16_Float => 8,
            TextureFormat.R32G32B32A32_Float => 16,
            TextureFormat.D24_UNorm_S8_UInt => 4,
            TextureFormat.D32_Float => 4,
            _ => 4
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        _resource.Dispose();
        _disposed = true;
    }
}
