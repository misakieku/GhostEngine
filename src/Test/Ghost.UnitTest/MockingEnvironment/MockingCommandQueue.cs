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
    public bool? CommandBufferWasRecording { get; }

    public RecordedQueueOp(CommandQueueType queueType, QueueOpType opType, ulong value = 0, bool? commandBufferWasRecording = null)
    {
        QueueType = queueType;
        OpType = opType;
        Value = value;
        CommandBufferWasRecording = commandBufferWasRecording;
    }

    public override string ToString()
    {
        var recordingState = CommandBufferWasRecording.HasValue
            ? $", Recording: {CommandBufferWasRecording.Value}"
            : string.Empty;
        return $"{QueueType}:{OpType}({Value}{recordingState})";
    }
}

internal class MockingCommandQueue : ICommandQueue
{
    private readonly bool _validateCommandBufferState;

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

    public MockingCommandQueue(CommandQueueType type, bool validateCommandBufferState = true)
    {
        Type = type;
        _validateCommandBufferState = validateCommandBufferState;
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
        var wasRecording = commandBuffer.State.IsRecording;
#if GHOST_UNITTEST
        lock (GlobalRecordedOps)
        {
            GlobalRecordedOps.Add(new RecordedQueueOp(Type, QueueOpType.Submit, (ulong)commandBuffer.Type, wasRecording));
        }
#endif
        ValidateCommandBufferState(wasRecording);
    }

    public void Submit(params scoped ReadOnlySpan<ICommandBuffer> commandBuffers)
    {
        if (commandBuffers.IsEmpty)
        {
            return;
        }

        var wasRecording = false;
        for (var i = 0; i < commandBuffers.Length; i++)
        {
            wasRecording |= commandBuffers[i].State.IsRecording;
        }

#if GHOST_UNITTEST
        lock (GlobalRecordedOps)
        {
            GlobalRecordedOps.Add(new RecordedQueueOp(Type, QueueOpType.Submit, (ulong)commandBuffers[0].Type, wasRecording));
        }
#endif
        ValidateCommandBufferState(wasRecording);
    }

    private void ValidateCommandBufferState(bool wasRecording)
    {
        if (_validateCommandBufferState && wasRecording)
        {
            throw new InvalidOperationException("Cannot submit a command buffer while it is recording.");
        }
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
