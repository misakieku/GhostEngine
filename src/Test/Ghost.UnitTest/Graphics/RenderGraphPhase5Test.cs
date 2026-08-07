using Ghost.Core;
using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.UnitTest.MockingEnvironment;

namespace Ghost.UnitTest.Graphics;

#if GHOST_UNITTEST
public partial class RenderGraphTest
{
    private static readonly ViewState s_phase5ViewState = new(2560, 1440, 2560, 1440);

    [TestMethod]
    public void TestPhase5_NativeQueuesReplayExactForkJoinDependencies()
    {
        SetupPhase4SplitPipeline();

        var execution = CompileAndExecutePhase5(RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execution.Dump);
        Assert.IsFalse(execution.Dump.IsCacheHit);
        Assert.IsTrue(execution.GraphicsSubmission.IsValid);
        Assert.IsTrue(execution.ComputeSubmission.IsValid);
        Assert.AreEqual(CommandQueueType.Graphics, execution.GraphicsSubmission.QueueType);
        Assert.AreEqual(CommandQueueType.Compute, execution.ComputeSubmission.QueueType);

        var expectedTypes = new[]
        {
            CommandBufferType.Graphics,
            CommandBufferType.Compute,
            CommandBufferType.Graphics,
            CommandBufferType.Graphics
        };
        Assert.IsTrue(expectedTypes.SequenceEqual(_graphicsEngine.RequestedCommandBufferTypes));
        Assert.IsTrue(expectedTypes.SequenceEqual(_graphicsEngine.AcquiredCommandBuffers.Select(commandBuffer => commandBuffer.Type)));
        Assert.AreSame(_graphicsCommandAllocator, _graphicsEngine.AcquiredCommandBuffers[0].LastBeginAllocator);
        Assert.AreSame(_computeCommandAllocator, _graphicsEngine.AcquiredCommandBuffers[1].LastBeginAllocator);
        Assert.AreSame(_graphicsCommandAllocator, _graphicsEngine.AcquiredCommandBuffers[2].LastBeginAllocator);
        Assert.AreSame(_graphicsCommandAllocator, _graphicsEngine.AcquiredCommandBuffers[3].LastBeginAllocator);
        Assert.IsTrue(_graphicsEngine.AcquiredCommandBuffers.All(commandBuffer => commandBuffer.EndCount == 1 && !commandBuffer.State.IsRecording));
        Assert.IsEmpty(MockingCommandQueue.GlobalRecordedOps);

        var commandBuffers = _graphicsEngine.AcquiredCommandBuffers.ToArray();
        _frameScheduler.Flush();

        var ops = MockingCommandQueue.GlobalRecordedOps;
        Assert.HasCount(10, ops);
        AssertQueueOp(ops[0], CommandQueueType.Graphics, QueueOpType.Submit, (ulong)commandBuffers[0].InstanceId);
        AssertQueueOp(ops[1], CommandQueueType.Graphics, QueueOpType.Signal, 1);
        AssertQueueOp(ops[2], CommandQueueType.Compute, QueueOpType.Wait, 1);
        AssertQueueOp(ops[3], CommandQueueType.Compute, QueueOpType.Submit, (ulong)commandBuffers[1].InstanceId);
        AssertQueueOp(ops[4], CommandQueueType.Compute, QueueOpType.Signal, execution.ComputeSubmission.FenceValue);
        AssertQueueOp(ops[5], CommandQueueType.Graphics, QueueOpType.Submit, (ulong)commandBuffers[2].InstanceId);
        AssertQueueOp(ops[6], CommandQueueType.Graphics, QueueOpType.Signal, 2);
        AssertQueueOp(ops[7], CommandQueueType.Graphics, QueueOpType.Wait, execution.ComputeSubmission.FenceValue);
        AssertQueueOp(ops[8], CommandQueueType.Graphics, QueueOpType.Submit, (ulong)commandBuffers[3].InstanceId);
        AssertQueueOp(ops[9], CommandQueueType.Graphics, QueueOpType.Signal, execution.GraphicsSubmission.FenceValue);
        Assert.AreEqual(4, _graphicsEngine.ReturnedCommandBufferCount);
    }

    [TestMethod]
    public void TestPhase5_CacheHitPreservesQueueGraphAndFenceValuesRemainMonotonic()
    {
        SetupPhase4SplitPipeline();
        var firstExecution = CompileAndExecutePhase5(RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(firstExecution.Dump);
        _frameScheduler.Flush();
        var firstOpShape = MockingCommandQueue.GlobalRecordedOps
            .Select(op => (op.QueueType, op.OpType))
            .ToArray();
        var firstTypes = _graphicsEngine.RequestedCommandBufferTypes.ToArray();

        ResetPhase5Scenario();
        SetupPhase4SplitPipeline();
        var cachedExecution = CompileAndExecutePhase5(RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(cachedExecution.Dump);
        Assert.IsTrue(cachedExecution.Dump.IsCacheHit);
        Assert.IsTrue(firstExecution.Dump.CommandStream.SequenceEqual(cachedExecution.Dump.CommandStream));
        Assert.IsTrue(firstTypes.SequenceEqual(_graphicsEngine.RequestedCommandBufferTypes));
        Assert.IsGreaterThan(firstExecution.GraphicsSubmission.FenceValue, cachedExecution.GraphicsSubmission.FenceValue);
        Assert.IsGreaterThan(firstExecution.ComputeSubmission.FenceValue, cachedExecution.ComputeSubmission.FenceValue);

        _frameScheduler.Flush();
        var cachedOps = MockingCommandQueue.GlobalRecordedOps;
        Assert.HasCount(firstOpShape.Length, cachedOps);
        for (var i = 0; i < firstOpShape.Length; i++)
        {
            Assert.AreEqual(firstOpShape[i].QueueType, cachedOps[i].QueueType);
            Assert.AreEqual(firstOpShape[i].OpType, cachedOps[i].OpType);
        }

        ResetPhase5Scenario();
        SetupPhase4SplitPipeline();
        _graphicsEngine.ResetCommandBufferTracking();
        var forcedExecution = _renderGraph.CompileAndExecute(
            _executionContext,
            s_phase5ViewState,
            RGExecutionFlags.GenerateDump | RGExecutionFlags.ForceGraphics).GetValueOrThrow();
        Assert.IsNotNull(forcedExecution.Dump);
        Assert.IsTrue(forcedExecution.Dump.IsCacheHit);
        Assert.IsTrue(firstExecution.Dump.CommandStream.SequenceEqual(forcedExecution.Dump.CommandStream));
        Assert.IsTrue(_graphicsEngine.RequestedCommandBufferTypes.All(type => type == CommandBufferType.Graphics));
        Assert.IsFalse(forcedExecution.ComputeSubmission.IsValid);
        _frameScheduler.Flush();
        Assert.IsFalse(MockingCommandQueue.GlobalRecordedOps.Any(op => op.OpType == QueueOpType.Wait));
    }

    [TestMethod]
    public void TestPhase5_DependencyFailureRollsBackTransferredOwnershipAndExecutionRecovers()
    {
        SetupPhase4SplitPipeline();
        var faultingScheduler = new FaultInjectingFrameScheduler(_frameScheduler)
        {
            FailOnAddDependencyCall = 0
        };
        var faultingContext = CreatePhase5ExecutionContext(faultingScheduler);
        _graphicsEngine.ResetCommandBufferTracking();

        var failure = _renderGraph.CompileAndExecute(faultingContext, s_phase5ViewState);

        Assert.IsTrue(failure.IsFailure);
        Assert.HasCount(4, _graphicsEngine.AcquiredCommandBuffers);
        Assert.AreEqual(4, _graphicsEngine.ReturnedCommandBufferCount);
        Assert.IsTrue(_graphicsEngine.AcquiredCommandBuffers.All(commandBuffer => commandBuffer.EndCount == 1));
        Assert.IsEmpty(MockingCommandQueue.GlobalRecordedOps);
        _frameScheduler.Flush();
        Assert.IsEmpty(MockingCommandQueue.GlobalRecordedOps, "Rollback must leave no pending scheduler submissions.");

        _renderGraph.Reset();
        _graphicsEngine.ResetCommandBufferTracking();
        SetupPhase4SplitPipeline();
        var recoveredExecution = CompileAndExecutePhase5().GetValueOrThrow();
        Assert.IsTrue(recoveredExecution.GraphicsSubmission.IsValid);
        Assert.IsTrue(recoveredExecution.ComputeSubmission.IsValid);
        _frameScheduler.Flush();
        Assert.IsTrue(MockingCommandQueue.GlobalRecordedOps.Any(op => op.QueueType == CommandQueueType.Compute && op.OpType == QueueOpType.Submit));
    }

    private Result<RGExecution, Error> CompileAndExecutePhase5(RGExecutionFlags flags = RGExecutionFlags.Default)
    {
        _graphicsEngine.ResetCommandBufferTracking();
        var executionContext = CreatePhase5ExecutionContext(_frameScheduler);
        return _renderGraph.CompileAndExecute(executionContext, s_phase5ViewState, flags);
    }

    private RenderGraphExecutionContext CreatePhase5ExecutionContext(IFrameScheduler frameScheduler)
    {
        return new RenderGraphExecutionContext(
            _graphicsEngine,
            frameScheduler,
            _graphicsCommandAllocator,
            _computeCommandAllocator);
    }

    private void ResetPhase5Scenario()
    {
        _frameScheduler.Flush();
        _renderGraph.Reset();
        _graphicsEngine.ResetCommandBufferTracking();
        MockingCommandQueue.GlobalRecordedOps.Clear();
    }

    private static void AssertQueueOp(
        RecordedQueueOp op,
        CommandQueueType expectedQueue,
        QueueOpType expectedType,
        ulong expectedValue)
    {
        Assert.AreEqual(expectedQueue, op.QueueType);
        Assert.AreEqual(expectedType, op.OpType);
        Assert.AreEqual(expectedValue, op.Value);
    }
}
#endif
