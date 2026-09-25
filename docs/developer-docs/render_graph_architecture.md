# Render Graph — Architecture Reference

**Module**: `Ghost.Graphics.RenderGraphModule` · `src/Runtime/Ghost.Graphics/RenderGraphModule`
**Audience**: contributors changing the graph itself. For writing passes, see the [render graph articles](../documents/articles/render-graph/overview.md).

This page documents the implementation: compile stages, data structures, invariants, and the failure modes each stage guards. It supersedes the earlier revision, which described `SignalFence` / `SubmitQueue` / `GPUWait` opcodes and an `ExtractTexture` API that do not exist in the current code.

---

## Module map

| File | Responsibility |
|---|---|
| `RenderGraph.cs` | Public facade. Owns registry, compiler, executor, context, builder, blackboard, memory pool. Dump generation and command-stream disassembly. |
| `RenderGraphTypes.cs` | Descriptors (`RGTextureDesc`, `ViewState`), execution enums, dump record types, `RGCommandStream` sync-marker codec. |
| `RenderGraphBuilder.cs` | `IRenderGraphBuilder` / `IRaster…` / `ICompute…` / `IUnsafe…` and the single reusable implementation. |
| `RenderGraphPass.cs` | `RenderGraphPass` base + `RenderGraphPass<TPassData>` + the three typed subclasses. Pooled. |
| `RenderGraphNativePass.cs` | `NativeRenderPass` — a merged run of logical passes plus attachment info. |
| `RenderGraphNativePassBuilder.cs` | Merge decisions and load/store-op inference. |
| `RenderGraphContext.cs` | Execute-time `IUnsafeRenderContext` implementation: resource resolution, bindless indices, PSO resolution, push constants, properties. |
| `RenderGraphExecutionContext.cs` | Per-frame runtime services (engine, scheduler, allocators, final-command-buffer hook). Never cached. |
| `RenderGraphResourcePool.cs` | `RenderGraphObjectPool`, `RenderGraphResource`, `RenderGraphResourceRegistry` (import/create, size resolution, backing allocation, producer/consumer tracking). |
| `RenderGraphResourceSet.cs` | Order-preserving deduplicated `Identifier` set (linear scan over `List<T>`). |
| `RenderGraphCompiler.cs` | Orchestration: validation gate, cache lookup, culling, DAG, reorder, reachability, async schedule, emission. |
| `RenderGraphCompiler.Synchronization.cs` | Pass usage plan, queue handoff construction and emission. |
| `RenderGraphCompiler.Barrier.cs` | Usage resolution, implicit transitions, aliasing predecessors, closing barriers, `RequiresBarrierBetweenPasses`. |
| `RenderGraphAliasing.cs` | `AliasingPlan`, `PlacedResource`, `ResourceHeap` first-fit placer, `ResizeCachedSlots`, cache restore. |
| `RenderGraphResourceOrdering.cs` | Per-resource use bitmasks and the happens-before aliasing test. |
| `RenderGraphHasher.cs` | Structural graph hash (XxHash64). |
| `RenderGraphCompilationCache.cs` | Single-slot `CachedCompilation` store. |
| `RenderGraphExecutor.cs` | Command-stream interpreter, command-buffer acquisition/return, submission transaction, rollback. |
| `RenderGraphValidator.cs` | Declaration and pass-shape rules. Entire file is `#if GHOST_SAFETY_CHECKS`. |
| `RenderGraphPropertyAllocator.cs` | 1 MB persistently-mapped upload ring, 256 B alignment, raw-SRV descriptor per slice. |
| `RenderGraphBlackboard.cs` | `Type -> RenderGraphPass` map for build-time payload sharing. |
| `IRenderGraphValidationResourceProvider.cs` | Seam letting the validator read type/name without depending on the registry. Implemented by the registry. |

---

## Design invariants

**Integer indices, never object references.** `_passes` (`List<RenderGraphPass>`) is the single source of truth. Compiled passes, native passes, `CompiledBarrier`, `PlacedResource` and every command-stream entry reference passes by `int` and resources by `Identifier<RGResource>`. This is what makes a cache entry safe to hold across frames while pass objects are recycled to the pool every `Reset()`.

**Two index spaces.** *Pass index* = position in `_passes`, assigned at declaration, stable within a frame, used by the hash. *Schedule index* = position in the compiled order after culling and reordering. Everything downstream of stage 5 operates in schedule space. `PlacedResource.firstUsePass` / `lastUsePass` are schedule indices despite the name.

**Compile is deterministic.** The cache is keyed on a structural hash and several tests compare disassembled streams across frames, so dictionary enumeration order, unstable sorts, or pointer-derived values in the compile pipeline manifest as a permanently-missing cache rather than as intermittent corruption.

**Allocation discipline.** Stage-local scratch uses `AllocationManager.CreateStackScope()`. Structures whose lifetime escapes the stage (`PassDependencyNode.dependents`, `AliasingPlan`, `NativeRenderPass.mergedPassIndices`, the command `BufferWriter`) are constructed with the caller's `allocationHandle` — `BuildDAG` takes it as a parameter precisely so returning cannot rewind allocations the graph still references. DAG nodes, native passes and the command writer are disposed in `finally`.

The managed allocations that survive are the `List<T>` inside each pass's `RenderGraphResourceSet` fields. They are not pooled directly; the **pass object that owns them** is (`RenderGraphObjectPool` holds a static `ObjectPool<T>` per concrete `*RenderGraphPass<TPassData>`), and `Reset()` only calls `.Clear()`. So those buffers are paid once per concrete pass type per process.

The executor's six scratch arrays grow by `Array.Resize` until reaching steady-state capacity, after which replay performs no allocation.

> Earlier revisions of this document quoted specific performance figures (a 443 MB → 205.9 MB 4K pipeline, and a ~4–5 µs cache hit). Neither is reproducible from the source. Re-measure before restating them.

---

## `CompileAndExecute`

```csharp
public Result<RGExecution, Error> CompileAndExecute(
    in RenderGraphExecutionContext executionContext,
    ViewState viewState,
    RGExecutionFlags flags = RGExecutionFlags.Default)
```

1. `_resourceRegistry.ResolveTextureSizes(viewState)` — relative descriptors become `resolvedWidth/Height`; imported textures are skipped.
2. `RenderGraphHasher.ComputeGraphHash(_passes, _resourceRegistry)`.
3. `_compiler.Compile(viewState, graphHash, _passes, _memoryPool.AllocationHandle)`.
4. `_context.RelativeScale = graph.scale`.
5. `_executor.Execute(executionContext, _context, graph, flags, out graphicsSubmission, out computeSubmission)`.
6. For every extracted resource: `QueueReplace(dst, src)` when `ReleaseAfterExtract`, otherwise `QueueSwap(dst, src)` plus `ReleasePooledResourceDeferred(src)`. Both take effect at frame retire so in-flight readers of the persistent handle never observe a not-yet-written transient.
7. `GenerateDump(graph, viewState)` when `RGExecutionFlags.GenerateDump`.

`CompiledGraph` is a `using` local; only the cache retains artifacts. `_memoryPool` is a `MemoryPool<TLSF, TLSF.CreationOptions>` (16 B alignment, 16 MB initial chunk) used solely for its `AllocationHandle`.

---

## Compilation stages

| # | Method | Output |
|---|---|---|
| — | `RenderGraphValidator.ValidateGraph` (safety-checks only, once per distinct hash) | throws on invalid graph |
| 1 | `MarkPassesWithSideEffects` | `pass.hasSideEffects` |
| 2 | `CullPasses` | `pass.culled` |
| 3 | inline loop | `compiledPasses: UnsafeList<int>` in declaration order |
| 4 | `BuildDAG` | `UnsafeArray<PassDependencyNode>` (`passIndex`, `inDegree`, `dependents`) |
| 5 | `ReorderPasses` | `compiledPasses` permuted |
| 6 | `BuildPassReachability` | `scheduleIndexByPassIndex`, `reachability: byte[passCount²]` |
| 7 | `BuildDependencyWindowSchedule` | `effectiveQueues`, `syncBoundaries` |
| 8 | `FinalizeScheduleReachability` | `commandBufferIds`, reachability closed over segments |
| 9 | `RenderGraphResourceOrdering.Build` | use bitmasks, `_afterAllUsesMasks`, first/last schedule indices |
| 10 | `RenderGraphAliasingBuilder.Build` | `AliasingPlan`, `totalHeapSize` |
| 11 | `AllocateBackingResources` | heap + one backing `Handle<GPUResource>` per resource |
| 12 | `BuildNativeRenderPasses` | `UnsafeList<NativeRenderPass>` with inferred ops |
| 13 | `BuildExecutionCommands` | `BufferWriter` command stream |
| 14 | `SetCached` | `CachedCompilation` |

Ordering constraints: 5 before 6 (reachability is indexed by schedule position); 6 before 7 (the planner reads reachability); 7 before 8 and 8 before 9 (aliasing must see segment order); 9 before 10; 10 before 11 and 12 (merge decisions consult aliasing).

---

## Culling

```csharp
passes[i].culled = passes[i].allowCulling && !passes[i].hasSideEffects;
```

`hasSideEffects` is set when a pass writes a resource with `isImported || isExtracted`. Then the list is walked **backwards**, and for each surviving pass `UncullDependencies` recurses through `resourceReads[type]`, color attachments, `depthAccess.id`, and `randomAccess`, resolving each resource's `producerPasses` and un-culling them. Backward iteration means each decision is taken with downstream information already settled, so one pass suffices.

Culled passes are excluded from `compiledPasses` and never reach the command stream. They remain in the registry and in the dump, with `QueueDecision.Culled`.

---

## DAG construction

Per-resource `lastWriter: int` (filled `-1`) and `lastReaders: UnsafeList<UnsafeList<int>>` drive three hazard rules:

| Hazard | Edge |
|---|---|
| RAW | `ProcessRead`: `lastWriter[r] → i` |
| WAW | `ProcessWrite`: `lastWriter[r] → i` |
| WAR | `ProcessWrite`: every `lastReaders[r] → i`, then `lastReaders[r].Clear()` |

`resourceCreates` are processed as writes. `randomAccess` entries are processed as both read and write. Attachments are decomposed by `AccessFlags`, so a `ReadWrite` depth attachment contributes both. `AddEdge` skips self-edges and deduplicates against existing `dependents`, so `inDegree` counts distinct pass pairs.

Side-effecting passes are additionally chained in declaration order via `lastSideEffect`, because their ordering cannot be derived from resource data.

Nodes are allocated in the *original* compiled-pass index space; `dagIndexByPassIndex` in stage 6 maps back after reordering.

---

## Reordering

Kahn's algorithm, but the ready list is scored rather than consumed FIFO (`SelectBestCandidatePass`):

| Rule | Score |
|---|---|
| last scheduled and candidate both Raster, and `AttachmentsMatch` | +1000 |
| `SharesResources` (intersection of `resourceReads`) | +100 |
| `candidate.asyncCompute` | +50 |
| ready-list position | −0.01 × index |

`RemoveAtSwapBack` makes ready-list position unstable, so the tie-break favors earlier-listed candidates without ever overriding rules 1–3. Rule 2 shortens resource lifetimes, which is what creates aliasing opportunities in stage 10.

Early-outs when `passCount <= 1`.

---

## Reachability and the async planner

`reachability` is a `byte[passCount²]` matrix. Stage 6 fills direct edges then propagates in reverse schedule order (`source` descending), which closes transitivity because all relations point forward.

Stage 7, `BuildDependencyWindowSchedule`:

```text
if passCount < 3: return
currentGfxCbId = 0; nextCbId = 1
for candidateIndex in 0..passCount:
    if syncBoundaries[candidateIndex].isValid: continue      // already a boundary
    if !IsAsyncComputeCandidate(pass): continue
    joinIndex = FindFirstDependent(candidateIndex)           // first reachable forward pass
    if joinIndex < 0: continue
    groupEndIndex = extend while next pass is a candidate with the SAME joinIndex
    window = (groupEndIndex+1 .. joinIndex-1):
        Unsafe pass        -> legalWindow = false unless allowAsyncComputeOverlap
                              (if allowed, it also counts as independent graphics work)
        Raster pass        -> hasIndependentGraphicsWork = true
    if !legalWindow || !hasIndependentGraphicsWork: continue
    hasGraphicsProducer = any earlier pass NOT already on Compute that reaches the group
    assign effectiveQueues[group] = Compute
    computeCbId, overlapGfxCbId, joinGfxCbId = nextCbId++ (three times)
    boundaries at candidateIndex (Compute, producer = hasGraphicsProducer ? currentGfxCbId : none),
               groupEndIndex+1 (Graphics, no producer),
               joinIndex (Graphics, producer = computeCbId)
    currentGfxCbId = joinGfxCbId; candidateIndex = joinIndex
```

`IsAsyncComputeCandidate`: `type == Compute && asyncCompute && !hasSideEffects && maxColorIndex < 0 && depthAccess.id.IsInvalid && renderTargetWrites.Count == 0`.

Regions chain: each region forks on the segment its predecessor rejoined, and the producer scan skips passes already assigned to Compute, so nested regions do not wait on each other's compute work. Command-buffer IDs are monotonic from `nextCbId`.

Stage 8, `FinalizeScheduleReachability`: assigns `commandBufferIds` by walking boundaries; marks every pair ordered when they share a command-buffer ID or a queue type; adds boundary producer relations; then propagates transitively forward. This is the reachability that stage 9 consumes, so aliasing proofs include segment order.

---

## Resource ordering and aliasing

`RenderGraphResourceOrdering` stores, per resource, a bitmask of schedule indices where it is used (`_useMasks`, `WordCount = ceil(passCount/64)`), plus `_afterAllUsesMasks` and first/last schedule indices. `BuildAfterAllUsesMasks` sets bit *d* for resource *r* when every use of *r* reaches *d*:

```text
AllUsesHappenBefore(a, b) := ∀ d: (b used at d) ⇒ (a reaches d)     // bitmask subset test
CanAlias(a, b)           := AllUsesHappenBefore(a, b) || AllUsesHappenBefore(b, a)
```

Interval overlap on `[firstUse, lastUse]` is insufficient because the schedule is only a partial order; the bitmask subset test over the closed reachability relation is the actual proof.

`RenderGraphAliasingBuilder.Build` excludes imported resources, extracted resources, and `HeapType.Upload` buffers; sizes the remainder via `IResourceAllocator.GetSizeInfo`; sorts **size descending**; then first-fits into a simulated `ResourceHeap` of unbounded size (`ulong.MaxValue`) with 64 KB alignment for both textures and buffers. `CanPlaceAtOffset` refuses any partial overlap — a candidate must start at the same offset and not extend past an occupied block — so a slot is exactly one offset with one membership list. `totalHeapSize` is the peak extent aligned to 64 KB. A second loop fills `aliasedLogicalResources` by comparing offsets pairwise (O(slots²)).

`ResizeCachedSlots` handles cache-hit-with-growth: it walks cached placements, reuses the representative offset for members sharing an old offset, re-derives each slot's size as the max aligned size of its members, and lays slots out sequentially. Alias membership is preserved verbatim, so command bytes stay valid.

`AllocateBackingResources` releases any previous heap, then allocates one heap of `totalHeapSize + 65536` at 64 KB alignment with `HeapFlags.AllowAllBufferAndTexture` / `HeapType.Default`, and creates each non-imported resource as `ResourceAllocationType.Suballocation` at its `heapOffset`. Extracted resources instead take `CreatePooledTexture` / `CreatePooledBuffer`; upload buffers take a dedicated committed buffer. Only suballocated and upload handles are appended to `_allocatedBackingResources`.

---

## Barrier state model

State is `ResourceBarrierData(layout, access, sync)`. The compiler tracks `CompiledResourceState(state, isValid, writes)` per resource, seeded from `initialBarrierState` for imported resources that declare one and `Undefined` otherwise.

`ResolvePassResourceUsages` folds declarations into one `ResolvedPassResourceUsage` per resource, and `ApplyUsage` merges: identical `(usageClass, layout, access)` OR-folds `sync` and raises priority; otherwise higher priority wins, with `Explicit > GenericWrite > GenericRead > None`. Compute-pass writes resolve to `UnorderedAccess`; raster and unsafe writes resolve to `usageClass: None` and are rejected earlier by the validator.

Read target states come from `GetBufferReadBarrierData`: textures → `ShaderResource/ShaderResource` with a pass-type sync mask (compute `ComputeShading`, raster `PixelShading|NonPixelShading`, unsafe `All`); buffers → `layout: Undefined` with `IndirectArgument/ExecuteIndirect` when `BufferUsage.IndirectArgument` is set, else `ShaderResource`. Note that buffers are layout-agnostic throughout — only `access` and `sync` carry information for them.

`BuildPassResourceUsagePlan` flattens per-pass usages into one array plus `PassResourceUsageRange[start,count]`, so emission is allocation-free per pass.

### Emission

`EmitPassPrologueBarriers` reserves `IssueBarriers` + a placeholder count, writes handoff acquires then `EmitImplicitTransitions`, and backpatches the count — rewinding entirely when zero barriers were produced. For a native pass, `EmitPassPrologueBarriersForMergedPasses` hoists the prologues of all merged passes before `BeginNativePass`, because transitions cannot occur inside an open render pass.

`EmitImplicitTransitions` per usage:

1. A queued acquire for this (scheduleIndex, resource) subsumes the transition: record state, emit nothing.
2. `TryGetAliasingPredecessor` — first use of a non-imported resource sharing a slot with more than one member — selects the predecessor with the largest last-use schedule index that provably precedes it, and sets `FirstUsage | Discard`.
3. Exact state match elides, **unless** `forceUavOrdering`: same state, `access == UnorderedAccess`, and either side writes. That case emits with `BarrierFlags.Force`, because a UAV WAW would otherwise be deleted by state equality.
4. Invalid tracked state adds `FirstUsage | Discard`; valid state adds `ExplicitSource`.

`EmitClosingBarriers` restores `finalBarrierState` for imported resources, including the never-used case (compare `initialBarrierState` to `finalBarrierState`).

### Queue handoffs

`BuildQueueHandoffs` keeps `LastScheduledResourceUse[resourceCount × 2]` (one slot per tracked queue) and creates a `QueueHandoff` when a use lands on the opposite queue from the last recorded one, the command-buffer IDs differ, at least one side writes, and the producer reaches the consumer. Duplicates fold by OR-ing target `access` and `sync`. Handoff state is `Common` for textures, `Undefined` for buffers, always `NoAccess`/`None`.

A second pass handles aliasing successors, since the predecessor is a different logical ID; those acquires also carry `FirstUsage | Discard`.

Release barriers are emitted at the boundary that ends the producer's segment (`FindCommandBufferEndBoundary`); acquires in the consumer's prologue.

---

## Native render passes

`BuildNativeRenderPasses` walks schedule order. Non-raster passes close the current native pass and execute outside any render pass. A raster pass merges when all five hold:

1. no `syncBoundaries[i].isValid` at this schedule index (boundary splits the run);
2. neither side has random-access resources (`pass.randomAccess.Count > 0 || nativePass.allowUAVWrites`);
3. `AttachmentsMatch` — same color count, same texture per slot, same depth texture;
4. `HasOnlyAttachmentUsages` — every create/read/write resource is one of the pass's attachments;
5. `!RequiresBarrierBetweenPasses` — no non-imported write at its first use that shares an aliasing slot and is a render target of the pass.

`InferLoadStoreOps` then runs per native pass using its first and last merged schedule indices:

| Load op | Condition |
|---|---|
| `Clear` (+ descriptor clear values) | `IsFirstUse(resource, firstScheduleIndex)` and `clearAtFirstUse` |
| `DontCare` | `IsFirstUse` and not `clearAtFirstUse` |
| `DontCare` | `AccessFlags.Discard` set |
| `Load` | `AccessFlags.Read` set, or plain continuation |

| Store op | Condition |
|---|---|
| `Store` | not the last use |
| `Store` | last use and (depth: `isImported \|\| isExtracted`, or not `discardAtLastUse`) |
| `DontCare` | last use and `discardAtLastUse` |

Stencil ops mirror depth only when `format.IsStencilFormat()` (currently `D24_UNorm_S8_UInt`); otherwise `NoAccess`.

---

## Command stream

`RGExecutionOpType` is a `byte` opcode; payloads are fixed-width and written through `BufferWriter`, read through `SpanReader`.

| Opcode | Payload |
|---|---|
| `IssueBarriers` | `int count`, then `count × CompiledBarrier` |
| `BeginNativePass` | `int nativePassIndex` |
| `ExecutePass` | `int logicalPassIndex` |
| `EndNativePass` | — |
| `CommandBufferSyncPoint` | `CommandQueueType nextType`, `int producerCount`, `int[] producerIds` |

`CompiledBarrier`: `resource`, `sourceState`, `handoffState`, `targetState`, `aliasingPredecessor`, `flags: BarrierFlags`, `resourceType`, `sourceQueue`, `destinationQueue`.

`BarrierFlags`: `FirstUsage`, `Discard`, `ExplicitSource`, `Force`, `QueueRelease`, `QueueAcquire`.

`RGCommandStream.WriteSyncMarker` validates that every producer ID is strictly less than the next command-buffer ID and contains no duplicates; `ValidateProducerIds` throws `ArgumentException` otherwise. Relative IDs are assigned in stream order: the first buffer is 0 and each marker ends buffer N and starts N+1.

Three structural checks in `BuildExecutionCommands` throw `InvalidOperationException`: non-contiguous command-buffer IDs across a boundary, a pass whose effective queue disagrees with its boundaries, and a sync boundary inside a native pass.

---

## Executor and submission

`RenderGraphExecutor.Execute` acquires a Graphics command buffer from `FrameScheduler.GetPooledCommandBuffer`, calls `context.BeginNewFrame` (which re-binds frame and view CBVs), then interprets the stream:

- `IssueBarriers` → `ExecuteBarrierBatch`, decoding into `BarrierDesc` via `BarrierDesc.Texture` / `.Buffer`, flushing every 64 into one `ICommandBuffer.Barrier` call. `QueueRelease` is skipped under `ForceGraphics`; `QueueAcquire` under `ForceGraphics` falls through to the generic transition path, so acquires become plain source→target barriers rather than disappearing.
- `BeginNativePass` → `SetViewport` (from depth attachment, else color slot 0), build `PassRenderTargetDesc[]` + `PassDepthStencilDesc`, `BeginRenderPass(..., allowUAVWrites)`, then `context.SetRenderTargetFormats` so PSO keys pick up the attachment formats.
- `ExecutePass` → `graph.passes[idx].Execute(context)`.
- `EndNativePass` → `EndRenderPass`.
- `CommandBufferSyncPoint` → end the active buffer, acquire the next on `marker.NextCommandBufferType` with the marker's producer IDs, `BeginNewFrame` again. Throws if inside a native pass.

After the stream, `OnFinalGraphicsCommandBuffer` runs only when the final active buffer is Graphics. `SubmitCommandBuffers` opens a `SubmissionTransaction`, submits every buffer (ownership transfers on `Submit`), adds `AddDependency` only where queue types differ, and commits. `RGExecution` carries the terminal Graphics submission and possibly-invalid Compute submission.

Rollback paths: barrier or submit errors and pass-lambda exceptions call `RollbackRecording` (end the open render pass if needed, end the buffer, return every acquired buffer exactly once); submission failures additionally `RollbackSubmissionTransaction`. Either way `Execute` returns an `Error`.

---

## Graph hash

`ComputeGraphHash` writes into a `BufferWriter` and returns `XxHash64.HashToUInt64`.

**Resource definitions first, in registry order**, preceded by `ResourceCount`: `type`, `isImported`, `isExtracted`, and when extracted the `extractionTarget` hash plus flags; `hasInitialBarrierState` and the state when present; `hasFinalBarrierState` and the state when present. Then per type — imported textures: format, dimension, usage, width, height, mipLevels, slice. Transient textures: format, sizeMode, then width/height (Absolute) or scaleX/scaleY (Relative), dimension, mipLevels, slice, usage, clearAtFirstUse, discardAtLastUse, clearColor, clearDepth, clearStencil. Buffers: `Size`, `Stride`, `Usage`, `HeapType`.

**Then passes**, preceded by count: `type`, `allowCulling`, `asyncCompute`, `allowAsyncComputeOverlap`, `WritesExternalResource`, depth attachment as (`id.Value`, `accessFlags`, `usage`), `maxColorIndex`, then per color slot (`id.Value`, `accessFlags`, `usage`), then every `resourceReads` / `resourceWrites` / `resourceCreates` set per type plus `randomAccess` and `renderTargetWrites`, then `GetRenderFuncHashCode()`.

Resource sets are **sorted by ID** before writing (`WriteResourceSet` copies to scratch, sorts, writes), so declaration order within a set does not affect the hash.

Excluded by design: pass and resource names, `ViewState` and resolved dimensions, `TPassData` values, and backing handles for imported textures.

`GetRenderFuncHashCode` is `RuntimeHelpers.GetHashCode(renderFunc.Method)`, XOR-ed with the target's runtime identity hash when the delegate has one. A per-frame closure therefore changes the hash and forces recompilation — the safe failure mode.

---

## Compilation cache

`RenderGraphCompilationCache` is single-slot: `_cachedHash`, `_hasCachedData`, one `CachedCompilation`. `TryGetCached` compares hashes; `SetCached` disposes the prior entry before building the new one.

`CachedCompilation` holds: `compiledPassIndices`, `passCulledFlags`, `nativePasses` (merged-index lists deep-copied), `logicalToPhysical`, `placedResources` + flattened `aliasedLogicalResources`, `totalHeapSize`, `resourceFirstUseScheduleIndices`, `resourceLastUseScheduleIndices`, `backingResources`, `commandBytes`, `viewState`.

On a perfect hit, `RestoreFromCache` re-applies `passCulledFlags` and rebuilds an `AliasingPlan` from the cached placements; `RestoreBackingResources` re-points non-imported resources at cached handles, except extracted ones which always take a fresh pooled handle. On a hit with growth, `ResizeCachedSlots` plus `AllocateBackingResources` run instead and `cached.viewState` is updated.

`UpdateBackingResource(logicalIndex, handle)` is called from `AllocateBackingResources` per resource so a partially-failed allocation cannot leave a stale cached handle.

---

## Validation

`RenderGraphValidator` is wholly `#if GHOST_SAFETY_CHECKS`. `ValidateDeclaration` runs per builder call (random access, render target, color and depth attachment) and `ValidatePass` runs at `Dispose()`; `ValidateGraph` runs at compile, memoized on `_validatedGraphHash` so a structurally identical frame is validated once.

Rules: render function required; raster needs slot 0 color or a depth attachment; non-raster may not declare attachments; only unsafe may declare `renderTargetWrites`; attachment/depth/UAV usage classes conflict per resource (`GetConcreteUsageGroup`: color = 1, depth = 2, UAV = 3; conflict when both non-zero and unequal); attachment and depth usage require a texture; each resource set must match its declared `RGResourceType`; generic writes on non-compute passes require an explicit attachment, random-access or unsafe usage.

The builder's `Reject` sets `_faulted` and throws; `Dispose` then short-circuits so the first rejection is the only error surfaced. `ThrowIfDisposed` is `DEBUG`-only, while `Reject` and validation are safety-checks-only, so a Release frame performs no validation at all.

---

## Diagnostics

`GenerateDump` re-reads the command stream through `DisassembleCommandStream`, which decodes opcodes into `List<string>` while reconstructing effective queues (`effectiveQueues`), per-pass sync boundaries (`syncBoundariesBefore/After`), and command-buffer types. Barrier lines are rendered in four shapes — `QueueRelease`, `QueueAcquire`, `Aliasing`, `Transition` — chosen by flags and `aliasingPredecessor` validity.

`GetQueueDecision` derives `RGQueueDecision` from culled state, pass type, the async hint, and the effective queue. `PassDumpInfo.NativePassIndex` is found by scanning `nativePasses` for one containing the pass index, defaulting to `NativeRenderPass.Invalid` (`index = -1`), so it is `-1` for compute, unsafe, and culled passes.

`ResourceDumpInfo` reports both lifetime pairs: `FirstUsePass` / `LastUsePass` from the registry (pass-index space) and `ScheduledFirstUseIndex` / `ScheduledLastUseIndex` from the compiled ordering (schedule-index space). Only the scheduled pair feeds aliasing and barriers.

---

## Known source issues

Surfaced while documenting; none are fixed as of this writing.

1. **`RenderGraphUtility` copy/clear helpers fail validation.** `AddCopyBufferPass`, `AddCopyTexturePass`, `AddClearRenderTargetPass` and `AddClearDepthStencilPass` declare a generic write on an **unsafe** pass, which `ValidatePass` rejects with *"generic writes are ambiguous for Unsafe passes"* at builder disposal. They throw in safety-checks builds and have no engine call sites. `AddClearBufferPass` (uses `UseRandomAccessBuffer`) and `AddBlitPass` (raster + attachment) are unaffected. Fix: migrate to `UseRenderTargetTexture` or `UseRandomAccess*`.
2. **`GPUResourceLeakException` is unwired.** `ThrowIfRefCountNonZero` has no call site anywhere in the solution.
3. **`PassDumpInfo.AsyncCompute` and `AsyncRequested` are redundant.** Both are assigned `pass.asyncCompute` in `GenerateDump`; `EffectiveQueue` and `QueueDecision` are the informative fields.
4. **HZB history reads may be discarded.** `GhostRenderPipeline.cs` imports the view's HZB texture without `initialState`, and meshlet cull pass 1 reads it before anything writes it this frame. An imported resource without `hasInitialBarrierState` seeds an invalid tracked state, so `EmitImplicitTransitions` attaches `FirstUsage | Discard`. Confirm whether the import needs an explicit initial state.
5. **Stale test name.** `TestAsyncPlanner_PassZeroComputeWithFewerThanFourPassesSucceeds` predates the async threshold being lowered from four to three compiled passes.
