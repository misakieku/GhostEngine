using Ghost.Core;
using Ghost.Graphics.Contracts;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Utilities;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.D3D12;

internal class D3D12GraphicsEngine : IGraphicsEngine
{
    private readonly IRenderSystem _renderSystem;

#if DEBUG
    private readonly D3D12DebugLayer _debugLayer;
#endif

    private readonly D3D12RenderDevice _device;
    private readonly DxcShaderCompiler _shaderCompiler;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12PipelineLibrary _pipelineLibrary;
    private readonly D3D12ResourceAllocator _resourceAllocator;
    private readonly D3D12CommandBuffer _copyCommandBuffer;

    private ImmutableArray<IRenderer> _renderers;

    private bool _disposed;

    public IRenderDevice Device => _device;
    public IShaderCompiler ShaderCompiler => _shaderCompiler;
    public IPipelineLibrary PipelineLibrary => _pipelineLibrary;
    public IResourceDatabase ResourceDatabase => _resourceDatabase;
    public IResourceAllocator ResourceAllocator => _resourceAllocator;
    public ICommandBuffer CopyCommandBuffer => _copyCommandBuffer;

    public D3D12GraphicsEngine(IRenderSystem renderSystem)
    {
        _renderSystem = renderSystem;

#if DEBUG
        _debugLayer = new D3D12DebugLayer();
#endif
        _device = new D3D12RenderDevice();
        _shaderCompiler = new DxcShaderCompiler();
        _descriptorAllocator = new D3D12DescriptorAllocator(_device);

        _resourceDatabase = new D3D12ResourceDatabase(_descriptorAllocator);
        _pipelineLibrary = new D3D12PipelineLibrary(_device, _resourceDatabase);
        _resourceAllocator = new D3D12ResourceAllocator(renderSystem, _device, _descriptorAllocator, _resourceDatabase, _pipelineLibrary);

        _copyCommandBuffer = new D3D12CommandBuffer(
            _device,
            _pipelineLibrary,
            _resourceDatabase,
            _resourceAllocator,
            _descriptorAllocator,
            CommandBufferType.Copy);

        _renderers = ImmutableArray<IRenderer>.Empty;

        _pipelineLibrary.InitializeLibrary(null);
    }

    ~D3D12GraphicsEngine()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public IRenderer CreateRenderer()
    {
        ThrowIfDisposed();

        var renderer = new D3D12Renderer(this, _resourceDatabase);
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
        return new D3D12SwapChain(_resourceDatabase, _descriptorAllocator, _device, desc, _renderSystem.MaxFrameLatency);
    }

    public void RenderFrame(ICommandBuffer commandBuffer)
    {
        ThrowIfDisposed();

        _copyCommandBuffer.Begin();

        foreach (var renderer in _renderers)
        {
            renderer.Render(commandBuffer);
        }

        _copyCommandBuffer.End().ThrowIfFailed();
        _resourceAllocator.ReleaseTempResources();
        _descriptorAllocator.ResetCbvSrvUavDynamicHeap();
        _descriptorAllocator.ResetDSVDynamicHeap();
        _descriptorAllocator.ResetRTVDynamicHeap();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _copyCommandBuffer.Dispose();

        _resourceAllocator.Dispose();
        _pipelineLibrary.Dispose();
        _resourceDatabase.Dispose();

        _descriptorAllocator.Dispose();
        _shaderCompiler.Dispose();
        _device.Dispose();
#if DEBUG
        _debugLayer.Dispose();
#endif

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
