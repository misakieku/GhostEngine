# Render Graph Sync-Point Command Buffer Integration Context

Date: 2026-08-04

Status: Planning handoff. The minimal frame scheduler is implemented and verified. Full render-graph synchronization-point integration has not started.

## Start Here In A New Session

Read these files in order:

1. This context file.
2. [Render graph sync-point command-buffer integration plan](render_graph_sync_point_command_buffer_integration_plan.md).
3. [Render graph architecture](../../developer-docs/render_graph_architecture.md).
4. [Earlier remediation review](../2026-08-03/render_graph_barrier_async_compute_remediation_review.md), while observing the stale-guidance warning below.
5. [Step 7 CPU verification report](../render_graph_step7_cpu_verification_report.md).

Historical warning: [frame_scheduler_design.md](frame_scheduler_design.md) is the obsolete pre-implementation scheduler draft.
Do not use its old `void Submit`, `Reset`, shared-fence alternatives, raw queue-opcode activation, or projected file locations as
current guidance. The implemented API and the new integration plan supersede it.

Repository root:

```text
F:/csharp/GhostEngine
```

Solution:

```text
src/GhostEngine.slnx
```

Authoritative build and test environment: Windows x64. WSL failures involving Windows targets, apphosts, NuGet fallback folders,
or Windows paths are environmental and are not authoritative for this repository.

## Critical Architecture Correction

Do not introduce `CompiledQueueBatch`, `RenderGraphQueueBatch`, `QueueSegment`, or an equivalent render-graph-owned submission object.

The native `ICommandBuffer` is already the scheduler's unit of work and dependency node. The intended flow is:

```text
Render-graph pass DAG
    -> structural synchronization points in the existing binary command stream
    -> multiple native ICommandBuffer instances created while executing that stream
    -> IFrameScheduler.Submit(commandBuffer)
    -> SubmissionHandle dependencies
    -> IFrameScheduler.Flush()
```

The render graph may keep execution-time scratch storage containing the actual native command buffers and returned submission
handles. It must not compile or cache a second abstraction that duplicates a command buffer's queue affinity, submission identity,
dependencies, or lifetime.

The earlier remediation review contains a stale `CompiledQueueBatch` proposal in section 14.2. That proposal is explicitly rejected
and must not be implemented. The phrase "queue batch" elsewhere in older documents should be interpreted as "one submitted native
command buffer," not as a new compiled graph type.

The same directory also contains `frame_scheduler_design.md`, an obsolete draft written before the scheduler implementation. Its
interface, fence alternatives, reset model, queue-opcode activation, and projected file locations do not describe the accepted
implementation. Use the actual `IFrameScheduler` source and this plan instead.

## Goal

Enable dependency-aware Graphics/Compute overlap by making the render graph split its existing binary command stream at
synchronization points, record each resulting region into a separate native command buffer, end every command buffer, and submit
those command buffers through the frame scheduler.

`EnableAsyncCompute(true)` remains eligibility metadata. It does not guarantee Compute-queue execution. Work without a legal and useful overlap window remains on Graphics.

## Mandatory Responsibility Split

### Render graph owns

- Pass declarations and canonical resource usage.
- RAW, WAR, WAW, side-effect, and explicit pass dependencies.
- Pass culling and legal pass reordering.
- Resource states and barrier generation.
- Native render-pass merging.
- Transient resource lifetimes and aliasing correctness.
- Finding Graphics/Compute launch and join synchronization points.
- Emitting structural synchronization markers into the existing binary command stream.
- Acquiring, recording, and ending the native command buffers created from those markers.
- Declaring dependencies between the resulting `SubmissionHandle` values.

### Frame scheduler owns

- Deferred native submission of complete `ICommandBuffer` instances.
- Queue selection from immutable `ICommandBuffer.Type`.
- Same-queue FIFO ordering.
- Cross-queue waits and signals.
- One producer-owned fence timeline per physical queue.
- Monotonic fence values.
- Opaque submission completion tracking.
- Frame completion and CPU waits.
- Pending-submission DAG validation.
- Returning scheduler-owned submitted command buffers to the graphics-engine pool.

### Render graph must not own

- Native `ICommandQueue` calls.
- Native `IFence` values or fence allocation.
- Cached `SubmissionHandle` values.
- Cached absolute synchronization values.
- Scheduler topological ordering.
- A parallel compiled batch/submission model.

## Scheduler Contract Already Implemented

API: [IFrameScheduler.cs](../../../src/Runtime/Ghost.Graphics/FrameScheduling/IFrameScheduler.cs)

Implementation: [FrameScheduler.cs](../../../src/Runtime/Ghost.Graphics/Services/FrameScheduler.cs)

Opaque handles: [SubmissionHandle.cs](../../../src/Runtime/Ghost.Graphics/FrameScheduling/SubmissionHandle.cs)

Frame completion: [FrameCompletionInfo.cs](../../../src/Runtime/Ghost.Graphics/FrameScheduling/FrameCompletionInfo.cs)

Current operations:

```csharp
SubmissionHandle Submit(ICommandBuffer commandBuffer);
void AddDependency(SubmissionHandle producer, SubmissionHandle dependent);
void Transition(CommandQueueType source, CommandQueueType destination);
bool IsComplete(SubmissionHandle submission);
FrameCompletionInfo Flush();
void WaitForFrame(scoped in FrameCompletionInfo completion);
void WaitIdle();
```

Important semantics:

- `Submit` is deferred. Native queues are not touched until `Flush`.
- Ownership of a submitted command buffer transfers to the scheduler.
- The scheduler rejects command buffers that are still recording.
- Same-queue submissions receive implicit FIFO dependencies.
- `Transition(source, destination)` means the latest source submission must precede the next destination submission.
- `AddDependency` expresses an exact dependency between two returned handles.
- Foreign handles, stale dependent handles, self-dependencies, cycles, and unresolved transitions are rejected.
- The scheduler precomputes the complete topological order before any native submission.
- A failed `Flush` returns unsubmitted pooled command buffers and restores the last successfully flushed queue handles.
- Critical scheduler validation is active in Release, not only safety builds.
- Scheduler operation is render-thread-only.

Current physical topology is one Graphics queue, one Compute queue, and one Copy queue. Full render-graph integration initially
targets Graphics and Compute. Streaming continues to own Copy command recording and uses the same scheduler independently.

## Completed Work

The earlier remediation work completed the following before the scheduler phase:

- Conservative containment of async-eligible compute passes on Graphics.
- Canonical resource declarations preserving read/write dependency memberships.
- One effective barrier usage per pass/resource.
- Correct UAV `ReadWrite` handling.
- Attachment/UAV conflict absorption and stable barrier ordering.
- Transient alias integration and exact fresh/cache restoration.
- Immediate, disposal-time, and compiler-backstop validation in safety builds.
- Plain Release removal of dedicated render-graph validation.
- Command-stream, cache, aliasing, native-pass merge, and validation tests.
- Shipping compile benchmarks with allocation-free declaration and warm-cache paths.

The minimal scheduler phase then completed:

- Device-lifetime frame scheduler with frame-scoped pending DAG state.
- Deferred Graphics/Compute/Copy command-buffer submission.
- Per-queue fence timelines and frame completion tracking.
- RenderEngine integration and one frame allocator per queue per in-flight frame.
- Streaming migration from `AsyncCopyPipeline` to `IFrameScheduler`.
- Removal of `AsyncCopyPipeline.cs`.
- Failure-path hardening for scheduler cycles and failed streaming command-buffer closure.
- Debug-only queue-operation instrumentation under `GHOST_UNITTEST`.

## Current Render-Graph Behavior

Compiler: [RenderGraphCompiler.cs](../../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphCompiler.cs)

- `Compile` currently culls passes and builds a linear `compiledPasses` list.
- `ReorderPasses` exists but its call is currently commented out.
- `BuildExecutionCommands` emits barriers, native-pass boundaries, and pass callbacks into one binary stream.
- No queue synchronization opcodes are currently emitted.
- Async-eligible Compute callbacks execute on the Graphics command buffer.

Execution opcodes: [RenderGraphTypes.cs](../../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphTypes.cs)

`RGExecutionOpType` still declares the old `GPUWait`, `SignalFence`, and `SubmitQueue` values. They are not currently emitted. They
represent the invalid pre-containment model and must be removed or replaced by structural command-buffer synchronization markers.
They must not be revived as raw queue/fence operations.

Executor: [RenderGraphExecutor.cs](../../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphExecutor.cs)

- `Execute` still accepts two command buffers, two native queues, and one fence.
- It records normal pass/barrier operations into one active command buffer.
- It retains dead cases for `GPUWait`, `SignalFence`, and `SubmitQueue`.
- The full integration must remove direct queue/fence parameters and make the executor acquire multiple command buffers through `IGraphicsEngine`.

Public execution entry: [RenderGraph.cs](../../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraph.cs)

- `CompileAndExecute` still accepts caller-provided Graphics/Compute command buffers, queues, and a fence.
- Its signature and ownership model must change during execution integration.
- Compilation caching currently stores the binary command bytes and no absolute fence values.

Compilation cache: [RenderGraphCompilationCache.cs](../../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphCompilationCache.cs)

- Cache parity is already heavily tested.
- Structural sync markers may be cached as command bytes.
- Native command buffers, scheduler handles, and fence values must never enter the cache.

Aliasing: [RenderGraphAliasing.cs](../../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphAliasing.cs)

- Current lifetimes are based on the contained linear schedule.
- Full async integration must prevent aliasing when resource lifetimes may overlap across queues.
- A conservative no-alias decision is preferred when cross-queue happens-before cannot be proven.

Outer frame integration: [RenderEngine.cs](../../../src/Runtime/Ghost.Graphics/RenderEngine.cs)

- RenderEngine currently records one main Graphics command buffer.
- Streaming finalization, render-pipeline commands, and present transitions are recorded into that buffer.
- The main command buffer is submitted to `IFrameScheduler`, followed by one `Flush`, then presentation.
- Full integration must define a Graphics prelude, render-graph-created command buffers, and a Graphics presentation epilogue while retaining one frame-level `Flush` before `PresentAll`.

Streaming integration: [ResourceStreamingProcessor.cs](../../../src/Runtime/Ghost.Engine/Streaming/ResourceStreamingProcessor.cs)

- Streaming records Copy uploads into a pooled Copy command buffer.
- It submits through the frame scheduler and retains an opaque `SubmissionHandle`.
- This remains outside render-graph scheduling.

## Agreed Sync-Point Direction

The only new compiled scheduling information belongs in the existing binary command stream as structural command-buffer boundary metadata.

A synchronization boundary must be able to express:

- End the current native command buffer.
- Assign that command buffer an implicit relative ID based on command-stream order.
- Select the type of the next native command buffer.
- List zero or more earlier relative command-buffer IDs that the next command buffer depends on.

Relative IDs exist only as integer payloads in the binary command stream. During execution, reusable scratch maps each relative ID
to the `SubmissionHandle` returned for the actual native command buffer. The executor declares exact dependencies with
`AddDependency` after the destination command buffer is submitted. No compiled object is created for the ID.

`Transition(source, destination)` remains available for external latest-source/next-destination boundaries. Render-graph
synchronization should prefer exact inline producer IDs so a consumer never waits on unrelated later work from the same source
queue.

A fork/join schedule should materialize as native command buffers, for example:

```text
Graphics command buffer 0: producer
Compute command buffer 0: async-eligible compute
Graphics command buffer 1: independent graphics
Graphics command buffer 2: consumer/join
```

Required dependencies:

```text
Graphics 0 -> Compute 0
Compute 0  -> Graphics 2
```

Graphics queue FIFO supplies:

```text
Graphics 0 -> Graphics 1 -> Graphics 2
```

There is no separate compiled object representing those four submissions. The four native command buffers are the submissions.

## Failure Atomicity Requirement

Command-buffer acquisition, `Begin()`, pass callbacks, barrier/native-pass recording, and command-buffer `End()` may fail. The
render graph must not leave a partially submitted frame merely because an error occurred while recording a later command buffer.

Preferred execution shape:

1. Scan the command stream and record every native command buffer created by sync points.
2. End every recorded command buffer successfully.
3. If initial or post-marker acquisition/`Begin`, recording, or `End` fails, return all acquired buffers exactly once and submit none.
4. Only after successful recording, submit those command buffers to the scheduler and declare their dependencies.
5. Let the outer frame perform the single `Flush`.

Execution-time scratch storage containing actual `ICommandBuffer` references and `SubmissionHandle` values is allowed. It must be
cleared every execution and must not become compilation output or cache data.

If scheduler ownership transfer can fail after the first successful `Submit`, the implementation must provide an explicit rollback
path before claiming transactional behavior. Do not silently rely on a later frame to flush or clean partial pending work.

## Configuration And Validation Rules

- `Debug`, `Debug_Editor`, `Release_Dev`, and `Release_Editor` retain `GHOST_SAFETY_CHECKS`.
- Plain `Release` intentionally compiles dedicated render-graph usage validation out.
- Scheduler ownership, handle, generation, recording-state, transition, and DAG validation remains enabled in all configurations.
- `GlobalRecordedOps` and every test referencing it remain under `GHOST_UNITTEST`.
- Benchmarks must not use `RGExecutionFlags.GenerateDump`.
- Benchmark comparisons use BenchmarkDotNet `DefaultJob`.
- Preserve the binary command-stream architecture.
- Preserve AOT compatibility and avoid per-frame managed allocations in warm execution paths.

## Verification Baseline Before Full Integration

Latest authoritative Windows results:

- Frame scheduler tests: 9 passed, 0 failed.
- Render-graph tests: 22 passed, 0 failed.
- Full Debug unit suite: 92 passed, 0 failed.
- Debug x64 solution build: 0 errors.
- Release x64 solution build: 0 errors.
- Project-aware LSP diagnostics: clean for scheduler integration files.
- Independent scheduler review: no blocker or high-severity findings.

Commands, run from `src/` in Windows Git Bash:

```shell
dotnet test --project Test/Ghost.UnitTest/Ghost.UnitTest.csproj -c Debug -p:Platform=x64 --no-restore --filter "ClassName~FrameScheduler"
dotnet test --project Test/Ghost.UnitTest/Ghost.UnitTest.csproj -c Debug -p:Platform=x64 --no-restore --filter "ClassName~RenderGraph"
dotnet test --project Test/Ghost.UnitTest/Ghost.UnitTest.csproj -c Debug -p:Platform=x64 --no-restore
dotnet build GhostEngine.slnx -c Debug -p:Platform=x64 --no-restore
dotnet build GhostEngine.slnx -c Release -p:Platform=x64 --no-restore
```

Shipping benchmark baseline from the prior phase:

- Declaration: 2.500 us, 0 B/op.
- Cold compile: 16.339 us, 1,288 B/op.
- Warm compile: 6.569 us, 0 B/op.

## Test Files To Extend

- [FrameSchedulerTest.cs](../../../src/Test/Ghost.UnitTest/Graphics/FrameSchedulerTest.cs)
- [RenderGraphTest.cs](../../../src/Test/Ghost.UnitTest/Graphics/RenderGraphTest.cs)
- [RenderGraphValidationTest.cs](../../../src/Test/Ghost.UnitTest/Graphics/RenderGraphValidationTest.cs)
- [MockingCommandBuffer.cs](../../../src/Test/Ghost.UnitTest/MockingEnvironment/MockingCommandBuffer.cs)
- [MockingCommandQueue.cs](../../../src/Test/Ghost.UnitTest/MockingEnvironment/MockingCommandQueue.cs)
- [MockingGraphicsEngine.cs](../../../src/Test/Ghost.UnitTest/MockingEnvironment/MockingGraphicsEngine.cs)

Required new coverage is listed phase-by-phase in the implementation plan.

## Known Blockers And Residual Risks

- No representative render graph currently executes through a real D3D12 command buffer in a shipping-like pipeline.
- D3D12 debug-layer validation, GPU-based validation, PIX captures, presentation validation, and visual correctness remain blocked until that pipeline exists.
- Cross-queue enhanced-barrier behavior must be validated against the actual D3D12 backend before real async execution is considered complete.
- Current aliasing logic is not concurrency-aware.
- Current RenderEngine assumes one main Graphics command buffer and requires a controlled prelude/graph/epilogue ownership change.

Generated IIS Express `sessionKey` values under ignored `.vs/applicationhost.config` files were reported by gitleaks during the
prior session. They are encrypted local Visual Studio configuration, not service API credentials, and were marked as false
positives. Do not modify those ignored `.vs` files as part of this task.

## Worktree Safety

The repository has extensive pre-existing user and package/project changes. Do not reset, checkout, clean, or revert unrelated
files. Work with relevant existing changes and leave unrelated modifications untouched.

The scheduler API files and implementation may still appear untracked or unstaged in the shared worktree even though they are the
accepted baseline for this next phase. Inspect status before editing, but do not discard them.

## First Action In The New Session

1. Read this context file and the linked implementation plan completely.
2. Inspect the exact current diff for the scheduler and render-graph files without reverting anything.
3. Confirm the user has approved implementation, not merely planning.
4. Implement only Phase 1 from the plan.
5. Run its focused gate and stop for review before beginning the next phase.

Do not begin by designing a compiled queue-batch type. The first implementation artifact is structural synchronization-point encoding in the existing command stream, backed by focused CPU tests.
