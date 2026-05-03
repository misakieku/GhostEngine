using Ghost.Graphics.RHI;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingGraphicsEngine : IGraphicsEngine
{
    private readonly MockingRenderDevice _renderDevice;
    private readonly MockingResourceDatabase _resourceDatabase;
    private readonly MockingResourceAllocator _resourceAllocator;

    public IRenderDevice Device => _renderDevice;

    public IPipelineLibrary PipelineLibrary => throw new NotImplementedException();

    public IResourceDatabase ResourceDatabase => _resourceDatabase;

    public IResourceAllocator ResourceAllocator => _resourceAllocator;

    public MockingGraphicsEngine()
    {
        _renderDevice = new MockingRenderDevice();
        _resourceDatabase = new MockingResourceDatabase();
        _resourceAllocator = new MockingResourceAllocator(_resourceDatabase);
    }

    public void BeginFrame(ulong submittedFrame)
    {
    }

    public ICommandAllocator CreateCommandAllocator(CommandBufferType type = CommandBufferType.Graphics)
    {
        return new MockingCommandAllocator();
    }

    public ICommandBuffer CreateCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        return new MockingCommandBuffer(_resourceDatabase, type);
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
        return new MockingCommandBuffer(_resourceDatabase, type);
    }

    public void ReturnPooledCommandBuffer(ICommandBuffer commandBuffer)
    {
    }

    public void Dispose()
    {
        _resourceAllocator.Dispose();
        _resourceDatabase.Dispose();
        _renderDevice.Dispose();
    }
}
