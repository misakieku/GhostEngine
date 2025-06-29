using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;
using Ghost.Graphics.DX12.Utilities;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Ghost.Graphics.DX12;

internal class DX12RenderView : IRenderView
{
    private const int _RENDER_TARGET_VIEW_HEAP_SIZE = 1024;
    private const int _DEPTH_STENCIL_VIEW_HEAP_SIZE = 256;

    private readonly DX12GraphicsDevice _graphicsDevice;
    private readonly SwapChainPresenter _swapChainPresenter;

    private readonly IDXGISwapChain4 _swapChain;
    private readonly ID3D12Resource[] _renderTargets;
    private readonly uint[] _renderTargetDescriptorIndexes;
    private uint _backBufferIndex;

    private readonly ID3D12CommandAllocator[] _commandAllocators;
    private readonly ID3D12GraphicsCommandList10 _commandList;

    private readonly ID3D12Fence1 _fence;
    private readonly AutoResetEvent _fenceEvent;
    private readonly ulong[] _fenceValues;

    private readonly D3D12DescriptorAllocator _rtvHeap;

    private readonly ICommandBuffer _commandBuffer;

    private readonly Lock _lock = new();
    private uint _pendingWidth;
    private uint _pendingHeight;
    private bool _resizeRequested;

    private bool _disposed;

    public DX12RenderView(DX12GraphicsDevice graphicsDevice, in SwapChainPresenter swapChainSurface)
    {
        _graphicsDevice = graphicsDevice;
        _swapChainPresenter = swapChainSurface;

        _rtvHeap = new(_graphicsDevice.Device, DescriptorHeapType.RenderTargetView, _RENDER_TARGET_VIEW_HEAP_SIZE);

        _fenceEvent = new AutoResetEvent(false);
        _renderTargets = new ID3D12Resource[GraphicsPipeline.FRAME_COUNT];
        _fenceValues = new ulong[GraphicsPipeline.FRAME_COUNT];
        _renderTargetDescriptorIndexes = new uint[GraphicsPipeline.FRAME_COUNT];

        InitializeSwapChain(out _swapChain);
        InitializeCommandObjects(out _commandAllocators, out _commandList, out _fence);
        CreateRenderTargets();

        _commandBuffer = new DX12CommandBuffer(_commandList);
    }

    private void InitializeSwapChain(out IDXGISwapChain4 swapChain)
    {
        var swapChainDesc = new SwapChainDescription1
        {
            Width = _swapChainPresenter.Width,
            Height = _swapChainPresenter.Height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.Backbuffer | Usage.RenderTargetOutput,
            BufferCount = GraphicsPipeline.FRAME_COUNT,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.AllowTearing
        };

        switch (_swapChainPresenter.Type)
        {
            case SwapChainPresenter.TargetType.Composition:
                var swapChain1 = _graphicsDevice.DXGIFactory.CreateSwapChainForComposition(_graphicsDevice.CommandQueue, swapChainDesc);
                swapChain = swapChain1.QueryInterface<IDXGISwapChain4>();
                swapChain1.Dispose();

                _backBufferIndex = swapChain.CurrentBackBufferIndex;
                _swapChainPresenter.SwapChainPanelNative!.SetSwapChain(swapChain);
                break;
            case SwapChainPresenter.TargetType.Hwnd:
                var swapChainFullscreenDesc = new SwapChainFullscreenDescription
                {
                    Windowed = true,
                };

                var swapChain2 = _graphicsDevice.DXGIFactory.CreateSwapChainForHwnd(
                    _graphicsDevice.CommandQueue,
                    _swapChainPresenter.Hwnd,
                    swapChainDesc,
                    swapChainFullscreenDesc,
                    null);
                swapChain = swapChain2.QueryInterface<IDXGISwapChain4>();
                swapChain2.Dispose();
                break;
            default:
                throw new ArgumentException("Unsupported swap chain surface type.");
        }
    }

    private void InitializeCommandObjects(out ID3D12CommandAllocator[] commandAllocator, out ID3D12GraphicsCommandList10 commandList, out ID3D12Fence1 fence)
    {
        commandAllocator = new ID3D12CommandAllocator[GraphicsPipeline.FRAME_COUNT];
        for (var i = 0; i < GraphicsPipeline.FRAME_COUNT; i++)
        {
            commandAllocator[i] = _graphicsDevice.Device.CreateCommandAllocator(CommandListType.Direct);
        }

        commandList = _graphicsDevice.Device.CreateCommandList<ID3D12GraphicsCommandList10>(CommandListType.Direct, commandAllocator[0], null!);
        commandList.Close();
        fence = _graphicsDevice.Device.CreateFence<ID3D12Fence1>(_fenceValues[_backBufferIndex], FenceFlags.None);

        _fenceValues[_backBufferIndex]++;
    }

    private void CreateRenderTargets()
    {
        for (var i = 0u; i < GraphicsPipeline.FRAME_COUNT; i++)
        {
            _renderTargets[i] = _swapChain.GetBuffer<ID3D12Resource>(i);
            _renderTargets[i].Name = $"RenderTarget_{i}";
            _renderTargetDescriptorIndexes[i] = _rtvHeap.AllocateDescriptor();

            var rtvHandle = _rtvHeap.GetCpuHandle(_renderTargetDescriptorIndexes[i]);
            _graphicsDevice.Device.CreateRenderTargetView(_renderTargets[i], null, rtvHandle);
        }
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

        for (var i = 0; i < GraphicsPipeline.FRAME_COUNT; i++)
        {
            if (_renderTargets[i] is not null)
            {
                _renderTargets[i].Dispose();
                _rtvHeap.ReleaseDescriptor(_renderTargetDescriptorIndexes[i]);
            }

            _fenceValues[i] = _fenceValues[_backBufferIndex];
        }

        _swapChain.ResizeBuffers(GraphicsPipeline.FRAME_COUNT, newWidth, newHeight, Format.B8G8R8A8_UNorm, SwapChainFlags.AllowTearing).CheckError();

        CreateRenderTargets();
        _backBufferIndex = _swapChain.CurrentBackBufferIndex;
    }

    public ICommandBuffer BeginRender()
    {
        _backBufferIndex = _swapChain.CurrentBackBufferIndex;

        var commandAllocator = _commandAllocators[_backBufferIndex];
        commandAllocator.Reset();
        _commandList.Reset(commandAllocator, null);

        _commandList.ResourceBarrierTransition(_renderTargets[_backBufferIndex], ResourceStates.Present, ResourceStates.RenderTarget);

        return _commandBuffer;
    }

    public void Render()
    {
    }

    public void EndRender()
    {
        _commandList.ResourceBarrierTransition(_renderTargets[_backBufferIndex], ResourceStates.RenderTarget, ResourceStates.Present);
        _commandList.Close();

        _graphicsDevice.CommandQueue.ExecuteCommandLists(new[] { _commandList });

        _swapChain.Present(1, PresentFlags.None).CheckError();

        WaitNextFrame();
    }

    public void WaitNextFrame()
    {
        var fenceValue = _fenceValues[_backBufferIndex];

        if (_graphicsDevice.CommandQueue.Signal(_fence, fenceValue).Failure)
        {
            return;
        }

        if (_fence.CompletedValue < _fenceValues[_backBufferIndex]
            && _fence.SetEventOnCompletion(_fenceValues[_backBufferIndex], _fenceEvent.SafeWaitHandle.DangerousGetHandle()).Success)
        {
            _fenceEvent.WaitOne();
        }

        _fenceValues[_backBufferIndex]++;
    }

    public void WaitIdle()
    {
        var fenceValue = _fenceValues[_backBufferIndex];
        if (_graphicsDevice.CommandQueue.Signal(_fence, fenceValue).Success
            && _fence.SetEventOnCompletion(fenceValue, _fenceEvent.SafeWaitHandle.DangerousGetHandle()).Success)
        {
            _fenceEvent.WaitOne();
            _fenceValues[_backBufferIndex]++;
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

        foreach (var commandAllocator in _commandAllocators)
        {
            commandAllocator.Dispose();
        }
        _commandAllocators.AsSpan().Clear();

        foreach (var renderTarget in _renderTargets)
        {
            renderTarget.Dispose();
        }
        _renderTargets.AsSpan().Clear();

        _swapChain.Dispose();
        _commandList.Dispose();

        _fence.Dispose();
        _fenceEvent.Dispose();

        _rtvHeap.Dispose();

        _backBufferIndex = 0;
        _fenceValues.AsSpan().Clear();

        _disposed = true;
    }
}