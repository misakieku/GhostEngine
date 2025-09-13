using Ghost.Graphics.RHI;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12GraphicsEngine : IGraphicsEngine
{
#if DEBUG
    private readonly D3D12DebugLayer _debugLayer;
#endif

    private readonly D3D12RenderDevice _device;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12ResourceAllocator _resourceAllocator;

    private readonly D3D12PipelineStateController _stateController;

    public IRenderDevice Device => _device;
    public IResourceAllocator ResourceAllocator => _resourceAllocator;

    public IPipelineStateController PipelineStateController => _stateController;

    public D3D12GraphicsEngine(RenderSystem renderSystem)
    {
#if DEBUG
        _debugLayer = new();
#endif

        _device = new();
        _descriptorAllocator = new(_device);
        _resourceAllocator = new(renderSystem, _device, _descriptorAllocator);

        _stateController = new(_device);
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
        _stateController.Dispose();
        _descriptorAllocator.Dispose();
        _resourceAllocator.Dispose();
        _device.Dispose();

#if DEBUG
        _debugLayer.Dispose();
#endif
    }
}