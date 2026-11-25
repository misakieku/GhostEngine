using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;

namespace Ghost.UnitTest.Graphics;

#if GHOST_UNITTEST
[TestClass]
[DoNotParallelize]
public class FrameSchedulerTest
{
    [TestInitialize]
    public void Initialize()
    {
        MockingCommandQueue.GlobalRecordedOps.Clear();
    }

    [TestCleanup]
    public void Cleanup()
    {
        MockingCommandQueue.GlobalRecordedOps.Clear();
    }

    [TestMethod]
    public void TestSubmitIsDeferredUntilFlush()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var allocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        var commandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, allocator);

        var submission = scheduler.Submit(commandBuffer);

        Assert.IsEmpty(MockingCommandQueue.GlobalRecordedOps);

        var completion = scheduler.Flush();

        Assert.HasCount(2, MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[0].OpType);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[0].QueueType);
        Assert.AreEqual(QueueOpType.Signal, MockingCommandQueue.GlobalRecordedOps[1].OpType);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[1].QueueType);
        Assert.AreEqual(1UL, MockingCommandQueue.GlobalRecordedOps[1].Value);
        Assert.IsTrue(submission.IsValid);
        Assert.IsTrue(scheduler.IsComplete(submission));
        Assert.AreEqual(1UL, completion.FrameNumber);
    }

    [TestMethod]
    public void TestDependencyEmitsSourceSignalAndDestinationWait()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var copyAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        var copyCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Copy, copyAllocator);
        var graphicsCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);

        var copySubmission = scheduler.Submit(copyCommandBuffer);
        var graphicsSubmission = scheduler.Submit(graphicsCommandBuffer);
        scheduler.AddDependency(copySubmission, graphicsSubmission);

        scheduler.Flush();

        Assert.HasCount(5, MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(CommandQueueType.Copy, MockingCommandQueue.GlobalRecordedOps[0].QueueType);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[0].OpType);
        Assert.AreEqual(QueueOpType.Signal, MockingCommandQueue.GlobalRecordedOps[1].OpType);
        Assert.AreEqual(CommandQueueType.Copy, MockingCommandQueue.GlobalRecordedOps[1].QueueType);
        Assert.AreEqual(QueueOpType.Wait, MockingCommandQueue.GlobalRecordedOps[2].OpType);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[2].QueueType);
        Assert.AreEqual(1UL, MockingCommandQueue.GlobalRecordedOps[2].Value);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[3].OpType);
        Assert.AreEqual(QueueOpType.Signal, MockingCommandQueue.GlobalRecordedOps[4].OpType);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[4].QueueType);
    }

    [TestMethod]
    public void TestTransitionAppliesToNextDestinationSubmission()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var copyAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        var copyCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Copy, copyAllocator);
        var graphicsCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);

        scheduler.Submit(copyCommandBuffer);
        scheduler.Transition(CommandQueueType.Copy, CommandQueueType.Graphics);
        scheduler.Submit(graphicsCommandBuffer);

        scheduler.Flush();

        Assert.HasCount(5, MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(QueueOpType.Wait, MockingCommandQueue.GlobalRecordedOps[2].OpType);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[2].QueueType);
        Assert.AreEqual(1UL, MockingCommandQueue.GlobalRecordedOps[2].Value);
    }

    [TestMethod]
    public void TestDirectAndComputeForkJoinPreservesQueueDependencies()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = engine.CreateCommandAllocator(CommandBufferType.Compute);
        var directBefore = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var compute = CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator);
        var directAfter = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);

        var directBeforeSubmission = scheduler.Submit(directBefore);
        var computeSubmission = scheduler.Submit(compute);
        var directAfterSubmission = scheduler.Submit(directAfter);
        scheduler.AddDependency(directBeforeSubmission, computeSubmission);
        scheduler.AddDependency(computeSubmission, directAfterSubmission);

        scheduler.Flush();

        Assert.HasCount(8, MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[0].QueueType);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[0].OpType);
        Assert.AreEqual(QueueOpType.Signal, MockingCommandQueue.GlobalRecordedOps[1].OpType);
        Assert.AreEqual(CommandQueueType.Compute, MockingCommandQueue.GlobalRecordedOps[2].QueueType);
        Assert.AreEqual(QueueOpType.Wait, MockingCommandQueue.GlobalRecordedOps[2].OpType);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[3].OpType);
        Assert.AreEqual(QueueOpType.Signal, MockingCommandQueue.GlobalRecordedOps[4].OpType);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[5].QueueType);
        Assert.AreEqual(QueueOpType.Wait, MockingCommandQueue.GlobalRecordedOps[5].OpType);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[6].OpType);
        Assert.AreEqual(QueueOpType.Signal, MockingCommandQueue.GlobalRecordedOps[7].OpType);
        Assert.AreEqual(2UL, MockingCommandQueue.GlobalRecordedOps[7].Value);
    }

    [TestMethod]
    public void TestMultipleComputeRegionsWaitOnlyForTheirDeclaredProducerFences()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = engine.CreateCommandAllocator(CommandBufferType.Compute);
        var graphicsProducer = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        var firstCompute = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator));
        scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        var firstJoin = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        var secondCompute = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator));
        var finalJoin = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        scheduler.AddDependency(graphicsProducer, firstCompute);
        scheduler.AddDependency(firstCompute, firstJoin);
        scheduler.AddDependency(firstJoin, secondCompute);
        scheduler.AddDependency(secondCompute, finalJoin);

        scheduler.Flush();

        var waits = MockingCommandQueue.GlobalRecordedOps.Where(op => op.OpType == QueueOpType.Wait).ToArray();
        Assert.HasCount(4, waits);
        Assert.AreEqual(CommandQueueType.Compute, waits[0].QueueType);
        Assert.AreEqual(graphicsProducer.FenceValue, waits[0].Value);
        Assert.AreEqual(CommandQueueType.Graphics, waits[1].QueueType);
        Assert.AreEqual(firstCompute.FenceValue, waits[1].Value);
        Assert.AreEqual(CommandQueueType.Compute, waits[2].QueueType);
        Assert.AreEqual(firstJoin.FenceValue, waits[2].Value);
        Assert.AreEqual(CommandQueueType.Graphics, waits[3].QueueType);
        Assert.AreEqual(secondCompute.FenceValue, waits[3].Value);
        Assert.IsLessThan(secondCompute.FenceValue, firstCompute.FenceValue, "The first Graphics join must not wait for the later Compute region.");
        Assert.AreEqual(6, engine.ReturnedCommandBufferCount);
    }

    [TestMethod]
    public void TestCrossFrameDependencyWaitsForPriorSubmission()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var copyAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        var copyCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Copy, copyAllocator);

        var copySubmission = scheduler.Submit(copyCommandBuffer);
        scheduler.Flush();
        MockingCommandQueue.GlobalRecordedOps.Clear();

        var graphicsCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var graphicsSubmission = scheduler.Submit(graphicsCommandBuffer);
        scheduler.AddDependency(copySubmission, graphicsSubmission);
        scheduler.Flush();

        Assert.HasCount(3, MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(QueueOpType.Wait, MockingCommandQueue.GlobalRecordedOps[0].OpType);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[0].QueueType);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[1].OpType);
        Assert.AreEqual(QueueOpType.Signal, MockingCommandQueue.GlobalRecordedOps[2].OpType);
    }

    [TestMethod]
    public void TestDependencyCycleIsRejectedBeforeQueueSubmission()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = engine.CreateCommandAllocator(CommandBufferType.Compute);
        var graphicsCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var computeCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator);

        var graphicsSubmission = scheduler.Submit(graphicsCommandBuffer);
        var computeSubmission = scheduler.Submit(computeCommandBuffer);
        scheduler.AddDependency(graphicsSubmission, computeSubmission);
        scheduler.AddDependency(computeSubmission, graphicsSubmission);

        Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Flush());
        Assert.IsEmpty(MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(2, engine.ReturnedCommandBufferCount);

        scheduler.Flush();
    }

    [TestMethod]
    public void TestUnresolvedTransitionReturnsPendingCommandBuffer()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var copyAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);
        var copyCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Copy, copyAllocator);

        scheduler.Submit(copyCommandBuffer);
        scheduler.Transition(CommandQueueType.Copy, CommandQueueType.Graphics);

        Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Flush());
        Assert.IsEmpty(MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(1, engine.ReturnedCommandBufferCount);

        scheduler.Flush();
    }

    [TestMethod]
    public void TestSubmissionTransactionRollbackRestoresPendingStateWithoutReusingFenceValues()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = engine.CreateCommandAllocator(CommandBufferType.Compute);
        var externalGraphics = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var externalGraphicsSubmission = scheduler.Submit(externalGraphics);
        scheduler.Transition(CommandQueueType.Graphics, CommandQueueType.Compute);

        var transaction = scheduler.BeginSubmissionTransaction(2, 1);
        var rolledBackCompute = CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator);
        var rolledBackGraphics = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var rolledBackComputeSubmission = scheduler.Submit(rolledBackCompute);
        var rolledBackGraphicsSubmission = scheduler.Submit(rolledBackGraphics);
        scheduler.AddDependency(rolledBackComputeSubmission, rolledBackGraphicsSubmission);
        scheduler.RollbackSubmissionTransaction(transaction);

        Assert.AreEqual(2, engine.ReturnedCommandBufferCount);
        var committedCompute = CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator);
        var committedComputeSubmission = scheduler.Submit(committedCompute);
        Assert.IsGreaterThan(0UL, rolledBackComputeSubmission.FenceValue);
        Assert.IsGreaterThan(rolledBackComputeSubmission.FenceValue, committedComputeSubmission.FenceValue);
        Assert.ThrowsExactly<ArgumentException>(
            () => scheduler.AddDependency(externalGraphicsSubmission, rolledBackComputeSubmission),
            "A rolled-back dependent handle must not alias a later submission at the same pending index.");
        Assert.ThrowsExactly<ArgumentException>(
            () => scheduler.AddDependency(rolledBackComputeSubmission, committedComputeSubmission),
            "A rolled-back producer handle must be rejected before the frame flushes.");

        scheduler.Flush();

        Assert.HasCount(5, MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[0].QueueType);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[0].OpType);
        Assert.AreEqual(CommandQueueType.Compute, MockingCommandQueue.GlobalRecordedOps[2].QueueType);
        Assert.AreEqual(QueueOpType.Wait, MockingCommandQueue.GlobalRecordedOps[2].OpType);
        Assert.AreEqual(externalGraphicsSubmission.FenceValue, MockingCommandQueue.GlobalRecordedOps[2].Value);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[3].OpType);
        Assert.AreEqual(QueueOpType.Signal, MockingCommandQueue.GlobalRecordedOps[4].OpType);
        Assert.AreEqual(committedComputeSubmission.FenceValue, MockingCommandQueue.GlobalRecordedOps[4].Value);
        Assert.AreEqual(4, engine.ReturnedCommandBufferCount);

        MockingCommandQueue.GlobalRecordedOps.Clear();
        var laterGraphics = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var laterGraphicsSubmission = scheduler.Submit(laterGraphics);
        Assert.ThrowsExactly<ArgumentException>(
            () => scheduler.AddDependency(rolledBackComputeSubmission, laterGraphicsSubmission),
            "A rolled-back producer handle must remain invalid after the scheduler generation advances.");
        scheduler.Flush();
        Assert.AreEqual(5, engine.ReturnedCommandBufferCount);
    }

    [TestMethod]
    public void TestSubmissionTransactionMustResolveBeforeFlushAndRejectsStaleToken()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        var transaction = scheduler.BeginSubmissionTransaction(0, 0);

        Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Flush());

        scheduler.RollbackSubmissionTransaction(transaction);
        Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.CommitSubmissionTransaction(transaction));
        scheduler.Flush();
    }

    [TestMethod]
    public void TestForeignSubmissionHandleIsRejected()
    {
        using var engine = new MockingGraphicsEngine();
        using var firstScheduler = new FrameScheduler(engine);
        using var secondScheduler = new FrameScheduler(engine);
        using var firstAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var secondAllocator = engine.CreateCommandAllocator(CommandBufferType.Compute);
        var firstCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, firstAllocator);
        var secondCommandBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, secondAllocator);

        var foreignSubmission = firstScheduler.Submit(firstCommandBuffer);
        var dependentSubmission = secondScheduler.Submit(secondCommandBuffer);

        Assert.ThrowsExactly<ArgumentException>(() => secondScheduler.AddDependency(foreignSubmission, dependentSubmission));

        firstScheduler.Flush();
        secondScheduler.Flush();
    }

    [TestMethod]
    public void TestRecordingCommandBufferIsRejected()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var allocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        var commandBuffer = engine.GetPooledCommandBuffer(CommandBufferType.Graphics);
        commandBuffer.Begin(allocator);

        Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Submit(commandBuffer));
        Assert.IsEmpty(MockingCommandQueue.GlobalRecordedOps);

        Assert.IsTrue(commandBuffer.End().IsSuccess);
        engine.ReturnPooledCommandBuffer(commandBuffer);
    }

    [TestMethod]
    public void TestFlushPendingFollowedByCrossBatchDependencyDoesNotFalseCycle()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var copyAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);

        var copyHandle = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Copy, copyAllocator));
        scheduler.FlushPending();
        MockingCommandQueue.GlobalRecordedOps.Clear();

        var graphicsHandle = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        scheduler.AddDependency(copyHandle, graphicsHandle);

        var completion = scheduler.Flush();

        Assert.AreEqual(1UL, completion.FrameNumber);
        Assert.HasCount(3, MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(QueueOpType.Wait, MockingCommandQueue.GlobalRecordedOps[0].OpType);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[0].QueueType);
        Assert.AreEqual(copyHandle.FenceValue, MockingCommandQueue.GlobalRecordedOps[0].Value);
        Assert.AreEqual(QueueOpType.Submit, MockingCommandQueue.GlobalRecordedOps[1].OpType);
        Assert.AreEqual(QueueOpType.Signal, MockingCommandQueue.GlobalRecordedOps[2].OpType);
    }

    [TestMethod]
    public void TestFlushPendingFollowedByTransitionToNextBatch()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var copyAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);

        var copyHandle = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Copy, copyAllocator));
        scheduler.Transition(CommandQueueType.Copy, CommandQueueType.Graphics);
        scheduler.FlushPending();
        MockingCommandQueue.GlobalRecordedOps.Clear();

        scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        scheduler.Flush();

        Assert.HasCount(3, MockingCommandQueue.GlobalRecordedOps);
        Assert.AreEqual(QueueOpType.Wait, MockingCommandQueue.GlobalRecordedOps[0].OpType);
        Assert.AreEqual(CommandQueueType.Graphics, MockingCommandQueue.GlobalRecordedOps[0].QueueType);
        Assert.AreEqual(copyHandle.FenceValue, MockingCommandQueue.GlobalRecordedOps[0].Value);
    }

    [TestMethod]
    public void TestRepeatedCrossQueueWaitIsSuppressedAcrossFlushes()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var copyAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);

        var copyHandle = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Copy, copyAllocator));
        var gfx0 = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        scheduler.AddDependency(copyHandle, gfx0);
        scheduler.FlushPending();

        var gfx1 = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        scheduler.AddDependency(copyHandle, gfx1);
        scheduler.Flush();

        var waits = MockingCommandQueue.GlobalRecordedOps.Where(op => op.OpType == QueueOpType.Wait).ToArray();
        Assert.HasCount(1, waits, "Destination queue must not re-wait on a fence value already waited on in an earlier batch.");
    }

    [TestMethod]
    public void TestUnresolvedTransitionWithNoPendingSubmissionsThrowsOnFlush()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var copyAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);

        scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Copy, copyAllocator));
        scheduler.FlushPending();
        scheduler.Transition(CommandQueueType.Copy, CommandQueueType.Graphics);

        Assert.ThrowsExactly<InvalidOperationException>(() => scheduler.Flush());
        scheduler.Flush();
    }

    [TestMethod]
    public void TestWaitIdleDoesNotIncrementSubmittedFrame()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);

        scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        scheduler.WaitIdle();

        Assert.AreEqual(0UL, scheduler.SubmittedFrame);
        Assert.AreEqual(1, engine.ReturnedCommandBufferCount);
    }

    [TestMethod]
    public void TestConsecutiveSameQueueSubmissionsBatchIntoSingleSubmitAndSignal()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);

        var cb0 = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var cb1 = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var cb2 = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);

        var h0 = scheduler.Submit(cb0);
        var h1 = scheduler.Submit(cb1);
        var h2 = scheduler.Submit(cb2);
        scheduler.Flush();

        Assert.HasCount(2, MockingCommandQueue.GlobalRecordedOps);
        var submitOp = MockingCommandQueue.GlobalRecordedOps[0];
        var signalOp = MockingCommandQueue.GlobalRecordedOps[1];
        Assert.AreEqual(QueueOpType.Submit, submitOp.OpType);
        Assert.AreEqual(3, submitOp.BatchSize);
        Assert.IsNotNull(submitOp.BatchCommandBufferIds);
        Assert.IsTrue(new ulong[] { (ulong)((MockingCommandBuffer)cb0).InstanceId, (ulong)((MockingCommandBuffer)cb1).InstanceId, (ulong)((MockingCommandBuffer)cb2).InstanceId }.SequenceEqual(submitOp.BatchCommandBufferIds));
        Assert.AreEqual(QueueOpType.Signal, signalOp.OpType);
        Assert.AreEqual(3UL, signalOp.Value);
        Assert.IsTrue(scheduler.IsComplete(h0));
        Assert.IsTrue(scheduler.IsComplete(h1));
        Assert.IsTrue(scheduler.IsComplete(h2));
    }

    [TestMethod]
    public void TestCrossQueueDependencySplitsSameQueueBatch()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = engine.CreateCommandAllocator(CommandBufferType.Compute);

        var gfx0 = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        var gfx1 = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        var comp0 = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator));
        var gfx2 = scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));

        scheduler.AddDependency(gfx0, comp0);
        scheduler.AddDependency(comp0, gfx2);
        scheduler.Flush();

        var ops = MockingCommandQueue.GlobalRecordedOps;
        var gfx0SignalIdx = ops.FindIndex(op => op.QueueType == CommandQueueType.Graphics && op.OpType == QueueOpType.Signal && op.Value == gfx0.FenceValue);
        var compWaitIdx = ops.FindIndex(op => op.QueueType == CommandQueueType.Compute && op.OpType == QueueOpType.Wait && op.Value == gfx0.FenceValue);
        var gfx2WaitIdx = ops.FindIndex(op => op.QueueType == CommandQueueType.Graphics && op.OpType == QueueOpType.Wait && op.Value == comp0.FenceValue);

        Assert.IsGreaterThanOrEqualTo(0, gfx0SignalIdx, "gfx0 must be signaled before comp0 waits on it.");
        Assert.IsLessThan(compWaitIdx, gfx0SignalIdx);
        Assert.IsLessThan(gfx2WaitIdx, compWaitIdx);
    }

    [TestMethod]
    public void TestFreshSchedulerHasInvalidHandlesOnUnusedQueues()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);

        scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));
        var completion = scheduler.Flush();

        Assert.IsTrue(completion.GraphicsSubmission.IsValid);
        Assert.IsFalse(completion.ComputeSubmission.IsValid);
        Assert.IsFalse(completion.CopySubmission.IsValid);
        scheduler.WaitForFrame(in completion);
    }

    private static ICommandBuffer CreateExecutableCommandBuffer(
        MockingGraphicsEngine engine,
        CommandBufferType type,
        ICommandAllocator allocator)
    {
        var commandBuffer = engine.GetPooledCommandBuffer(type);
        commandBuffer.Begin(allocator);
        Assert.IsTrue(commandBuffer.End().IsSuccess);
        return commandBuffer;
    }
}
#endif
