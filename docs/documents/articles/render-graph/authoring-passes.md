# Authoring Passes

Everything a frame is made of is a pass. This page covers the authoring surface end to end: creating a pass, declaring what it touches, attaching a payload, supplying the render function, and the controls that steer culling and queue assignment.

All types named here live in `Ghost.Graphics.RenderGraphModule`. The engine-side helpers live in `Ghost.Engine.Utilities.RenderGraphUtility`.

## The builder lifecycle

`RenderGraph` exposes three entry points, one per pass type:

```csharp
IRasterRenderGraphBuilder  AddRasterRenderPass<TPassData>(string name)
IComputeRenderGraphBuilder AddComputeRenderPass<TPassData>(string name)
IUnsafeRenderGraphBuilder  AddUnsafeRenderPass<TPassData>(string name)
    where TPassData : struct
```

Each returns a builder, and every one of them returns **the same `RenderGraphBuilder` instance**. The graph holds one builder for its lifetime and calls `Reset(pass)` on it each time you add a pass. Two consequences follow, and both are strict rules rather than style preferences:

- **Consume the builder before adding another pass.** The reference you hold is silently repointed at the next pass. Keeping a builder around and calling into it later configures a different pass than the one you meant to.
- **Dispose the builder to close the pass.** `Dispose()` is not cleanup — it is the finalize step. It runs the per-pass validation rules and marks the pass immutable. Always write `using var builder = ...` or an explicit `using` block.

```csharp
using (var builder = rg.AddComputeRenderPass<MyPassData>("MyPass"))
{
    // declare resources, attachments, pass data, render func
}   // <- pass is finalized here
```

After disposal, further calls on that builder throw `ObjectDisposedException` in `DEBUG` builds. The next `Add*RenderPass` call resets the flag along with the rest of the builder's state, so the single instance is reusable across the frame — the exception exists to catch a builder held past its own pass, not to forbid adding a second pass. If a declaration was rejected under `GHOST_SAFETY_CHECKS`, the builder records the fault and disposal completes quietly instead of throwing a second, unrelated error on top of the real one.

Calling `Add*RenderPass` without disposing the previous builder does **not** finalize that pass — the reset simply abandons it mid-construction, unvalidated. Always let the `using` run.

### Pass names and resource names

Pass names are stored unconditionally and appear in dumps and disassembly. Resource names (`CreateTexture(desc, "SceneColor")`) are retained **only under `GHOST_SAFETY_CHECKS`**; in release they cost nothing and diagnostics fall back to `Resource_<index>`. Never make engine logic depend on a resource name — treat names as a debug-build affordance.

## Declaring resource access

The graph knows about a resource only if you say so. A read or write that is not declared is invisible to dependency analysis, barrier insertion, lifetime calculation and culling. This is the single most important rule in the module.

### AccessFlags

| Flag | Meaning |
|---|---|
| `None` | No access. |
| `Read` | The pass samples or loads the resource. Existing contents must survive. |
| `Write` | The pass modifies the resource. Other pixels may already be there. |
| `Discard` | Previous contents are not needed. |
| `WriteAll` | `Write \| Discard` — the pass overwrites everything. Use for fullscreen passes. |
| `ReadWrite` | `Read \| Write` — read-modify-write. |

`Discard` is not advisory. It drives load-op inference for attachments (`DontCare` instead of `Load`) and participates in aliasing and first-use barriers, so choosing `WriteAll` over `Write` on a fullscreen pass is a real bandwidth saving on tile-based hardware.

### The declaration methods

```csharp
Identifier<RGTexture> UseTexture(Identifier<RGTexture> texture, AccessFlags accessMode)
Identifier<RGBuffer>  UseBuffer(Identifier<RGBuffer> buffer, AccessFlags accessMode)
```

Both return the resource they were given, which is what makes the chaining idiom work — create and declare in one expression, or declare at the point of use:

```csharp
var scene = builder.UseBuffer(importedScene, AccessFlags.Read);
var hzb   = builder.UseTexture(hzbTexture, AccessFlags.Read);
```

For unordered access, use the explicit methods rather than `UseTexture(..., ReadWrite)`:

```csharp
Identifier<RGTexture> UseRandomAccessTexture(Identifier<RGTexture> texture)   // raster + unsafe builders
Identifier<RGBuffer>  UseRandomAccessBuffer(Identifier<RGBuffer> buffer)      // raster + unsafe builders
Identifier<RGTexture> UseRenderTargetTexture(Identifier<RGTexture> texture, AccessFlags flags = AccessFlags.Write)  // unsafe builder only
```

`UseRandomAccess*` implies `ReadWrite`, records the resource in the pass's random-access set, and selects the UAV barrier state. `UseRenderTargetTexture` is the unsafe builder's way of saying "this pass writes a render-target view without a native render pass" — it exists so UAV/copy passes can still declare RT writes legibly.

### Attachments (raster passes only)

```csharp
void SetColorAttachment(Identifier<RGTexture> texture, int index, AccessFlags flags = AccessFlags.Write)
void SetDepthAttachment(Identifier<RGTexture> texture, AccessFlags flags = AccessFlags.ReadWrite)
```

Color slots run `0 .. RHIUtility.MAX_RENDER_TARGETS - 1` (currently **8**). Setting a slot to a different texture than the one already bound there is rejected; re-setting it to the same texture is a no-op that lets you widen the access flags.

`SetDepthAttachment` derives its usage class from the flags: `Write` selects depth-write, otherwise depth-read. The defaults are deliberately conservative — `Write` assumes a partial update, so a fullscreen clear or blit should pass `WriteAll` explicitly.

Attachments are the mechanism that enables [native render pass merging](synchronization.md). A raster pass that reads a texture and writes the result through a UAV instead of an attachment forfeits that optimization, and pays a transition instead.

### Which pass types may declare what

| Declaration | Raster | Compute | Unsafe |
|---|---|---|---|
| `UseTexture` / `UseBuffer` with `Read` | yes | yes | yes |
| `UseTexture` / `UseBuffer` with `Write` | **rejected** | yes (resolves to UAV) | **rejected** |
| `SetColorAttachment` / `SetDepthAttachment` | yes | **rejected** | **rejected** |
| `UseRandomAccessTexture` / `UseRandomAccessBuffer` | yes | **not on the interface** — declare `Write` instead | yes |
| `UseRenderTargetTexture` | n/a | n/a | yes |

The "rejected" cells are validation errors under `GHOST_SAFETY_CHECKS`, not silent misbehaviour. A generic write on a raster or unsafe pass is ambiguous — the graph cannot tell whether you meant an attachment, a UAV, or an RTV clear — so it refuses to guess: *"generic writes are ambiguous for `{pass.type}` passes; declare an attachment, random-access usage, or explicit unsafe usage."* Compute passes are the exception: a declared write there means UAV access, which is unambiguous, and it is the **only** way a compute pass obtains a texture or buffer UAV.

### Conflicting usages

Each resource may hold one concrete usage class per pass — colour attachment, depth (read or write), or unordered access, the first two requiring a texture. Declaring the same resource under two different classes in one pass is rejected: *"`UnorderedAccess` conflicts with `ColorAttachment` for the whole-resource range."*

## Supplying the render function

```csharp
public delegate void PassRenderFunc<TPassData, TRenderContext>(ref readonly TPassData data, TRenderContext ctx)
    where TPassData : struct
    where TRenderContext : IRenderGraphContext;

builder.SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IRasterRenderContext>  renderFunc);  // raster
builder.SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IComputeRenderContext> renderFunc);  // compute
builder.SetRenderFunc<TPassData>(PassRenderFunc<TPassData, IUnsafeRenderContext>  renderFunc);  // unsafe
```

The context parameter type is what ties the builder to the pass type — a raster pass cannot be given a compute render function, and the overload set makes that a compile error.

**Write the lambda as `static`.** The delegate is stored on the pooled pass object and invoked at replay time. A `static` lambda converts to a cached delegate with no target object; a capturing lambda allocates a closure every frame, which defeats the zero-GC-per-frame contract. Everything the render function needs must therefore travel in `TPassData` — including shader handles, dispatch sizes, and bindless indices you cannot know until execute time. A pass with no render function is a validation error, but an **empty** render function is legal and is the correct shape for a pure hardware clear: bind the attachment, set `clearAtFirstUse` on the descriptor, and let the load op do the work. Nothing needs to be drawn. Note that such a pass is only kept if something consumes its output or it writes an imported/extracted resource — otherwise culling removes it, so clear-only passes on dead transients need `AllowPassCulling(false)`.

## Pass data and the blackboard

```csharp
void SetPassData<T>(scoped in T passData, bool addToBlackboard = false) where T : struct
```

The struct is copied into the pooled pass object. If `T` does not match the pass's own `TPassData`, `SetPassData` throws `ArgumentException` — in every configuration, since it is a plain programming error rather than a data-validation one.

`TPassData` is a build-time payload: assembled while you author the frame, read back inside the render function at replay time. Keep it unmanaged and cheap to copy. Storing `Identifier<RG*>` values is the normal pattern — they are integers, stable for the frame, and resolvable to real resources only inside a pass lambda.

The optional blackboard lets later build code read an earlier pass's payload. `RenderGraphBlackboard` is keyed by `TPassData` type, so **one pass per data type** can be registered — a second registration of the same type overwrites the first. `Get<T>()` throws `KeyNotFoundException` when absent; `TryGet<T>(out bool exist)` returns a null reference instead, so check the flag before dereferencing. The blackboard is cleared by `Reset()`, making it strictly a within-frame build-time facility. It has no effect on dependencies, barriers, or culling — the graph does not know that a blackboard read is a resource read, so anything touching a resource still needs a `Use*` declaration.

## Steering culling

```csharp
void AllowPassCulling(bool value)   // default: true
```

A pass is culled when nothing observable consumes its outputs. Because culling is derived from declared access, a pass whose only side effect is something the graph cannot see — writing to a persistent buffer through a raw command call, updating a CPU-visible mapping — must opt out with `AllowPassCulling(false)`.

The engine's meshlet cull pass is the canonical case; it owns the one-time work-graph backing memory initialization, so dropping it would corrupt every later dispatch:

```csharp
using var builder = rg.AddComputeRenderPass<MeshletCullPass1Data>("MeshletCull_Pass1");

// This pass carries the one-time backing memory initialization, so it must never be culled.
builder.AllowPassCulling(false);
```

You get the same protection for free when a pass writes an **imported** or **extracted** resource: such a write marks the pass as having side effects, and side-effecting passes are never culled regardless of `allowCulling`.

## Steering queue assignment

```csharp
void EnableAsyncCompute(bool value)              // compute builder
void AllowAsyncComputeOverlap(bool allow = true) // unsafe builder
```

`EnableAsyncCompute(true)` is a **hint**. The dependency-window planner promotes the pass to a Compute command buffer only when it can find independent graphics work to overlap it with; otherwise the pass stays on Graphics and nothing bad happens. A pass is only ever considered when it is a compute pass with no side effects and no raster attachments. The mechanics are on [Synchronization](synchronization.md); to find out what actually happened for a given frame, read `PassDumpInfo.QueueDecision`.

`AllowAsyncComputeOverlap` is the unsafe builder's counterpart. An unsafe pass inside an overlap window normally invalidates that window, because raw command-buffer work cannot be reasoned about. Calling `AllowAsyncComputeOverlap(true)` asserts that the pass's native operations do not conflict with a concurrent compute queue, which both permits the window and lets the unsafe pass count as the independent graphics work the planner needs. It is opt-in per pass, defaults to off, and is part of the graph hash — flipping it recompiles.

## The execute-time context

Inside a render function you get one of `IRasterRenderContext`, `IComputeRenderContext`, or `IUnsafeRenderContext`. All three extend `IRenderGraphContext`:

| Member | Purpose |
|---|---|
| `GetActualTexture` / `GetActualBuffer` / `GetActualResource` | Resolve a graph identifier to its backing `Handle<GPU*>`. |
| `GetActualBindlessIndex(texture \| buffer, access, subResource)` | Bindless descriptor index, optionally for one subresource. `GetActualBindlessIndices` is the batched variant for mip chains and arrays. |
| `SetUserData(u0, u1, u2, u3, target)` | Push constants — four `uint`s, `uint.MaxValue` meaning "unset". |
| `SetUserDataWithProperties<TProperty>(in property, u1, u2, u3, target)` | Copy an unmanaged property struct into the property ring buffer and bind it. |
| `TrySetActiveShaderPass(shader, passIndex \| semantic, pipelineOverride)` | Resolve and bind a PSO. Returns `false` when the pipeline cannot be resolved. |
| `ExecuteIndirect(cmdSignature, maxCommandCount, argBuffer, argOffset, countBuffer, countOffset)` | Indirect dispatch/draw. |
| `ClearBuffer(buffer, sizeInBytes, clearValue, dstOffset)` | Zero or fill a buffer through a staging upload buffer. |
| `ResourceManager`, `ResourceDatabase`, `RelativeScale` | Direct engine resource services, and the resolved relative-size scale for this frame. |

`IRasterRenderContext` adds material and mesh binding plus mesh-shader dispatch: `SetActiveMaterial`, `SetActiveMaterialPass`, `TrySetActiveMaterialPass`, `SetActiveMesh`, `DispatchMesh`, `SetViewport`, `SetScissorRect`. `IComputeRenderContext` adds `SetActiveCompute`, `DispatchCompute`, and the work-graph pair `SetProgram` / `DispatchGraph`. `IUnsafeRenderContext` inherits both surfaces and adds the escapes: `GetCommandBufferUnsafe`, `MapBuffer`, `UnmapBuffer`, `WriteBuffer<T>`.

Two behaviours are worth knowing before you write a pass:

- **`TrySetActive*` versus `SetActive*`.** The `Try` variants return `false` when pipeline state cannot be resolved — a missing shader cache entry, an incompatible stage topology — and log a warning. The throwing variants raise `InvalidResourceHandleException` on a bad handle. Prefer `Try` and bail out of the pass; that is what `AddBlitPass` does.
- **PSO keys include the current attachments.** Graphics pipeline keys fold in the render-target and depth-stencil formats of the active native pass, so the same shader in a different attachment configuration is a different pipeline. Bind attachments through `SetColorAttachment` / `SetDepthAttachment` and the graph keeps those formats correct; bypassing to raw command calls breaks the correspondence.

### Frame and view constant buffers

`RenderGraph.SetFrameData(Handle<GPUBuffer>)` and `SetViewData(Handle<GPUBuffer>)` supply per-frame and per-view constant buffers, bound at fixed root-signature slots (`FRAME_DATA_CBV_SLOT` = 2, `VIEW_DATA_CBV_SLOT` = 1). They are set once per frame outside any pass, and the graph re-binds them at the start of **every command-buffer segment** — including after each async-compute sync boundary. You never rebind them in a render function.

### Push constants and the property ring buffer

Push constants are the cheap path: four 32-bit values at root-signature slot 0, set per pass or per draw, no descriptor traffic.

For anything larger, `SetUserDataWithProperties<TProperty>` copies `TProperty` into a 1 MB persistently-mapped ring buffer with 256-byte alignment and returns a bindless raw-SRV index for that slice. The struct's field layout is the shader-visible contract, and the whole struct must fit the ring's wraparound rules — when the offset would exceed 1 MB, the allocator wraps to the start rather than growing. Property slices are released at the start of the next frame.

Note the differing defaults: `SetUserData` targets `DataTarget.Graphics`, while `SetUserDataWithProperties` targets `DataTarget.Compute`. Pass `target:` explicitly rather than relying on either default.

### A real compute pass

The engine's meshlet cull pass shows the intended shape — declare access, fill a payload, resolve bindless indices at replay time, bind properties, dispatch:

```csharp
using var builder = rg.AddComputeRenderPass<MeshletCullPass1Data>("MeshletCull_Pass1");
builder.AllowPassCulling(false);

visibleMeshlets = builder.CreateBuffer(new BufferDesc
{
    Size = bufferSize,
    Stride = entrySize,
    Usage = BufferUsage.Structured | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
}, "VisibleMeshlets_Pass1");

builder.UseBuffer(visibleMeshlets, AccessFlags.Write);
builder.UseBuffer(counterBuffer, AccessFlags.ReadWrite);

if (hzbTexture.IsValid)
{
    builder.UseTexture(hzbTexture, AccessFlags.Read);
}

builder.SetPassData(passData);
builder.SetRenderFunc<MeshletCullPass1Data>(static (ref readonly passData, computeCtx) =>
{
    computeCtx.ClearBuffer(passData.counterBuffer, CullConstants.COUNTER_BUFFER_SIZE);

    var visibleUav = computeCtx.ResourceDatabase.GetBindlessIndex(
        computeCtx.GetActualBuffer(passData.visibleMeshletsPass1).AsResource(), BindlessAccess.UnorderedAccess);

    var props = new InternalMeshletCullGraphShaderProperties { visibleMeshletsUav = visibleUav, /* ... */ };

    // setProgramDesc is built from passData's work-graph program identifier and flags.
    computeCtx.SetProgram(in setProgramDesc);
    computeCtx.SetUserDataWithProperties(in props, target: DataTarget.Compute);
});
```

(`passData.visibleMeshletsPass1` is the payload field; the local `visibleMeshlets` above is the identifier stored into it when the payload was assembled.)

The `if (hzbTexture.IsValid)` guard is idiomatic and worth copying: an invalid identifier declared as a use is a declaration against a nonexistent resource, while skipping the declaration simply means the pass has no dependency on it. Optional inputs should be conditionally declared, never declared-as-invalid.

### A real utility pass

`RenderGraphUtility.AddBlitPass` is the compact form of a raster pass, and the pattern to imitate for simple fullscreen work:

```csharp
using var builder = rg.AddRasterRenderPass<BlitPassData>("BlitPass");
builder.SetColorAttachment(dst, 0, AccessFlags.WriteAll);
builder.UseTexture(src, AccessFlags.Read);

builder.SetPassData(new BlitPassData { srcBuffer = src, blitShader = blitShader });
builder.SetRenderFunc<BlitPassData>(static (ref readonly passData, renderCtx) =>
{
    if (!renderCtx.TrySetActiveShaderPass(passData.blitShader, PassSemantic.Forward))
    {
        return;
    }

    var property = new HiddenBlitShaderProperties
    {
        mainTex = renderCtx.GetActualBindlessIndex(passData.srcBuffer),
        sampler_mainTex = (uint)renderCtx.ResourceManager.StaticSampler.LinearClamp.Value,
    };

    renderCtx.SetUserDataWithProperties(property, target: DataTarget.Graphics);
    renderCtx.DispatchMesh(1, 1, 1);
});
```

## Viewport, scissor, and the engine helpers

You do not set viewport or scissor for ordinary raster work. At `BeginNativePass` the executor derives both from the attachment extents — depth-stencil if present, otherwise color slot 0 — and applies them before the merged passes run. `SetViewport` and `SetScissorRect` remain on `IRasterRenderContext` for sub-region passes.

`RenderGraphUtility` provides the common pass shapes; using them keeps the pipeline consistent:

| Helper | Pass type | Use |
|---|---|---|
| `AddBlitPass(src, dst, blitShader)` | Raster | Blit a texture through a shader. |
| `AddClearBufferPass(targetBuffer, sizeInBytes, clearValue)` | Unsafe | Clear a buffer via upload staging plus DMA copy. |
| `AddCopyBufferPass(dst, src, numBytes, dstOffset, srcOffset)` | Unsafe | DMA buffer copy. |
| `AddCopyTexturePass(dst, src)` | Unsafe | Full-region texture copy. |
| `AddClearRenderTargetPass(target, clearColor)` | Unsafe | Explicit RTV clear. |
| `AddClearDepthStencilPass(target, ...)` | Unsafe | Explicit DSV clear. |

Prefer a hardware clear on an attachment (`clearAtFirstUse`) over `AddClearRenderTargetPass`; the latter is for when no native render pass is involved. These helpers each add a real pass to the graph — they are not free.

> **Known issue.** The four copy/clear-view helpers declare a generic write on an unsafe pass (`UseTexture(dst, WriteAll)` and friends), which the validator rejects with *"generic writes are ambiguous for Unsafe passes"* at builder disposal. They therefore throw in `GHOST_SAFETY_CHECKS` builds and have no engine call sites yet; only `AddBlitPass` and `AddClearBufferPass` are usable as written. Each needs migrating to `UseRenderTargetTexture` or `UseRandomAccess*`. Until then, write those passes yourself following the rules above.

## Rules that bite

1. **Undeclared access is invisible.** No dependency, no barrier, no lifetime extension, no protection from culling. The graph will happily reorder, alias, or delete the pass you forgot to declare.
2. **One builder, reused, and disposal finalizes it.** Never hold a `IRenderGraphBuilder` across an `Add*RenderPass` call, and always wrap it in `using`.
3. **A raster pass needs an attachment.** Color slot 0 or a depth attachment, otherwise validation fails.
4. **Non-compute passes cannot declare generic writes.** Use an attachment, `UseRandomAccess*`, or `UseRenderTargetTexture`.
5. **One usage class per resource per pass.** Attachment plus UAV on the same resource in one pass is a conflict.
6. **`static` lambdas only.** A capturing lambda allocates per frame.
7. **`SetPassData` type must match the pass,** and the blackboard holds one entry per payload type at build time only — it creates no dependencies.
8. **`EnableAsyncCompute` is a hint.** Verify with `QueueDecision`, do not assume.
9. **Optional resources: skip the declaration, don't declare invalid.**
10. **Names are debug-build-only for resources.** Never key logic on `GetResourceName`.

## Next

- [Resources and Aliasing](resources.md) — what `CreateTexture` and `ImportTexture` actually create, relative sizing, and how transient memory is shared.
