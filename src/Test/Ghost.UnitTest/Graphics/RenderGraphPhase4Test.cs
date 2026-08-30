using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.UnitTest.MockingEnvironment;

namespace Ghost.UnitTest.Graphics;

#if GHOST_UNITTEST
public partial class RenderGraphTest
{
    private static readonly ViewState s_phase4ViewState = new(1920, 1080, 1920, 1080);

    [TestMethod]
    public void TestPhase4_SyncMarkersRecordAndSubmitEndedGraphicsCommandBuffersInOrder()
    {
        SetupPhase4SplitPipeline();
        var execution = CompileAndExecute(s_phase4ViewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execution.Dump);

        var expectedSegmentCount = GetCommandBufferSegmentCount(execution.Dump);
        var acquiredCommandBuffers = _graphicsEngine.AcquiredCommandBuffers.ToArray();
        Assert.HasCount(expectedSegmentCount, acquiredCommandBuffers);
        Assert.HasCount(expectedSegmentCount, _graphicsEngine.RequestedCommandBufferTypes);
        Assert.IsTrue(_graphicsEngine.RequestedCommandBufferTypes.All(type => type == CommandBufferType.Graphics));
        Assert.IsTrue(acquiredCommandBuffers.All(commandBuffer => commandBuffer.Type == CommandBufferType.Graphics));
        Assert.IsTrue(acquiredCommandBuffers.All(commandBuffer => commandBuffer.BeginCount == 1));
        Assert.IsTrue(acquiredCommandBuffers.All(commandBuffer => commandBuffer.EndCount == 1));
        Assert.IsTrue(acquiredCommandBuffers.All(commandBuffer => !commandBuffer.State.IsRecording));
        Assert.IsTrue(execution.GraphicsSubmission.IsValid);
        Assert.IsFalse(execution.ComputeSubmission.IsValid);
        Assert.IsFalse(MockingCommandQueue.GlobalRecordedOps.Any(op => op.OpType == QueueOpType.Submit));

        _frameScheduler.Flush();

        var submitOps = MockingCommandQueue.GlobalRecordedOps
            .Where(op => op.OpType == QueueOpType.Submit)
            .ToArray();
        Assert.HasCount(expectedSegmentCount, submitOps);
        Assert.IsTrue(submitOps.All(op => op.QueueType == CommandQueueType.Graphics));
        Assert.IsTrue(submitOps.All(op => op.CommandBufferWasRecording == false));
        Assert.IsFalse(MockingCommandQueue.GlobalRecordedOps.Any(op => op.OpType == QueueOpType.Wait));
        for (var i = 0; i < submitOps.Length; i++)
        {
            Assert.AreEqual((ulong)acquiredCommandBuffers[i].InstanceId, submitOps[i].Value);
        }

        Assert.AreEqual((ulong)acquiredCommandBuffers[^1].InstanceId, submitOps[^1].Value, "The final relative command buffer must be submitted.");
        Assert.AreEqual(expectedSegmentCount, _graphicsEngine.ReturnedCommandBufferCount);
    }

    [TestMethod]
    public void TestPhase4_InitialLifecycleFailuresRollbackAndExecutionRecovers()
    {
        SetupTestRenderPipeline();
        _graphicsEngine.FailNextCommandBufferAcquisition = true;

        var acquisitionFailure = CompileAndExecute(s_phase4ViewState);

        Assert.IsTrue(acquisitionFailure.IsFailure);
        Assert.IsEmpty(_graphicsEngine.AcquiredCommandBuffers);
        Assert.AreEqual(0, _graphicsEngine.ReturnedCommandBufferCount);
        AssertNoPhase4Submissions();

        ResetPhase4Scenario();
        SetupTestRenderPipeline();
        var recoveredExecution = CompileAndExecute(s_phase4ViewState).GetValueOrThrow();
        Assert.IsTrue(recoveredExecution.GraphicsSubmission.IsValid, "Reusable execution scratch must be empty after acquisition rollback.");

        ResetPhase4Scenario();
        var failingCommandBuffer = new MockingCommandBuffer(_resourceDatabase, CommandBufferType.Graphics)
        {
            FailOnBegin = true
        };
        _graphicsEngine.QueueCommandBuffer(failingCommandBuffer);
        SetupTestRenderPipeline();

        var beginFailure = CompileAndExecute(s_phase4ViewState);

        Assert.IsTrue(beginFailure.IsFailure);
        Assert.HasCount(1, _graphicsEngine.AcquiredCommandBuffers);
        Assert.HasCount(1, _graphicsEngine.ReturnedCommandBuffers);
        Assert.AreSame(failingCommandBuffer, _graphicsEngine.ReturnedCommandBuffers[0]);
        Assert.AreEqual(1, failingCommandBuffer.BeginCount);
        Assert.AreEqual(0, failingCommandBuffer.EndCount);
        AssertNoPhase4Submissions();
    }

    [TestMethod]
    public void TestPhase4_PostMarkerLifecycleFailuresReturnEveryAcquiredBufferExactlyOnce()
    {
        SetupPhase4SplitPipeline();
        _graphicsEngine.FailCommandBufferAcquisitionAtRequest = 1;

        var acquisitionFailure = CompileAndExecute(s_phase4ViewState);

        Assert.IsTrue(acquisitionFailure.IsFailure);
        Assert.HasCount(1, _graphicsEngine.AcquiredCommandBuffers);
        Assert.HasCount(2, _graphicsEngine.RequestedCommandBufferTypes);
        Assert.HasCount(1, _graphicsEngine.ReturnedCommandBuffers);
        Assert.AreSame(_graphicsEngine.AcquiredCommandBuffers[0], _graphicsEngine.ReturnedCommandBuffers[0]);
        Assert.AreEqual(1, _graphicsEngine.AcquiredCommandBuffers[0].EndCount);
        AssertNoPhase4Submissions();

        ResetPhase4Scenario();
        var firstCommandBuffer = new MockingCommandBuffer(_resourceDatabase, CommandBufferType.Graphics);
        var failingCommandBuffer = new MockingCommandBuffer(_resourceDatabase, CommandBufferType.Graphics)
        {
            FailOnBegin = true
        };
        _graphicsEngine.QueueCommandBuffer(firstCommandBuffer);
        _graphicsEngine.QueueCommandBuffer(failingCommandBuffer);
        SetupPhase4SplitPipeline();

        var beginFailure = CompileAndExecute(s_phase4ViewState);

        Assert.IsTrue(beginFailure.IsFailure);
        Assert.HasCount(2, _graphicsEngine.AcquiredCommandBuffers);
        Assert.HasCount(2, _graphicsEngine.ReturnedCommandBuffers);
        Assert.AreSame(firstCommandBuffer, _graphicsEngine.ReturnedCommandBuffers[0]);
        Assert.AreSame(failingCommandBuffer, _graphicsEngine.ReturnedCommandBuffers[1]);
        Assert.AreEqual(1, firstCommandBuffer.EndCount);
        Assert.AreEqual(1, failingCommandBuffer.BeginCount);
        Assert.AreEqual(0, failingCommandBuffer.EndCount);
        AssertNoPhase4Submissions();
    }

    [TestMethod]
    public void TestPhase4_RecordingFailuresReturnEveryBufferWithoutSubmission()
    {
        SetupPhase4SplitPipeline(throwInComputeCallback: true);

        var callbackFailure = CompileAndExecute(s_phase4ViewState);

        Assert.IsTrue(callbackFailure.IsFailure);
        Assert.IsGreaterThanOrEqualTo(2, _graphicsEngine.AcquiredCommandBuffers.Count);
        Assert.AreEqual(_graphicsEngine.AcquiredCommandBuffers.Count, _graphicsEngine.ReturnedCommandBufferCount);
        Assert.IsTrue(_graphicsEngine.AcquiredCommandBuffers.All(commandBuffer => commandBuffer.EndCount == 1));
        AssertNoPhase4Submissions();

        ResetPhase4Scenario();
        var firstCommandBuffer = new MockingCommandBuffer(_resourceDatabase, CommandBufferType.Graphics);
        var failingCommandBuffer = new MockingCommandBuffer(_resourceDatabase, CommandBufferType.Graphics)
        {
            FailOnEnd = true
        };
        _graphicsEngine.QueueCommandBuffer(firstCommandBuffer);
        _graphicsEngine.QueueCommandBuffer(failingCommandBuffer);
        SetupPhase4SplitPipeline();

        var endFailure = CompileAndExecute(s_phase4ViewState);

        Assert.IsTrue(endFailure.IsFailure);
        Assert.HasCount(2, _graphicsEngine.AcquiredCommandBuffers);
        Assert.HasCount(2, _graphicsEngine.ReturnedCommandBuffers);
        Assert.AreSame(firstCommandBuffer, _graphicsEngine.ReturnedCommandBuffers[0]);
        Assert.AreSame(failingCommandBuffer, _graphicsEngine.ReturnedCommandBuffers[1]);
        Assert.AreEqual(1, firstCommandBuffer.EndCount);
        Assert.AreEqual(1, failingCommandBuffer.EndCount);
        Assert.IsTrue(failingCommandBuffer.State.IsRecording);
        Assert.AreEqual(1, _graphicsEngine.DiscardedCommandBufferCount, "A command buffer left recording by End failure must not re-enter the pool.");
        AssertNoPhase4Submissions();
    }

    [TestMethod]
    public void TestPhase4_CacheHitPreservesNativeCommandBufferSplitting()
    {
        SetupPhase4SplitPipeline();
        var firstExecution = CompileAndExecute(s_phase4ViewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(firstExecution.Dump);
        var firstSegmentCount = _graphicsEngine.AcquiredCommandBuffers.Count;
        var firstRequestedTypes = _graphicsEngine.RequestedCommandBufferTypes.ToArray();

        ResetPhase4Scenario();
        SetupPhase4SplitPipeline();
        var cachedExecution = CompileAndExecute(s_phase4ViewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(cachedExecution.Dump);

        Assert.IsTrue(cachedExecution.Dump.IsCacheHit);
        Assert.IsTrue(firstExecution.Dump.CommandStream.SequenceEqual(cachedExecution.Dump.CommandStream));
        Assert.HasCount(firstSegmentCount, _graphicsEngine.AcquiredCommandBuffers);
        Assert.IsTrue(firstRequestedTypes.SequenceEqual(_graphicsEngine.RequestedCommandBufferTypes));
        Assert.IsTrue(_graphicsEngine.AcquiredCommandBuffers.All(commandBuffer => commandBuffer.Type == CommandBufferType.Graphics));
    }

    private void ResetPhase4Scenario()
    {
        _frameScheduler.Flush();
        _renderGraph.Reset();
        _graphicsEngine.ResetCommandBufferTracking();
        MockingCommandQueue.GlobalRecordedOps.Clear();
    }

    private static void AssertNoPhase4Submissions()
    {
        Assert.IsFalse(MockingCommandQueue.GlobalRecordedOps.Any(op => op.OpType == QueueOpType.Submit));
    }
}
#endif
