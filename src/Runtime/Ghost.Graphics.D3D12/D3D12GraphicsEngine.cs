#if DEBUG
#define ENABLE_DEBUG_LAYER
#endif

using Ghost.Core;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
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
    private readonly struct CommandBufferReturnEntry
    {
        public readonly ICommandBuffer commandBuffer;
        public readonly ulong returnFrame;

        public CommandBufferReturnEntry(ICommandBuffer commandBuffer, ulong returnFrame)
        {
            this.commandBuffer = commandBuffer;
            this.returnFrame = returnFrame;
        }
    }

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

    private readonly Queue<ICommandBuffer> _commandBufferPool;
    private readonly Queue<CommandBufferReturnEntry> _commandBufferReturnQueue;

    private ulong _currentFrame;
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

        _commandBufferPool = new Queue<ICommandBuffer>(4);
        _commandBufferReturnQueue = new Queue<CommandBufferReturnEntry>(4);
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

    public ICommandBuffer GetPooledCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        if (_commandBufferPool.TryDequeue(out var commandBuffer))
        {
            return commandBuffer;
        }
        else
        {
            return CreateCommandBuffer(type);
        }
    }

    public void ReturnPooledCommandBuffer(ICommandBuffer commandBuffer)
    {
        _commandBufferReturnQueue.Enqueue(new CommandBufferReturnEntry(commandBuffer, _currentFrame));
    }

    public ISwapChain CreateSwapChain(SwapChainDesc desc)
    {
        ThrowIfDisposed();
        return new DXGISwapChain(_resourceDatabase, _descriptorAllocator, _device, desc, _desc.FrameBufferCount);
    }

    public Result BeginFrame(ulong currentFrame)
    {
        ThrowIfDisposed();

        _currentFrame = currentFrame;
        _resourceDatabase.BeginFrame(currentFrame);
        return Result.Success();
    }

    public Result EndFrame(ulong completedFrame)
    {
        ThrowIfDisposed();

        _resourceDatabase.EndFrame(completedFrame);

        while (_commandBufferReturnQueue.TryPeek(out var entry) && entry.returnFrame <= completedFrame)
        {
            _commandBufferPool.Enqueue(entry.commandBuffer);
            _commandBufferReturnQueue.Dequeue();
        }

        return Result.Success();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
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
