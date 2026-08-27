using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.MockingEnvironment;

internal sealed class FaultInjectingFrameScheduler : IFrameScheduler
{
    private readonly IFrameScheduler _inner;
    private int _addDependencyCallCount;

    public int FailOnAddDependencyCall
    {
        get;
        set;
    } = -1;

    public ulong SubmittedFrame => _inner.SubmittedFrame;

    public FaultInjectingFrameScheduler(IFrameScheduler inner)
    {
        _inner = inner;
    }

    public ICommandBuffer GetPooledCommandBuffer(CommandBufferType type = CommandBufferType.Graphics)
    {
        return _inner.GetPooledCommandBuffer(type);
    }

    public void ReturnPooledCommandBuffer(ICommandBuffer commandBuffer)
    {
        _inner.ReturnPooledCommandBuffer(commandBuffer);
    }

    public void PrepareSubmissions(int additionalSubmissionCount)
    {
        _inner.PrepareSubmissions(additionalSubmissionCount);
    }

    public SubmissionTransaction BeginSubmissionTransaction(int additionalSubmissionCount, int additionalDependencyCount)
    {
        return _inner.BeginSubmissionTransaction(additionalSubmissionCount, additionalDependencyCount);
    }

    public void CommitSubmissionTransaction(SubmissionTransaction transaction)
    {
        _inner.CommitSubmissionTransaction(transaction);
    }

    public void RollbackSubmissionTransaction(SubmissionTransaction transaction)
    {
        _inner.RollbackSubmissionTransaction(transaction);
    }

    public SubmissionHandle Submit(ICommandBuffer commandBuffer)
    {
        return _inner.Submit(commandBuffer);
    }

    public void AddDependency(SubmissionHandle producer, SubmissionHandle dependent)
    {
        if (_addDependencyCallCount++ == FailOnAddDependencyCall)
        {
            throw new InvalidOperationException("Injected scheduler dependency failure.");
        }

        _inner.AddDependency(producer, dependent);
    }

    public void Transition(CommandQueueType source, CommandQueueType destination)
    {
        _inner.Transition(source, destination);
    }

    public bool IsComplete(SubmissionHandle submission)
    {
        return _inner.IsComplete(submission);
    }

    public FrameCompletionInfo Flush()
    {
        return _inner.Flush();
    }

    public void WaitForFrame(scoped in FrameCompletionInfo completion)
    {
        _inner.WaitForFrame(in completion);
    }

    public void WaitIdle()
    {
        _inner.WaitIdle();
    }

    public void Dispose()
    {
    }
}
