using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;
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
    private readonly D3D12RenderTarget[] _backBuffers;
    private bool _disposed;

    public uint Width
    {
        get; private set;
    }

    public uint Height
    {
        get; private set;
    }

    public uint BufferCount
    {
        get;
    }

    public D3D12SwapChain(IDXGIFactory7* pFactory, ID3D12CommandQueue* pCommandQueue, SwapChainDesc desc)
    {
        _backBuffers = new D3D12RenderTarget[D3D12PipelineResource.BACK_BUFFER_COUNT];

        Width = desc.width;
        Height = desc.height;
        BufferCount = D3D12PipelineResource.BACK_BUFFER_COUNT;

        CreateSwapChain(pFactory, pCommandQueue, desc);
        CreateBackBuffers();
    }

    private void CreateSwapChain(IDXGIFactory7* pFactory, ID3D12CommandQueue* commandQueue, SwapChainDesc desc)
    {
        var swapChainDesc = new SwapChainDescription1
        {
            Width = desc.width,
            Height = desc.height,
            Format = ConvertTextureFormat(desc.format),
            SampleDesc = new SampleDescription(1, 0),
            BufferUsage = Usage.BackBuffer | Usage.RenderTargetOutput,
            BufferCount = D3D12PipelineResource.BACK_BUFFER_COUNT,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.AllowTearing,
            Stereo = false,
        };

        using ComPtr<IDXGISwapChain1> tempSwapChain = default;

        switch (desc.target.Type)
        {
            case SwapChainTargetType.Composition:
                pFactory->CreateSwapChainForComposition((IUnknown*)commandQueue, &swapChainDesc, null, tempSwapChain.GetAddressOf());

                // Set the composition surface
                if (desc.target.CompositionSurface != null)
                {
                    using var swapChainPanelNative = ISwapChainPanelNative.FromSwapChainPanel(desc.target.CompositionSurface);
                    swapChainPanelNative.SetSwapChain((IntPtr)tempSwapChain.Get());
                }
                break;

            case SwapChainTargetType.WindowHandle:
                var swapChainFullscreenDesc = new SwapChainFullscreenDescription
                {
                    Windowed = true,
                };

                pFactory->CreateSwapChainForHwnd(
                    (IUnknown*)commandQueue,
                    desc.target.WindowHandle,
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
    }

    private void CreateBackBuffers()
    {
        for (uint i = 0; i < BufferCount; i++)
        {
            ComPtr<ID3D12Resource> backBuffer = default;
            _swapChain.Get()->GetBuffer(i, __uuidof<ID3D12Resource>(), backBuffer.GetVoidAddressOf());
            backBuffer.Get()->SetName($"SwapChain_BackBuffer_{i}");

            _backBuffers[i] = D3D12RenderTarget.Create(backBuffer.Move(), Width, Height, 1, TextureFormat.B8G8R8A8_UNorm, RenderTargetType.Color);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IRenderTarget GetCurrentBackBuffer()
    {
        return _backBuffers[_swapChain.Get()->GetCurrentBackBufferIndex()];
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

        // Release old back buffers and render targets
        for (var i = 0; i < _backBuffers.Length; i++)
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
        if (_disposed)
        {
            return;
        }

        for (var i = 0; i < _backBuffers.Length; i++)
        {
            _backBuffers[i]?.Dispose();
        }

        _swapChain.Dispose();
        _disposed = true;
    }
}