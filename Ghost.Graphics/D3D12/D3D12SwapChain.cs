using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of swap chain interface
/// </summary>
internal unsafe class D3D12SwapChain : IUnknownObject<IDXGISwapChain4>, ISwapChain
{
    private readonly D3D12ResourceDatabase _resourceDatabase;

    private UnsafeArray<Handle<Texture>> _backBuffers;

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

        IDXGISwapChain1* pTempSwapChain = default;

        switch (desc.target.type)
        {
            case SwapChainTargetType.Composition:
                ThrowIfFailed(pFactory->CreateSwapChainForComposition((IUnknown*)commandQueue, &swapChainDesc, null, &pTempSwapChain));

                // Set the composition surface
                if (desc.target.compositionSurface != null)
                {
                    using var swapChainPanelNative = ISwapChainPanelNative.FromSwapChainPanel(desc.target.compositionSurface);
                    swapChainPanelNative.SetSwapChain((IntPtr)pTempSwapChain);
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
                    &pTempSwapChain);
                break;

            default:
                throw new ArgumentException("Unsupported swap chain target type.");
        }

        IDXGISwapChain4* pSwapChain = default;
        pTempSwapChain->QueryInterface(__uuidof(pSwapChain), (void**)&pSwapChain);
        pTempSwapChain->Release();

        nativeObject.Attach(pSwapChain);
    }

    private void CreateBackBuffers()
    {
        for (uint i = 0; i < BufferCount; i++)
        {
            ID3D12Resource* pBackBuffer = default;
            nativeObject.Get()->GetBuffer(i, __uuidof(pBackBuffer), (void**)&pBackBuffer);
            pBackBuffer->SetName($"SwapChain_BackBuffer_{i}");

            _backBuffers[i] = _resourceDatabase.ImportExternalResource(pBackBuffer, ResourceState.Present).AsTexture();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle<Texture> GetCurrentBackBuffer()
    {
        ThrowIfDisposed();
        return _backBuffers[nativeObject.Get()->GetCurrentBackBufferIndex()];
    }

    public void Present(bool vsync = true)
    {
        ThrowIfDisposed();

        var presentFlags = 0u;
        var syncInterval = vsync ? 1u : 0u;

        ThrowIfFailed(nativeObject.Get()->Present(syncInterval, presentFlags));
    }

    public void Resize(uint width, uint height)
    {
        ThrowIfDisposed();

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
        if (nativeObject.Get()->ResizeBuffers(BufferCount, width, height, DXGI_FORMAT_B8G8R8A8_UNORM, (uint)DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING).FAILED)
        {
            throw new InvalidOperationException("Failed to resize swap chain buffers.");
        }

        Width = width;
        Height = height;

        // Recreate back buffers
        CreateBackBuffers();
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed)
        {
            return;
        }

        for (var i = 0; i < _backBuffers.Count; i++)
        {
            _resourceDatabase.ReleaseResource(_backBuffers[i].AsResource());
        }

        _backBuffers.Dispose();

        base.Dispose(disposing);
    }
}
