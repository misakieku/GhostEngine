# Compilation and Caching

Compilation is where the graph's promises are kept: declared access becomes dependencies, dependencies become order and barriers, and order becomes a byte stream the executor can replay without thinking. This page follows one `CompileAndExecute` call through to submission, and explains the cache that makes the steady state cheap.

Mechanics that belong to a single stage — aliasing proofs, barrier emission, queue assignment — are covered on their own pages and only referenced here.

## The top-level sequence

```csharp
public Result<RGExecution, Error> CompileAndExecute(
    in RenderGraphExecutionContext executionContext,
    ViewState viewState,
    RGExecutionFlags flags = RGExecutionFlags.Default)
```

1. `ResolveTextureSizes(viewState)` — relative descriptors become concrete extents.
2. `RenderGraphHasher.ComputeGraphHash(passes, resources)` — a structural fingerprint.
3. `_compiler.Compile(viewState, graphHash, passes, memoryPool)` — cache hit or fresh compile.
4. `_context.RelativeScale = graph.scale` — publish the resolved scale for the frame.
5. `_executor.Execute(executionContext, context, graph, flags, out graphics, out compute)` — replay the stream.
6. Deferred extraction — `QueueSwap` / `QueueReplace` for every extracted resource.
7. `GenerateDump(...)` — only when `RGExecutionFlags.GenerateDump` is set.

The `CompiledGraph` is a `using` local, so its `AliasingPlan` is released when the call returns; the reusable artifacts live in the cache, not in the graph. Failures at any step return an `Error` before command recording begins, so a failed compile leaves no partial GPU work behind.

## The compilation cache

`RenderGraphCompiler` holds exactly one `RenderGraphCompilationCache`, which holds **one** entry: a hash and a `CachedCompilation`. There is no eviction policy and no multi-entry table, because there is no need for one — a `RenderGraph` instance is bound to a single view and rebuilt identically every frame.

On lookup there are three outcomes.

**Perfect hit.** The hash matches and `viewState.CalculateScale(cached.viewState)` is not greater than one in any component. The frame restores cached culled flags onto the pass list, rebuilds an `AliasingPlan` as a shallow copy of the cached placements, and re-points every non-imported resource at its cached backing handle via `RestoreBackingResources`. No culling, no DAG, no reordering, no barrier computation, no allocation. Extracted resources are the exception: they take a fresh pooled handle each frame regardless, because their lifetime extends past the graph.

**Hit with growth.** The hash matches but the view is larger. Placements must be resized while the compiled structure stays valid, so `ResizeCachedSlots` recomputes offsets and slot sizes from the new resolved dimensions and **preserves alias groups exactly**, then `AllocateBackingResources` rebuilds the heap and suballocations. Command bytes, native passes, culled flags and schedule indices are reused untouched. The returned `scale` is `float2.one`.

**Miss.** Full compilation, described below, ending with `SetCached` storing a copy of every artifact.

Shrinking the viewport takes the perfect-hit path. This is deliberate: the cached backing resources stay at their larger size and the frame renders into a smaller viewport of them, which avoids both a reallocation and — since resolved dimensions are not in the hash — a recompile. The cost is that peak memory is not reclaimed until the viewport grows again, which is the right trade for a window drag but worth knowing if you animate resolution. The dump reports both the planned `SizeInBytes` and the resolved texture extents, so an over-sized resource is visible when it matters.

## Stages of a fresh compile

In order, all inside `Compile`:

| # | Stage | Produces |
|---|---|---|
| 1 | `MarkPassesWithSideEffects` | `pass.hasSideEffects` |
| 2 | `CullPasses` | `pass.culled` |
| 3 | Build `compiledPasses` | surviving pass indices, in declaration order |
| 4 | `BuildDAG` | `PassDependencyNode[]` with `inDegree` and `dependents` |
| 5 | `ReorderPasses` | `compiledPasses` permuted by Kahn + heuristics |
| 6 | `BuildPassReachability` | `scheduleIndexByPassIndex`, transitive reachability matrix |
| 7 | `BuildDependencyWindowSchedule` | `effectiveQueues`, `syncBoundaries` |
| 8 | `FinalizeScheduleReachability` | `commandBufferIds`, reachability closed over segments |
| 9 | `RenderGraphResourceOrdering.Build` | per-resource use bitmasks, first/last schedule indices |
| 10 | `RenderGraphAliasingBuilder.Build` | `AliasingPlan`, `totalHeapSize` |
| 11 | `AllocateBackingResources` | the heap plus one backing handle per resource |
| 12 | `BuildNativeRenderPasses` | `NativeRenderPass[]` with inferred load/store ops |
| 13 | `BuildExecutionCommands` | the binary command stream |
| 14 | `SetCached` | the cache entry |

Stage order is not arbitrary. Reordering (5) must precede reachability (6) because reachability is indexed by schedule position. Reachability must precede the async planner (7), which must precede the ordering bitmasks (9), because aliasing has to respect segment order as well as data dependencies. Aliasing (10) must precede both backing allocation (11) and native-pass merging (12), because merge decisions consult whether an aliasing barrier is required.

Under `GHOST_SAFETY_CHECKS`, whole-graph validation runs before any of this — but only when the hash differs from the last validated hash, so a structurally identical frame is validated once rather than every frame.

## Culling

Culling is dead-code elimination over declared resource access, and it runs in two steps.

First, every pass is seeded as culled when it permits it:

```csharp
passes[i].culled = passes[i].allowCulling && !passes[i].hasSideEffects;
```

Second, the compiler walks the pass list **backwards** and, for every pass that survived, recursively un-culls the producers of everything it reads — `resourceReads`, color attachments, the depth attachment, and random-access resources. Working backwards means each un-cull decision is made with the downstream answer already known, so one pass suffices.

A pass is un-culled for one of three reasons: something observable consumes its outputs; it writes an imported or extracted resource (`hasSideEffects`); or it opted out with `AllowPassCulling(false)`. Side effects override `allowCulling` deliberately — writing outside the graph is never dead work.

Culled passes are dropped from `compiledPasses` and never appear in the command stream. Their resources still exist in the registry and still occupy the dump, which is how you diagnose an over-aggressive cull: the resource's `ConsumerPasses` list is empty.

## The dependency graph

`BuildDAG` derives edges purely from read and write declarations, tracking per resource a `lastWriter` index and a `lastReaders` set:

| Hazard | Edge emitted |
|---|---|
| Read after write (RAW) | previous writer → this pass |
| Write after write (WAW) | previous writer → this pass |
| Write after read (WAR) | every reader since the last write → this pass, then the reader set resets |

Resource **creation** is processed as a write, so a pass that creates a transient orders after whoever last touched that identifier — normally nobody, since creation implies a fresh index. Random-access (UAV) declarations are processed as both a read and a write, which is what makes UAV chains order correctly. Attachments are decomposed by their `AccessFlags`: a `ReadWrite` depth attachment contributes both a read edge and a write edge.

Finally, passes with side effects are chained to each other in declaration order (`lastSideEffect` → this pass). Since side-effecting passes are never culled and their ordering cannot be derived from resource data, declaration order is the only defensible interpretation.

Edges are deduplicated — `AddEdge` checks `dependents` before incrementing `inDegree` — so a pass pair sharing ten resources produces one edge.

## Reordering

`ReorderPasses` runs Kahn's topological sort, but instead of a FIFO ready queue it scores every ready pass and takes the best:

| Rule | Score | Intent |
|---|---|---|
| Both raster and attachments match the last scheduled pass | +1000 | Maximize native-pass merging |
| Shares a read resource with the last scheduled pass | +100 | Memory locality — shorten lifetimes |
| Pass requested async compute | +50 | Keep async candidates adjacent |
| Position in the ready list | −0.01 × index | Tie-break toward declaration order |

The last rule is what makes the result deterministic and what keeps a graph you authored in a sensible order from being scrambled. Because the ready list is mutated with `RemoveAtSwapBack`, the tie-break depends on list position rather than stable order — the small magnitude of the penalty means declaration order wins ties without ever overriding rules 1–3.

Rule 2 is why reordering helps memory: a consumer scheduled immediately after its producer narrows that resource's live range, which is exactly the interval the aliaser then exploits. The heuristic optimizes for merging first and aliasing second, which matches how much each saves on a real frame.

## Reachability, async windows, ordering, aliasing

Stages 6–10 are mechanical but interlocking, and detailed elsewhere:

- **Reachability** (6, 8) is a `byte[passCount²]` matrix closed transitively by reverse iteration. It answers "can pass A affect pass B" in one load, and is the input to both the async planner and the aliasing proof. Segment order is folded in at stage 8, so aliasing respects queue placement.
- **Async windows** (7) are on [Synchronization](synchronization.md).
- **Resource ordering** (9) builds per-resource use bitmasks over schedule indices; `CanAlias` is the happens-before test. See [Resources and Aliasing](resources.md).
- **Aliasing and placement** (10) and **backing allocation** (11) are likewise on the resources page.
- **Native passes** (12) — merge gates and load/store inference — are on the synchronization page.

## The command stream

Stage 13 serializes execution into a flat `byte` buffer via `BufferWriter`. The executor is a straight-line interpreter over it, with no branching decisions left to make at replay time.

| Opcode | Payload | Emitted by |
|---|---|---|
| `IssueBarriers` | `int count`, then `count × CompiledBarrier` | Pass prologues, native-pass prologues, queue releases, closing barriers |
| `BeginNativePass` | `int nativePassIndex` | The native-pass merger |
| `ExecutePass` | `int logicalPassIndex` | Once per surviving pass, in schedule order |
| `EndNativePass` | — | Closes a merged run |
| `CommandBufferSyncPoint` | `CommandQueueType nextType`, `int producerCount`, `int[] producerIds` | The async planner |

`CompiledBarrier` carries the resource ID, source/handoff/target `ResourceBarrierData`, an optional `aliasingPredecessor`, `BarrierFlags`, resource type, and source/destination queues — everything the executor needs to build a `BarrierDesc` without consulting graph state.

Barrier batches are **backpatched**: the opcode and a placeholder count are written first, barriers are appended, then the count is rewritten in place. If the count ends at zero the writer position rewinds, so empty batches cost nothing. That is why the stream has no padding and no sentinel values.

Native-pass prologues are the interesting case: all barriers for *every* merged pass are hoisted before `BeginNativePass`, because once a render pass is open you cannot transition resources inside it. The compiler verifies the invariant it depends on and throws if a sync boundary lands inside a native pass.

Three structural checks in stage 13 throw rather than degrade — non-contiguous command-buffer IDs across boundaries, a pass whose queue contradicts its boundaries, and a sync boundary inside a native pass. Each indicates a compiler bug, and each would produce silent GPU corruption if recorded anyway.

## The graph hash

`ComputeGraphHash` fingerprints structure only, deliberately excluding anything that varies per frame without changing compilation.

**Resource definitions**, in registry order: type, `isImported`, `isExtracted`, and for extracted resources the target handle hash plus extraction flags; both barrier states when present; then per type — textures get format, size mode, and either absolute dimensions or relative scale factors, plus dimension, mips, slices, usage, clear and discard flags, and the clear values; imported textures get the resolved-shape subset instead. Buffers get `Size`, `Stride`, `Usage`, `HeapType`.

**Passes**, in declaration order: type, `allowCulling`, `asyncCompute`, `allowAsyncComputeOverlap`, whether the pass writes an external resource, then depth and color attachments as (resource ID, access flags, barrier usage), then every resource set, then `GetRenderFuncHashCode()`.

**Resource sets are sorted by ID before writing.** Declaration order must not change the hash, or `UseBuffer(a)` before `UseBuffer(b)` would invalidate the cache relative to the reverse.

**What is excluded, and why:**

| Excluded | Reason |
|---|---|
| Pass names, resource names | Diagnostic only; names are stripped from release builds anyway |
| `ViewState` | Resolved sizes are not hashed — relative descriptors are, so resizing hits the cache |
| `TPassData` values | Frame-varying data must not trigger recompilation |
| Backing handles for imported textures | Descriptor instead, so swap-chain recreation preserves the hash |

`GetRenderFuncHashCode` combines the method's runtime identity hash with the delegate target's when present. That is the safety valve on the static-lambda rule from [Authoring Passes](authoring-passes.md): a closure is a different target each frame, so it produces a different hash and forces recompilation rather than silently replaying a stale command stream. It costs you the cache, which is the correct failure mode for a per-frame allocation.

Determinism is a correctness requirement here, not a nicety. Several tests compare disassembled command streams across frames, and `TestAsyncPlanner_MultipleRegionsCacheHitPreservesStructure` asserts hash equality plus equality of the whole disassembled line list. Any source of nondeterminism in the compile pipeline — dictionary enumeration order, unstable sorts, pointer values — shows up as a cache that never hits.

## What the cache stores

`CachedCompilation` holds the reusable artifacts: compiled pass indices, per-pass culled flags, native passes (with their merged-index lists deep-copied), the logical-to-physical aliasing map, placed-resource metadata and the flattened aliased-resource list, total heap size, per-resource first/last schedule indices, the backing handles, the command bytes, and the view state the compilation was produced for.

What it does **not** keep: the `RenderGraphPass` objects themselves (recycled to the pool each frame, and re-derived from `passCulledFlags` on restore), the dependency DAG, the reachability matrix, and anything in `RenderGraphContext` — which holds runtime ownership and is never retained by the cache.

`SetCached` disposes the previous entry before storing, so the cache is a single-slot swap rather than a growing table. `RenderGraph.InvalidateCache()` forces a full recompile on the next frame, which is the escape hatch when you suspect a stale entry.

`UpdateBackingResource` exists because allocation happens per resource after the plan is built: the registry writes each new handle straight into the cache entry so a partial-failure frame cannot leave a dangling cached handle.

## Allocation discipline

Compilation allocates heavily and must not touch the GC. Three patterns carry that:

- **Stack scopes.** `AllocationManager.CreateStackScope()` bounds all stage-local scratch — DAG arrays, ready lists, usage records, the reachability matrix. Leaving the scope frees everything at once.
- **Caller-owned handles where lifetimes escape.** `BuildDAG` takes the scheduling scope's handle explicitly rather than creating its own, with the reason stated in the source: returning from `BuildDAG` must not rewind allocations the graph still references. The same discipline applies to `AliasingPlan` and `NativeRenderPass`, which are built with the caller's `allocationHandle`.
- **Explicit disposal in `finally`.** DAG nodes, native passes and the command writer are disposed in `finally` blocks, because the cache copies their contents and the originals are per-frame scratch.

The managed allocations that survive are the `List<T>` fields inside each pass's `RenderGraphResourceSet` members. They are not pooled directly — what is pooled is the **pass object that owns them** (`RenderGraphObjectPool` keeps a static pool per concrete `RasterRenderGraphPass<T>` / `ComputeRenderGraphPass<T>` / `UnsafeRenderGraphPass<T>`), and `Reset()` only clears the lists. So those `List<T>` buffers are paid once per concrete pass type per process, never per frame.

## Rules that bite

1. **The cache is one slot.** Alternate two structurally different graphs on the same `RenderGraph` and you recompile every frame. Use one graph per view, which is what `GPUViewContext` does.
2. **Changing a descriptor invalidates; changing a value does not.** Editing `RGTextureDesc` or a `BufferDesc` forces recompilation. Changing `TPassData` contents never does.
3. **Relative sizing is what makes resizing free.** Absolute-sized transients bake dimensions into the hash.
4. **Shrink does not reclaim.** Peak memory holds at the high-water viewport until it grows again.
5. **Reordering is heuristic, not stable.** If you need two passes in declaration order and nothing connects them, they may be reordered — declare a dependency, or rely on the side-effect chain.
6. **Culled passes are invisible downstream.** Anything the graph cannot see through a declaration needs `AllowPassCulling(false)`.
7. **A cache miss is invisible without a dump.** `Dump.IsCacheHit` is only populated when you pass `RGExecutionFlags.GenerateDump`, in any build configuration. When a frame's cost looks wrong, enable the flag for one frame and compare hashes across two frames.

## Next

- [Debugging and Validation](debugging.md) — reading the dump, the validation rules, and the failure modes this pipeline is designed to catch.
