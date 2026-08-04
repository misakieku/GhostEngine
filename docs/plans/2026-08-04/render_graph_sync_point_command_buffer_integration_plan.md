# Render Graph Sync-Point Command Buffer Integration Plan

Date: 2026-08-04

Status: Proposed implementation plan. No full-integration source changes have started.

> **New-session context:** Read
> [render_graph_sync_point_command_buffer_integration_context.md](render_graph_sync_point_command_buffer_integration_context.md)
> before implementing this plan. It records the accepted scheduler baseline, current source behavior, stale guidance to avoid,
> verification commands, environment constraints, and worktree safety requirements.

## Objective

Integrate dependency-aware async Compute execution without creating a render-graph batch abstraction.

The render graph will identify synchronization points in the pass DAG, serialize structural command-buffer boundaries into its
existing binary command stream, create multiple native `ICommandBuffer` instances while executing that stream, end those
command buffers, and submit them directly through `IFrameScheduler`.

The native command buffer is the submission unit. `SubmissionHandle` is the dependency token. The scheduler is the submission graph.

## Non-Negotiable Design Rules

1. Do not add `CompiledQueueBatch`, `RenderGraphQueueBatch`, `QueueSegment`, or an equivalent compiled/cached submission descriptor.
2. Preserve the existing binary render-graph command stream.
3. Store synchronization boundaries as structural command-stream metadata only.
4. Never compile or cache native fence values, scheduler IDs, generations, or `SubmissionHandle` values.
5. The render graph never calls `ICommandQueue.Submit`, `ICommandQueue.Signal`, `ICommandQueue.Wait`, or owns an `IFence`.
6. The executor acquires native command buffers from `IGraphicsEngine` and records commands directly into them.
7. Every command buffer is ended successfully before ownership transfers to `IFrameScheduler.Submit`.
8. Same-queue ordering comes from the scheduler's FIFO dependency insertion.
9. Render-graph cross-queue ordering uses inline relative command-buffer IDs mapped to returned handles and declared through
   `AddDependency`. `Transition` remains available for external latest-source/next-destination boundaries.
10. `EnableAsyncCompute(true)` is eligibility metadata. The compiler may demote the pass to Graphics.
11. The initial render-graph integration covers Graphics and Compute. Copy remains streaming-owned.
12. One frame-level scheduler `Flush` occurs before presentation.
13. Warm execution and warm-cache compilation must remain free of per-frame managed allocation after reusable scratch capacity is established.
14. Each phase below must pass its gate and receive review before the next phase begins.

## Target Execution Model

A useful async region has a Graphics producer, eligible Compute work, independent Graphics work, and a Graphics consumer:

```text
Pass dependencies

Graphics producer G0
      |          \
      |           +---------------- Graphics-independent G1
      v
Compute C0
      |
      +---------------------------- Graphics consumer G2
```

The render graph records four actual native command buffers:

```text
Graphics ICommandBuffer 0: G0
Compute  ICommandBuffer 0: C0
Graphics ICommandBuffer 1: G1
Graphics ICommandBuffer 2: G2
```

Scheduler dependencies:

```text
Graphics 0 -> Compute 0
Compute 0  -> Graphics 2
```

Scheduler same-queue FIFO adds:

```text
Graphics 0 -> Graphics 1 -> Graphics 2
```

There is no fifth object describing those submissions. The four native command buffers and their returned handles are the complete scheduling graph.

If no independent Graphics work exists, C0 is recorded into a Graphics command buffer and no cross-queue synchronization point is emitted.

## Structural Sync-Point Encoding

Replace the dead raw queue/fence command-stream model with a structural command-buffer boundary opcode. The exact enum and
payload field names may follow local naming conventions, but the serialized semantics must be equivalent to:

```text
CommandBufferSyncPoint
    nextCommandBufferType
    dependencyCount
    producerCommandBufferIds[dependencyCount]
```

The first command buffer has implicit relative ID `0`. Each marker ends command buffer `N` and starts command buffer `N + 1`.
Every producer ID in that marker is therefore a relative ID less than `N + 1`, and each listed dependency applies to the newly
started command buffer.

When encountered during recording, this marker means:

1. End the current native command buffer.
2. Retain that ended command buffer at its implicit relative ID in reusable executor scratch.
3. Retain the inline producer IDs as the dependency metadata for the next command buffer.
4. Acquire and begin a new native command buffer of `nextCommandBufferType`.

The marker does not submit, signal, or wait on a native queue. It contains no fence value, scheduler identity, generation, native object, or cached `SubmissionHandle`.

The current one-Graphics/one-Compute topology represents the fork/join sequence as:

```text
ID 0, G0 -> marker starts Compute ID 1  with dependencies [0]
ID 1, C0 -> marker starts Graphics ID 2 with dependencies []
ID 2, G1 -> marker starts Graphics ID 3 with dependencies [1]
ID 3, G2 -> end of stream closes the final command buffer
```

During submission replay, the executor submits the actual ended command buffers in relative-ID order and stores each returned
handle in reusable scratch at the same ID. After submitting a destination, it resolves each inline producer ID to its exact handle
and calls `AddDependency(producer, destination)`. Same-queue FIFO remains implicit in the scheduler, so the compiler only
serializes dependencies not already guaranteed by queue order.

Relative integer IDs in command bytes are synchronization-point references, not compiled batch descriptors. There is no per-ID
compiled object or cached metadata array. `Transition` is not required for internal render-graph edges and remains available for
external producers whose latest-source/next-destination semantics are intentionally sufficient.

## Compiler Ordering

Queue assignment and synchronization-point placement must occur before queue-sensitive barrier serialization and before native render-pass merging is finalized.

The intended compile order is:

1. Validate canonical pass/resource declarations in safety builds.
2. Cull unused passes and retain side-effect roots.
3. Build the pass dependency DAG with RAW, WAR, WAW, creation, and side-effect edges.
4. Classify each pass's requested and effective queue.
5. Find legal async Compute overlap windows.
6. Produce a deterministic linear CPU recording order plus synchronization boundaries.
7. Resolve queue-sensitive barriers.
8. Build native render passes without crossing synchronization boundaries.
9. Build a concurrency-safe aliasing plan.
10. Serialize barriers, pass execution, native-pass operations, and structural sync markers into the existing command stream.
11. Cache the resulting relative command bytes and existing compilation metadata.

Compiler-local temporary arrays, sets, or bitsets used to calculate pass order and boundaries are allowed. They must be released after serialization and must not become a compiled submission model.

## Async Candidate Selection

A pass is eligible for Compute only when all of the following are true:

- The pass is a Compute pass.
- `asyncCompute` was requested.
- Its commands and declared resource usages are legal on the Compute queue.
- Every required producer can complete before its launch synchronization point.
- Every consumer can be delayed until a legal join synchronization point.
- At least one Graphics pass between launch and join is independent of the Compute work.
- Moving the pass does not violate side effects, imported-resource ordering, native render-pass boundaries, or aliasing safety.

Candidate construction:

1. Determine the candidate's full prerequisite frontier.
2. Identify the latest required Graphics producer boundary.
3. Determine all direct and transitive consumers of candidate outputs.
4. Identify the earliest Graphics consumer that requires a join.
5. Find Graphics passes between launch and join that do not depend on the candidate and do not conflict through shared resource usage.
6. Group compatible Compute passes when they can share launch/join boundaries without reducing overlap.
7. Use original pass index as the deterministic tie-breaker.
8. Demote the candidate when no legal independent Graphics work remains.

With one physical Compute queue, the initial planner permits only one active Compute region at a time. Overlapping candidates must
be grouped into that region or conservatively demoted. Nested or competing Compute regions are not required for the first correct
integration.

Do not blindly re-enable the currently commented `ReorderPasses` call. Reuse its DAG helpers only after verifying their dependency semantics against the canonical declaration tests.

## Barrier Requirements

Barrier compilation remains a render-graph responsibility.

For each resource use:

- Resolve one effective state per pass/resource/range, preserving the existing canonical barrier work.
- Use the command buffer's effective queue when resolving queue-specific layouts and synchronization scopes.
- Emit cross-queue handoff barriers at legal command-buffer boundaries.
- Keep UAV ordering explicit for `ReadWrite` and write-after-write cases.
- Keep aliasing metadata attached to the first legal use of the successor resource.
- Never place a synchronization boundary inside an active native render pass.

The dependency between producer and consumer command buffers guarantees queue execution order. It does not replace resource state transitions.

The D3D12 backend's enhanced-barrier requirements must be inspected before choosing whether a cross-queue handoff uses a
consumer-side transition or paired release/acquire barriers. CPU mocks alone are not sufficient evidence for the final native form.

## Aliasing Requirements

The current scalar pass lifetime is insufficient once Graphics and Compute command buffers may overlap.

For two logical resources A and B to share physical memory, the compiler must prove one of:

```text
all uses of A happen-before all uses of B
```

or:

```text
all uses of B happen-before all uses of A
```

The proof must use existing pass dependencies and the synchronization ordering introduced by the graph. A linear CPU recording position is not sufficient.

Initial policy:

- Preserve aliasing when existing DAG reachability proves total ordering.
- Do not introduce extra cross-queue waits solely to recover aliasing savings in the first integration.
- Allocate separate memory when the two lifetimes are incomparable.
- Preserve exact fresh/cache alias membership and heap-size restoration.

This policy may reduce aliasing opportunities but cannot create concurrent use of the same physical memory.

## Execution And Failure Atomicity

`RenderGraphExecutor` will use two execution stages.

### Recording stage

1. Acquire the initial native command buffer from `IGraphicsEngine`.
2. Begin it with the frame allocator matching its immutable type.
3. Interpret normal barrier, native-pass, and pass callback opcodes directly into the active command buffer.
4. At each sync marker, end the active command buffer and retain it in reusable executor scratch.
5. Acquire and begin the next native command buffer.
6. End and retain the final command buffer when the stream ends.
7. Submit nothing during this stage.

If initial or post-marker command-buffer acquisition, `Begin()`, any pass callback, render-pass operation, barrier recording, or `End()` fails:

- Close an active native render pass when required.
- Return every acquired but unsubmitted command buffer exactly once, including buffers retained at earlier markers.
- Clear command-buffer, dependency, and handle scratch.
- Leave the scheduler with no graph submissions.
- Return the existing error result.

### Submission stage

Only after every command buffer ended successfully:

1. Replay recorded command buffers and their inline dependency IDs in relative-ID order.
2. Call `Submit` for each ended native command buffer and store the returned handle at that same relative ID.
3. Resolve every producer ID attached to the destination and call `AddDependency(producerHandle, destinationHandle)`.
4. Do not emit redundant same-queue dependencies already guaranteed by scheduler FIFO.
5. Retain returned handles only in execution scratch or the existing runtime execution result.
6. Clear caller ownership after each successful scheduler ownership transfer.
7. Do not call `Flush`; the outer frame owns the single flush.

Before implementation, verify the failure path after the first scheduler ownership transfer. If `Submit`, `Transition`, or
`AddDependency` can fail after earlier buffers transferred, add a narrowly scoped scheduler rollback operation for pending,
not-yet-flushed work. Do not claim transactional execution while a partial pending graph can survive an executor failure.

Reusable scratch may use pre-sized managed arrays/lists owned by the executor or existing pooled allocation APIs. It may contain
actual `ICommandBuffer` references, boundary wait metadata, and `SubmissionHandle` values. It is runtime state, is cleared after
each execution, and is never cached.

## RenderEngine Integration

The final frame shape is:

```text
Graphics prelude
    -> render-graph-created Graphics/Compute command buffers
    -> Graphics presentation epilogue
    -> IFrameScheduler.Flush
    -> PresentAll
```

Required ownership changes:

- The Graphics prelude records streaming finalization and any commands that must precede the graph.
- The prelude is ended and submitted before render-graph execution begins.
- The render graph acquires and submits all command buffers created by its sync points.
- The runtime-only `RGExecution` result exposes the latest terminal Graphics and Compute handles needed by the caller; these handles are never compiled or cached.
- RenderEngine acquires a new Graphics command buffer for swap-chain present transitions.
- The epilogue is ended and submitted through the scheduler.
- RenderEngine adds an exact dependency from every relevant terminal graph handle to the epilogue handle; same-queue FIFO may make the Graphics edge redundant, but terminal Compute remains explicit.
- RenderEngine calls `Flush` once and then presents.
- Frame allocators are reset only after `WaitForFrame` confirms the corresponding frame completion.

`RenderGraph.CompileAndExecute` must stop accepting raw queues and a fence. Its execution inputs should be limited to the graphics
engine, frame scheduler, current frame's Graphics/Compute allocators, view state, and execution flags. Prefer direct parameters or
a narrow execution context; do not use that context to recreate a compiled submission graph.

The exact RenderPipeline call boundary must be changed carefully because the current `RenderContext.CommandBuffer` assumes one
long-lived Graphics command buffer. The integration must ensure no caller keeps recording into a buffer after ownership transfers
to the scheduler.

## Compilation Cache And Hashing

The cache may contain:

- Existing compiled pass indices.
- Existing native render-pass metadata.
- Existing aliasing metadata and backing resources.
- Binary command bytes containing structural sync markers.

The cache must not contain:

- Native command-buffer references.
- Scheduler handles or IDs.
- Scheduler generations.
- Fence values.
- Queue objects.
- Executor scratch.

Requested async eligibility is already part of the graph hash. If effective scheduling also depends on a global execution policy
or device capability, that policy must be included in cache identity or represented in a way that cannot reuse queue-specific
barriers incorrectly.

Fresh compilation and cache hits must produce identical:

- Command-buffer split positions.
- Effective queue choices.
- Cross-queue dependency semantics.
- Native render-pass boundaries.
- Barrier bytes.
- Aliasing decisions.

## Phased Implementation

### Phase 1: Replace Dead Queue/Fence Opcodes With Structural Sync Markers

Scope:

- Add the structural command-buffer sync opcode, inline relative producer IDs, and serializer/disassembler support.
- Validate that producer IDs are earlier than their destination and are unique within one marker.
- Remove `GPUWait`, `SignalFence`, and `SubmitQueue` from the active execution contract.
- Delete their raw queue/fence handling from `RenderGraphExecutor`.
- Keep the compiler from emitting sync markers in production during this phase.
- Add focused binary encode/decode and dump tests.

Primary files:

- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphTypes.cs`
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphCompiler.cs`
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphExecutor.cs`
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraph.cs`
- `src/Test/Ghost.UnitTest/Graphics/RenderGraphTest.cs`

Gate:

- Existing 22 render-graph tests pass unchanged.
- New sync-marker serialization/disassembly tests pass.
- No native queue/fence operation is reachable from render-graph execution.
- Current contained single-Graphics behavior is unchanged.
- Stop for review.

### Phase 2: Build The Dependency-Window Sync-Point Planner

Scope:

- Build deterministic pass-DAG reachability needed for async eligibility.
- Compute requested versus effective queue for every compiled pass.
- Find producer launch and consumer join points.
- Find independent Graphics work in each candidate window.
- Group compatible Compute candidates.
- Demote no-overlap and ambiguous candidates.
- Produce compiler-local boundary metadata and serialize sync markers into test command streams/dumps.
- Do not enable native Compute execution yet.

Primary files:

- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphCompiler.cs`
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphPass.cs`
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphHasher.cs`
- render-graph dump/disassembly code in `RenderGraph.cs`
- `src/Test/Ghost.UnitTest/Graphics/RenderGraphTest.cs`

Required tests:

- Eligible fork/join emits launch, independent-Graphics, and join boundaries.
- Serial Graphics -> Compute -> Graphics with no independent work is demoted.
- Multiple compatible Compute passes share one async region.
- Conflicting Compute candidates are grouped or demoted deterministically.
- Raster and unsafe passes never receive Compute command buffers.
- Side effects and imported-resource dependencies are preserved.
- Declaration order does not affect the sync schedule or hash.
- Cache hits reproduce identical sync markers.

Gate:

- The dump proves a real structural overlap window.
- Native Compute submission remains disabled.
- Cold/warm compile benchmark regression is measured and documented.
- Stop for review.

### Phase 3: Make Native Passes, Barriers, And Aliasing Sync-Aware

Scope:

- Ensure native render-pass merging stops at every command-buffer boundary.
- Resolve barriers using effective queue assignment.
- Implement the D3D12-correct cross-queue handoff representation behind the RHI barrier API.
- Replace scalar alias assumptions with DAG happens-before checks.
- Conservatively disable aliasing for incomparable cross-queue lifetimes.
- Extend cache data only where existing barrier/alias metadata requires it; do not add submission descriptors.

Primary files:

- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphCompiler.Barrier.cs`
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphAliasing.cs`
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphCompiler.cs`
- native render-pass builder files
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphCompilationCache.cs`
- D3D12 barrier translation files for validation, if backend changes are required

Required tests:

- Cross-queue SRV/UAV producer-consumer transition.
- Cross-queue UAV `ReadWrite` and write-after-write ordering.
- Attachment use before or after Compute use.
- Native render pass cannot cross a sync marker.
- Concurrent logical lifetimes do not alias.
- Totally ordered cross-queue lifetimes may retain aliasing.
- Fresh/cache barrier and alias parity.

Gate:

- All barrier, aliasing, validation, and cache tests pass.
- No real Compute submission yet.
- Stop for review.

### Phase 4: Record Multiple Native Command Buffers, Forced To Graphics

Scope:

- Change `RenderGraphExecutor` to acquire command buffers from `IGraphicsEngine`.
- Split recording at structural sync markers.
- End every command buffer.
- Implement reusable execution scratch and recording rollback.
- Submit the resulting command buffers through `IFrameScheduler`.
- Temporarily map every requested command-buffer type to Graphics and suppress cross-queue waits.
- Keep multiple Graphics command buffers so lifecycle and splitting are exercised without async risk.
- Remove raw command-buffer, queue, and fence parameters from `CompileAndExecute` as appropriate for this stage.

Required tests:

- Number and type of acquired command buffers match sync boundaries.
- Every submitted command buffer was ended.
- Same-queue submission order matches stream order.
- Final command buffer is always submitted.
- Initial command-buffer acquisition/`Begin()` failure submits nothing and leaves scratch empty.
- Post-marker acquisition/`Begin()` failure returns all earlier retained buffers exactly once.
- Pass callback failure returns all acquired buffers and submits none.
- `End()` failure returns all unsubmitted buffers exactly once.
- Cache-hit execution splits command buffers identically.
- Warm execution performs no managed allocation after scratch warm-up.

Gate:

- Mock execution parity passes with all work on Graphics.
- Full unit suite and Debug/Release builds pass.
- Stop for review.

### Phase 5: Materialize Graphics/Compute Dependencies Through The Scheduler

Scope:

- Stop forcing eligible command buffers to Graphics behind a development execution switch.
- Begin Compute command buffers with the current frame's Compute allocator.
- Replay inline producer IDs as exact scheduler handle dependencies.
- Keep `Transition` for external latest-source/next-destination integration, not internal graph edges.
- Preserve same-queue FIFO without redundant graph dependencies.
- Validate terminal Compute work joins the next required Graphics submission.
- Ensure scheduler rollback exists for any post-transfer executor failure path.

Required tests:

- Graphics producer submit/signal precedes Compute wait/submit.
- Independent Graphics submission does not wait for Compute.
- Graphics consumer waits for the exact required Compute submission.
- Multiple async regions do not accidentally wait on unrelated later work.
- Terminal Compute dependency reaches the final Graphics submission.
- Cross-frame fence values remain monotonic and are absent from cache bytes.
- Scheduler cycle and unresolved-transition cleanup remains intact.

Gate:

- Exact ordered mock queue operations pass for fresh and cached graphs.
- Forced-Graphics fallback still produces correct output and ordering.
- Full unit suite and Debug/Release builds pass.
- Stop for review.

### Phase 6: Integrate Frame Prelude, Graph Submissions, And Presentation Epilogue

Scope:

- Split the current RenderEngine main Graphics recording into explicit prelude and epilogue command buffers.
- Submit the prelude before render-graph-created command buffers.
- Record swap-chain present transitions in the epilogue.
- Make the epilogue depend on terminal graph work.
- Keep one scheduler `Flush` for the complete frame.
- Present only after that flush enqueues all queue operations.
- Retain frame-completion-based allocator reset and resource retirement.

Primary files:

- `src/Runtime/Ghost.Graphics/RenderEngine.cs`
- `src/Runtime/Ghost.Graphics/RenderContext.cs` or the existing render-context owner
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraph.cs`
- `src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphExecutor.cs`
- render-pipeline call sites

Required tests:

- Prelude precedes every graph root that depends on it.
- Epilogue/present transitions wait for terminal Compute work.
- `PresentAll` occurs after scheduler flush.
- No caller records into a submitted command buffer.
- Frame allocators are not reset before frame completion.
- Streaming Copy submission continues to coexist with graph submissions.

Gate:

- End-to-end mock frame ordering passes.
- Full unit suite and Debug/Release x64 builds pass.
- Stop for review before real-GPU enablement.

### Phase 7: Add A Representative D3D12 Render-Graph Pipeline

Scope:

Create or wire a minimal real pipeline containing:

- A Graphics producer.
- Async-eligible Compute work.
- Independent Graphics work.
- A Graphics consumer/join.
- An imported swap-chain output.
- At least one transient resource.
- At least one UAV dependency.

Validation:

- D3D12 debug layer.
- GPU-based validation.
- Device-removal diagnostics.
- Presentation and visual correctness.
- PIX capture showing command-list boundaries and queue waits/signals.

Gate:

- No D3D12 validation errors.
- Correct image is presented.
- PIX proves the intended Graphics/Compute overlap exists.
- No claim of complete GPU validation before this gate.
- Stop for review.

### Phase 8: Measure, Tune, And Decide Default Policy

Scope:

- Add GPU timestamps around candidate Compute and independent Graphics work.
- Compare forced-Graphics and async execution.
- Measure synchronization overhead and frame-time effect.
- Keep structural demotion for candidates with no overlap.
- Add a minimum-work heuristic only if measurement justifies it.
- Re-run declaration, cold compile, warm compile, and execution benchmarks.
- Confirm warm paths remain allocation-free where they were previously allocation-free.

Gate:

- Async mode produces a measurable improvement on the representative workload.
- No correctness, barrier, aliasing, cache, or presentation regression.
- Default enablement policy is based on captured data, not pass type alone.

## Required Diagnostic Dump Changes

The graph dump should make the scheduling decision reviewable without exposing scheduler internals. For every pass, print:

- Original pass index and name.
- Requested async eligibility.
- Effective command-buffer type.
- Synchronization boundary before/after the pass when present.
- Cross-queue source/destination associated with the boundary.
- Demotion reason when an async request remains on Graphics.

Do not print cached fence values because none should exist.

## Performance Constraints

- No new managed allocation in the declaration benchmark.
- Warm compilation should remain 0 B/op.
- Executor scratch may allocate while growing to a new high-water mark, then must reuse capacity.
- Do not use LINQ in compiler or executor hot paths.
- Use existing unmanaged collections or reusable arrays where they match current ownership.
- Do not sort the full resource space when declared/use sets are sufficient.
- Do not enable `RGExecutionFlags.GenerateDump` in performance benchmarks.

Any measured compile regression above the existing Step 7 baseline must be reported with the phase that introduced it rather than deferred to the end.

## Review Checklist For Every Phase

- No `CompiledQueueBatch` or equivalent type was introduced.
- No unrelated user changes were reverted.
- All edited public API has XML documentation.
- Command buffers are ended before scheduler ownership transfer.
- No raw queue/fence operation was added to the render graph.
- Cache data remains relative and device-independent.
- Fresh and cached behavior are identical.
- Failure paths return unsubmitted command buffers exactly once.
- Scheduler validation remains active in Release.
- `GlobalRecordedOps` use remains guarded by `GHOST_UNITTEST`.
- Focused tests pass before broad tests/builds run.
- Project-aware LSP and pi-lens diagnostics are clean.
- `git diff --check` and the 200-character line limit pass.
- Residual native/GPU risks are recorded explicitly.

## Completion Criteria

Full integration is complete only when all of the following are true:

1. The existing binary command stream contains structural synchronization boundaries rather than raw queue/fence operations.
2. The render graph creates and records multiple native command buffers from those boundaries.
3. The render graph submits those command buffers directly to `IFrameScheduler`.
4. Cross-queue render-graph dependencies are expressed with exact submission handles resolved from inline relative IDs.
5. No render-graph-owned compiled batch/submission abstraction exists.
6. Every command buffer is ended before submission and has a valid allocator lifetime.
7. Final Graphics presentation depends on all graph work that can affect the frame.
8. Transient aliasing remains valid under concurrent queue execution.
9. Fresh and cached graph execution produce identical command-buffer boundaries and dependencies.
10. Forced-Graphics mode remains a correct fallback.
11. All focused and full CPU tests pass in authoritative Windows runs.
12. Debug and Release x64 solution builds pass.
13. A real D3D12 render graph passes debug-layer and GPU-based validation.
14. PIX demonstrates real overlap and measurement demonstrates a frame-time benefit.

## First Implementation Step After Approval

Implement Phase 1 only:

- Replace the dead raw queue/fence opcode contract with the structural sync marker.
- Add serialization, disassembly, and focused tests.
- Keep production execution on the current single Graphics command buffer.
- Run the Phase 1 gate.
- Stop and present the diff for review.

Do not start the dependency-window planner or multi-command-buffer execution in the same change.
