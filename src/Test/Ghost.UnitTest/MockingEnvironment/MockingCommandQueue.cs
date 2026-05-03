using Ghost.Graphics.RHI;
using System.Diagnostics;

namespace Ghost.UnitTest.MockingEnvironment;

internal class MockingCommandQueue : ICommandQueue
{
    public CommandQueueType Type
    {
        get;
    }

    public string Name
    {
        get; set;
    } = "MockCommandQueue";

    public MockingCommandQueue(CommandQueueType type)
    {
        Type = type;
    }

    public ulong Signal(IFence fence, ulong value)
    {
        var mockingFence = fence as MockingFence;
        Debug.Assert(mockingFence != null);

        mockingFence.Signal(value);
        return value;
    }

    public void Submit(ICommandBuffer commandBuffer)
    {
    }

    public void Submit(params scoped ReadOnlySpan<ICommandBuffer> commandBuffers)
    {
    }

    public void Wait(IFence fence, ulong value)
    {
        Thread.Sleep(Random.Shared.Next(10, 50));
    }

    public void Dispose()
    {
    }
}
