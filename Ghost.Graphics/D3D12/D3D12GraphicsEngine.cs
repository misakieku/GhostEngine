using Ghost.Graphics.RHI;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12GraphicsEngine : IGraphicsEngine
{
#if DEBUG
    private readonly D3D12DebugLayer _debugLayer;
#endif

    private readonly D3D12RenderDevice _device;
    private readonly D3D12PipelineStateController _stateController;
    private readonly D3D12ResourceAllocator _resourceAllocator;

    private readonly D3D12PipelineStateController _pipelineState;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    public IRenderDevice Device => _device;
    public IPipelineStateController PipelineStateController => _stateController;
    public IResourceAllocator ResourceAllocator => _resourceAllocator;

    public D3D12GraphicsEngine(RenderSystem renderSystem)
    {
#if DEBUG
        _debugLayer = new();
#endif

        _device = new();
        _stateController = new(_device);
        _resourceAllocator = new(_device, renderSystem);

        _pipelineState = new(_device);
        _descriptorAllocator = new(_device);
    }

    public IRenderer CreateRenderer()
    {
        return new D3D12Renderer(this, _resourceAllocator);
    }

    public ICommandBuffer CreateCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        return new D3D12CommandBuffer(_device, _stateController, _descriptorAllocator, type);
    }

    public ISwapChain CreateSwapChain(SwapChainDesc desc)
    {
        return new D3D12SwapChain(_device.DXGIFactory, ((D3D12CommandQueue)_device.ComputeQueue).NativeQueue, desc);
    }

    public void Dispose()
    {
        _descriptorAllocator.Dispose();
        _resourceAllocator.Dispose();
        _device.Dispose();

#if DEBUG
        _debugLayer.Dispose();
#endif
    }
}