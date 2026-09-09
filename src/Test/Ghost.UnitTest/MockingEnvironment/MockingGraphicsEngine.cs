using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingGraphicsEngine : IGraphicsEngine
{
    private const int COMMAND_BUFFER_TYPE_COUNT = 3;

    private readonly MockingRenderDevice _renderDevice;
    private readonly MockingResourceDatabase _resourceDatabase;
    private readonly MockingResourceAllocator _resourceAllocator;
    private readonly Stack<MockingCommandBuffer>[] _commandBufferPools;
    private readonly Queue<MockingCommandBuffer> _scriptedCommandBuffers;
    private readonly bool _ownsDependencies;

    private sealed class MockingCommandSignature : ICommandSignature
    {
        public string Name { get; set; } = nameof(MockingCommandSignature);
        public IntPtr NativePointer => IntPtr.Zero;

        public void Dispose()
        {
        }
    }

    public IRenderDevice Device => _renderDevice;

    public IPipelineLibrary PipelineLibrary => throw new NotImplementedException();

    public IResourceDatabase ResourceDatabase => _resourceDatabase;

    public IResourceAllocator ResourceAllocator => _resourceAllocator;

    public int ReturnedCommandBufferCount
    {
        get;
        private set;
    }

    public int DiscardedCommandBufferCount
    {
        get;
        private set;
    }

    public bool FailNextCommandBufferAcquisition
    {
        get;
        set;
    }

    public int FailCommandBufferAcquisitionAtRequest
    {
        get;
        set;
    } = -1;

    public List<MockingCommandBuffer> AcquiredCommandBuffers
    {
        get;
    } = new(8);

    public List<MockingCommandBuffer> ReturnedCommandBuffers
    {
        get;
    } = new(8);

    public List<CommandBufferType> RequestedCommandBufferTypes
    {
        get;
    } = new(8);

    public MockingCommandBuffer LastAcquiredCommandBuffer => AcquiredCommandBuffers[^1];

    public MockingGraphicsEngine()
    {
        _renderDevice = new MockingRenderDevice();
        _resourceDatabase = new MockingResourceDatabase();
        _resourceAllocator = new MockingResourceAllocator(_resourceDatabase);
        _commandBufferPools = CreateCommandBufferPools();
        _scriptedCommandBuffers = new Queue<MockingCommandBuffer>();
        _ownsDependencies = true;
    }

    public MockingGraphicsEngine(
        MockingRenderDevice renderDevice,
        MockingResourceDatabase resourceDatabase,
        MockingResourceAllocator resourceAllocator)
    {
        _renderDevice = renderDevice;
        _resourceDatabase = resourceDatabase;
        _resourceAllocator = resourceAllocator;
        _commandBufferPools = CreateCommandBufferPools();
        _scriptedCommandBuffers = new Queue<MockingCommandBuffer>();
        _ownsDependencies = false;
    }

    public void BeginFrame(ulong submittedFrame)
    {
    }

    public ICommandAllocator CreateCommandAllocator(CommandBufferType type = CommandBufferType.Graphics)
    {
        return new MockingCommandAllocator(type);
    }

    public ICommandBuffer CreateCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        return new MockingCommandBuffer(_resourceDatabase, type);
    }

    public ICommandSignature CreateCommandSignature(scoped in CommandSignatureDesc desc, Key128<PipelineState> pipelineKey)
    {
        return new MockingCommandSignature();
    }

    public IFence CreateFence(ulong initialValue = 0)
    {
        return new MockingFence(initialValue);
    }

    public ISwapChain CreateSwapChain(SwapChainDesc desc)
    {
        throw new NotImplementedException();
    }

    public void EndFrame(ulong completedFrame)
    {
    }

    public ICommandBuffer GetPooledCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        var requestIndex = RequestedCommandBufferTypes.Count;
        RequestedCommandBufferTypes.Add(type);
        if (FailNextCommandBufferAcquisition || requestIndex == FailCommandBufferAcquisitionAtRequest)
        {
            FailNextCommandBufferAcquisition = false;
            FailCommandBufferAcquisitionAtRequest = -1;
            throw new InvalidOperationException("Injected command-buffer acquisition failure.");
        }

        MockingCommandBuffer commandBuffer;
        if (_scriptedCommandBuffers.Count > 0)
        {
            commandBuffer = _scriptedCommandBuffers.Dequeue();
        }
        else if (!_commandBufferPools[(int)type].TryPop(out commandBuffer!))
        {
            commandBuffer = new MockingCommandBuffer(_resourceDatabase, type);
        }

        AcquiredCommandBuffers.Add(commandBuffer);
        return commandBuffer;
    }

    public void ReturnPooledCommandBuffer(ICommandBuffer commandBuffer)
    {
        if (commandBuffer is not MockingCommandBuffer mockingCommandBuffer)
        {
            throw new ArgumentException("Unexpected command-buffer implementation.", nameof(commandBuffer));
        }

        ReturnedCommandBufferCount++;
        ReturnedCommandBuffers.Add(mockingCommandBuffer);
        if (mockingCommandBuffer.State.IsRecording)
        {
            DiscardedCommandBufferCount++;
            mockingCommandBuffer.Dispose();
            return;
        }

        _commandBufferPools[(int)mockingCommandBuffer.Type].Push(mockingCommandBuffer);
    }

    public void QueueCommandBuffer(MockingCommandBuffer commandBuffer)
    {
        _scriptedCommandBuffers.Enqueue(commandBuffer);
    }

    public void ResetCommandBufferTracking()
    {
        AcquiredCommandBuffers.Clear();
        ReturnedCommandBuffers.Clear();
        RequestedCommandBufferTypes.Clear();
        ReturnedCommandBufferCount = 0;
        DiscardedCommandBufferCount = 0;
    }

    public void Dispose()
    {
        if (!_ownsDependencies)
        {
            return;
        }

        _resourceAllocator.Dispose();
        _resourceDatabase.Dispose();
        _renderDevice.Dispose();
    }

    private static Stack<MockingCommandBuffer>[] CreateCommandBufferPools()
    {
        var pools = new Stack<MockingCommandBuffer>[COMMAND_BUFFER_TYPE_COUNT];
        for (var i = 0; i < pools.Length; i++)
        {
            pools[i] = new Stack<MockingCommandBuffer>();
        }

        return pools;
    }
}
