using Ghost.Core;
using Ghost.Graphics.RHI;

namespace Ghost.Graphics.Services;

public class AsyncCopyPipeline : IDisposable
{
    private readonly IRenderDevice _device;

    private readonly ICommandAllocator _commandAllocator;
    private readonly ICommandBuffer _commandBuffer;
    private readonly IFence _fence;

    private ulong _fenceValue;

    internal AsyncCopyPipeline(IGraphicsEngine engine)
    {
        _device = engine.Device;

        _commandAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);
        _commandBuffer = engine.CreateCommandBuffer(CommandBufferType.Copy);
        _fence = engine.CreateFence(0);

        _commandAllocator.Name = $"AsyncCopyPipeline_CommandAllocator";
        _commandBuffer.Name = $"AsyncCopyPipeline_CommandBuffer";
        _fence.Name = "AsyncCopyPipeline_Fence";
    }

    ~AsyncCopyPipeline()
    {
        Dispose();
    }

    internal void Begin()
    {
        _commandAllocator.Reset();
        _commandBuffer.Begin(_commandAllocator);
    }

    internal Result End()
    {
        var result = _commandBuffer.End();

        if (result.IsSuccess)
        {
            _device.CopyQueue.Submit(_commandBuffer);
            _device.CopyQueue.Signal(_fence, ++_fenceValue);
        }

        return result;
    }

    public ICommandBuffer GetCommandBuffer()
    {
        return _commandBuffer;
    }

    public ulong CurrentFenceValue()
    {
        return _fence.CompletedValue;
    }

    public ulong SignaledFenceValue()
    {
        return _fenceValue;
    }

    public bool IsCopyComplete()
    {
        return _fence.CompletedValue >= _fenceValue;
    }

    public void WaitIdle()
    {
        _fence.WaitForValue(_fenceValue);
    }

    public Task WaitAsync()
    {
        return _fence.WaitForValueAsync(_fenceValue);
    }

    public void Dispose()
    {
        _commandAllocator.Dispose();
        _commandBuffer.Dispose();
        _fence.Dispose();

        GC.SuppressFinalize(this);
    }
}
