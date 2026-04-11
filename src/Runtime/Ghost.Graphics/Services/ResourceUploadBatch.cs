using Ghost.Core;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;

namespace Ghost.Graphics.Services;

public class ResourceUploadBatch
{
    private readonly IRenderDevice _device;

    private readonly ICommandAllocator _commandAllocator;
    private readonly ICommandBuffer _commandBuffer;

    internal ResourceUploadBatch(IGraphicsEngine engine)
    {
        _device = engine.Device;

        _commandAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);
        _commandBuffer = engine.CreateCommandBuffer(CommandBufferType.Copy);
    }

    public void Begin()
    {
        _commandBuffer.Begin(_commandAllocator);
    }

    public Result End()
    {
        var r = _commandBuffer.End();
        if (r.IsFailure)
        {
            return r;
        }

        _device.GraphicsQueue.Submit(_commandBuffer);
        return Result.Success();
    }

    public void WaitIdle()
    {
        _device.CopyQueue.WaitIdle();
    }

    public Task WaitAsync()
    {
        return _device.CopyQueue.WaitAsync();
    }
}
