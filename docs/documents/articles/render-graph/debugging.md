# Debugging and Validation

## Work in Progress

**Graph debugging tooling is early.** There is one graph-specific diagnostic — a text **dump** listing passes, resources, memory placement, and a disassembled command stream. There is no visualizer, no lifetime chart, no per-pass GPU markers, and no switch to disable aliasing or clobber resources. Day-to-day debugging means reading that text alongside a capture from Pix or Nsight Graphics.

It answers the questions that block work — did my pass run, where did it run, what touched my resource — but manually. If you are coming from a toolchain with a graph inspector, expect to be slower here for now.

## What Changed About Debugging

Four things stop being true once passes go through the graph, and each one is a surprise the first time:

- **Your pass may not run at all.** Culling removes it silently. No warning, no error, no trace in a capture.
- **Your pass may not run where you put it.** Reordering moves it; async compute moves it to a different command list. The order you authored is a suggestion.
- **Your resource may share an address with another one.** Two textures occupy the same memory at different times, so a capture tool shows one allocation under several names, and contents before the first write are undefined.
- **A crash points at the executor, not at your code.** Render functions run during replay, long after the frame was assembled, so the stack says `RenderGraphExecutor.Execute` rather than naming your pass.

The dump is the bridge back from replay to your code. Everything below is about reading it.

## Was My Pass Culled?

Get a dump and look at `IsCulled` for your pass:

```csharp
var execution = renderGraph
    .CompileAndExecute(executionContext, viewState, RGExecutionFlags.GenerateDump)
    .GetValueOrThrow();

RenderGraphDump dump = execution.Dump!;
var mine = dump.Passes.Single(p => p.Name == "MyPass");
Logger.Info($"culled={mine.IsCulled} queue={mine.EffectiveQueue} decision={mine.QueueDecision}");
```

If it was culled, find the resource it writes in `dump.Resources` and check `ConsumerPasses`. An empty list means nothing declared a read of it — usually a missing `UseTexture` / `UseBuffer`, sometimes a consumer that was itself culled. The fix is the declaration, or `AllowPassCulling(false)` when the pass's real effect is something the graph cannot see.

A culled pass has no `ExecutePass` line in `dump.CommandStream` at all, which is why "nothing happened" is otherwise hard to distinguish from "it ran and produced nothing".

## Why Is My Compute Pass Not Overlapping?

Read `QueueDecision`:

| Value | Cause |
|---|---|
| `AsyncNotRequested` | `EnableAsyncCompute(true)` was never called |
| `IneligiblePassType` | Not a compute pass, or it has attachments or side effects |
| `Culled` | The pass was culled |
| `NoLegalOverlapWindow` | Eligible, but there is nothing to overlap it with |
| `AsyncComputeSelected` | It ran on the compute queue |

`NoLegalOverlapWindow` almost always means the frame has no independent raster work between this pass and the first pass that reads its results — which is a graph-shape problem, not a flag problem. Moving the consumer later, or doing unrelated graphics work in between, is what unlocks it. A compute pass at the very end of a frame can never overlap.

Nothing warns when the graph declines. `EffectiveQueue` is what the pass actually ran on, and `AsyncCompute` / `AsyncRequested` in the dump only echo your hint, so read `QueueDecision` and `EffectiveQueue` and ignore the other two.

## Why Is There an Extra Barrier?

`dump.CommandStream` lists every barrier, and each line names its cause:

```text
[0000] IssueBarriers (2 barriers)
       ├─ Transition: SceneColor [Texture #3], Source: [...], Target: [Layout: RenderTarget, ...], Flags: FirstUsage, Discard
       ├─ Aliasing: DepthMip0 [Texture #7] -> GBuffer0 [Texture #9] -> Layout: UnorderedAccess, Flags: FirstUsage, Discard
[0001] BeginNativePass #0 (ColorCount: 2, HasDepth: True)
[0002] ExecutePass #4 'DepthPrepass' [Raster] -> EffectiveQueue: Graphics, QueueDecision: AsyncNotRequested
[0003] EndNativePass
[0004] CommandBufferSyncPoint -> NextType: Compute, DependsOn: [0]
```

| Line | Meaning |
|---|---|
| `Transition` with `FirstUsage, Discard` | First use of a fresh transient. Contents are undefined until something writes it. |
| `Aliasing: A -> B` | B is taking over A's memory. |
| `QueueRelease` / `QueueAcquire` | A resource crossing between the graphics and compute command lists. |
| `Transition` with `Force` | Ordering, not a state change — two compute passes writing the same UAV. |

The `#N` after `ExecutePass` is the pass index, matching `PassDumpInfo.Index`; the `[NNNN]` prefix is only a line counter and is *not* the execution position of the pass.

If a barrier's resource shows `ProducerPass` and `ConsumerPasses` lists that do not match what your code actually does, you have a declaration bug: an undeclared access is invisible, and an over-broad one (a `Read` nothing performs, a UAV where an attachment belonged) costs a transition that would not otherwise exist.

## My Resource Has Garbage In It

Look for `FirstUsage, Discard` on that resource. A transient's memory held something earlier in the frame and the graph discards it, so a shader that reads before writing reads garbage. Two fixes, and which one is right depends on intent:

- **The read is supposed to see this frame's data.** Something is missing upstream — check `ProducerPass` and whether that pass was culled.
- **The read is supposed to see a previous frame's data.** The resource must not be transient. Import it, and give the import an `initialState` so the graph knows not to discard.

For an attachment, `clearAtFirstUse` with a clear value is the cheaper answer. For a buffer, clear it explicitly with `ctx.ClearBuffer`.

## Why Did My Frame Recompile?

Compare `dump.GraphHash` across two frames. If it changes every frame, something structural varies — most often a resource descriptor built from frame-varying data (use `Relative` sizing, or move the value into pass data), or a render function written as a capturing lambda, whose closure changes the hash each frame. A hash that never changes but always misses means two graphs are alternating on one `RenderGraph` instance.

`renderGraph.InvalidateCache()` forces a recompile next frame. If that makes a problem disappear, the hashes matched when they should not have — that is a bug worth reporting rather than working around.

## Finding Your Pass in a Capture

Pass names never reach the GPU. They exist only in the dump: the D3D12 backend emits no event markers, and `BeginRenderPass` has no name slot. So correlate on **resources** instead:

- **Build with `GHOST_SAFETY_CHECKS`.** Resource names are applied to D3D12 objects only in those builds; in release every texture is `Resource_17` and you are left matching on format and size.
- **Anchor on the back buffer.** `SwapChain_BackBuffer_0` is always named, and is the final pass's color target, so it locates the end of the frame.
- **Find the render pass by its attachments.** A merged native render pass shows as one D3D12 render pass with all its attachments and clear values. A pass you expected to merge appearing alone did not merge — revisit what else it touches.
- **Expect one allocation, several names.** Aliased resources share an offset, so a texture viewer may show contents belonging to a different logical resource that has not been written yet this frame.
- **Work-graph dispatches look different.** The engine's cull pass uses `SetProgram` / `DispatchGraph`, which appears as GPU work-graph evidence rather than a plain dispatch.

The handoff between the two tools is the pass index: take `ExecutePass #N` from the dump, look up `dump.Passes` for `N`, and you have the name, type, and resource list for whatever the capture is showing you.

## Narrowing a Problem

There is no single-pass mode and no way to bypass compilation, so reduce the graph yourself: `Reset()`, add only the suspect pass with `AllowPassCulling(false)`, and execute. Three levers do most of the work:

- **`RGExecutionFlags.ForceGraphics`** records every segment on the graphics queue and drops the queue-release barriers, turning the frame into one sequential submission. If an artifact disappears under it, the problem is in a queue handoff. The dump still reports the planned async decisions, so you can read the schedule while replaying serially.
- **Import a resource** instead of leaving it transient. It is excluded from memory sharing, so a capture shows it boringly and consistently across frames.
- **Extract a resource** you want to inspect into a persistent handle, which gives it a named, unaliased texture in the capture's viewer.

The shape of every workaround is the same: strip the frame down to one queue, one pass, and imported resources until the behavior is obvious, then add structure back one piece at a time.

## Validation Errors

Under `GHOST_SAFETY_CHECKS`, declaration mistakes throw `InvalidOperationException` at the point of the bad call rather than producing a broken frame. The message names the pass, the resource, and the conflict:

| Message | Cause |
|---|---|
| `does not have a render function` | `SetRenderFunc` never called |
| `must have at least one color or depth attachment` | Raster pass with no attachments |
| `declares raster attachments but has pass type X` | Attachment on a compute or unsafe pass |
| `generic writes are ambiguous for X passes` | `Write` on a raster or unsafe pass without an attachment or UAV usage |
| `X conflicts with Y for the whole-resource range` | Two roles for one resource in one pass |
| `X requires a texture resource` | Attachment or depth usage on a buffer |
| `Color attachment at index N is already set to a different texture` | Slot reuse with a different texture |

Read the **first** exception, not the last: the first rejection sets a fault flag so disposal stays quiet instead of burying the real error under a second one.

Outside safety-check builds all of this compiles away and the same mistakes become GPU artifacts. The tests in `src/Test/Ghost.UnitTest/Graphics/RenderGraph*Test.cs` exercise these rules against a mocking device, which is the fastest way to check whether the graph is behaving or whether your pass is.

## Next

- [Overview](overview.md) to re-read the model, or apply this in `src/Runtime/Ghost.Engine/RenderPipeline/`.
