using System.Collections.Immutable;
using System.Runtime.CompilerServices;

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

    private ImmutableArray<IRenderer> _renderers;

    private bool _disposed;

    public IRenderDevice Device => _device;
    public IPipelineLibrary PipelineLibrary => _pipelineLibrary;
    public IResourceDatabase ResourceDatabase => _resourceDatabase;
    public IResourceAllocator ResourceAllocator => _resourceAllocator;
    public ICommandBuffer CopyCommandBuffer => _copyCommandBuffer;

    public D3D12GraphicsEngine(IRenderSystem renderSystem)
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

        _renderers = ImmutableArray<IRenderer>.Empty;
    }

    ~D3D12GraphicsEngine()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(D3D12GraphicsEngine));
        }
    }

    public IRenderer CreateRenderer()
    {
        ThrowIfDisposed();

        var renderer = new D3D12Renderer(this, _resourceAllocator, _resourceDatabase);
        ImmutableInterlocked.Update(ref _renderers, renderers => renderers.Add(renderer));
        return renderer;
    }

    public void RemoveRenderer(IRenderer renderer)
    {
        ThrowIfDisposed();
        ImmutableInterlocked.Update(ref _renderers, renderers => renderers.Remove(renderer));
    }

    public void ClearRenderers()
    {
        ThrowIfDisposed();
        ImmutableInterlocked.Update(ref _renderers, renderers => renderers.Clear());
    }

    public ICommandBuffer CreateCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        ThrowIfDisposed();

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
        ThrowIfDisposed();
        return new D3D12SwapChain(_resourceDatabase, _device.DXGIFactory, ((D3D12CommandQueue)_device.GraphicsQueue).NativeQueue, desc);
    }

    public void BeginFrame()
    {
        ThrowIfDisposed();

        foreach (var renderer in _renderers)
        {
            renderer.ExecutePendingResize();
        }
    }

    public void RenderFrame()
    {
        ThrowIfDisposed();

        foreach (var renderer in _renderers)
        {
            renderer.Render();
        }
    }

    public void EndFrame()
    {
        ThrowIfDisposed();
        _resourceAllocator.ReleaseTempResources();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _copyCommandBuffer.Dispose();
        _pipelineLibrary.Dispose();

        _resourceAllocator.Dispose();
        _resourceDatabase.Dispose();

        _descriptorAllocator.Dispose();
        _device.Dispose();
#if DEBUG
        _debugLayer.Dispose();
#endif

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
