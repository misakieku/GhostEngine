# Resources

A resource is a texture or a buffer the graph knows about. Creating one describes it rather than allocating it — the GPU memory arrives at compile time, once the graph knows which resources can share. This page is about the choices you make: which kind, what size, and how long it lives.

## Choosing a Kind

Ask what owns the data:

- **Something temporary inside this frame?** Create it transiently with `CreateTexture` / `CreateBuffer`. The graph allocates it, recycles its memory, and deletes it when the frame ends.
- **It already exists and outlives the frame?** Import it with `ImportTexture` / `ImportBuffer` — the swap-chain back buffer, a persistent history texture, a scene buffer built earlier.
- **This frame produces it and next frame consumes it?** Create it transiently, then extract it so a persistent handle receives the result.

| Kind | Created by | Lives | Shares memory with other resources |
|---|---|---|---|
| Transient | `CreateTexture` / `CreateBuffer` | One frame | Yes |
| Imported | `ImportTexture` / `ImportBuffer` | Beyond the graph | No |
| Extracted | transient + `QueueTextureExtraction` / `QueueBufferExtraction` | Beyond the graph, after the frame | No |

The distinction that matters is whether the graph may reuse the memory. Transients are disposable; imported and extracted resources have a life outside the frame and are pinned. Getting this wrong is the difference between a correct frame and a resource whose contents were quietly recycled by an unrelated pass.

Resources are addressed by `Identifier<RGTexture>` / `Identifier<RGBuffer>`, which are integers valid for one frame. `Reset()` clears the registry, so index 3 in one frame is a different texture the next. Never cache an identifier across frames — persist a `Handle<GPUTexture>` by importing or extracting instead.

## Creating a Transient

```csharp
var sceneColor = builder.CreateTexture(
    RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm),
    "SceneColor");

var counter = builder.CreateBuffer(new BufferDesc
{
    Size = 128,
    Stride = 4,
    Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
}, "CounterBuffer");
```

Textures take an `RGTextureDesc`; buffers take the RHI's `BufferDesc`. Neither touches GPU memory, and neither validates size or format until compile.

`RGTextureDesc` has four factories: `Absolute(width, height, format)`, `Relative(scale, format)`, `Relative(scaleX, scaleY, format)`, and `RelativeDepth(scale)` for depth-stencil. They default to `clearAtFirstUse` and `discardAtLastUse` both true, and the colour factories to `TextureUsage.RenderTarget | ShaderResource`.

There is also `CreateTexture(existingTexture, "name")`, which clones another resource's descriptor — useful for a ping-pong pair, and it returns an invalid identifier if handed one, so it composes with optional inputs.

## Sizing for the Viewport

Relative textures are resolved against a `ViewState` at compile time:

```csharp
var viewState = new ViewState(
    renderView.ScreenSize.x, renderView.ScreenSize.y,   // what Relative scales against
    renderView.ScreenSize.x, renderView.ScreenSize.y);  // pre-upscale render size

renderGraph.CompileAndExecute(executionContext, viewState);
```

`Relative(0.5f, ...)` is half the viewport, whatever the window is doing. The fourth and fifth fields exist for upscalers that render at one resolution and present at another.

**Prefer `Relative` for anything resolution-derived.** The graph caches a frame's compilation, keyed on the resource descriptors — and for relative textures it records the *scale*, not the resolved pixel size. Dragging the window therefore reuses the cached compilation and only resizes the memory. An `Absolute` texture bakes its dimensions into that key, so every distinct size is a fresh compile. Reserve `Absolute` for genuinely fixed sizes: counter buffers, indirect argument buffers, small lookup tables.

## Importing an Existing Resource

```csharp
var backBuffer = renderGraph.ImportTexture(renderView.ColorTexture);
var sceneBuffer = renderGraph.ImportBuffer(gpuScene.InstanceBuffer);
```

The descriptor is read back from the resource database, so the import reflects the real resource rather than your estimate. A failed lookup logs an error and returns an invalid identifier — check `IsValid` before using it.

Imports can declare the state the resource is in when the frame starts, and the state to leave it in at the end. You need these when something outside the graph touches the resource on either side — the back buffer is the clear case, since it must be presentable after the frame and is in present state before it:

```csharp
var present = new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None);

var colorTarget = renderGraph.ImportTexture(
    renderView.ColorTexture,
    initialState: present,
    finalState: present,
    clearColor: new Color128(0.05f, 0.05f, 0.05f, 1.0f),
    clearAtFirstUse: true);
```

> **Note:** An import with no `initialState` starts the frame with unknown contents as far as the graph is concerned, so its first use **discards** rather than loads. If a pass reads what a previous frame wrote — history buffers, temporal accumulation, an HZB carried across frames — supply `initialState`. Without it the read is undefined.

## Keeping a Resource for Next Frame

Extract it from the pass that produces it:

```csharp
using (var builder = renderGraph.AddComputeRenderPass<BuildHzbPassData>("BuildHZB"))
{
    var hzb = builder.CreateTexture(RGTextureDesc.Absolute(...), "Hzb");
    builder.UseRandomAccessTexture(hzb);
    builder.QueueTextureExtraction(hzb, persistentHzbHandle);
    // ...
}
```

Extraction pins the resource out of memory sharing and gives it a fresh pooled backing each frame. It also counts as a read of the resource, so the pass that extracts it does not need a separate `UseTexture`.

The handover happens when the frame **retires**, not immediately. That is deliberate: GPU work in flight may still be sampling `persistentHzbHandle`, and swapping it at record time would let that reader see this frame's not-yet-written texture. Deferring is what makes history-buffer patterns correct without an explicit stall — but it also means you must not read `persistentHzbHandle` expecting this frame's results.

`ResourceExtractionFlags.ReleaseAfterExtract` replaces the destination's resource instead of swapping the two. Use the default unless you have a reason.

## Letting the Graph Share Memory

Transient resources whose uses never overlap are placed at the same offset in one video-memory heap, so a frame's memory follows its peak concurrency rather than its total size. You do not configure this, and you do not opt in — every transient is eligible.

Three consequences reach into your code:

**Contents are undefined before the first write.** A transient's memory held something else earlier in the frame, and the graph discards it rather than preserving it. If a pass reads a texture before writing it, that read is garbage. Either declare the read so the graph knows to preserve contents, or clear it first.

**Granularity is the whole resource.** Aliasing and state are decided per texture or buffer, never per mip level or array slice. Reading one mip still keeps the entire texture alive, so a mip chain that is partly dead and partly live should be two resources.

**A capture tool shows one allocation with several names.** This confuses debugging more than anything else in the graph. See [Debugging and Validation](debugging.md).

Two things are excluded because sharing them would be wrong: buffers created with `HeapType.Upload`, which the CPU maps, and anything imported or extracted, which outlives the frame.

## Avoid

- Caching an `Identifier<RGTexture>` in a field for next frame.
- Building a descriptor from frame-varying data — a resolution, a count that changes — instead of putting that value in pass data. It invalidates the cached compilation every frame.
- Leaving `initialState` off an import that a later frame reads.
- Reading an extraction's destination handle in the same frame that queued it.
- Storing a `Handle<GPUTexture>` in pass data rather than the graph identifier. Resolve it inside the render function with `GetActualTexture`.

## Next

- [Synchronization](synchronization.md) — what the graph derives from the declarations you wrote.
