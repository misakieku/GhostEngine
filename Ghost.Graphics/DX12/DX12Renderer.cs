using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;
using Ghost.Graphics.DX12.Utilities;
using System.Collections.Immutable;
using Win32;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;
using Win32.Graphics.Dxgi.Common;
using static Win32.Apis;

namespace Ghost.Graphics.DX12;

internal unsafe class DX12Renderer : IRenderer
{
    private class FrameResource : IDisposable
    {
        public readonly ID3D12CommandAllocator commandAllocator;
        public readonly ID3D12GraphicsCommandList10 commandList;
        public readonly ICommandBuffer commandBuffer;
        public ID3D12Resource backBuffer;
        public uint backBufferDescriptorIndexes;
        public ulong fenceValue;

        public FrameResource(DX12Renderer renderer, uint index)
        {
            commandAllocator = renderer._graphicsDevice.NativeDevice.CreateCommandAllocator(CommandListType.Direct);
            commandList = renderer._graphicsDevice.NativeDevice.CreateCommandList<ID3D12GraphicsCommandList10>(CommandListType.Direct, commandAllocator);
            commandBuffer = new DX12CommandBuffer(commandList);

            renderer.CreateBackBufferResource(index, out backBuffer, out backBufferDescriptorIndexes);

            commandList.Close();
        }

        public void ResetCommandList()
        {
            commandAllocator.Reset();
            commandList.Reset(commandAllocator, null);
        }

        public void IncrementFenceValue()
        {
            fenceValue++;
        }

        public void Dispose()
        {
            commandAllocator.Dispose();
            commandList.Dispose();
            backBuffer?.Dispose();
        }
    }

    private const int _RENDER_TARGET_VIEW_HEAP_SIZE = 1024;
    private const int _DEPTH_STENCIL_VIEW_HEAP_SIZE = 256;

    private readonly DX12GraphicsDevice _graphicsDevice;
    private readonly SwapChainPresenter _swapChainPresenter;

    private readonly ComPtr<IDXGISwapChain4> _swapChain;
    private readonly FrameResource[] _frameResources;
    private uint _backBufferIndex;

    private readonly ComPtr<ID3D12Fence1> _fence;
    private readonly AutoResetEvent _fenceEvent;

    private readonly D3D12DescriptorAllocator _rtvHeap;

    private ImmutableArray<IRenderPass> _renderPasses;

    private readonly Lock _lock = new();
    private uint _pendingWidth;
    private uint _pendingHeight;
    private bool _resizeRequested;

    private bool _disposed;

    public ReadOnlySpan<IRenderPass> RenderPasses => _renderPasses.AsSpan();

    public DX12Renderer(DX12GraphicsDevice graphicsDevice, in SwapChainPresenter swapChainSurface)
    {
        _graphicsDevice = graphicsDevice;
        _swapChainPresenter = swapChainSurface;

        _rtvHeap = new D3D12DescriptorAllocator(_graphicsDevice.NativeDevice, DescriptorHeapType.Rtv, _RENDER_TARGET_VIEW_HEAP_SIZE);
        _fenceEvent = new(false);
        _renderPasses = ImmutableArray<IRenderPass>.Empty;

        InitializeSwapChain();
        InitializeCommandObjects(out _frameResources, out _fence);
    }

    private void InitializeSwapChain()
    {
        var swapChainDesc = new SwapChainDescription1
        {
            Width = _swapChainPresenter.Width,
            Height = _swapChainPresenter.Height,
            Format = Format.B8G8R8A8Unorm,
            Stereo = false,
            SampleDesc = new(1, 0),
            BufferUsage = Usage.BackBuffer | Usage.RenderTargetOutput,
            BufferCount = GraphicsPipeline._FRAME_COUNT,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.AllowTearing
        };

        using ComPtr<IDXGISwapChain1> tempSwapChain = default;
        switch (_swapChainPresenter.Type)
        {
            case SwapChainPresenter.TargetType.Composition:
            {
                _graphicsDevice.DXGIFactory.Ptr->
                    CreateSwapChainForComposition(
                    (IUnknown*)_graphicsDevice.CommandQueue.Ptr,
                    &swapChainDesc, null,
                    (IDXGISwapChain1**)&tempSwapChain);

                fixed (void* swapChainPtr = &_swapChain)
                {
                    tempSwapChain.Get()->QueryInterface(__uuidof<IDXGISwapChain4>(), &swapChainPtr);
                }

                _swapChainPresenter.SwapChainPanelNative.SetSwapChain((IntPtr)_swapChain.Get());
                break;
            }
            case SwapChainPresenter.TargetType.Hwnd:
            {
                var swapChainFullscreenDesc = new SwapChainFullscreenDescription
                {
                    Windowed = true,
                };

                _graphicsDevice.DXGIFactory.Ptr->
                    CreateSwapChainForHwnd(
                    (IUnknown*)_graphicsDevice.CommandQueue.Ptr,
                    _swapChainPresenter.Hwnd,
                    &swapChainDesc,
                    &swapChainFullscreenDesc,
                    null,
                    (IDXGISwapChain1**)&tempSwapChain);

                fixed (void* swapChainPtr = &_swapChain)
                {
                    tempSwapChain.Get()->QueryInterface(__uuidof<IDXGISwapChain4>(), &swapChainPtr);
                }

                break;
            }
            default:
                throw new ArgumentException("Unsupported swap chain surface type.");
        }

        _backBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
    }

    private void InitializeCommandObjects(out FrameResource[] frameResources)
    {
        frameResources = new FrameResource[GraphicsPipeline._FRAME_COUNT];
        for (var i = 0u; i < GraphicsPipeline._FRAME_COUNT; i++)
        {
            frameResources[i] = new FrameResource(this, i);
        }

        fixed (void* fencePtr = &_fence)
        {
            _graphicsDevice.NativeDevice.Ptr->CreateFence(0, FenceFlags.None, __uuidof<ID3D12Fence1>(), &fencePtr);
        }

        frameResources[0].IncrementFenceValue();
    }

    public void RequestResize(uint width, uint height)
    {
        lock (_lock)
        {
            if (_pendingWidth == width && _pendingHeight == height)
            {
                return;
            }

            _resizeRequested = true;
            _pendingWidth = width;
            _pendingHeight = height;
        }
    }

    private void CreateBackBufferResource(uint i, out ID3D12Resource backBuffer, out uint index)
    {
        backBuffer = _swapChain.GetBuffer<ID3D12Resource>(i);
        backBuffer.Name = $"BackBuffer_{i}";
        index = _rtvHeap.AllocateDescriptor();
        var rtvHandle = _rtvHeap.GetCpuHandle(index);
        _graphicsDevice.NativeDevice.CreateRenderTargetView(backBuffer, null, rtvHandle);
    }

    public void ExecutePendingResize()
    {
        if (!_resizeRequested)
        {
            return;
        }

        uint newWidth;
        uint newHeight;

        lock (_lock)
        {
            newWidth = _pendingWidth;
            newHeight = _pendingHeight;
            _resizeRequested = false;
        }

        WaitIdle();

        for (var i = 0; i < GraphicsPipeline._FRAME_COUNT; i++)
        {
            var backBuffer = _frameResources[i].backBuffer;
            if (backBuffer is not null)
            {
                backBuffer.Dispose();
                _rtvHeap.ReleaseDescriptor(_frameResources[i].backBufferDescriptorIndexes);
            }

            _frameResources[i].fenceValue = _frameResources[_backBufferIndex].fenceValue;
        }

        _swapChain.ResizeBuffers(GraphicsPipeline._FRAME_COUNT, newWidth, newHeight, Format.B8G8R8A8_UNorm, SwapChainFlags.AllowTearing).CheckError();

        for (var i = 0u; i < GraphicsPipeline._FRAME_COUNT; i++)
        {
            CreateBackBufferResource(i, out var backBuffer, out var index);

            _frameResources[i].backBuffer = backBuffer;
            _frameResources[i].backBufferDescriptorIndexes = index;
        }
    }

    public void Render()
    {
        _backBufferIndex = _swapChain.CurrentBackBufferIndex;

        var frameResource = _frameResources[_backBufferIndex];
        frameResource.ResetCommandList();
        frameResource.commandList.ResourceBarrierTransition(_frameResources[_backBufferIndex].backBuffer!, ResourceStates.Present, ResourceStates.RenderTarget);

        foreach (var pass in _renderPasses)
        {
            pass.Execute(frameResource.commandBuffer);
        }

        frameResource.commandList.ResourceBarrierTransition(_frameResources[_backBufferIndex].backBuffer!, ResourceStates.RenderTarget, ResourceStates.Present);
        frameResource.commandList.Close();

        _graphicsDevice.CommandQueue.ExecuteCommandList(frameResource.commandList);

        _swapChain.Present(1, PresentFlags.None).CheckError();

        WaitNextFrame();
    }

    public void WaitNextFrame()
    {
        var resource = _frameResources[_backBufferIndex];
        if (_graphicsDevice.CommandQueue.Signal(_fence, resource.fenceValue).Failure)
        {
            return;
        }

        if (_fence.CompletedValue < resource.fenceValue
            && _fence.SetEventOnCompletion(resource.fenceValue, _fenceEvent.SafeWaitHandle.DangerousGetHandle()).Success)
        {
            _fenceEvent.WaitOne();
        }

        resource.IncrementFenceValue();
    }

    public void WaitIdle()
    {
        var resource = _frameResources[_backBufferIndex];
        if (_graphicsDevice.CommandQueue.Signal(_fence, resource.fenceValue).Success
            && _fence.SetEventOnCompletion(resource.fenceValue, _fenceEvent.SafeWaitHandle.DangerousGetHandle()).Success)
        {
            _fenceEvent.WaitOne();
            resource.IncrementFenceValue();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        WaitIdle();

        _swapChainPresenter.SwapChainPanelNative?.SetSwapChain(null);

        foreach (var pass in _renderPasses)
        {
            pass.Dispose();
        }

        foreach (var frameResource in _frameResources)
        {
            frameResource.Dispose();
        }

        _swapChain.Dispose();

        _fence.Dispose();
        _fenceEvent.Dispose();

        _rtvHeap.Dispose();

        _backBufferIndex = 0;

        _disposed = true;
    }
}