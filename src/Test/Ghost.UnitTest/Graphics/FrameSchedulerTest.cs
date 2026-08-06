using Ghost.Graphics.FrameScheduling;
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
