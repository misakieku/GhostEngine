using Ghost.Graphics.RHI;
using System.Diagnostics;

namespace Ghost.UnitTest.MockingEnvironment;

public enum QueueOpType
{
    Signal,
    Submit,
    Wait
}

public readonly struct RecordedQueueOp
{
    public CommandQueueType QueueType { get; }
    public QueueOpType OpType { get; }
    public ulong Value { get; }

    public RecordedQueueOp(CommandQueueType queueType, QueueOpType opType, ulong value = 0)
    {
        QueueType = queueType;
        OpType = opType;
        Value = value;
    }

    public override string ToString() => $"{QueueType}:{OpType}({Value})";
}

internal class MockingCommandQueue : ICommandQueue
{
#if GHOST_UNITTEST
    public static List<RecordedQueueOp> GlobalRecordedOps { get; } = new();
#endif

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
#if GHOST_UNITTEST
        lock (GlobalRecordedOps)
        {
            GlobalRecordedOps.Add(new RecordedQueueOp(Type, QueueOpType.Signal, value));
        }
#endif

        var mockingFence = fence as MockingFence;
        Debug.Assert(mockingFence != null);

        mockingFence.Signal(value);
        return value;
    }

    public void Submit(ICommandBuffer commandBuffer)
    {
#if GHOST_UNITTEST
        lock (GlobalRecordedOps)
        {
            GlobalRecordedOps.Add(new RecordedQueueOp(Type, QueueOpType.Submit, (ulong)commandBuffer.Type));
        }
#endif
    }

    public void Submit(params scoped ReadOnlySpan<ICommandBuffer> commandBuffers)
    {
#if GHOST_UNITTEST
        lock (GlobalRecordedOps)
        {
            GlobalRecordedOps.Add(new RecordedQueueOp(Type, QueueOpType.Submit, (ulong)commandBuffers[0].Type));
        }
#endif
    }

    public void Wait(IFence fence, ulong value)
    {
#if GHOST_UNITTEST
        lock (GlobalRecordedOps)
        {
            GlobalRecordedOps.Add(new RecordedQueueOp(Type, QueueOpType.Wait, value));
        }
#endif
    }

    public void Dispose()
    {
    }
}
