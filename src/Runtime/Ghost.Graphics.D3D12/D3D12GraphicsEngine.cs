#if DEBUG
#define ENABLE_DEBUG_LAYER
#endif

using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Utilities;
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
    private readonly D3D12DescriptorAllocator _descriptorAllocator;
    private readonly D3D12ResourceDatabase _resourceDatabase;
    private readonly D3D12PipelineLibrary _pipelineLibrary;
    private readonly D3D12ResourceAllocator _resourceAllocator;

    private readonly List<ICommandBuffer> _commandBufferPool;
    private readonly Queue<CommandBufferReturnEntry> _commandBufferReturnQueue;

    private ulong _cpuFrame;
    private bool _disposed;

    public IRenderDevice Device => _device;
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
        _descriptorAllocator = new D3D12DescriptorAllocator(_device);

        _resourceDatabase = new D3D12ResourceDatabase(_descriptorAllocator);
        _pipelineLibrary = new D3D12PipelineLibrary(_device);
        _resourceAllocator = new D3D12ResourceAllocator(_device, _descriptorAllocator, _resourceDatabase, _pipelineLibrary);

        _commandBufferPool = new List<ICommandBuffer>(4);
        _commandBufferReturnQueue = new Queue<CommandBufferReturnEntry>(4);
    }

    ~D3D12GraphicsEngine()
    {
        Dispose();
    }

    public ICommandAllocator CreateCommandAllocator(CommandBufferType type = CommandBufferType.Graphics)
    {
        Logger.DebugAssert(!_disposed);
        return new D3D12CommandAllocator(_device, type);
    }

    public ICommandBuffer CreateCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        Logger.DebugAssert(!_disposed);

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
        Logger.DebugAssert(!_disposed);

        for (var i = 0; i < _commandBufferPool.Count; i++)
        {
            if (_commandBufferPool[i].Type == type)
            {
                var commandBuffer = _commandBufferPool[i];
                _commandBufferPool.RemoveAndSwapBack(i);
                return commandBuffer;
            }
        }

        return CreateCommandBuffer(type);
    }

    public void ReturnPooledCommandBuffer(ICommandBuffer commandBuffer)
    {
        Logger.DebugAssert(!_disposed);
        _commandBufferReturnQueue.Enqueue(new CommandBufferReturnEntry(commandBuffer, _cpuFrame));
    }

    public ISwapChain CreateSwapChain(SwapChainDesc desc)
    {
        Logger.DebugAssert(!_disposed);
        return new DXGISwapChain(_resourceDatabase, _descriptorAllocator, _device, desc, _desc.FrameBufferCount);
    }

    public ICommandSignature CreateCommandSignature(ref readonly CommandSignatureDesc desc, Key128<PipelineState> pipelineKey)
    {
        Logger.DebugAssert(!_disposed);
        return new D3D12CommandSignature(_device, _pipelineLibrary, in desc, pipelineKey);
    }

    public void BeginFrame(ulong cpuFrame)
    {
        Logger.DebugAssert(!_disposed);

        _cpuFrame = cpuFrame;
        _resourceDatabase.BeginFrame(cpuFrame);
    }

    public void EndFrame(ulong gpuFrame)
    {
        Logger.DebugAssert(!_disposed);

        _resourceDatabase.EndFrame(gpuFrame);

        while (_commandBufferReturnQueue.TryPeek(out var entry) && entry.returnFrame < gpuFrame)
        {
            _commandBufferPool.Add(entry.commandBuffer);
            _commandBufferReturnQueue.Dequeue();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        while (_commandBufferReturnQueue.TryDequeue(out var entry))
        {
            entry.commandBuffer.Dispose();
        }

        foreach (var commandBuffer in _commandBufferPool)
        {
            commandBuffer.Dispose();
        }

        _resourceDatabase.ReleaseAllResourcesImmediately();

        _resourceAllocator.Dispose();
        _pipelineLibrary.Dispose();
        _resourceDatabase.Dispose();

        _descriptorAllocator.Dispose();
        _device.Dispose();
#if ENABLE_DEBUG_LAYER
        _debugLayer.Dispose();
#endif

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
