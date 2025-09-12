using Ghost.Graphics.Data;
using Ghost.Graphics.RHI;
using Win32;
using Win32.Graphics.Direct3D12;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of texture interface using resource handles
/// </summary>
internal unsafe class D3D12Texture : ITexture
{
    private readonly TextureHandle _handle;
    private readonly ComPtr<ID3D12Resource> _externalResource;
    private ResourceState _currentState;
    private bool _disposed;

    public uint Width
    {
        get;
    }

    public uint Height
    {
        get;
    }

    public uint Slice
    {
        get;
    }

    public TextureFormat Format
    {
        get;
    }

    public uint MipLevels
    {
        get;
    }

    public string Name
    {
        get; set;
    } = string.Empty;

    public ulong Size
    {
        get;
    }

    public ResourceState CurrentState => _currentState;

    public ID3D12Resource* NativeResource => _handle.IsValid ? _handle.ResourceHandle.GetAllocation().Resource : _externalResource.Get();

    public D3D12Texture(ComPtr<ID3D12Resource> resource, uint width, uint height, uint slice, TextureFormat format, uint mipLevels = 1)
    {
        _handle = TextureHandle.Invalid;
        _externalResource = resource.Move();

        Width = width;
        Height = height;
        Slice = slice;
        Format = format;
        MipLevels = mipLevels;
        _currentState = ResourceState.Common;
        Size = Width * Height * GetBytesPerPixel(Format);
    }

    public D3D12Texture(TextureHandle handle, ref readonly TextureDesc desc)
    {
        _handle = handle;
        _externalResource = default;

        Width = desc.Width;
        Height = desc.Height;
        Slice = desc.Slice;
        Format = desc.Format;

        var mipLevels = desc.MipLevels;
        if (mipLevels <= 0)
        {
            mipLevels = (uint)(Math.Floor(Math.Log2(Math.Max(Width, Height))) + 1);
        }

        MipLevels = mipLevels;
        _currentState = ResourceState.Common;
        Size = Width * Height * GetBytesPerPixel(Format);
    }

    ~D3D12Texture()
    {
        Dispose(false);
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

    public void SetCurrentState(ResourceState state)
    {
        _currentState = state;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (_handle.IsValid)
        {
            _handle.Dispose();
        }
        else
        {
            _externalResource.Dispose();
        }

        _disposed = true;
    }
}
