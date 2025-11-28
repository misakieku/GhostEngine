using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

using static TerraFX.Aliases.DXGI_Alias;

namespace Ghost.Graphics.D3D12;

/// <summary>
/// D3D12 implementation of swap chain interface
/// </summary>
internal unsafe class D3D12SwapChain : ISwapChain
{
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12RenderDevice _renderDevice;

    private UniquePtr<IDXGISwapChain4> _swapChain;
    private UnsafeArray<Handle<Texture>> _backBuffers;

    private readonly object? _compositionSurface;

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

    public D3D12SwapChain(D3D12ResourceDatabase resourceDatabase, D3D12DescriptorAllocator descriptorAllocator, D3D12RenderDevice device, SwapChainDesc desc)
    {
        Debug.Assert(desc.BufferCount >= 2);

        _resourceDatabase = resourceDatabase;
        _descriptorAllocator = descriptorAllocator;
        _renderDevice = device;

        _backBuffers = new UnsafeArray<Handle<Texture>>((int)desc.BufferCount, Allocator.Persistent);

        Width = desc.Width;
        Height = desc.Height;
        BufferCount = desc.BufferCount;

        CreateSwapChain(desc);
        CreateBackBuffers();

        _compositionSurface = desc.Target.CompositionSurface;
    }

    ~D3D12SwapChain()
    {
        Dispose();
    }

    private void CreateSwapChain(SwapChainDesc desc)
    {
        var swapChainDesc = new DXGI_SWAP_CHAIN_DESC1
        {
            Width = desc.Width,
            Height = desc.Height,
            Format = desc.Format.ToDXGIFormat(),
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

        var pFactory = _renderDevice.DXGIFactory.Get();
        var pCommandQueue = _renderDevice.NativeGraphicsQueue.Get();

        switch (desc.Target.Type)
        {
            case SwapChainTargetType.Composition:
                ThrowIfFailed(pFactory->CreateSwapChainForComposition((IUnknown*)pCommandQueue, &swapChainDesc, null, &pTempSwapChain));

                // Set the composition surface
                if (desc.Target.CompositionSurface != null)
                {
                    using var swapChainPanelNative = ISwapChainPanelNative.FromSwapChainPanel(desc.Target.CompositionSurface);
                    swapChainPanelNative.SetSwapChain((IntPtr)pTempSwapChain);
                }
                break;

            case SwapChainTargetType.WindowHandle:
                var swapChainFullscreenDesc = new DXGI_SWAP_CHAIN_FULLSCREEN_DESC
                {
                    Windowed = true,
                };

                pFactory->CreateSwapChainForHwnd(
                    (IUnknown*)pCommandQueue,
                    new HWND(desc.Target.WindowHandle.ToPointer()),
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

        _swapChain.Attach(pSwapChain);
    }

    private void CreateBackBuffers()
    {
        for (uint i = 0; i < BufferCount; i++)
        {
            ID3D12Resource* pBackBuffer = default;
            ThrowIfFailed(_swapChain.Get()->GetBuffer(i, __uuidof(pBackBuffer), (void**)&pBackBuffer));
            pBackBuffer->SetName($"SwapChain_BackBuffer_{i}");

            var rtv = _descriptorAllocator.AllocateRTV();
            _renderDevice.NativeDevice.Get()->CreateRenderTargetView(pBackBuffer, null, _descriptorAllocator.GetCpuHandle(rtv));

            var view = ResourceViewGroup.Invalid with
            {
                rtv = rtv
            };

            var handle = _resourceDatabase.ImportExternalResource(pBackBuffer, ResourceState.Present, view);
            _backBuffers[i] = handle.AsTexture();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle<Texture> GetCurrentBackBuffer()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _backBuffers[_swapChain.Get()->GetCurrentBackBufferIndex()];
    }

    public void Present(bool vsync = true)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var presentFlags = 0u;
        var syncInterval = vsync ? 1u : 0u;

        ThrowIfFailed(_swapChain.Get()->Present(syncInterval, presentFlags));
    }

    public void Resize(uint width, uint height)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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

        if (_compositionSurface != null)
        {
            using var panelNative = ISwapChainPanelNative.FromSwapChainPanel(_compositionSurface);
            panelNative.SetSwapChain(IntPtr.Zero);
        }

        for (var i = 0; i < _backBuffers.Count; i++)
        {
            _resourceDatabase.ReleaseResource(_backBuffers[i].AsResource());
        }

        _backBuffers.Dispose();
        _swapChain.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
