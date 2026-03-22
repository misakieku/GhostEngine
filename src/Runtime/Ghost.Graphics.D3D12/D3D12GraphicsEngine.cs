#if DEBUG
#define ENABLE_DEBUG_LAYER
#endif

using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.D3D12;

public static class D3D12GraphicsEngineFactory
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IGraphicsEngine Create(GraphicsEngineDesc desc)
    {
        return new D3D12GraphicsEngine(desc);
    }
}

internal class D3D12GraphicsEngine : IGraphicsEngine
{
    private readonly GraphicsEngineDesc _desc;

#if ENABLE_DEBUG_LAYER
    private readonly D3D12DebugLayer _debugLayer;
#endif
    private readonly D3D12RenderDevice _device;
    private readonly DxcShaderCompiler _shaderCompiler;
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12PipelineLibrary _pipelineLibrary;
    private readonly D3D12ResourceAllocator _resourceAllocator;

    private ImmutableArray<IRenderer> _renderers;

    private bool _disposed;

    public IRenderDevice Device => _device;
    public IShaderCompiler ShaderCompiler => _shaderCompiler;
    public IPipelineLibrary PipelineLibrary => _pipelineLibrary;
    public IResourceDatabase ResourceDatabase => _resourceDatabase;
    public IResourceAllocator ResourceAllocator => _resourceAllocator;

    public D3D12GraphicsEngine(GraphicsEngineDesc desc)
    {
        _desc = desc;

#if ENABLE_DEBUG_LAYER
        _debugLayer = new D3D12DebugLayer();
#endif
        _device = new D3D12RenderDevice();
        _shaderCompiler = new DxcShaderCompiler();
        _descriptorAllocator = new D3D12DescriptorAllocator(_device);

        _resourceDatabase = new D3D12ResourceDatabase(_descriptorAllocator);
        _pipelineLibrary = new D3D12PipelineLibrary(_device, _resourceDatabase);
        _resourceAllocator = new D3D12ResourceAllocator(_device, _descriptorAllocator, _resourceDatabase, _pipelineLibrary);

        _renderers = ImmutableArray<IRenderer>.Empty;

        _pipelineLibrary.InitializeLibrary(null);
    }

    ~D3D12GraphicsEngine()
    {
        Dispose();
    }

    [Conditional("DEBUG")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public IRenderer CreateRenderer()
    {
        ThrowIfDisposed();

        var renderer = new D3D12Renderer(this);
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

    public ICommandAllocator CreateCommandAllocator(CommandBufferType type = CommandBufferType.Graphics)
    {
        return new D3D12CommandAllocator(_device, type);
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
        return new D3D12SwapChain(_resourceDatabase, _descriptorAllocator, _device, desc, _desc.FrameBufferCount);
    }

    public Result BeginFrame(uint cpuFenceValue, uint gpuFenceValue)
    {
        ThrowIfDisposed();

        _resourceDatabase.BeginFrame(cpuFenceValue);
        return Result.Success();
    }

    public Result EndFrame(uint cpuFenceValue, uint gpuFenceValue)
    {
        ThrowIfDisposed();

        _resourceDatabase.EndFrame(gpuFenceValue);
        return Result.Success();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var renderer in _renderers)
        {
            renderer.Dispose();
        }

        _resourceDatabase.ReleaseAllResourcesImmediately();

        _resourceAllocator.Dispose();
        _pipelineLibrary.Dispose();
        _resourceDatabase.Dispose();

        _descriptorAllocator.Dispose();
        _shaderCompiler.Dispose();
        _device.Dispose();
#if ENABLE_DEBUG_LAYER
        _debugLayer.Dispose();
#endif

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
