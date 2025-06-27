using Ghost.Graphics.Contracts;
using Ghost.Graphics.Data;
using Ghost.Graphics.DX12.Utilities;
using System.Runtime.CompilerServices;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Ghost.Graphics.DX12;

internal class DX12RenderView : IRenderView
{
    private const int _RENDER_TARGET_VIEW_HEAP_SIZE = 1024;
    private const int _DEPTH_STENCIL_VIEW_HEAP_SIZE = 256;

    private readonly DX12GraphicsDevice _graphicsDevice;

    private readonly IDXGISwapChain4 _swapChain;
    private readonly ID3D12Resource[] _renderTargets;
    private readonly uint[] _renderTargetDescriptorIndexes;
    private uint _backBufferIndex;

    private readonly ID3D12CommandAllocator[] _commandAllocators;
    private readonly ID3D12GraphicsCommandList7 _commandList;

    private readonly ID3D12Fence1 _fence;
    private readonly AutoResetEvent _fenceEvent;
    private readonly ulong[] _fenceValues;

    private readonly D3D12DescriptorAllocator _rtvHeap;

    public DX12RenderView(DX12GraphicsDevice pipelineContext, in SwapChainSurface swapChainSurface)
    {
        _graphicsDevice = pipelineContext;

        _rtvHeap = new(_graphicsDevice.Device, DescriptorHeapType.RenderTargetView, _RENDER_TARGET_VIEW_HEAP_SIZE);

        _fenceEvent = new AutoResetEvent(false);
        _renderTargets = new ID3D12Resource[GraphicsPipeline.FRAME_COUNT];
        _fenceValues = new ulong[GraphicsPipeline.FRAME_COUNT];
        _renderTargetDescriptorIndexes = new uint[GraphicsPipeline.FRAME_COUNT];

        InitializeSwapChain(swapChainSurface, out _swapChain);
        InitializeCommandObjects(out _commandAllocators, out _commandList, out _fence);
        CreateRenderTargets();
    }

    private void InitializeSwapChain(in SwapChainSurface swapChainSurface, out IDXGISwapChain4 swapChain)
    {
        var swapChainDesc = new SwapChainDescription1
        {
            Width = swapChainSurface.Width,
            Height = swapChainSurface.Height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = GraphicsPipeline.FRAME_COUNT,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.AllowTearing
        };

        // NOTE: Not going to need it for now, this is for standalone applications.
        var swapChainFullscreenDesc = new SwapChainFullscreenDescription
        {
            Windowed = true,
        };

        switch (swapChainSurface.Type)
        {
            case SwapChainSurface.TargetType.Composition:
                var swapChain1 = _graphicsDevice.DXGIFactory.CreateSwapChainForComposition(_graphicsDevice.CommandQueue, swapChainDesc);
                swapChain = swapChain1.QueryInterface<IDXGISwapChain4>();

                _backBufferIndex = swapChain.CurrentBackBufferIndex;
                swapChainSurface.SwapChainPanelNative!.SetSwapChain(swapChain);
                break;
            case SwapChainSurface.TargetType.Hwnd:
                var swapChain2 = _graphicsDevice.DXGIFactory.CreateSwapChainForHwnd(
                    _graphicsDevice.CommandQueue,
                    swapChainSurface.Hwnd,
                    swapChainDesc,
                    swapChainFullscreenDesc,
                    null);
                swapChain = swapChain2.QueryInterface<IDXGISwapChain4>();
                break;
            default:
                throw new ArgumentException("Unsupported swap chain surface type.");
        }
    }

    private void InitializeCommandObjects(out ID3D12CommandAllocator[] commandAllocator, out ID3D12GraphicsCommandList7 commandList, out ID3D12Fence1 fence)
    {
        commandAllocator = new ID3D12CommandAllocator[GraphicsPipeline.FRAME_COUNT];
        for (var i = 0; i < GraphicsPipeline.FRAME_COUNT; i++)
        {
            commandAllocator[i] = _graphicsDevice.Device.CreateCommandAllocator(CommandListType.Direct);
        }

        commandList = _graphicsDevice.Device.CreateCommandList<ID3D12GraphicsCommandList7>(CommandListType.Direct, commandAllocator[0], null!);
        fence = _graphicsDevice.Device.CreateFence<ID3D12Fence1>(_fenceValues[_backBufferIndex], FenceFlags.None);

        _commandList.Close();
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

    public void Resize(uint width, uint height)
    {
        WaitIdle();

        for (var i = 0; i < GraphicsPipeline.FRAME_COUNT; i++)
        {
            _renderTargets[i].Dispose();
            _rtvHeap.ReleaseDescriptor(_renderTargetDescriptorIndexes[i]);

            _fenceValues[i] = _fenceValues[_backBufferIndex];
        }

        _swapChain.ResizeBuffers(GraphicsPipeline.FRAME_COUNT, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.AllowTearing).CheckError();

        CreateRenderTargets();
        _backBufferIndex = _swapChain.CurrentBackBufferIndex;
    }

    public void Render()
    {
        var commandAllocator = _commandAllocators[_backBufferIndex];
        commandAllocator.Reset();
        _commandList.Reset(commandAllocator, null);

        _commandList.Close();
        _graphicsDevice.CommandQueue.ExecuteCommandLists([_commandList]);

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

        _backBufferIndex = _swapChain.CurrentBackBufferIndex;
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
        if (_graphicsDevice.CommandQueue.Signal(_fence, fenceValue).Failure
            || _fence.SetEventOnCompletion(fenceValue, _fenceEvent.SafeWaitHandle.DangerousGetHandle()).Failure)
        {
            return;
        }

        _fenceEvent.WaitOne();
        _fenceValues[_backBufferIndex]++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Flush()
    {
        for (var i = 0; i < GraphicsPipeline.FRAME_COUNT; i++)
        {
            WaitIdle();
        }
    }

    public void Dispose()
    {
        Flush();

        foreach (var commandAllocator in _commandAllocators)
        {
            commandAllocator.Dispose();
        }

        foreach (var renderTarget in _renderTargets)
        {
            renderTarget.Dispose();
        }

        _swapChain.Dispose();
        _commandList.Dispose();

        _fence.Dispose();
        _fenceEvent.Dispose();

        _rtvHeap.Dispose();

        _backBufferIndex = 0;
        _fenceValues.AsSpan().Clear();
    }
}