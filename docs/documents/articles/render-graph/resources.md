# Resources and Aliasing

A render graph resource is a *descriptor plus a lifetime*, not a GPU allocation. The graph creates the real resource only at compile time, once it knows which offsets can be shared. This page covers the three resource kinds, how sizes are resolved, and how aliasing is proven safe.

## The three kinds

| Kind | Created by | Owned by | Aliased? | Backed by |
|---|---|---|---|---|
| **Transient** | `CreateTexture` / `CreateBuffer` | The graph | Yes | A suballocation in the graph's heap |
| **Imported** | `ImportTexture` / `ImportBuffer` | Outside code, before the frame | No | The existing handle you passed in |
| **Extracted** | a transient plus `QueueTextureExtraction` / `QueueBufferExtraction` | Outside code, after the frame | No | A pooled resource, swapped into your handle at retire |

The distinction that matters is *whether the graph may reuse the memory*. Transients are graph-owned and disposable; imported and extracted resources have a life outside the frame, so they are pinned. Declaring the wrong one is the difference between a correct frame and a resource whose contents were recycled by an unrelated pass.

## Identifiers

Resources are addressed by typed integer handles: `Identifier<RGTexture>` and `Identifier<RGBuffer>`. They are indices into the graph's resource registry, valid for one frame only, and `Identifier<T>.Invalid` is the null value. `RGResourceExtensions.AsResource()` bitcasts between the typed handles and the unified `Identifier<RGResource>` used internally — you rarely need it, but it is why a texture and a buffer can share dependency and aliasing machinery.

Because identifiers are integers, they are cheap to store in `TPassData` and to hash. They are **not** stable across frames: `Reset()` clears the registry, so index 3 in one frame may be a different texture in the next. Never cache an identifier outside the frame that produced it.

## Creating transients

```csharp
Identifier<RGTexture> CreateTexture(scoped in RGTextureDesc desc, string? name = null)
Identifier<RGBuffer>  CreateBuffer(scoped in BufferDesc desc, string? name = null)
```

Both record a descriptor and return immediately. No GPU memory is touched, and no validation of size or format happens until compile.

`RGTextureDesc` has four factory methods:

| Factory | Result |
|---|---|
| `Absolute(width, height, format, ...)` | Fixed pixel dimensions. |
| `Relative(scale, format, ...)` | Uniform fraction of the viewport. |
| `Relative(scaleX, scaleY, format, ...)` | Independent horizontal and vertical fractions. |
| `RelativeDepth(scale, clearDepth, ...)` | Relative depth-stencil, defaulting to `D32_Float` and `TextureUsage.DepthStencil`. |

All four default to `clearAtFirstUse = true` and `discardAtLastUse = true`, and the colour factories default to `TextureUsage.RenderTarget | TextureUsage.ShaderResource`. Buffers take a plain RHI `BufferDesc`, including its `HeapType`.

There is also a copy form, which clones an existing resource's descriptor under a new name:

```csharp
Identifier<RGTexture> CreateTexture(Identifier<RGTexture> texture, string? name = null)
```

It returns `Identifier<RGTexture>.Invalid` if handed an invalid source, so it composes with the optional-resource idiom from [Authoring Passes](authoring-passes.md).

### Clear and discard flags

`RGTextureDesc` carries `clearAtFirstUse`, `discardAtLastUse`, `clearColor`, `clearDepth` and `clearStencil`. These are **hardware load/store actions**, not CPU operations, and they only take effect when the texture is bound as a raster or native attachment.

`clearAtFirstUse` turns the attachment's load op into `Clear`; if it is false and the resource is at its first use, the load op becomes `DontCare`. `discardAtLastUse` turns the store op of the final attachment use into `DontCare`, dropping tile memory without writeback. Both are inferred per native pass — see [Synchronization](synchronization.md).

For a resource that is **not** an attachment — a compute UAV, or an imported texture — these flags do nothing. Clear it explicitly instead, with `ctx.ClearBuffer` inside a pass or with `AddClearBufferPass`, which stages through an upload buffer and a DMA copy.

## Relative sizing and `ViewState`

Relative textures are resolved against a `ViewState` immediately before compilation:

```csharp
public struct ViewState
{
    public uint viewportWidth, viewportHeight;   // what Relative scales against
    public uint actualWidth, actualHeight;       // pre-upscale render size
}
```

`ResolveTextureSizes` multiplies `scaleX/scaleY` by `viewportWidth/Height` and stores `resolvedWidth/resolvedHeight`. Imported textures are skipped — their dimensions are already fixed.

`actualWidth/actualHeight` exist for upscalers that need the render resolution distinct from the presentation resolution. They participate in `ViewState` equality and hashing, so changing them changes the view state even when the viewport does not.

The reason relative mode exists at all is the compilation cache. The graph hash records **scale factors, not resolved dimensions** (`RenderGraphHasher.ComputeTextureHash`). A window resize therefore produces the same hash, hits the cache, and only needs heap placements resized — not the graph recompiled. Absolute-sized transients defeat this: every distinct size is a distinct hash and a full recompile. Prefer `Relative` for anything resolution-derived, and reserve `Absolute` for genuinely fixed-size resources such as counter buffers.

## Importing external resources

```csharp
Identifier<RGTexture> ImportTexture(
    Handle<GPUTexture> texture,
    ResourceBarrierData? initialState = null,
    ResourceBarrierData? finalState = null,
    Color128 clearColor = default, float clearDepth = 1.0f, byte clearStencil = 0,
    bool clearAtFirstUse = false, bool discardAtLastUse = false)

Identifier<RGBuffer> ImportBuffer(
    Handle<GPUBuffer> buffer,
    ResourceBarrierData? initialBarrierState = null,
    ResourceBarrierData? finalBarrierState = null)
```

The descriptor is read back from the resource database, so the import reflects the real resource rather than a guess. A failed lookup logs an error and returns an invalid identifier — check `IsValid` before using the result.

`initialState` and `finalState` are the graph's contract with the world outside it. The initial state is assumed as the resource's state when the frame begins, so no transition is emitted to reach it; the final state is restored by closing barriers after the last pass. Omitting them means the graph tracks whatever state the resource actually ends in and emits no transition back, which is wrong for anything another subsystem touches next frame.

The back buffer is the canonical case — it must be in `Present` state both entering and leaving the graph:

```csharp
var presentBarrier = new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None);

var colorTarget = renderGraph.ImportTexture(
    renderView.ColorTexture,
    initialState: presentBarrier,
    finalState: presentBarrier,
    clearColor: new Color128(0.05f, 0.05f, 0.05f, 1.0f),
    clearAtFirstUse: true);
```

Persistent resources that only the graph ever touches still need care: an imported resource with no `initialState` begins the frame in `Undefined`, so its first use emits a **discard** transition. If a pass reads prior-frame contents — history buffers, temporal accumulation — supply `initialState` matching the state the resource was left in, or the read is undefined. The back buffer below is the clear-cut case; anything with cross-frame data follows the same rule.

## Extracting transients

Extraction hands a transient out of the graph to a persistent handle you own. Both methods live on the **pass builder**, not on `RenderGraph`, and are called while authoring the pass that produces the resource:

```csharp
void QueueTextureExtraction(Identifier<RGTexture> src, Handle<GPUTexture> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.None)
void QueueBufferExtraction(Identifier<RGBuffer> src, Handle<GPUBuffer> dst, ResourceExtractionFlags flags = ResourceExtractionFlags.None)
```

Calling it marks the resource extracted, which pins it out of the aliasing plan and gives it a fresh pooled backing resource each frame. It also declares the resource as read by that pass, so the extraction itself creates a dependency — you do not need a separate `UseTexture`.

`ResourceExtractionFlags.ReleaseAfterExtract` selects between two deferred behaviours, both applied after execution and both taking effect only when the frame **retires**:

| Flag | Path | Effect |
|---|---|---|
| `None` (default) | `QueueSwap(dst, src)` plus `ReleasePooledResourceDeferred(src)` | `dst` and `src` exchange resources inside the resource database; the displaced old resource recycles into the pool once it is no longer in flight. |
| `ReleaseAfterExtract` | `QueueReplace(dst, src)` | `dst` takes the new resource outright; the old one is released through the deferred path. |

Both exist because the swap cannot happen at record time. In-flight GPU work may still be sampling the persistent handle — the classic case is next frame reading this frame's history. Applying the substitution immediately would let that reader observe this frame's not-yet-written transient. Deferring to retire is what makes history-buffer patterns (HZB, temporal accumulation) correct without an explicit stall.

## Backing allocation

Compile produces exactly one heap for transients:

```csharp
var allocationDesc = new AllocationDesc
{
    Size = plan.totalHeapSize + 65536,               // 64 KB padding against overflow
    Alignment = 65536,                               // D3D12PlacedResourceOffset alignment
    HeapFlags = HeapFlags.AllowAllBufferAndTexture,  // buffers and textures may share the heap
    HeapType = HeapType.Default
};
_resourceHeap = allocator.Allocate(in allocationDesc, "RenderGraphResourceHeap");
```

`HeapFlags` is a single-valued enum, not a bitfield; `AllowAllBufferAndTexture` is the member that permits mixed suballocation. Every aliasable resource is then created with `ResourceAllocationType.Suballocation` at its planned offset. Resources that cannot share the heap get their own path:

| Resource | Backing |
|---|---|
| Imported | The handle you supplied; nothing allocated. |
| Extracted | `CreatePooledTexture` / `CreatePooledBuffer`, fresh each frame. |
| `HeapType.Upload` buffers | A dedicated committed buffer — CPU-mapped resources must not alias. |
| Everything else | Suballocated from `_resourceHeap` at `heapOffset`. |

The heap is released and recreated whenever the aliasing plan is rebuilt, and `Reset()` does not destroy it — placements are cached across frames.

## Aliasing

### Lifetime and the happens-before test

Aliasing two resources requires proof that their uses cannot interleave. Interval overlap on `[firstUse, lastUse]` is not that proof, because a schedule is only a partial order: two resources whose intervals overlap may still be unordered, and two that do not may still be unsafe if a third dependency puts them in an ambiguous relationship.

The graph instead builds a per-resource **use bitmask** over schedule indices — every pass that reads or writes the resource — and a per-pass reachability matrix. Resource A may alias B when every use of A happens before every use of B, or vice versa:

```text
AllUsesHappenBefore(A, B)  ==  ∀ pass p using A, ∀ pass q:  q is reachable from p  OR  q does not use B
CanAlias(A, B)             ==  AllUsesHappenBefore(A, B) || AllUsesHappenBefore(B, A)
```

Bitmask intersection answers this in `WordCount` (`ceil(passes / 64)`) word operations rather than a graph walk. `RenderGraphResourceOrdering.CanAlias` is the only gate the placement allocator consults.

This is also why the async-compute planner runs *before* aliasing: reachability must include command-buffer segment ordering, or two passes on different queues would look unordered when they are in fact serialized.

### Placement

`RenderGraphAliasingBuilder.Build` runs a simulation over an unbounded heap:

1. Collect aliasable resources, computing each size through `IResourceAllocator.GetSizeInfo`.
2. Sort **size descending**.
3. First-fit each resource into the simulated heap, testing every overlapping occupied block with `CanAlias`.
4. Take the peak extent, aligned to 64 KB, as `totalHeapSize`.

Alignment is 64 KB (`DEFAULT_TEXTURE_ALIGNMENT` / `DEFAULT_BUFFER_ALIGNMENT`) for both textures and buffers, matching D3D12's placed-resource offset requirement.

Placement is deliberately strict about *exact* slots. A candidate may overlap an occupied block only when it starts at the same offset and does not extend past it:

```csharp
// Alias membership and first-use barriers are represented per exact slot.
if (offset != block.offset || endOffset > blockEnd)
    return false;
```

Partial overlaps are refused outright. The consequence is that a slot is a set of resources sharing one offset and one membership list, which is what makes aliasing barriers well-defined. Resources in the same slot record each other in `aliasedLogicalResources` — they remain distinct logical resources with distinct descriptors, and the dump reports the whole set per resource.

### Aliasing barriers

Reusing an offset is only correct if the GPU agrees to forget the previous occupant. When a resource's first use is reached and it shares a slot, the compiler resolves the most recent former occupant — the alias candidate with the largest last-use schedule index that provably precedes it — and emits a barrier carrying `aliasingPredecessor` plus `FirstUsage | Discard`. At the RHI that becomes an aliasing transition (`isAliasing: true`), which forces the before-state to `Undefined` / `NoAccess` and sets the D3D12 texture-barrier discard flag, invalidating the old resource's contents at that offset. No residency management is involved: the backend places resources through D3D12MA, so this is a discard rather than an eviction and re-reside.

The same `FirstUsage | Discard` combination is emitted for any resource whose tracked state is invalid at first use, so a fresh transient with no alias predecessor still gets a discard rather than a load of garbage.

### Resizing without recompiling

When the cache hits but the view state grew, `ResizeCachedSlots` recomputes offsets and sizes while **preserving alias groups exactly**: it walks the cached placements, reuses each slot's representative offset for members sharing the old offset, and re-derives slot sizes from the new resolved dimensions. Command bytes, culled flags, native passes and schedule indices are untouched — which is the property `TestPhase3_ViewportGrowthPreservesAliasGroupsAndCommandBytes` asserts.

## Two lifetime representations

The registry keeps `firstUsePass` / `lastUsePass` in **pass index** space, accumulated as passes declare access. The ordering structure keeps first/last use in **schedule index** space, computed after culling and reordering. Only the schedule-index version feeds aliasing, barriers and load-store inference. The dump exposes both (`FirstUsePass` and `ScheduledFirstUseIndex`) precisely so a mismatch is diagnosable.

## Ownership and leaks

`RenderGraph.Dispose()` releases every tracked backing resource, the heap, and the registry. `_allocatedBackingResources` covers the suballocated and dedicated-upload allocations so dispose and heap-rebuild paths cannot miss one; **extracted resources are deliberately absent**, because their pooled handles belong to `ResourceManager`. `GPUResourceLeakException` exists to report a non-zero reference count at destroy time, but it currently has **no call site** — do not rely on it to catch a leak.

Transient identifiers must not outlive the frame. Nothing enforces this — an identifier is an integer, and using a stale one reads whatever the registry now holds at that index. Persistent state belongs in imported or extracted resources, never in a cached transient identifier.

## Rules that bite

1. **Identifiers die at `Reset()`.** Persist through import or extraction, not by caching.
2. **Transients alias; imported and extracted never do.** If a resource must survive the frame, it has to be one of the latter two.
3. **Upload buffers are excluded from aliasing.** CPU-mapped resources cannot share offsets.
4. **`clearAtFirstUse` / `discardAtLastUse` only apply to attachments.** UAVs and imports need explicit clears.
5. **Prefer `Relative` sizing for resolution-derived resources.** It keeps the graph hash stable across resizes.
6. **Imported resources whose prior contents matter need `initialState`.** Without it the first use discards, and a history read is undefined. `finalState` is needed whenever anything outside the graph touches the resource next.
7. **Extraction takes effect at retire, not at record time.** Do not read the destination handle expecting this frame's data.
8. **`ImportTexture` can return invalid.** Check `IsValid`; a failed lookup logs and continues.
9. **Usage classes are whole-resource.** Aliasing and barriers are decided per resource, never per mip or slice; `GetActualBindlessIndex(..., subResource:)` selects a descriptor for reading one subresource, but that does not narrow the resource's lifetime or state.

## Next

- [Synchronization](synchronization.md) — how the compiler turns these declarations into barriers, load/store ops, native passes and queue assignments.
