using Ghost.Graphics.RHI;

namespace Ghost.Graphics.D3D12;

internal unsafe class D3D12GraphicsEngine : IGraphicsEngine
{
#if DEBUG
    private readonly D3D12DebugLayer _debugLayer;
#endif

    private readonly D3D12RenderDevice _device;
    private readonly D3D12PipelineLibrary _pipelineLibrary;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;

    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12ResourceAllocator _resourceAllocator;

    private readonly D3D12CommandBuffer _copyCommandBuffer;

    public IRenderDevice Device => _device;
    public IPipelineLibrary PipelineLibrary => _pipelineLibrary;
    public IResourceDatabase ResourceDatabase => _resourceDatabase;
    public IResourceAllocator ResourceAllocator => _resourceAllocator;

    public D3D12GraphicsEngine(RenderSystem renderSystem)
    {
#if DEBUG
        _debugLayer = new();
#endif
        _device = new();
        _descriptorAllocator = new(_device);

        _resourceDatabase = new(_descriptorAllocator);
        _resourceAllocator = new(renderSystem, _device, _descriptorAllocator, _resourceDatabase);

        _pipelineLibrary = new(_device, _resourceDatabase);
        _copyCommandBuffer = new(
            _device,
            _pipelineLibrary,
            _resourceDatabase,
            _resourceAllocator,
            _descriptorAllocator,
            CommandBufferType.Copy);
    }

    ~D3D12GraphicsEngine()
    {
        Dispose();
    }

    public IRenderer CreateRenderer()
    {
        return new D3D12Renderer(this, _resourceAllocator, _resourceDatabase);
    }

    public ICommandBuffer CreateCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        return new D3D12CommandBuffer(
            _device,
            _pipelineLibrary,
            _resourceDatabase,
            _resourceAllocator,
            _descriptorAllocator,
            type);
    }

    public ISwapChain CreateSwapChain(SwapChainDesc desc)
    {
        return new D3D12SwapChain(_resourceDatabase, _device.DXGIFactory, ((D3D12CommandQueue)_device.ComputeQueue).NativeQueue, desc);
    }

    public void BeginFrame()
    {
        throw new NotImplementedException();
    }

    public void EndFrame()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _copyCommandBuffer.Dispose();
        _pipelineLibrary.Dispose();

        _resourceAllocator.Dispose();
        _resourceDatabase.Dispose();

        _descriptorAllocator.Dispose();
        _device.Dispose();
#if DEBUG
        _debugLayer.Dispose();
#endif

        GC.SuppressFinalize(this);
    }
}
