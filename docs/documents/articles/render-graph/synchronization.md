# Synchronization

Declaring resource access is what lets the graph reason; this page is about what it concludes. Four things are derived automatically: state transitions (barriers), attachment load/store actions, native render passes, and queue assignment.

## The state model

Every resource is tracked as a triple, `ResourceBarrierData`:

| Field | Type | Answers |
|---|---|---|
| `layout` | `BarrierLayout` | What form is the resource in right now? |
| `access` | `BarrierAccess` | Who is allowed to touch it? |
| `sync` | `BarrierSync` | Which pipeline stages must be flushed or made visible? |

All three are needed because they fail independently: a texture can be in `RenderTarget` layout while a *different* stage set still needs draining, and a buffer's access can change without its layout. `BarrierLayout.Present` and `BarrierLayout.Common` share value 0, and a few access/sync members are aliases (`IndirectArgument`/`Predication`, `ExecuteIndirect`/`Predication`), so equality is by value, not by name.

The compiler keeps a `CompiledResourceState` per resource — its last emitted triple plus whether that use wrote — seeded from an imported resource's `initialState` when one was supplied, and `Undefined` otherwise.

## Resolving what a pass wants

Each pass's declarations are folded into one resolved usage record per resource (`ResolvedPassResourceUsage`), combining the canonical sets from [Authoring Passes](authoring-passes.md) with a target state:

| Declaration | Usage class | Target state | Priority |
|---|---|---|---|
| `UseTexture(..., Read)` | `ShaderRead` | `ShaderResource` / `ShaderResource` / stage mask by pass type | GenericRead |
| `UseBuffer(..., Read)` on an `IndirectArgument` buffer | `IndirectArgument` | `Undefined` / `IndirectArgument` / `ExecuteIndirect` | GenericRead |
| `UseBuffer(..., Read)` otherwise | `ShaderRead` | `Undefined` / `ShaderResource` / stage mask | GenericRead |
| `UseTexture` / `UseBuffer(..., Write)` in a compute pass | `UnorderedAccess` | `UnorderedAccess` / `UnorderedAccess` / `ComputeShading` | GenericWrite |
| `SetColorAttachment` | `ColorAttachment` | `RenderTarget` / `RenderTarget` / `RenderTarget` | Explicit |
| `SetDepthAttachment` | `DepthWrite` or `DepthRead` | `DepthStencilWrite` or `DepthStencilRead` | Explicit |
| `UseRenderTargetTexture` (unsafe) | `ColorAttachment` | `RenderTarget` / `RenderTarget` / `RenderTarget` | Explicit |
| `UseRandomAccess*` | `UnorderedAccess` | `UnorderedAccess` / `UnorderedAccess` / `ComputeShading` (compute) or `AllShading` | Explicit |

Read-stage masks follow the pass type: compute gets `ComputeShading`, raster gets `PixelShading | NonPixelShading`, unsafe gets `All`.

When several declarations hit the same resource, `ApplyUsage` merges them: if the usage class, layout and access already agree, the `sync` masks OR together and priority rises. Otherwise the higher priority wins, so **explicit declarations always beat generic ones**. That is what makes `UseTexture(t, ReadWrite)` plus `SetColorAttachment(t, 0, Write)` resolve to an attachment rather than a UAV. Writes on raster and unsafe passes resolve to `None` — those declarations are rejected at build time, as covered on the previous page.

## Emitting barriers

For each pass in schedule order, the compiler walks its resolved usages and decides whether a transition is needed.

**Queue acquires win first.** If a handoff barrier is already scheduled to bring this resource into this pass on a different queue, the transition is subsumed: the state is recorded and no implicit barrier is emitted.

**Same-state transitions are elided.** When the tracked state exactly equals the target state, nothing is emitted. This is the common case inside a native pass, where many passes share attachments.

**...except UAV write-after-write.** Two compute passes both writing the same UAV resolve to an identical target state, so exact-state elision would delete a real hazard. The compiler detects it and forces a barrier anyway:

```csharp
var forceUavOrdering = hasSameState
    && usage.targetState.access == BarrierAccess.UnorderedAccess
    && (resourceState.writes || usage.writes);
```

That is `BarrierFlags.Force`, and `TestPhase3_ComputeUavWawForcesSameStateBarrier` pins it. It is the reason a UAV WAW shows up in the disassembly as a transition to the state the resource is already in.

**First use gets a discard.** A resource whose tracked state is invalid at first use emits `FirstUsage | Discard`. If it shares a heap slot, the most recent provable former occupant becomes `aliasingPredecessor`, and the barrier doubles as the aliasing transition described on [Resources and Aliasing](resources.md).

**Closing barriers restore imports.** After the last pass, every imported resource with a `finalState` that differs from its tracked state gets a transition. The case where the resource was never used at all is handled too: if it had an `initialState` differing from `finalState`, that transition is emitted, so a pass-free import still ends in the state you asked for.

## Queue handoffs

When a resource crosses a command-buffer segment boundary — Graphics to Compute or back — a fence-free intra-command-list barrier is not sufficient. The compiler emits a **release/acquire pair**:

| | Emitted at | Shape |
|---|---|---|
| **Release** | The boundary that ends the producer's segment | `sourceState` → handoff state, `NoAccess` / `None`, `BarrierHandoffType.Release` |
| **Acquire** | The consumer's pass prologue | handoff state → `targetState`, `BarrierHandoffType.Acquire`, plus `FirstUsage \| Discard` when it is an aliasing successor |

The handoff state is `Common` for textures and `Undefined` for buffers, always with `NoAccess`/`None`. A pair is only created when the producer wrote or the consumer writes, and only when the producer is reachable from the consumer in schedule order — so unrelated cross-queue resources cost nothing. Duplicate handoffs for the same source/destination/boundary are folded by OR-ing the target access and sync masks rather than emitted twice.

An aliasing successor is handled separately, after the logical-resource pass, because the predecessor is a *different* resource ID sharing the same memory.

## Load and store actions

Native passes infer attachment actions from lifetime position, which is what makes tile-based hardware efficient without you specifying them:

| Condition | Load op |
|---|---|
| First use of the resource, and `clearAtFirstUse` | `Clear` (with the descriptor's clear color/depth/stencil) |
| First use, and not `clearAtFirstUse` | `DontCare` |
| Not first use, `Discard` in the access flags | `DontCare` |
| Otherwise (read, or plain continuation) | `Load` |

| Condition | Store op |
|---|---|
| Last use, and not `discardAtLastUse` | `Store` |
| Last use, and `discardAtLastUse` | `DontCare` |
| Intermediate use | `Store` |

Depth gets one extra guard: even with `discardAtLastUse`, the store is forced to `Store` when the resource `isImported` or `isExtracted`, because something outside the graph still needs those pixels. Stencil actions mirror depth only when the format actually has a stencil; otherwise both are `NoAccess`.

## Native render passes

Consecutive raster passes with the same attachments are merged into one hardware render pass, so intermediate results stay in tile memory. A merge requires all five:

1. Both passes are `Raster` — compute and unsafe passes always break the current native pass.
2. Neither side has random-access (UAV) resources. This one is deliberately conservative.
3. Attachments match exactly: same color slot count, same texture per slot, same depth texture.
4. Every resource the pass reads, writes or creates *is* one of its attachments (`HasOnlyAttachmentUsages`). A merged pass that secretly sampled a third texture would need a transition mid-render-pass, which is illegal.
5. No barrier is required between them (`RequiresBarrierBetweenPasses`).

Rule 5 is where aliasing and merging interact: if the later pass writes a resource at its first use, that resource shares a heap slot with another, and it is one of the pass's render targets, a barrier is needed and the native pass splits. `TestPhase3_SyncBoundarySplitsCompatibleNativePasses` covers the queue-boundary version of the same rule.

Actions are inferred per native pass using its first and last merged schedule indices, so a resource whose uses all fall inside one native pass sees `Clear` then `DontCare` and never touches DRAM.

## Async compute

`EnableAsyncCompute(true)` marks a pass as a candidate. `IsAsyncComputeCandidate` additionally requires: compute type, no side effects, no color attachment, no depth attachment, no unsafe render-target writes. A compute pass that renders into a target is never a candidate.

The dependency-window planner then looks for a legal overlap:

```text
for each candidate pass C (skipping indices already carrying a sync boundary):
    join  = first pass that C can reach            // the consumer of C's results
    group = C plus following candidates sharing `join`
    window = (group end, join)

    window must contain at least one independent graphics pass
    window must contain no unsafe pass, unless AllowAsyncComputeOverlap
    -> emit: C..group on Compute, window on Graphics, join on Graphics
```

The three boundaries per region are:

| Boundary | Next segment | `DependsOn` |
|---|---|---|
| At `C` | Compute | the current graphics segment, if any graphics pass produces into the group |
| At `group end + 1` | Graphics | none — the overlap work is independent by construction |
| At `join` | Graphics | the Compute segment |

Command-buffer IDs come from a monotonic counter, and `currentGfxCbId` advances to each region's join buffer, so **regions nest and chain**: a later region forks on the segment its predecessor rejoined to, and the producer scan skips passes already assigned to Compute. That is `TestAsyncPlanner_MultipleComputeRegionsScheduledInSingleGraph` — two regions, six markers, region 2 forking on `[3]`.

Two thresholds: the planner returns immediately for graphs with fewer than three compiled passes, and a candidate at schedule index 0 is legal (a leading compute pass with no graphics producer forks with `DependsOn: [none]`, per `TestAsyncPlanner_PassZeroComputeWithFewerThanFourPassesSucceeds` — the name predates the threshold being lowered from four to three).

`AllowPassCulling(false)` has no bearing on candidacy: it affects culling only, and candidacy depends on `hasSideEffects`, attachments and pass type. A pass can be un-cullable and still be scheduled to Compute.

When no legal window exists the pass simply stays on Graphics. Nothing warns; read `PassDumpInfo.QueueDecision` to see which of `AsyncNotRequested`, `IneligiblePassType`, `Culled`, `AsyncComputeSelected` or `NoLegalOverlapWindow` applied.

## Folding segments back into reachability

After queue assignment, `FinalizeScheduleReachability` extends the dependency matrix: passes in the same command-buffer segment, or on the same queue, are ordered; boundary producers are added; then a reverse propagation closes transitivity.

This step is load-bearing beyond scheduling. `RenderGraphResourceOrdering` and the aliasing test consume the *final* reachability, so segment order participates in aliasing proofs. Without it, two passes on different queues would look unordered and be denied aliasing — which is exactly what `TestPhase3_IncomparableGraphicsAndComputeResourcesDoNotAlias` and `TestPhase3_TransitivelyOrderedCrossQueueResourcesMayAlias` bracket.

## Execution

`RenderGraphExecutor` interprets the command stream, holding one active command buffer acquired from the `FrameScheduler` pool:

| Opcode | Effect |
|---|---|
| `IssueBarriers` | Decode `CompiledBarrier[]`, batch into `BarrierDesc`s (flushing every 64), one `ICommandBuffer.Barrier` call per batch |
| `BeginNativePass` | Derive viewport/scissor from attachments, build `PassRenderTargetDesc[]` + `PassDepthStencilDesc`, `BeginRenderPass(allowUAVWrites)`, publish RT formats to the context |
| `ExecutePass` | `passes[i].Execute(context)` — your render function |
| `EndNativePass` | `EndRenderPass` |
| `CommandBufferSyncPoint` | End the active buffer, begin a new one on the marker's queue, re-bind frame and view constant buffers |

Publishing formats at `BeginNativePass` is what feeds the PSO cache key, so pipeline state varies with attachments automatically.

After the stream, `OnFinalGraphicsCommandBuffer` runs on the active buffer **only if the final segment is Graphics** and the execution context supplied a callback — the engine uses it to record present barriers without an isolated command buffer. A schedule that ends on a Compute segment never invokes it, so anything recorded there must not be required by the compute work. Submission then happens as a single `FrameScheduler` transaction: every buffer is submitted, cross-queue dependencies are added only where producer and consumer queue types differ, and the transaction commits atomically. `RGExecution` returns the terminal `GraphicsSubmission` and an invalid-or-real `ComputeSubmission`.

### Failure handling

Errors never leave a half-recorded frame. A barrier or submit failure, or any exception from a pass lambda, triggers `RollbackRecording`: end the open render pass if inside one, end the command buffer, and return every acquired buffer to the pool exactly once. A submission failure rolls back the transaction and returns buffers without submitting. Either way `Execute` returns an `Error` rather than throwing into the pipeline.

Five invariant checks throw instead of degrading, because each indicates a compiler bug rather than bad input: non-contiguous command-buffer IDs across a sync boundary, a pass whose queue disagrees with its boundaries, a sync boundary inside a native pass, a producer ID that does not precede its consumer, and an unknown opcode.

`RGExecutionFlags.ForceGraphics` records every segment on Graphics. Queue-**release** barriers are skipped outright; queue-**acquire** barriers are downgraded to ordinary source→target transitions with the handoff state discarded. Nothing crosses queues, but the barrier count does not fall to zero. The flag exists for tests and fallback debugging, and changes only what the executor emits — compilation is untouched, so the dump still reports the planned async decisions while the frame replays serially.

## Appendix: the barrier members this page uses

`BarrierLayout`, `BarrierAccess` and `BarrierSync` are RHI enumerations with many more members than the graph emits. Only these appear in render-graph compilation:

| Enum | Members used by the graph |
|---|---|
| `BarrierLayout` | `Undefined` (-1), `Common` / `Present` (0), `RenderTarget` (2), `UnorderedAccess` (3), `DepthStencilWrite` (4), `DepthStencilRead` (5), `ShaderResource` (6) |
| `BarrierAccess` | `Common` (0), `RenderTarget` (0x8), `UnorderedAccess` (0x10), `DepthStencilWrite` (0x20), `DepthStencilRead` (0x40), `ShaderResource` (0x80), `IndirectArgument` (0x200), `NoAccess` (0x80000000) |
| `BarrierSync` | `None` (0), `All` (0x1), `PixelShading` (0x10), `DepthStencil` (0x20), `RenderTarget` (0x40), `ComputeShading` (0x80), `ExecuteIndirect` (0x800), `AllShading` (0x1000), `NonPixelShading` (0x2000) |

Two aliases exist in the RHI and are indistinguishable by value: `BarrierAccess.IndirectArgument` == `Predication`, and `BarrierSync.ExecuteIndirect` == `Predication`. `BarrierLayout.Common` == `Present`. Comparisons in the compiler are by value, so an alias never causes a spurious transition.

**Buffers are layout-agnostic in this model.** Every buffer read resolves to `layout: Undefined` with a meaningful access and sync mask, and buffer UAV writes do the same. For buffers only `access` and `sync` carry information — which is why the state triple is still the right representation: textures need all three, buffers need two, and the code does not special-case the difference.

## Rules that bite

1. **Undeclared access means no barrier.** The elision logic is sound only over declared usage.
2. **Do not fight the load/store inference.** `clearAtFirstUse` and `discardAtLastUse` are per-resource; if a resource needs different treatment in different passes, split it into two resources.
3. **A merged native pass cannot touch non-attachments.** Reading any other resource forces a split and loses tile locality.
4. **UAV writes inside raster passes are excluded from merging.** If a pass needs both, expect two render passes.
5. **Async compute is opportunistic.** Verify with `QueueDecision`; never assume a dispatch ran on the compute queue.
6. **Cross-queue resources cost a release/acquire pair.** A resource ping-ponging between queues is slower than keeping that work on one queue.
7. **`ForceGraphics` hides handoff bugs.** Reproduce without it before concluding a handoff is at fault.

## Next

- [Compilation and Caching](compilation.md) — how all of the above is produced once and replayed, and the command-stream format.
