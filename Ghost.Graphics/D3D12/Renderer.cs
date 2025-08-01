using Ghost.Graphics.Contracts;
using Ghost.Graphics.D3D12.Utilities;
using Ghost.Graphics.Data;
using System.Collections.Immutable;
using Win32;
using Win32.Graphics.Direct3D12;
using Win32.Graphics.Dxgi;
using Win32.Graphics.Dxgi.Common;
using Win32.Numerics;

namespace Ghost.Graphics.D3D12;

// TODO: We should split the renderer and swap chain into different classes to allow for more flexibility in rendering pipelines.
// Each renderer can have a render target (swap chain or texture).
// When render target is null, skip the render pass execution.
internal unsafe class Renderer
{
    private struct FrameResource : IDisposable
    {
        public ComPtr<ID3D12CommandAllocator> commandAllocator;
        public ComPtr<ID3D12GraphicsCommandList10> commandList;
        public ComPtr<ID3D12Resource> backBuffer;

        public CommandList cmd;
        public RenderTargetDescriptor rtvDescriptor;
        public ulong fenceValue;

        public FrameResource(Renderer renderer, uint index)
        {
            renderer._graphicsDevice.NativeDevice.Ptr->CreateCommandAllocator(CommandListType.Direct, __uuidof<ID3D12CommandAllocator>(), commandAllocator.GetVoidAddressOf());
            renderer._graphicsDevice.NativeDevice.Ptr->CreateCommandList(0u, CommandListType.Direct, commandAllocator.Get(), null, __uuidof<ID3D12GraphicsCommandList10>(), commandList.GetVoidAddressOf());

            cmd = new(commandList.Get());
            rtvDescriptor = renderer.CreateBackBufferResource(index, backBuffer.GetAddressOf());
        }

        public readonly void ResetCommandBuffer()
        {
            commandAllocator.Get()->Reset();
            commandList.Get()->Reset(commandAllocator.Get(), null);
        }

        public readonly void ExecuteCommandBuffer(ID3D12CommandQueue* queue)
        {
            commandList.Get()->Close();
            var commandListPtr = (ID3D12CommandList*)commandList.Get();
            queue->ExecuteCommandLists(1, &commandListPtr);
        }

        public void IncrementFenceValue()
        {
            fenceValue++;
        }

        public void Dispose()
        {
            commandAllocator.Dispose();
            commandList.Dispose();
            backBuffer.Dispose();
            GraphicsPipeline.DescriptorAllocator.ReleaseRTV(rtvDescriptor);
        }
    }

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SwapChainPresenter _swapChainPresenter;

    private ComPtr<IDXGISwapChain4> _swapChain = default;
    private ComPtr<ID3D12Fence1> _fence = default;
    private uint _backBufferIndex;

    private readonly FrameResource[] _frameResources;
    private readonly AutoResetEvent _fenceEvent;

    private ImmutableArray<IRenderPass> _renderPasses;

    private readonly Lock _lock = new();
    private uint _viewPortWidth;
    private uint _viewPortHeight;
    private uint _pendingWidth;
    private uint _pendingHeight;
    private bool _resizeRequested;

    private bool _disposed;

    public ReadOnlySpan<IRenderPass> RenderPasses => _renderPasses.AsSpan();

    public Renderer(GraphicsDevice graphicsDevice, in SwapChainPresenter swapChainSurface)
    {
        _graphicsDevice = graphicsDevice;
        _swapChainPresenter = swapChainSurface;
        _viewPortWidth = swapChainSurface.Width;
        _viewPortHeight = swapChainSurface.Height;

        _fenceEvent = new(false);
        _renderPasses = [];

        InitializeSwapChain();
        InitializeFrameResource(out _frameResources);
    }

    private void InitializeSwapChain()
    {
        var swapChainDesc = new SwapChainDescription1
        {
            Width = _swapChainPresenter.Width,
            Height = _swapChainPresenter.Height,
            Format = D3D12PipelineResource.SWAP_CHAIN_BACK_BUFFER_FORMAT,
            SampleDesc = new SampleDescription(1, 0),
            BufferUsage = Usage.BackBuffer | Usage.RenderTargetOutput,
            BufferCount = GraphicsPipeline._FRAME_COUNT,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.AllowTearing,
            Stereo = false,
        };

        using ComPtr<IDXGISwapChain1> tempSwapChain = default;
        switch (_swapChainPresenter.Type)
        {
            case SwapChainPresenter.TargetType.Composition:
                _graphicsDevice.DXGIFactory.Ptr->CreateSwapChainForComposition((IUnknown*)_graphicsDevice.CommandQueue.Ptr, &swapChainDesc, null, tempSwapChain.GetAddressOf());
                break;
            case SwapChainPresenter.TargetType.Hwnd:
                var swapChainFullscreenDesc = new SwapChainFullscreenDescription
                {
                    Windowed = true,
                };

                _graphicsDevice.DXGIFactory.Ptr->CreateSwapChainForHwnd(
                    (IUnknown*)_graphicsDevice.CommandQueue.Ptr,
                    _swapChainPresenter.Hwnd,
                    &swapChainDesc,
                    &swapChainFullscreenDesc,
                    null,
                    tempSwapChain.GetAddressOf());
                break;
            default:
                throw new ArgumentException("Unsupported swap chain surface type.");
        }

        if (tempSwapChain.Get()->QueryInterface(__uuidof<IDXGISwapChain4>(), _swapChain.GetVoidAddressOf()).Failure)
        {
            throw new InvalidOperationException("Failed to create IDXGISwapChain4 interface.");
        }

        if (_swapChainPresenter.Type == SwapChainPresenter.TargetType.Composition)
        {
            _swapChainPresenter.SwapChainPanelNative.SetSwapChain((IntPtr)_swapChain.Get());
        }
        _backBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
    }

    private void InitializeFrameResource(out FrameResource[] frameResources)
    {
        frameResources = new FrameResource[GraphicsPipeline._FRAME_COUNT];
        for (var i = 0u; i < GraphicsPipeline._FRAME_COUNT; i++)
        {
            frameResources[i] = new FrameResource(this, i);
        }

        for (var i = 1u; i < GraphicsPipeline._FRAME_COUNT; i++)
        {
            ref var frameResource = ref frameResources[i];
            frameResource.commandList.Get()->Close();
        }

        _graphicsDevice.NativeDevice.Ptr->CreateFence(0, FenceFlags.None, __uuidof<ID3D12Fence1>(), _fence.GetVoidAddressOf());
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

    private RenderTargetDescriptor CreateBackBufferResource(uint i, ID3D12Resource** backBuffer)
    {
        _swapChain.Get()->GetBuffer(i, __uuidof<ID3D12Resource>(), (void**)backBuffer);
        (*backBuffer)->SetName($"BackBuffer_{i}");
        var rtvDescriptor = GraphicsPipeline.DescriptorAllocator.AllocateRTV();
        var rtvHandle = rtvDescriptor.CpuHandle;
        _graphicsDevice.NativeDevice.Ptr->CreateRenderTargetView(*backBuffer, null, rtvHandle);
        return rtvDescriptor;
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
            ref var frameResource = ref _frameResources[i];
            if (frameResource.backBuffer.Get() is not null)
            {
                var c = frameResource.backBuffer.Reset();
                GraphicsPipeline.DescriptorAllocator.ReleaseRTV(frameResource.rtvDescriptor);
            }

            frameResource.fenceValue = _frameResources[_backBufferIndex].fenceValue;
        }

        if (_swapChain.Get()->ResizeBuffers(GraphicsPipeline._FRAME_COUNT, newWidth, newHeight, Format.B8G8R8A8Unorm, SwapChainFlags.AllowTearing).Failure)
        {
            throw new InvalidOperationException("Failed to resize swap chain buffers.");
        }

        for (var i = 0u; i < GraphicsPipeline._FRAME_COUNT; i++)
        {
            var index = CreateBackBufferResource(i, _frameResources[i].backBuffer.GetAddressOf());
            _frameResources[i].rtvDescriptor = index;
        }

        _backBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();
        _viewPortWidth = newWidth;
        _viewPortHeight = newHeight;
    }

    public void Initialize()
    {
        ref var frameResource = ref _frameResources[_backBufferIndex];

        foreach (var pass in _renderPasses)
        {
            pass.Initialize(frameResource.cmd);
        }

        frameResource.ExecuteCommandBuffer(_graphicsDevice.CommandQueue);
        WaitIdle();
    }

    public void Render()
    {
        _backBufferIndex = _swapChain.Get()->GetCurrentBackBufferIndex();

        ref var frameResource = ref _frameResources[_backBufferIndex];
        var cpuHandle = frameResource.rtvDescriptor.CpuHandle;

        frameResource.ResetCommandBuffer();
        frameResource.commandList.Get()->ResourceBarrierTransition(frameResource.backBuffer.Get(), ResourceStates.Present, ResourceStates.RenderTarget);

        var clearColor = stackalloc float[4] { 1.0f, 0.0f, 1.0f, 1.0f };
        frameResource.commandList.Get()->ClearRenderTargetView(cpuHandle, clearColor, 0, null);

        var viewPort = new Viewport(_viewPortWidth, _viewPortHeight);
        var rect = new Rect(0, 0, (int)_viewPortWidth, (int)_viewPortHeight);
        frameResource.commandList.Get()->RSSetViewports(1, &viewPort);
        frameResource.commandList.Get()->RSSetScissorRects(1, &rect);

        frameResource.commandList.Get()->OMSetRenderTargets(1, &cpuHandle, false, null);

        foreach (var pass in _renderPasses)
        {
            pass.Execute(frameResource.cmd);
        }

        frameResource.commandList.Get()->ResourceBarrierTransition(frameResource.backBuffer.Get(), ResourceStates.RenderTarget, ResourceStates.Present);

        frameResource.ExecuteCommandBuffer(_graphicsDevice.CommandQueue.Ptr);
        if (_swapChain.Get()->Present(1, PresentFlags.None).Failure)
        {
            throw new InvalidOperationException("Failed to present swap chain.");
        }

        WaitNextFrame();
    }

    public void WaitNextFrame()
    {
        ref var resource = ref _frameResources[_backBufferIndex];
        if (_graphicsDevice.CommandQueue.Ptr->Signal((ID3D12Fence*)_fence.Get(), resource.fenceValue).Failure)
        {
            return;
        }

        var handle = new Handle((void*)_fenceEvent.SafeWaitHandle.DangerousGetHandle());
        if (_fence.Get()->GetCompletedValue() < resource.fenceValue
            && _fence.Get()->SetEventOnCompletion(resource.fenceValue, handle).Success)
        {
            _fenceEvent.WaitOne();
        }

        resource.IncrementFenceValue();
    }

    public void WaitIdle()
    {
        ref var resource = ref _frameResources[_backBufferIndex];
        _graphicsDevice.CommandQueue.Ptr->Signal((ID3D12Fence*)_fence.Get(), resource.fenceValue);

        var handle = new Handle((void*)_fenceEvent.SafeWaitHandle.DangerousGetHandle());
        if (_fence.Get()->SetEventOnCompletion(resource.fenceValue, handle).Success)
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

        _swapChainPresenter.SwapChainPanelNative.SetSwapChain(IntPtr.Zero);

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

        _backBufferIndex = 0;

        _disposed = true;
    }
}