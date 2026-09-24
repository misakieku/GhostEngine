# Render Graph Overview

The render graph is the system that turns a description of a frame — "run this compute dispatch, then these draws into these targets, then blit to the back buffer" — into GPU work that is correctly synchronized, economically memoryed, and submitted in one pass. It lives in `Ghost.Graphics.RenderGraphModule` (`src/Runtime/Ghost.Graphics/RenderGraphModule`) and is the foundation every engine render pipeline is written on.

This page explains what the graph is for, the vocabulary the rest of the set assumes, and where the module sits in GhostEngine. For hands-on material go straight to [Authoring Passes](authoring-passes.md).

## Why a graph instead of direct calls

The alternative to a render graph is immediate mode: acquire a command buffer, record a dispatch, set a render target, draw, submit. That works, and it is easy to reason about for one pass. It stops working as a frame grows, because four pieces of bookkeeping become load-bearing and none of them are local to the code that needs them:

- **Resource lifetime.** Every intermediate texture you allocate for a pass is either leaked, pooled by guesswork, or freed too early.
- **Synchronization.** Every write-then-read handoff across passes needs a barrier, and every barrier you omit is a silent race that reproduces on one GPU at one resolution.
- **Dead work.** A pass whose outputs nothing reads still costs a full frame of GPU time unless something notices.
- **Ordering.** Passes get reordered for throughput (batching render-state changes, shortening lifetimes), which is exactly the kind of change nobody makes by hand because it is fragile.

The render graph makes all four compiler problems instead of authoring problems. You declare *what* each pass reads and writes; the graph decides when things run, what memory they share, and which barriers exist.

## What the graph automates

| Job | Mechanism | Detail |
|---|---|---|
| Dead-pass elimination | Backward reachability from visible sinks | [Compilation and Caching](compilation.md) |
| Transient memory aliasing | Happens-before proof + single suballocated heap | [Resources and Aliasing](resources.md) |
| Barrier and state insertion | Resolved per-pass usage, implicit transition elision | [Synchronization](synchronization.md) |
| Load/store-op inference | First-use and last-use analysis per attachment | [Synchronization](synchronization.md) |
| Native render pass merging | Consecutive raster passes with identical attachments and no other resource touched | [Synchronization](synchronization.md) |
| Async compute scheduling | Dependency-window planner, multi-region | [Synchronization](synchronization.md) |
| Usage validation | Declaration and pass-shape rules | [Debugging and Validation](debugging.md) |

## Design goals

Three commitments shape the module's architecture, and they explain most of the API's sharper edges.

**Compile once, replay forever.** The graph does not walk pass objects at execute time. Compilation lowers the whole frame into a contiguous **binary command stream** — a byte buffer of fixed-width opcodes and integer indices — and `RenderGraphExecutor` is a small interpreter over it. Nothing in the execute path re-derives a decision the compiler already made. Its scratch arrays grow by amortized `Array.Resize` until they reach steady-state capacity, after which replay performs no allocation.

**Integer indices, never object references.** The `_passes` list in `RenderGraph` is the single source of truth. Compiled passes, native passes, barriers, aliasing placements and command-stream entries all refer to passes and resources by `int` index or `Identifier<T>`. A compiled graph therefore cannot hold a dangling reference to a pass that was recycled last frame, which is what makes cross-frame caching safe.

**Zero managed allocation per steady-state frame.** Pass objects come from `RenderGraphObjectPool`; compile scratch comes from `AllocationManager.CreateStackScope()` and `Misaki.HighPerformance` unmanaged containers; backing resources are suballocated from one heap. Pass data is an unmanaged `struct` you copy in, not a boxed parameter object. The intended steady state is a compilation-cache hit that replays a cached byte stream and reuses cached heap placements.

## The authoring model at a glance

You build a frame by adding passes to a `RenderGraph`. Each `Add*RenderPass` call hands you a builder; you declare the pass's resource accesses and attachments on that builder, attach a typed payload struct and a render function, and dispose the builder to close the pass.

There are exactly three pass types:

| Type | Created with | Context interface | Use for |
|---|---|---|---|
| Raster | `AddRasterRenderPass<T>()` | `IRasterRenderContext` | Draws and mesh-shader dispatches into color/depth attachments. Mergeable into native render passes. |
| Compute | `AddComputeRenderPass<T>()` | `IComputeRenderContext` | Dispatches, indirect dispatches, work-graph programs. Eligible for async compute. |
| Unsafe | `AddUnsafeRenderPass<T>()` | `IUnsafeRenderContext` | Raw `ICommandBuffer` access: copies, UAV clears, buffer mapping. Escapes the abstraction deliberately. |

A complete, minimal graph — one raster pass producing one transient texture, then executed:

```csharp
private struct FillPassData
{
    public Identifier<RGTexture> target;
    public Handle<Shader> shader;
}

_renderGraph.Reset();

Identifier<RGTexture> output;
using (var builder = _renderGraph.AddRasterRenderPass<FillPassData>("FillOutput"))
{
    output = builder.CreateTexture(
        RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm),
        "MyOutput");

    builder.SetColorAttachment(output, 0, AccessFlags.WriteAll);
    builder.SetPassData(new FillPassData { target = output, shader = myShader });
    builder.SetRenderFunc<FillPassData>(static (ref readonly data, ctx) =>
    {
        if (!ctx.TrySetActiveShaderPass(data.shader, PassSemantic.Forward))
        {
            return;
        }

        ctx.DispatchMesh(1, 1, 1);
    });
}

var viewState = new ViewState(width, height, width, height);
var result = _renderGraph.CompileAndExecute(executionContext, viewState);
result.ThrowIfFailed();
```

This fragment is complete for the graph's part of the job — declare, attach, schedule, execute — and abbreviated only where the RHI is concerned: mesh and material binding, viewport setup, and shader-property layout are covered by the RHI and shader documentation, not by this set.

Two rules are already visible. A raster pass must declare at least one color or depth attachment, because otherwise the graph has no render pass to merge it into. And the `using` is load-bearing: disposing the builder is what finalizes and validates the pass. Note also that `shader` travels in `FillPassData` rather than being captured — see the static-lambda rule on [Authoring Passes](authoring-passes.md).

## Vocabulary

| Term | Meaning |
|---|---|
| **Logical resource** | A graph-level texture or buffer, identified by `Identifier<RGTexture>` / `Identifier<RGBuffer>`. A descriptor plus a lifetime, not yet a GPU allocation. |
| **Backing resource** | The real `Handle<GPUTexture>` / `Handle<GPUBuffer>` the logical resource resolves to at compile time. Reachable inside a pass lambda only, via `GetActualTexture` / `GetActualBuffer`. |
| **Transient** | Created with `CreateTexture` / `CreateBuffer`. Lives only inside the graph; eligible for memory aliasing. |
| **Imported** | An external persistent resource brought in with `ImportTexture` / `ImportBuffer`. Never aliased; may carry explicit initial and final barrier states. |
| **Extracted** | A transient handed out of the graph after execution via `QueueTextureExtraction` / `QueueBufferExtraction`. Never aliased; backed by a pooled resource. |
| **Logical pass** | One pass you authored, with one index in `_passes`. |
| **Native pass** | A hardware render pass the compiler builds by merging consecutive compatible raster passes. |
| **Pass index** | Position in `_passes`, stable within a frame and used by the hash. |
| **Schedule index** | Position in the *compiled* order after culling and reordering. Lifetimes, aliasing and barriers are all expressed in schedule indices, which is why `PlacedResource.firstUsePass` and friends are not pass indices. |
| **Command buffer segment** | A maximal run of passes recorded into one `ICommandBuffer`, delimited in the stream by `CommandBufferSyncPoint` opcodes. Segments may be on different queues. |

The pass-index / schedule-index distinction is the one most worth internalising: everything you author is in pass-index space, everything the compiler emits is in schedule-index space, and the dump reports both (`FirstUsePass` versus `ScheduledFirstUseIndex`).

## Frame lifecycle

```text
Reset()                      recycle passes, clear blackboard + resource registry
  |
  v
Add*RenderPass<T>()  x N     declare resources, attachments, pass data, render funcs
  |
  v
CompileAndExecute(executionContext, viewState, flags)
  |
  +-- ResolveTextureSizes(viewState)      relative -> absolute extents
  +-- ComputeGraphHash(...)               structure-only fingerprint
  +-- Compile(...)
  |     +-- cache hit  -> restore placements + command stream
  |     +-- cache miss -> cull, DAG, reorder, schedule, alias, merge, emit
  |     +-- AllocateBackingResources(...) one heap, suballocated per placement
  |     v
  +-- Execute(...)           interpret command stream, record command buffers
        |
        +-- FrameScheduler submission transaction (+ cross-queue dependencies)
        v
      RGExecution { GraphicsSubmission, ComputeSubmission, Dump? }
```

`Reset()` and `CompileAndExecute()` bracket the frame; nothing about a previous frame survives into the next build except what is in the compilation cache. See [Compilation and Caching](compilation.md) for the compile stages and [Debugging and Validation](debugging.md) for reading the dump.

## Where it sits in the engine

```text
RenderEngine
  |-- GraphicsEngine / FrameScheduler / ResourceManager / ShaderLibrary / PipelineLibrary
  v
GhostRenderPipeline  (IRenderPipeline)
  |-- GPUScene, GPUViewManager
  v
GPUViewContext  -- one RenderGraph per view, reused across frames
  |
  v
RenderGraph  ->  compiled command stream  ->  ICommandBuffer(s)  ->  FrameScheduler
```

The pipeline owns no command buffers and no fences. It records passes, then hands the graph a `RenderGraphExecutionContext` — `IGraphicsEngine`, the `FrameScheduler`, and this frame's graphics and compute command allocators — and the graph acquires pooled command buffers and submits them through a scheduler transaction. `GhostRenderPipeline` keeps one `RenderGraph` per view in `GPUViewContext`, resets it each frame, imports the swap-chain colour target and the persistent HZB, and calls `CompileAndExecute` once per view.

Convenience helpers for common passes — `AddBlitPass`, `AddClearBufferPass`, `AddCopyTexturePass`, `AddClearRenderTargetPass`, `AddClearDepthStencilPass` — live in `Ghost.Engine` as extension methods on `RenderGraph` (`RenderGraphUtility`), not in the graph module. The module deliberately ships no policy helpers; the engine layer adds them.

## Relation to UE5 RDG and Unity's render graph

GhostEngine's graph follows the same core idea as Unreal's Render Dependency Graph and Unity's render graph: split setup from execution, derive dependencies from declared resource access, and automate lifetime, barriers and culling. The differences are mostly in how compile results are reused and how parameters reach shaders.

| Aspect | UE5 RDG | GhostEngine |
|---|---|---|
| Dependency source | Scanned from a reflected shader-parameter struct (`SHADER_PARAMETER_RDG_TEXTURE` and friends) | Declared imperatively: `UseTexture`, `UseBuffer`, `SetColorAttachment`, `UseRandomAccessTexture` |
| Pass parameters | `AllocParameters<T>()`, graph-lifetime pointer | Typed `struct` copied in by `SetPassData` |
| Execute payload | Pass lambdas invoked per pass | Same, but driven by a prebuilt binary opcode stream |
| Cross-frame reuse | Rebuilt and recompiled each frame | Compilation cache keyed on a structural graph hash |
| Shader binding | `SetShaderParameters` over reflected struct | Bindless indices (`GetActualBindlessIndex`) plus push constants and a raw-SRV property ring buffer |
| Async compute | Per-pass `ERDGPassFlags::AsyncCompute` | `EnableAsyncCompute(true)` as a hint; a dependency-window planner decides queue assignment |
| Debug surface | RDG Insights, immediate mode, many CVars | `RenderGraphDump` with disassembled command stream, plus validation under `GHOST_SAFETY_CHECKS` |

Unity's render graph occupies the same conceptual ground for custom scriptable pipelines; its documentation set is the closest structural analogue to this one, and is worth reading if you want the same ideas in a GC-friendly C# framing.

## Current limitations

- **Two resource types.** `RGResourceType` covers `Texture` and `Buffer`. Acceleration structures are not modelled yet, so ray-tracing resources must be managed outside the graph.
- **Text-only diagnostics.** There is no RDG-Insights-style visualizer. Inspection goes through `RenderGraphDump` — heap blocks, per-pass queue decisions, per-resource aliasing sets, and a disassembled command stream.
- **Validation is a safety-check build feature.** Declaration and pass-shape rules compile out unless `GHOST_SAFETY_CHECKS` is defined, so a Release frame performs no validation at all.
- **No parallel recording.** Both build and replay are single-threaded. Pass render functions are invoked sequentially by the executor; there is no multi-threaded command-list recording of the kind RDG offers.
- **Relative scale is CPU-side only.** `IRenderGraphContext.RelativeScale` is resolved during compile but not yet uploaded to shaders — see the `TODO` in `RenderGraphContext`.

## Scope of this set

These six pages cover the render graph: declaration, scheduling, memory, synchronization, compilation, and diagnosis. They deliberately do **not** cover the RHI surface a pass ultimately calls into — `BufferDesc` and `TextureDesc` fields, `TextureUsage` / `BufferUsage` / `BindlessAccess` members, shader and material binding, root-signature layout, or the full `BarrierLayout` / `BarrierAccess` / `BarrierSync` enumerations. The barrier members this set actually references are listed in the appendix on [Synchronization](synchronization.md); everything else lives in the RHI documentation and API reference.

The practical consequence: a pass assembled from these pages will validate, compile, schedule and dump correctly. Making it actually issue GPU work requires the RHI side.

## Next steps

- [Authoring Passes](authoring-passes.md) — the builder API in full: pass types, `AccessFlags`, attachments, pass data, the blackboard, and binding properties.
- [Resources and Aliasing](resources.md) — transient, imported and extracted resources, relative sizing, and how aliasing is proven safe.
- [Synchronization](synchronization.md) — barriers, load/store inference, native pass merging, and async compute scheduling.
- [Compilation and Caching](compilation.md) — the compile stages, the graph hash, and the command stream format.
- [Debugging and Validation](debugging.md) — the dump, the disassembly format, validation rules, and the mistakes this system is designed to catch.
