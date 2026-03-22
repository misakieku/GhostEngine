using Ghost.Core;
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

    public float ScaleX
    {
        get; private set;
    }

    public float ScaleY
    {
        get; private set;
    }

    public D3D12SwapChain(D3D12ResourceDatabase resourceDatabase, D3D12DescriptorAllocator descriptorAllocator, D3D12RenderDevice device, SwapChainDesc desc, uint bufferCount)
    {
        Debug.Assert(bufferCount >= 2);

        _resourceDatabase = resourceDatabase;
        _descriptorAllocator = descriptorAllocator;
        _renderDevice = device;

        _backBuffers = new UnsafeArray<Handle<Texture>>((int)bufferCount, Allocator.Persistent);

        Width = desc.Width;
        Height = desc.Height;

        var pSwapChian = CreateSwapChain(desc, bufferCount);
        _swapChain.Attach(pSwapChian);

        CreateBackBuffers();
        SetScale(desc.ScaleX, desc.ScaleY);

        if (desc.Target.Type == SwapChainTargetType.Composition)
        {
            _compositionSurface = desc.Target.CompositionSurface;
        }
    }

    ~D3D12SwapChain()
    {
        Dispose();
    }

    private IDXGISwapChain4* CreateSwapChain(SwapChainDesc desc, uint bufferCount)
    {
        var swapChainDesc = new DXGI_SWAP_CHAIN_DESC1
        {
            Width = desc.Width,
            Height = desc.Height,
            Format = desc.Format.ToDXGIFormat(),
            SampleDesc = new DXGI_SAMPLE_DESC(1, 0),
            BufferUsage = DXGI_USAGE_BACK_BUFFER | DXGI_USAGE_RENDER_TARGET_OUTPUT,
            BufferCount = bufferCount,
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

                if (desc.Target.CompositionSurface != null)
                {
                    using var compositionSurface = ISwapChainPanelNative.FromSwapChainPanel(desc.Target.CompositionSurface);
                    compositionSurface.SetSwapChain((nint)pTempSwapChain);
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

        return pSwapChain;
    }

    private void CreateBackBuffers()
    {
        for (uint i = 0; i < _backBuffers.Count; i++)
        {
            ID3D12Resource* pBackBuffer = default;
            ThrowIfFailed(_swapChain.Get()->GetBuffer(i, __uuidof(pBackBuffer), (void**)&pBackBuffer));
            pBackBuffer->SetName($"SwapChain_BackBuffer_{i}");

            var rtv = _descriptorAllocator.AllocateRTV();
            var cpuHandle = _descriptorAllocator.GetCpuHandle(rtv);
            _renderDevice.NativeDevice.Get()->CreateRenderTargetView(pBackBuffer, null, cpuHandle);

            var view = ResourceViewGroup.Invalid with
            {
                rtv = rtv
            };

            var barrierData = new ResourceBarrierData
            {
                access = BarrierAccess.NoAccess,
                layout = BarrierLayout.Present,
                sync = BarrierSync.None,
            };

            var handle = _resourceDatabase.ImportExternalResource(pBackBuffer, barrierData, view);
            _backBuffers[i] = handle.AsTexture();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Handle<Texture> GetCurrentBackBuffer()
    {
        Debug.Assert(!_disposed);
        return _backBuffers[_swapChain.Get()->GetCurrentBackBufferIndex()];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<Handle<Texture>> GetBackBuffers()
    {
        Debug.Assert(!_disposed);
        return _backBuffers.AsSpan();
    }

    public void Present(bool vsync = true)
    {
        Debug.Assert(!_disposed);

        var presentFlags = 0u;
        var syncInterval = vsync ? 1u : 0u;

        ThrowIfFailed(_swapChain.Get()->Present(syncInterval, presentFlags));
    }

    public void Resize(uint width, uint height)
    {
        Debug.Assert(!_disposed);

        if (Width == width && Height == height)
        {
            return;
        }

        // Release old back buffers and render targets
        for (var i = 0; i < _backBuffers.Count; i++)
        {
            _resourceDatabase.ReleaseResourceImmediately(_backBuffers[i].AsResource());
        }

        ThrowIfFailed(_swapChain.Get()->ResizeBuffers((uint)_backBuffers.Count, width, height, DXGI_FORMAT_B8G8R8A8_UNORM, (uint)DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING));

        Width = width;
        Height = height;

        CreateBackBuffers();
    }

    public void SetScale(float scaleX, float scaleY)
    {
        Debug.Assert(!_disposed);

        var inverseScaleX = 1.0f / scaleX;
        var inverseScaleY = 1.0f / scaleY;

        var inverseScaleMatrix = new DXGI_MATRIX_3X2_F
        {
            _11 = inverseScaleX, // Scale X
            _22 = inverseScaleY, // Scale Y
            _12 = 0.0f,
            _21 = 0.0f,
            _31 = 0.0f, // Offset X
            _32 = 0.0f // Offset Y
        };

        _swapChain.Get()->SetMatrixTransform(&inverseScaleMatrix);

        ScaleX = scaleX;
        ScaleY = scaleY;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_compositionSurface != null)
        {
            using var compositionSurface = ISwapChainPanelNative.FromSwapChainPanel(_compositionSurface);
            compositionSurface.SetSwapChain(0);
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
