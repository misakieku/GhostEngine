# Historical FrameScheduler Design Draft

- **Date:** 2026-08-04
- **Status:** Obsolete and superseded
- **Context:** Historical pre-implementation exploration for GhostEngine render graph Step 8

> **Do not implement this draft.** Its `void Submit`, `Reset`, shared-fence alternatives, internal batch grouping,
> raw `SubmitQueue`/`SignalFence`/`GPUWait` activation, and projected file locations do not match the accepted scheduler.
> Read [the current handoff context](render_graph_sync_point_command_buffer_integration_context.md),
> [the accepted integration plan](render_graph_sync_point_command_buffer_integration_plan.md), and the implemented
> [`IFrameScheduler`](../../../src/Runtime/Ghost.Graphics/FrameScheduling/IFrameScheduler.cs) instead. The native
> `ICommandBuffer` is the submission node; no `CompiledQueueBatch` or equivalent render-graph submission object is permitted.

The remainder of this file is retained only as historical decision context.

---

## Background

Steps 1–7 of the render-graph remediation produced a correct, well-tested CPU compilation pipeline. All passes currently execute on the Graphics/Direct queue. Step 8 introduces real async compute scheduling and cross-producer GPU synchronization.

Two alternatives were considered and rejected before this design:

| Alternative | Problem |
| --- | --- |
| Software command buffer (Unreal/Unity style) | Massive rewrite; only needed for multi-backend or RHI thread. GhostEngine is D3D12-only, single-threaded submission. |
| External frame scheduler owns all submission | With thin native command buffers the scheduler cannot reorder commands — it reduces to pure indirection with no value unless scoped correctly. |

The design here keeps thin native `ICommandBuffer` wrappers, keeps recording explicit, and introduces a `IFrameScheduler` whose sole responsibility is to hide fence-value management and batch command-buffer submission across independent work producers.

---

## Design Goals

1. **Hide fence values from all call sites.** No producer (render graph, streaming, AS builder) manages raw fence counter values.
2. **Schedule at `ICommandBuffer` granularity.** The unit of scheduling is a fully recorded, closed command buffer — not individual commands.
3. **Coordinate independent producers.** Streaming uploads, render graph passes, and async compute passes each submit through the same scheduler without knowing about each other's fence values.
4. **Single real submission point per frame.** `Flush()` is the only place `ExecuteCommandLists` is called. Before that, submissions are queued as entries in the scheduler.
5. **No new abstraction over recording.** `ICommandBuffer` remains a thin native wrapper; callers record and close it themselves before passing to the scheduler.

---

## Core Interface

```csharp
/// <summary>
/// Coordinates command buffer submission and cross-queue fence synchronization
/// for a single frame. Call Submit/Transition freely during the frame, then
/// call Flush once at the end to issue all GPU work in dependency order.
/// </summary>
public interface IFrameScheduler
{
    /// <summary>
    /// Enqueues a fully recorded, closed command buffer for execution.
    /// The target queue is inferred from commandBuffer.Type.
    /// The caller is responsible for calling End() before Submit().
    /// </summary>
    void Submit(ICommandBuffer commandBuffer);

    /// <summary>
    /// Inserts a cross-queue synchronization edge: all work previously submitted
    /// to the <paramref name="from"/> queue must complete on the GPU before any
    /// work subsequently submitted to the <paramref name="to"/> queue begins.
    /// The scheduler owns the fence value; callers never see it.
    /// </summary>
    void Transition(CommandQueueType from, CommandQueueType to);

    /// <summary>
    /// Resolves all pending submissions and transitions into a dependency-ordered
    /// sequence of ExecuteCommandLists + Signal + Wait calls, then issues them.
    /// Returns per-queue fence values that callers use to recycle command allocators.
    /// Must be called exactly once per frame, after all Submit/Transition calls.
    /// </summary>
    FrameCompletionInfo Flush();

    /// <summary>
    /// Resets scheduler state for the next frame. Call after GPU work from
    /// the previous frame is confirmed complete (or after the allocator ring
    /// buffer has advanced past the recycled frame).
    /// </summary>
    void Reset();
}

/// <summary>
/// Per-queue fence values at the point Flush() issued GPU work.
/// Store these and check them against queue.GetCompletedValue() before
/// resetting command allocators from this frame.
/// </summary>
public readonly struct FrameCompletionInfo
{
    public readonly ulong GraphicsFenceValue;
    public readonly ulong ComputeFenceValue;
    public readonly ulong CopyFenceValue;
}
```

---

## Internal Data Model

The scheduler builds two lists as `Submit` and `Transition` are called:

```text
_entries: List<SchedulerEntry>
_transitions: List<TransitionEdge>

struct SchedulerEntry
{
    ICommandBuffer commandBuffer;
    CommandQueueType queue;          // from commandBuffer.Type
    int sequenceIndex;               // insertion order
}

struct TransitionEdge
{
    CommandQueueType from;
    CommandQueueType to;
    int afterSequenceIndex;          // index of last Submit before this Transition
}
```

A `Transition(from, to)` call records an edge at the current sequence position. `Flush` uses this to determine:

- Where to insert `queue.Signal(fence, value)` calls
- Where to insert `queue.Wait(fence, value)` calls before the next batch on `to`

---

## Flush Algorithm

```text
Flush():
  1. Group consecutive same-queue SchedulerEntries into batches.
     A batch boundary occurs at every TransitionEdge.

  2. For each batch:
     a. If this batch has any incoming TransitionEdge (wait dependency):
        - The source queue's batch must be submitted first.
        - Insert: sourceQueue.Signal(sharedFence, ++fenceValue)
        - Insert: destinationQueue.Wait(sharedFence, fenceValue)
     b. Call destinationQueue.ExecuteCommandLists(batch.commandBuffers)

  3. After all batches: signal each active queue with its final fence value.

  4. Return FrameCompletionInfo with per-queue final fence values.
```

A single shared `IFence` per queue pair is sufficient for D3D12 (timeline fence). Alternatively one global timeline fence with monotonically increasing values.

---

## A Frame in Practice

```csharp
// ── Streaming manager (Copy queue) ─────────────────────────────────────
streamingCmd.Begin(copyAllocator);
// ... record texture uploads ...
streamingCmd.End();
frameScheduler.Submit(streamingCmd);

// Signal: copy queue must finish before graphics reads uploaded textures
frameScheduler.Transition(CommandQueueType.Copy, CommandQueueType.Graphics);

// ── Async compute (Compute queue) ──────────────────────────────────────
cullingCmd.Begin(computeAllocator);
// ... record GPU culling / AS build ...
cullingCmd.End();
frameScheduler.Submit(cullingCmd);

// Signal: compute must finish before graphics consumes culling results
frameScheduler.Transition(CommandQueueType.Compute, CommandQueueType.Graphics);

// ── Render graph executor (Graphics queue) ─────────────────────────────
// RenderGraphExecutor.Execute() records the compiled graph command stream.
// At each SubmitQueue opcode boundary the executor calls:
//   graphicsCmd.End();
//   frameScheduler.Submit(graphicsCmd);
//   // begin new graphicsCmd for next batch
// At each SignalFence+GPUWait opcode pair the executor calls:
//   frameScheduler.Transition(srcQueue, dstQueue);
graphicsCmd.Begin(graphicsAllocator);
renderGraph.Execute(frameScheduler, graphicsCmd, context);
// Execute() ends and submits graphicsCmd internally via frameScheduler

// ── Engine main loop — end of frame ───────────────────────────────────
var completion = frameScheduler.Flush();

// Store completion values; recycle allocators once GPU catches up
allocatorRing.MarkFrame(frameIndex, completion);
frameScheduler.Reset();
```

---

## Allocator Lifecycle

Command allocators must not be reset until the GPU has finished all command lists recorded through them. `FrameCompletionInfo` provides the fence values to check.

```text
Ring buffer: allocator[frameIndex % FRAMES_IN_FLIGHT]

Start of frame N:
  - Check: graphicsQueue.GetCompletedValue() >= completion[N - FRAMES_IN_FLIGHT].GraphicsFenceValue
  - If yes: allocator[N % FRAMES_IN_FLIGHT].Reset()   ← safe
  - Same check for compute and copy allocators
```

`FRAMES_IN_FLIGHT` is typically 2 (double-buffer) or 3 (triple-buffer).

---

## Render Graph Executor Integration

The executor already has `SignalFence`, `SubmitQueue`, and `GPUWait` opcodes in its switch statement (emitted by the compiler but suppressed in Step 2). In Step 8 these are re-enabled and mapped to scheduler calls:

| Opcode | Current behavior (Step 2) | Step 8 behavior |
| --- | --- | --- |
| `SubmitQueue` | Dead code | `activeCmd.End(); frameScheduler.Submit(activeCmd); begin new cmd` |
| `SignalFence` | Dead code | `frameScheduler.Transition(srcQueue, ...)` (paired with GPUWait) |
| `GPUWait` | Dead code | Consumed with the paired `SignalFence`; no separate call needed |

The executor receives `IFrameScheduler` as a parameter alongside `ICommandBuffer`. The caller (engine frame loop) owns both.

---

## External Producer Integration

Any system that produces GPU work outside the render graph follows the same two-call pattern:

```csharp
cmd.End();
frameScheduler.Submit(cmd);
frameScheduler.Transition(CommandQueueType.Copy, CommandQueueType.Graphics); // if needed
```

No fence values, no queue handles, no coordination with the render graph required. The scheduler resolves all dependencies at `Flush` time.

---

## Step 8 Implementation Sequence

1. **Define `IFrameScheduler`, `FrameCompletionInfo`, and `FrameScheduler` (concrete class).**  
   Unit-testable with mock queues and fences. No render graph changes yet.

2. **Wire `FrameScheduler` into the engine frame loop.**  
   Replace direct `queue.Submit` calls with `frameScheduler.Submit`. Verify single-queue (Graphics-only) behavior is identical to before.

3. **Add `IFrameScheduler` parameter to `RenderGraphExecutor.Execute`.**  
   Activate `SubmitQueue`/`SignalFence`/`GPUWait` opcode handling.

4. **Enable async-compute pass emission in `RenderGraphCompiler`.**  
   Compiler emits `SubmitQueue` + `SignalFence`/`GPUWait` at queue boundaries derived from pass eligibility metadata (already hashed from Step 1).

5. **Add `FrameScheduler` unit tests.**  
   Test single-queue flush, copy→graphics transition, compute→graphics transition, and multi-transition ordering. Use mock `ICommandQueue` and `IFence`.

6. **Integrate streaming manager.**  
   Streaming upload manager calls `frameScheduler.Submit` and `frameScheduler.Transition` instead of direct queue submission.

7. **Measure and verify.**  
   - Confirm frame timing and fence values are correct under PIX/RenderDoc.
   - Confirm allocator recycling does not race with GPU.
   - Run all existing render-graph tests with `FrameScheduler` wired in.

---

## Open Questions

- **Single shared fence vs. per-queue-pair fences:** A single global timeline fence with monotonically increasing values is simplest. Per-pair fences allow more parallelism but require more bookkeeping. Decide at implementation time.
- **Mid-frame Transition validation:** Under `GHOST_SAFETY_CHECKS`, assert that a `Transition(from, to)` is not issued after `Flush()` has been called, and that `Submit` is not called with a non-closed command buffer.
- **Present synchronization:** The swap-chain present call needs the graphics queue to have finished the final back-buffer pass. `FrameCompletionInfo.GraphicsFenceValue` provides this; the present wrapper checks it or the engine stalls appropriately.
- **Multiple transitions to the same destination:** If both Copy→Graphics and Compute→Graphics transitions are declared, the graphics queue must wait for both. The flush algorithm must accumulate all incoming wait values per batch and issue all waits before that batch's `ExecuteCommandLists`.

---

## Files Affected (Projected)

| File | Change |
| --- | --- |
| `src/Runtime/Ghost.Graphics.RHI/IFrameScheduler.cs` | New interface |
| `src/Runtime/Ghost.Graphics.RHI/FrameCompletionInfo.cs` | New struct |
| `src/Runtime/Ghost.Graphics.D3D12/D3D12FrameScheduler.cs` | New concrete implementation |
| `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphExecutor.cs` | Accept `IFrameScheduler`; activate queue opcodes |
| `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphCompiler.cs` | Re-enable async-compute pass emission |
| `src/Runtime/Ghost.Engine/RenderPipeline.cs` (or equivalent frame loop) | Replace direct queue submission with scheduler |
| `src/Test/Ghost.UnitTest/Graphics/FrameSchedulerTest.cs` | New unit tests |
