using Ghost.Graphics.Contracts;
using Ghost.Graphics.RHI;
using Ghost.Graphics.D3D12.Utilities;
using Win32;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;
using Win32.Graphics.Dxgi.Common;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of swap chain interface
/// </summary>
internal unsafe class D3D12SwapChain : ISwapChain
{
    private ComPtr<IDXGISwapChain4> _swapChain;
    private readonly D3D12Texture[] _backBuffers;
    private uint _currentBackBufferIndex;
    private bool _disposed;

    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public uint BufferCount { get; }

    public D3D12SwapChain(ComPtr<IDXGIFactory7> factory, ID3D12CommandQueue* commandQueue, SwapChainDesc desc)
    {
        _backBuffers = new D3D12Texture[desc.BufferCount];

        Width = desc.Width;
        Height = desc.Height;
        BufferCount = desc.BufferCount;

        CreateSwapChain(factory, commandQueue, desc);
        CreateBackBuffers();
    }

    private void CreateSwapChain(ComPtr<IDXGIFactory7> factory, ID3D12CommandQueue* commandQueue, SwapChainDesc desc)
    {
        var swapChainDesc = new SwapChainDescription1
        {
            Width = desc.Width,
            Height = desc.Height,
            Format = ConvertTextureFormat(desc.Format),
            SampleDesc = new SampleDescription(1, 0),
            BufferUsage = Usage.BackBuffer | Usage.RenderTargetOutput,
            BufferCount = desc.BufferCount,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.AllowTearing,
            Stereo = false,
        };

        using ComPtr<IDXGISwapChain1> tempSwapChain = default;

        switch (desc.Target.Type)
        {
            case SwapChainTargetType.Composition:
                factory.Get()->CreateSwapChainForComposition((IUnknown*)commandQueue, &swapChainDesc, null, tempSwapChain.GetAddressOf());

                // Set the composition surface
                if (desc.Target.CompositionSurface != null)
                {
                    var swapChainPanelNative = ISwapChainPanelNative.FromSwapChainPanel(desc.Target.CompositionSurface);
                    swapChainPanelNative.SetSwapChain((IntPtr)tempSwapChain.Get());
                }
                break;

            case SwapChainTargetType.WindowHandle:
                var swapChainFullscreenDesc = new SwapChainFullscreenDescription
                {
                    Windowed = true,
                };

                factory.Get()->CreateSwapChainForHwnd(
                    (IUnknown*)commandQueue,
                    desc.Target.WindowHandle,
                    &swapChainDesc,
                    &swapChainFullscreenDesc,
                    null,
                    tempSwapChain.GetAddressOf());
                break;

            default:
                throw new ArgumentException("Unsupported swap chain target type.");
        }

        if (tempSwapChain.Get()->QueryInterface(__uuidof<IDXGISwapChain4>(), _swapChain.GetVoidAddressOf()).Failure)
        {
            throw new InvalidOperationException("Failed to create IDXGISwapChain4 interface.");
        }

        _currentBackBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
    }

    private void CreateBackBuffers()
    {
        for (uint i = 0; i < BufferCount; i++)
        {
            ComPtr<ID3D12Resource> backBuffer = default;
            _swapChain.Get()->GetBuffer(i, __uuidof<ID3D12Resource>(), backBuffer.GetVoidAddressOf());
            backBuffer.Get()->SetName($"SwapChain_BackBuffer_{i}");

            _backBuffers[i] = new D3D12Texture(backBuffer.Move(), Width, Height, TextureFormat.B8G8R8A8_UNorm);
        }
    }

    public ITexture GetCurrentBackBuffer()
    {
        _currentBackBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
        return _backBuffers[_currentBackBufferIndex];
    }

    public void Present(bool vsync = true)
    {
        var presentFlags = PresentFlags.None;
        var syncInterval = vsync ? 1u : 0u;

        if (_swapChain.Get()->Present(syncInterval, presentFlags).Failure)
        {
            throw new InvalidOperationException("Failed to present swap chain.");
        }
    }

    public void Resize(uint width, uint height)
    {
        if (Width == width && Height == height)
            return;

        // Release old back buffers
        for (int i = 0; i < _backBuffers.Length; i++)
        {
            _backBuffers[i]?.Dispose();
        }

        // Resize the swap chain
        if (_swapChain.Get()->ResizeBuffers(BufferCount, width, height, Format.B8G8R8A8Unorm, SwapChainFlags.AllowTearing).Failure)
        {
            throw new InvalidOperationException("Failed to resize swap chain buffers.");
        }

        Width = width;
        Height = height;

        // Recreate back buffers
        CreateBackBuffers();
        _currentBackBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
    }

    private static Format ConvertTextureFormat(TextureFormat format)
    {
        return format switch
        {
            TextureFormat.R8G8B8A8_UNorm => Format.R8G8B8A8Unorm,
            TextureFormat.B8G8R8A8_UNorm => Format.B8G8R8A8Unorm,
            TextureFormat.R16G16B16A16_Float => Format.R16G16B16A16Float,
            TextureFormat.R32G32B32A32_Float => Format.R32G32B32A32Float,
            _ => throw new ArgumentException($"Unsupported texture format: {format}")
        };
    }

    public void Dispose()
    {
        if (_disposed) return;

        for (int i = 0; i < _backBuffers.Length; i++)
        {
            _backBuffers[i]?.Dispose();
        }

        _swapChain.Dispose();
        _disposed = true;
    }
}
