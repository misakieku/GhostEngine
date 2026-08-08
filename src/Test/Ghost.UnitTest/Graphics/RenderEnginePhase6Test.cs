using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;

namespace Ghost.UnitTest.Graphics;

/// <summary>
/// Phase 6 — frame prelude, graph submissions, and presentation epilogue integration.
///
/// Tests verify the ordering contract at the scheduler / queue-operation level without
/// exercising the full threaded RenderEngine.  Each test manually drives the same sequence
/// that RenderEngine.RenderLoop executes:
///
///   Graphics prelude  ─────────────────────────────────────────────────────┐
///   RenderGraph submissions (Graphics + optional Compute)                   │  frame
///   Graphics epilogue (present transitions, depends on terminal Compute)    │
///   IFrameScheduler.Flush                                                   │
///   PresentAll (not modelled here — scheduler-level only)                  ─┘
/// </summary>
#if GHOST_UNITTEST
[TestClass]
[DoNotParallelize]
public class RenderEnginePhase6Test
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

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static ICommandBuffer CreateExecutableCommandBuffer(
        MockingGraphicsEngine engine, CommandBufferType type, ICommandAllocator allocator)
    {
        var cmd = engine.GetPooledCommandBuffer(type);
        cmd.Begin(allocator);
        cmd.End();
        return cmd;
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// When the graph produces a terminal Compute submission the epilogue must Wait for it
    /// before its own Submit so that present-transition barriers execute after all Compute
    /// work has finished.
    /// </summary>
    [TestMethod]
    public void TestPhase6_EpilogueWaitsForTerminalCompute()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = engine.CreateCommandAllocator(CommandBufferType.Compute);

        // --- Prelude ---
        var prelude = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        scheduler.Submit(prelude);

        // --- Graph: single Compute segment (terminal) ---
        var computeBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator);
        var computeHandle = scheduler.Submit(computeBuffer);

        // --- Epilogue (what RenderEngine does) ---
        var epilogue = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var epilogueHandle = scheduler.Submit(epilogue);

        // RenderEngine adds explicit dependency when ComputeSubmission.IsValid
        scheduler.AddDependency(computeHandle, epilogueHandle);

        scheduler.Flush();

        var allOps = MockingCommandQueue.GlobalRecordedOps.ToList();
        var graphicsOps = allOps.Where(op => op.QueueType == CommandQueueType.Graphics).ToList();
        var computeOps = allOps.Where(op => op.QueueType == CommandQueueType.Compute).ToList();

        // Compute queue: Submit + Signal only — no waits needed
        Assert.AreEqual(2, computeOps.Count);
        Assert.AreEqual(QueueOpType.Submit, computeOps[0].OpType);
        Assert.AreEqual(QueueOpType.Signal, computeOps[1].OpType);

        // Graphics queue: Submit(prelude) + Signal, then Wait(ComputeFence) + Submit(epilogue) + Signal
        Assert.AreEqual(5, graphicsOps.Count);
        Assert.AreEqual(QueueOpType.Submit, graphicsOps[0].OpType);  // prelude submit
        Assert.AreEqual(QueueOpType.Signal, graphicsOps[1].OpType);  // prelude signal
        Assert.AreEqual(QueueOpType.Wait, graphicsOps[2].OpType);    // epilogue waits for Compute
        Assert.AreEqual(QueueOpType.Submit, graphicsOps[3].OpType);  // epilogue submit
        Assert.AreEqual(QueueOpType.Signal, graphicsOps[4].OpType);  // epilogue signal

        // The Wait value must match the Compute signal value
        Assert.AreEqual(computeOps[1].Value, graphicsOps[2].Value);
    }

    /// <summary>
    /// When the graph produces no Compute submissions (ForceGraphics fallback or empty graph)
    /// the epilogue must not insert any Wait ops — FIFO ordering on the Graphics queue is sufficient.
    /// </summary>
    [TestMethod]
    public void TestPhase6_GraphicsOnlyFrameEpilogueHasNoComputeWait()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);

        // Prelude
        var prelude = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        scheduler.Submit(prelude);

        // Graph: Graphics-only segment (ForceGraphics / empty graph path)
        var graphBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        scheduler.Submit(graphBuffer);

        // Epilogue — ComputeSubmission is invalid so RenderEngine adds no dependency
        var epilogue = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        scheduler.Submit(epilogue);

        scheduler.Flush();

        // No Wait ops anywhere — all Graphics, all FIFO
        Assert.IsFalse(MockingCommandQueue.GlobalRecordedOps.Any(op => op.OpType == QueueOpType.Wait));
        Assert.IsTrue(MockingCommandQueue.GlobalRecordedOps.All(op => op.QueueType == CommandQueueType.Graphics));

        // Three rounds of Submit + Signal
        var ops = MockingCommandQueue.GlobalRecordedOps.ToList();
        Assert.AreEqual(6, ops.Count);
        for (var i = 0; i < 3; i++)
        {
            Assert.AreEqual(QueueOpType.Submit, ops[i * 2].OpType);
            Assert.AreEqual(QueueOpType.Signal, ops[i * 2 + 1].OpType);
        }
    }

    /// <summary>
    /// Streaming Copy submissions submitted by the resource streaming processor through
    /// the scheduler coexist with prelude, graph Compute, and epilogue without ordering
    /// conflicts.
    /// </summary>
    [TestMethod]
    public void TestPhase6_StreamingCopyCoexistsWithPreludeComputeAndEpilogue()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = engine.CreateCommandAllocator(CommandBufferType.Compute);
        using var copyAllocator = engine.CreateCommandAllocator(CommandBufferType.Copy);

        // Streaming Copy (submitted by ResourceStreamingProcessor before the prelude ends)
        var copyBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Copy, copyAllocator);
        scheduler.Submit(copyBuffer);

        // Prelude
        var prelude = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        scheduler.Submit(prelude);

        // Graph: Compute segment
        var computeBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator);
        var computeHandle = scheduler.Submit(computeBuffer);

        // Epilogue
        var epilogue = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var epilogueHandle = scheduler.Submit(epilogue);
        scheduler.AddDependency(computeHandle, epilogueHandle);

        scheduler.Flush();

        var allOps = MockingCommandQueue.GlobalRecordedOps.ToList();
        var graphicsOps = allOps.Where(op => op.QueueType == CommandQueueType.Graphics).ToList();
        var computeOps = allOps.Where(op => op.QueueType == CommandQueueType.Compute).ToList();
        var copyOps = allOps.Where(op => op.QueueType == CommandQueueType.Copy).ToList();

        // Copy queue: Submit + Signal
        Assert.AreEqual(2, copyOps.Count);
        Assert.AreEqual(QueueOpType.Submit, copyOps[0].OpType);
        Assert.AreEqual(QueueOpType.Signal, copyOps[1].OpType);

        // Compute queue: Submit + Signal
        Assert.AreEqual(2, computeOps.Count);
        Assert.AreEqual(QueueOpType.Submit, computeOps[0].OpType);
        Assert.AreEqual(QueueOpType.Signal, computeOps[1].OpType);

        // Graphics queue: Submit+Signal (prelude), Wait+Submit+Signal (epilogue)
        Assert.AreEqual(5, graphicsOps.Count);
        Assert.AreEqual(QueueOpType.Submit, graphicsOps[0].OpType);
        Assert.AreEqual(QueueOpType.Signal, graphicsOps[1].OpType);
        Assert.AreEqual(QueueOpType.Wait, graphicsOps[2].OpType);
        Assert.AreEqual(QueueOpType.Submit, graphicsOps[3].OpType);
        Assert.AreEqual(QueueOpType.Signal, graphicsOps[4].OpType);

        // Epilogue's Wait value matches Compute Signal value
        Assert.AreEqual(computeOps[1].Value, graphicsOps[2].Value);
    }

    /// <summary>
    /// A full frame with prelude, Graphics producer, Compute segment, Graphics join, and epilogue
    /// produces the correct cross-queue wait pattern: Graphics join waits for Compute, epilogue
    /// waits for terminal Compute.
    /// </summary>
    [TestMethod]
    public void TestPhase6_FullGraphicsComputeGraphicsFrameOrdering()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = engine.CreateCommandAllocator(CommandBufferType.Compute);

        // Prelude
        var prelude = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        scheduler.Submit(prelude);

        // Graph: Graphics producer → Compute → Graphics join
        var graphProducer = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var graphProducerHandle = scheduler.Submit(graphProducer);

        var computeBuffer = CreateExecutableCommandBuffer(engine, CommandBufferType.Compute, computeAllocator);
        var computeHandle = scheduler.Submit(computeBuffer);
        scheduler.AddDependency(graphProducerHandle, computeHandle); // Graphics → Compute

        var graphJoin = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var graphJoinHandle = scheduler.Submit(graphJoin);
        scheduler.AddDependency(computeHandle, graphJoinHandle); // Compute → Graphics join

        // Epilogue depends on terminal Compute
        var epilogue = CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator);
        var epilogueHandle = scheduler.Submit(epilogue);
        scheduler.AddDependency(computeHandle, epilogueHandle);

        scheduler.Flush();

        var allOps = MockingCommandQueue.GlobalRecordedOps.ToList();
        var graphicsOps = allOps.Where(op => op.QueueType == CommandQueueType.Graphics).ToList();
        var computeOps = allOps.Where(op => op.QueueType == CommandQueueType.Compute).ToList();

        // Compute queue: Wait(Graphics producer fence) + Submit + Signal
        Assert.AreEqual(3, computeOps.Count);
        Assert.AreEqual(QueueOpType.Wait, computeOps[0].OpType);
        Assert.AreEqual(QueueOpType.Submit, computeOps[1].OpType);
        Assert.AreEqual(QueueOpType.Signal, computeOps[2].OpType);

        // Graphics queue has two Wait ops: one for the join, one for the epilogue
        var graphicsWaits = graphicsOps.Where(op => op.OpType == QueueOpType.Wait).ToList();
        Assert.AreEqual(2, graphicsWaits.Count);

        // Both waits are for the Compute signal value
        var computeSignalValue = computeOps[2].Value;
        Assert.IsTrue(graphicsWaits.All(w => w.Value == computeSignalValue));

        // PresentAll always comes after Flush: no Compute ops after epilogue signal
        var lastComputeIndex = allOps.FindLastIndex(op => op.QueueType == CommandQueueType.Compute);
        var epilogueSignalIndex = allOps.FindLastIndex(op => op.QueueType == CommandQueueType.Graphics && op.OpType == QueueOpType.Signal);
        Assert.IsGreaterThan(lastComputeIndex, epilogueSignalIndex);
    }

    /// <summary>
    /// When both GraphicsSubmission and ComputeSubmission are invalid (empty graph),
    /// submitting the epilogue with no dependency and flushing succeeds cleanly.
    /// </summary>
    [TestMethod]
    public void TestPhase6_EmptyGraphProducesPreludeAndEpilogueOnly()
    {
        using var engine = new MockingGraphicsEngine();
        using var scheduler = new FrameScheduler(engine);
        using var graphicsAllocator = engine.CreateCommandAllocator(CommandBufferType.Graphics);

        // Prelude
        scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));

        // No graph submissions (empty graph returns default RGExecution with invalid handles)

        // Epilogue — ComputeSubmission.IsValid == false, so no dependency added
        scheduler.Submit(CreateExecutableCommandBuffer(engine, CommandBufferType.Graphics, graphicsAllocator));

        var completion = scheduler.Flush();

        Assert.IsTrue(completion.IsValid);
        Assert.IsFalse(MockingCommandQueue.GlobalRecordedOps.Any(op => op.OpType == QueueOpType.Wait));
        Assert.AreEqual(4, MockingCommandQueue.GlobalRecordedOps.Count); // 2 × (Submit + Signal)
    }
}
#endif
