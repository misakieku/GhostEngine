using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.DXGI_Alias;

using Ghost.Graphics.Core;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of swap chain interface
/// </summary>
internal unsafe class D3D12SwapChain : ISwapChain
{
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private ComPtr<IDXGISwapChain4> _swapChain;
    private UnsafeArray<Handle<Texture>> _backBuffers;
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

    public D3D12SwapChain(D3D12ResourceDatabase resourceDatabase, IDXGIFactory7* pFactory, ID3D12CommandQueue* pCommandQueue, SwapChainDesc desc)
    {
        _resourceDatabase = resourceDatabase;

        _backBuffers = new UnsafeArray<Handle<Texture>>(D3D12PipelineResource.BACK_BUFFER_COUNT, Allocator.Persistent);

        Width = desc.width;
        Height = desc.height;
        BufferCount = D3D12PipelineResource.BACK_BUFFER_COUNT;

        CreateSwapChain(pFactory, pCommandQueue, desc);
        CreateBackBuffers();
    }

    private void CreateSwapChain(IDXGIFactory7* pFactory, ID3D12CommandQueue* commandQueue, SwapChainDesc desc)
    {
        var swapChainDesc = new DXGI_SWAP_CHAIN_DESC1
        {
            Width = desc.width,
            Height = desc.height,
            Format = desc.format.ToDXGIFormat(),
            SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
            BufferUsage = DXGI_USAGE_BACK_BUFFER | DXGI_USAGE_RENDER_TARGET_OUTPUT,
            BufferCount = D3D12PipelineResource.BACK_BUFFER_COUNT,
            Scaling = DXGI_SCALING_STRETCH,
            SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD,
            AlphaMode = DXGI_ALPHA_MODE_IGNORE,
            Flags = (uint)DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING,
            Stereo = false,
        };

        using ComPtr<IDXGISwapChain1> tempSwapChain = default;

        switch (desc.target.type)
        {
            case SwapChainTargetType.Composition:
                pFactory->CreateSwapChainForComposition((IUnknown*)commandQueue, &swapChainDesc, null, tempSwapChain.GetAddressOf());

                // Set the composition surface
                if (desc.target.compositionSurface != null)
                {
                    using var swapChainPanelNative = ISwapChainPanelNative.FromSwapChainPanel(desc.target.compositionSurface);
                    swapChainPanelNative.SetSwapChain((IntPtr)tempSwapChain.Get());
                }
                break;

            case SwapChainTargetType.WindowHandle:
                var swapChainFullscreenDesc = new DXGI_SWAP_CHAIN_FULLSCREEN_DESC
                {
                    Windowed = true,
                };

                pFactory->CreateSwapChainForHwnd(
                    (IUnknown*)commandQueue,
                    new HWND(desc.target.windowHandle.ToPointer()),
                    &swapChainDesc,
                    &swapChainFullscreenDesc,
                    null,
                    tempSwapChain.GetAddressOf());
                break;

            default:
                throw new ArgumentException("Unsupported swap chain target type.");
        }

        IDXGISwapChain4* pSwapChain = default;
        tempSwapChain.Get()->QueryInterface(__uuidof(pSwapChain), (void**)&pSwapChain);

        _swapChain.Attach(pSwapChain);
    }

    private void CreateBackBuffers()
    {
        for (uint i = 0; i < BufferCount; i++)
        {
            ComPtr<ID3D12Resource> backBuffer = default;
            _swapChain.Get()->GetBuffer(i, backBuffer.IID(), backBuffer.PPV());
            backBuffer.Get()->SetName($"SwapChain_BackBuffer_{i}");

            _backBuffers[i] = _resourceDatabase.ImportExternalResource(backBuffer, ResourceState.Present).AsTexture();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle<Texture> GetCurrentBackBuffer()
    {
        return _backBuffers[_swapChain.Get()->GetCurrentBackBufferIndex()];
    }

    public void Present(bool vsync = true)
    {
        var presentFlags = 0u;
        var syncInterval = vsync ? 1u : 0u;

        if (_swapChain.Get()->Present(syncInterval, presentFlags).FAILED)
        {
            throw new InvalidOperationException("Failed to present swap chain.");
        }
    }

    public void Resize(uint width, uint height)
    {
        if (Width == width && Height == height)
        {
            return;
        }

        // Release old back buffers and render targets
        for (var i = 0; i < _backBuffers.Count; i++)
        {
            _resourceDatabase.ReleaseResource(_backBuffers[i].AsResource());
        }

        // Resize the swap chain
        if (_swapChain.Get()->ResizeBuffers(BufferCount, width, height, DXGI_FORMAT_B8G8R8A8_UNORM, (uint)DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING).FAILED)
        {
            throw new InvalidOperationException("Failed to resize swap chain buffers.");
        }

        Width = width;
        Height = height;

        // Recreate back buffers
        CreateBackBuffers();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        for (var i = 0; i < _backBuffers.Count; i++)
        {
            _resourceDatabase.ReleaseResource(_backBuffers[i].AsResource());
        }

        _swapChain.Dispose();
        _backBuffers.Dispose();
        _disposed = true;
    }
}
