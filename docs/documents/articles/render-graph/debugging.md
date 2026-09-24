# Debugging Render Graph Passes

## Current status

**Debugging tooling for the render graph is work in progress.** Today there is exactly one graph-specific diagnostic: a **text-based dump** (`RenderGraphDump`) with a disassembled command stream. There is no graph visualizer, no lifetime chart, no immediate mode, no per-resource transition log, and no clobber or extend-lifetime switches. Expect to spend time reading text and correlating it against an external GPU capture.

What exists is enough to answer the questions that actually block work — did my pass run, where did it run, what state did it leave its resources in, why is this memory still allocated — but the workflow is manual. Plan around that rather than assuming parity with a mature toolchain.

## Why a graph changes the debugging model

Four things stop being true once passes go through the graph, and each one is a surprise the first time:

- **Your pass may not run at all.** Culling removes it silently. No warning, no error, no trace in the capture.
- **Your pass may not run where you put it.** Reordering moves it, and async scheduling moves it to a different command-buffer segment. The order you authored is a suggestion.
- **Your resource may share an address with another resource.** Aliasing means two textures occupy the same heap offset at different times. Contents before first write are undefined, and a capture tool shows one allocation with two names.
- **A crash stack points at the executor, not at your build code.** Pass lambdas run during replay, long after the frame was assembled, inside an interpreter loop. The stack tells you `RenderGraphExecutor.Execute`, not which `AddComputeRenderPass` call created the pass.

The dump is the bridge back from replay to authoring. Learn its two index spaces and it stops being a wall of text.

## What reaches a GPU capture

This is the single most useful thing to know before opening Pix or Nsight Graphics, because the answer is "less than you'd hope".

| Graph concept | Visible in a capture? | How |
|---|---|---|
| Resource names | **Yes**, in `GHOST_SAFETY_CHECKS` builds | Applied through `ID3D12Object::SetName` and `D3D12MA_Allocation::SetName` when the resource database registers a resource or allocation. In release, names degrade to `Resource_<index>`. |
| Swap-chain back buffers | **Yes** | Named `SwapChain_BackBuffer_<i>` unconditionally. |
| Pass names (`AddRasterRenderPass("MyPass")`) | **No** | Used only by the dump. `BeginRenderPass` has no name parameter and no event markers are emitted anywhere in the D3D12 backend. |
| Native render pass boundaries | **Yes**, structurally | Real `BeginRenderPass` / `EndRenderPass` pairs, so attachments and clear values are visible per pass. |
| Barriers and transitions | **Yes** | Emitted as ordinary D3D12 resource barriers; aliasing and discard flags come through. |
| Command-buffer segments | **Yes**, as separate submissions | Each segment is its own command list submitted through `FrameScheduler`, so queue boundaries are visible as submission boundaries. |
| Async compute overlap | **Yes** | Graphics and compute submissions interleave on the GPU timeline. |
| Bindless descriptor indices | **Yes** | Properties arrive as raw-SRV indices into a bindless heap; inspect via the descriptor, not a name. |

So the correlation key is **the resources on the attachments**, not the pass name. Build a safety-checks build before capturing, or every texture in the frame is `Resource_17` and you are correlating by format and dimensions alone.

Practical consequence: to identify "my pass" in a capture, look for a render pass whose attachments are the textures you named, or a dispatch whose UAV descriptors resolve to your named buffer. The dump's `ExecutePass` lines give you the same anchor in schedule order, which is why the two are read together.

## Getting a dump

```csharp
var execution = renderGraph
    .CompileAndExecute(executionContext, viewState, RGExecutionFlags.GenerateDump)
    .GetValueOrThrow();

RenderGraphDump dump = execution.Dump!;

Logger.Info($"Hash 0x{dump.GraphHash:X16}  cacheHit={dump.IsCacheHit}  heap={dump.TotalHeapSize}");
foreach (var line in dump.CommandStream)
{
    Logger.Info(line);
}
```

The dump is opt-in because it allocates managed `List<T>` contents and walks the whole graph. Never enable it in a frame you care about measuring. `Dump` is null unless the flag is set.

`dump.CommandStream` is the disassembled form of the binary stream — the same bytes the executor interpreted, so the line order *is* the execution order. `dump.Passes` and `dump.Resources` are lookup tables keyed by the indices that appear in those lines.

## Reading the dump

### Passes

`PassDumpInfo` per authored pass: `Index` (pass index), `Name`, `Type`, `IsCulled`, `NativePassIndex`, `AsyncCompute` / `AsyncRequested`, `EffectiveQueue`, `QueueDecision`, `SyncBoundaryBefore` / `SyncBoundaryAfter`, and the read/write/create resource ID lists.

Read `QueueDecision`, not `AsyncCompute` — both async booleans currently mirror the same `EnableAsyncCompute` hint and cannot tell you what happened. `EffectiveQueue` is the queue the pass was actually recorded on. `NativePassIndex` is `-1` for compute, unsafe, and culled passes: every surviving raster pass belongs to exactly one native pass, even when it merges with nothing and becomes a single-pass render pass of its own.

### Resources

`ResourceDumpInfo` per registry entry: `LogicalResourceId`, `Name`, `Type`, `SizeInBytes`, `HeapOffset`, `IsImported`, `IsExtracted`, `ProducerPass`, `ConsumerPasses`, `AliasedWithResources`, and **two lifetime pairs** — `FirstUsePass`/`LastUsePass` in pass-index space (from the registry, as declared) and `ScheduledFirstUseIndex`/`ScheduledLastUseIndex` in schedule-index space (after culling and reordering).

Only the scheduled pair feeds aliasing and barriers. When they disagree, the scheduled pair is the truth about the frame and the declared pair is the truth about your intent — which is exactly the gap you are usually hunting.

### Memory blocks

`MemoryBlocks` groups placements by `heapOffset`. Each `HeapBlockDumpInfo` lists every logical resource occupying that slot, so one block with four `AliasedLogicalResources` is four textures in 64 KB of VRAM instead of four allocations. `TotalHeapSize` is the graph's whole transient footprint.

### The command stream

```text
[0000] IssueBarriers (2 barriers)
       ├─ Transition: SceneColor [Texture #3], Source: [Layout: Undefined, Access: NoAccess, Sync: None], Target: [Layout: RenderTarget, Access: RenderTarget, Sync: RenderTarget], Flags: FirstUsage, Discard
       ├─ Aliasing: DepthMip0 [Texture #7] -> GBuffer0 [Texture #9] -> Layout: UnorderedAccess, Flags: FirstUsage, Discard
[0001] BeginNativePass #0 (ColorCount: 2, HasDepth: True)
[0002] ExecutePass #4 'DepthPrepass' [Raster] -> AsyncRequested: False, EffectiveQueue: Graphics, QueueDecision: AsyncNotRequested
[0003] EndNativePass
[0004] CommandBufferSyncPoint -> SourceType: Graphics, NextType: Compute, DependsOn: [0], DependencyTypes: [Graphics]
[0005] ExecutePass #2 'MeshletCull_Pass1' [Compute] -> AsyncRequested: True, EffectiveQueue: Compute, QueueDecision: AsyncComputeSelected
[0006] CommandBufferSyncPoint -> SourceType: Compute, NextType: Graphics, DependsOn: [none], DependencyTypes: [none]
```

Four barrier line shapes, and each one names its cause:

| Line | Meaning |
|---|---|
| `Transition: ... Flags: FirstUsage, Discard` | First use of a fresh transient. Nothing to transition *from*; contents are undefined until written. |
| `Aliasing: A -> B -> <state>` | B is taking over A's heap slot. The aliasing/discard barrier that makes reuse legal. |
| `QueueRelease: ... Graphics -> Compute` | End of a segment; A is handed off. `Access: NoAccess, Sync: None` is expected here. |
| `QueueAcquire: ... Graphics -> Compute` | Start of the consumer segment on the new queue, taking the resource out of handoff state. |

A plain `Transition` with `Flags: ExplicitSource` is the ordinary case. `Force` means the resource was already in the target state and the barrier exists only to order a UAV write-after-write — see [Synchronization](synchronization.md).

`DependsOn: [1]` lists producer **command-buffer IDs** in stream order, not pass indices: the first buffer is 0 and each `CommandBufferSyncPoint` ends buffer N and starts N+1. Three consecutive sync points are one async region — fork to Compute, return to Graphics for the overlap, then join.

## Recipes

**"My pass produced nothing."** Check `IsCulled` in `dump.Passes`. If true, the resource it writes has an empty `ConsumerPasses` list — either nothing declares a read of it, or the consumer was itself culled. The fix is a declaration, or `AllowPassCulling(false)` if the side effect is something the graph cannot see.

**"My pass ran at the wrong time."** Find its `ExecutePass #N` line — `N` is the **pass index**, matching `PassDumpInfo.Index` — and note the line's position. The `[NNNN]` prefix is the disassembly instruction counter, incremented for every opcode including barriers and sync points, so it is *not* a schedule index; two adjacent passes can be three counters apart. If your pass moved from where you authored it, either a dependency permitted it or the reorder heuristic pulled it toward a pass sharing its resources. To force adjacency, declare a resource dependency — see [Compilation and Caching](compilation.md).

**"My output is garbage on the first frame."** Look for `FirstUsage, Discard` on that resource. Aliasing guarantees nothing about prior contents; if the shader reads before writing, the read is undefined. Either set `clearAtFirstUse` with a clear value, or declare `Read` so the graph loads instead of discarding.

**"My compute pass isn't overlapping."** Read `QueueDecision`:

| Value | Cause |
|---|---|
| `AsyncNotRequested` | `EnableAsyncCompute` was never called |
| `IneligiblePassType` | Not a compute pass |
| `Culled` | The pass was culled |
| `NoLegalOverlapWindow` | Requested, but no legal window exists |

`NoLegalOverlapWindow` means one of: fewer than three compiled passes; no independent graphics work between the pass and its first consumer; an unsafe pass in that window without `AllowAsyncComputeOverlap`; or the pass has a color/depth attachment or side effects, which disqualifies it as a candidate.

**"Why is there an extra barrier?"** Match the barrier line to the flags. `Force` is UAV WAW ordering. `Aliasing` is a heap-slot handover. `QueueRelease`/`QueueAcquire` is a segment crossing. `ExplicitSource` alone is a legitimate state change. If you see a transition you cannot explain, the resource's `ProducerPass`/`ConsumerPasses` lists show which passes the compiler believes touch it — a pass you forgot to declare shows up here as an absent consumer.

**"Memory didn't shrink."** Expected. Viewport growth reallocates placements; shrink reuses the larger backing at a smaller viewport, deliberately, to avoid reallocation and recompilation. `TotalHeapSize` holds at the high-water mark. See [Resources and Aliasing](resources.md).

**"Every frame recompiles."** Compare `GraphHash` across frames. Something structural varies per frame — usually a descriptor built from frame-varying data (use `Relative` sizing instead), a resource created in a different order, or a capturing lambda, whose different target object changes `GetRenderFuncHashCode()` each frame.

**"I think a cached entry is stale."** `renderGraph.InvalidateCache()` forces a full recompile next frame. If the problem disappears, the two frames' hashes matched when they shouldn't have — that is a hash bug, and worth reporting rather than working around.

## Isolating a pass

The graph has no single-pass mode, so build one:

```csharp
_renderGraph.Reset();
using (var builder = _renderGraph.AddComputeRenderPass<MyData>("SuspectPass"))
{
    builder.AllowPassCulling(false);
    // ... declare only the suspect pass's resources ...
}
var execution = _renderGraph.CompileAndExecute(executionContext, viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
```

Three techniques that pay off repeatedly:

- **Collapse to one queue** with `RGExecutionFlags.ForceGraphics`. Every segment records on Graphics, queue-release barriers are dropped and acquires degrade to plain transitions, so the frame becomes a single sequential submission. Cross-queue races disappear; if the artifact goes away, the bug is in a handoff. This is the closest thing to a "disable async compute" switch.
- **Pin a resource across frames** by importing it instead of leaving it transient, so a capture can show you its contents at several points in time rather than one aliased snapshot. Imported resources are excluded from aliasing, which makes them boring and readable.
- **Extract what you want to see.** `QueueTextureExtraction` into a persistent handle gives a named, non-aliased resource you can inspect in the capture tool's texture viewer. This is how you look at an intermediate the graph would otherwise recycle.

Note that `ForceGraphics` changes only what the executor emits, not what compilation planned — the dump still reports the planned async decisions, so you can read the schedule while replaying serially.

## Working with external captures

Since pass names do not survive into captures:

- **Capture a safety-checks build.** Resource names are the only reliable correlation key, and they cost nothing in release.
- **Anchor on the back buffer.** `SwapChain_BackBuffer_0` is named unconditionally and is always the imported color target of the final pass, so it locates the end of the frame in any capture.
- **Use native render pass structure.** Merged passes appear as one D3D12 render pass with all attachments and clear values visible. A pass you expected to merge but which appears alone in the capture did not merge — go back to the merge gates on [Synchronization](synchronization.md).
- **Expect one allocation, several names.** Aliased resources share a heap offset. A texture viewer showing stale-looking contents at an offset may be showing a different logical resource that has not been written yet in this frame.
- **Work-graph dispatches differ.** The engine's meshlet cull uses `SetProgram` / `DispatchGraph`, which appears as GPU-work-graph evidence rather than a plain dispatch, and its backing-memory initialization flag is latched only after a successful execution.
- **Correlate by index.** Take the `ExecutePass #N` line at the point of interest, then look up `dump.Passes` for `N` to get the name, type, and resource lists. That is the intended handoff between the text dump and the capture.

For GPU-side performance analysis of a captured frame — per-pass timing, draw-call inspection, NVTX/D3DPERF stage attribution — the `nsight-graphics-analyzer` tooling drives Nsight Graphics from the command line, which is useful once you know *which* submission in the capture corresponds to your pass.

## Validation errors

Under `GHOST_SAFETY_CHECKS`, declaration mistakes throw `InvalidOperationException` at the point of the bad call rather than producing a broken frame. Messages name the pass, the resource, and the conflict:

| Message | Cause |
|---|---|
| `does not have a render function` | `SetRenderFunc` never called |
| `must have at least one color or depth attachment` | Raster pass with no attachments |
| `declares raster attachments but has pass type X` | Attachment on a compute or unsafe pass |
| `declares unsafe render-target usage but has pass type X` | `UseRenderTargetTexture` outside an unsafe pass |
| `generic writes are ambiguous for X passes` | `Write` declared on a raster or unsafe pass without an attachment or UAV usage |
| `X conflicts with Y for the whole-resource range` | Two usage classes on one resource in one pass |
| `X requires a texture resource` | Attachment or depth usage on a buffer |
| `the read declaration records it as Buffer` | Same identifier declared as both types |
| `Color attachment at index N is already set to a different texture` | Slot reuse with a different texture |
| `Depth attachment is already set to a different texture` | Same, for depth |

Validation runs in three phases: per declaration inside the builder, per pass at `Dispose()`, and whole-graph at compile — the last only once per distinct graph hash. The first rejection sets a fault flag, so disposal after a rejection stays quiet instead of raising a second error that hides the real one. Read the **first** exception, not the last.

Outside safety-checks builds all of this compiles away, and the mistakes become GPU artifacts instead. The tests in `src/Test/Ghost.UnitTest/Graphics/RenderGraph*Test.cs` exercise the same rules against a mocking device, which is the other way to get a fast answer about whether the graph is behaving.

## Known gaps

| Capability | Status | Use instead |
|---|---|---|
| Graph visualizer / lifetime chart | Not implemented | `dump.MemoryBlocks` plus `ScheduledFirstUseIndex`/`ScheduledLastUseIndex`, read by hand |
| GPU event markers per pass | Not implemented | Native render pass structure and attachment resource names in the capture |
| Immediate mode (bypass compilation) | Not implemented | `RGExecutionFlags.ForceGraphics`, plus a single-pass graph |
| Per-resource transition log | Not implemented | Filter `dump.CommandStream` lines by resource name |
| Clobber / extend-lifetime switches | Not implemented | Import the resource, or extract it, to remove it from aliasing |
| GPU timing | Not graph-specific | Capture in Pix or Nsight Graphics and read the submission timeline |

Each gap has the same workaround shape: **reduce the graph until the behavior is obvious** — one queue, one pass, imported resources — then add structure back one piece at a time.

## Next

- [Overview](overview.md) to re-read the model, or start applying this in `src/Runtime/Ghost.Engine/RenderPipeline/`.
