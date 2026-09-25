# Writing Render Passes

A pass is one unit of rendering work — a compute dispatch, a set of draws, a clear. This page walks through writing them, from the smallest useful example up to the controls that steer culling and queue assignment.

Every example here is complete for the graph's side of the work. Binding meshes, materials and shader properties in detail belongs to the RHI, and is linked where it matters.

## The Shape of a Pass

Every pass is written the same four steps:

```csharp
using (var builder = renderGraph.AddComputeRenderPass<MyPassData>("MyPass"))   // 1. create
{
    var output = builder.CreateBuffer(desc, "MyOutput");                       // 2. declare
    builder.UseBuffer(output, AccessFlags.Write);

    builder.SetPassData(new MyPassData { output = output });                   // 3. hand over data

    builder.SetRenderFunc<MyPassData>(static (ref readonly data, ctx) =>       // 4. record GPU work
    {
        // ctx.DispatchCompute(...);
    });
}
```

The builder is what you declare on; the pass data struct carries values into the render function; the render function records the actual GPU commands against a context. The `using` is not decoration — **disposing the builder is what closes the pass** and runs its validation.

> **Note:** `RenderGraph` reuses a single builder object for the whole frame, so the one you hold starts configuring the *next* pass as soon as you call `Add*RenderPass` again. Always finish a pass inside its `using` block.

## Your First Pass: Fill a Texture

A raster pass drawing fullscreen into one transient texture:

```csharp
private struct FillPassData
{
    public Identifier<RGTexture> target;
    public Handle<Shader> shader;
}

Identifier<RGTexture> sceneColor;
using (var builder = renderGraph.AddRasterRenderPass<FillPassData>("FillSceneColor"))
{
    sceneColor = builder.CreateTexture(
        RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm),
        "SceneColor");

    builder.SetColorAttachment(sceneColor, 0, AccessFlags.WriteAll);
    builder.SetPassData(new FillPassData { target = sceneColor, shader = myShader });

    builder.SetRenderFunc<FillPassData>(static (ref readonly data, ctx) =>
    {
        if (!ctx.TrySetActiveShaderPass(data.shader, PassSemantic.Forward))
        {
            return;
        }

        ctx.DispatchMesh(1, 1, 1);
    });
}
```

Three things to notice. The texture is created *inside* the pass that first writes it, which is what lets the graph see it as this pass's output. A raster pass must declare at least one color or depth attachment — with none, there is no render pass to draw into. And the viewport and scissor are set for you from the attachment's size, so this pass does not mention them.

`TrySetActiveShaderPass` returns `false` when pipeline state cannot be resolved, which is why the early return is there. The un-prefixed `SetActive*` variants throw instead; prefer `Try` and bail out.

## Reading and Writing Resources

Tell the graph what a pass touches with `UseTexture` and `UseBuffer`, both of which return the resource so you can use the result inline:

```csharp
var scene       = builder.UseBuffer(importedScene, AccessFlags.Read);
var depth       = builder.UseTexture(depthTexture, AccessFlags.Read);
var history     = builder.UseTexture(historyTexture, AccessFlags.ReadWrite);
```

The access flags say how, and they matter beyond dependency tracking:

| Flag | Meaning |
|---|---|
| `Read` | Sample or load. Existing contents must survive. |
| `Write` | Modify. Other pixels may already be there. |
| `Discard` | Previous contents are not needed. |
| `WriteAll` | `Write | Discard` — overwrite everything. Use for fullscreen passes. |
| `ReadWrite` | Read-modify-write. |

`Discard` is not advisory: it turns an attachment's load action into "don't care" instead of "load", so passing `WriteAll` on a fullscreen pass is a real bandwidth saving rather than a hint the compiler may ignore.

> **Note:** Undeclared access is invisible. A read you do not declare gets no barrier before it, and the memory behind it may already have been handed to another resource. Declare everything.

## Running a Compute Pass

Compute passes declare writes with `UseTexture` / `UseBuffer` and `AccessFlags.Write`, which the graph reads as unordered-access usage — that is the only way a compute pass gets a UAV:

```csharp
private struct ReducePassData
{
    public Identifier<RGTexture> source;
    public Identifier<RGBuffer> result;
    public Handle<ComputeShader> shader;
    public uint groupCount;
}

using (var builder = renderGraph.AddComputeRenderPass<ReducePassData>("Reduce"))
{
    var result = builder.CreateBuffer(new BufferDesc
    {
        Size = 1024 * 4,
        Stride = 4,
        Usage = BufferUsage.Raw | BufferUsage.UnorderedAccess | BufferUsage.ShaderResource
    }, "ReduceResult");

    builder.UseTexture(source, AccessFlags.Read);
    builder.UseBuffer(result, AccessFlags.Write);

    builder.SetPassData(new ReducePassData { source = source, result = result, shader = computeShader, groupCount = 16 });

    builder.SetRenderFunc<ReducePassData>(static (ref readonly data, ctx) =>
    {
        ctx.SetActiveCompute(data.shader, 0);

        var props = new ReduceProperties
        {
            sourceIndex = ctx.GetActualBindlessIndex(data.source, BindlessAccess.ShaderResource),
            resultIndex = ctx.GetActualBindlessIndex(data.result, BindlessAccess.UnorderedAccess),
        };

        ctx.SetUserDataWithProperties(in props, target: DataTarget.Compute);
        ctx.DispatchCompute(data.groupCount, 1, 1);
    });
}
```

Bindless indices cannot be known while you are building the frame — the memory does not exist yet — which is why they are resolved inside the render function rather than passed in. `GetActualBindlessIndex` is only valid there for the same reason.

A compute pass may not declare attachments, and a raster or unsafe pass may not declare a plain `Write`: the graph cannot tell whether you meant an attachment, a UAV, or an explicit view clear, so it rejects the ambiguity rather than guessing. Raster and unsafe passes use `UseRandomAccessTexture` / `UseRandomAccessBuffer` for UAV access instead.

## Passing Data to the Render Function

The pass data struct is copied into the pass at build time and handed back to you by reference at execute time. It is the only channel from frame construction into GPU recording, so anything the render function needs travels in it — shader handles, dispatch sizes, viewport rectangles, indices.

```csharp
builder.SetPassData(new ReducePassData { source = source, groupCount = groupCount });
```

Keep it a plain unmanaged struct of small values. Storing `Identifier<RG*>` is normal and expected; storing a `Handle<GPUTexture>` is not, because the real resource does not exist until compile.

**Write the render function as a `static` lambda.** The delegate is stored on the pass and invoked later; a lambda that captures locals allocates a closure every frame. A static lambda has nothing to capture, so everything it needs must be in the data struct:

```csharp
builder.SetRenderFunc<ReducePassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(data.groupCount, 1, 1));
```

The graph does not reject a capturing lambda — it just costs you an allocation per frame per pass, and silently invalidates the compilation cache, because the graph hash includes the delegate's identity. If your frames recompile for no visible reason, a non-static lambda is the usual cause.

## Clearing a Target

For a texture bound as an attachment, the clear is a load action, not a pass:

```csharp
var desc = RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm, clearColor: new Color128(0, 0, 0, 1));

using (var builder = renderGraph.AddRasterRenderPass<ClearPassData>("ClearSceneColor"))
{
    builder.SetColorAttachment(sceneColor, 0, AccessFlags.WriteAll);
    builder.SetPassData(new ClearPassData { target = sceneColor });
    builder.SetRenderFunc<ClearPassData>(static (ref readonly data, ctx) => { });
}
```

An empty render function is legal and correct here — `clearAtFirstUse` (the default on `RGTextureDesc.Relative`) makes the hardware clear the attachment when the render pass begins. Depth uses `RGTextureDesc.RelativeDepth` with `clearDepth`, and `SetDepthAttachment`.

Buffers have no load actions, so clear them explicitly: `ctx.ClearBuffer(id, sizeInBytes)` inside a pass, or `AddClearBufferPass` to add a dedicated clear pass.

## Depth and Multiple Targets

A raster pass binds up to eight color slots and one depth attachment:

```csharp
builder.SetColorAttachment(gbuffer0, 0, AccessFlags.WriteAll);
builder.SetColorAttachment(gbuffer1, 1, AccessFlags.WriteAll);
builder.SetDepthAttachment(depth, AccessFlags.ReadWrite);
```

Consecutive raster passes with the *same* attachment set are merged into one hardware render pass, which is where most of the graph's performance comes from. Two habits protect merging: keep a pass's resource touches to its own attachments, and prefer attachments over UAV writes for anything a raster pass produces. [Synchronization](synchronization.md) explains what breaks merging.

The viewport and scissor follow the attachments automatically. Override with `ctx.SetViewport` / `ctx.SetScissorRect` only for sub-region passes.

## Copies and Raw Commands

The unsafe pass type exists for work the graph cannot model. It exposes the raw command buffer plus buffer mapping:

```csharp
using (var builder = renderGraph.AddUnsafeRenderPass<UploadPassData>("UploadIndirectArgs"))
{
    builder.UseRandomAccessBuffer(indirectArgs);
    builder.SetPassData(new UploadPassData { target = indirectArgs, count = drawCount });

    builder.SetRenderFunc<UploadPassData>(static (ref readonly data, ctx) =>
    {
        ctx.WriteBuffer(data.target, in data.count, offset);
    });
}
```

`GetCommandBufferUnsafe()`, `MapBuffer` / `UnmapBuffer`, `WriteBuffer<T>` and the full raster and compute surfaces are all available here. Use it sparingly: an unsafe pass always breaks render pass merging, and it is never a candidate for async compute.

Plain GPU-to-GPU copies are the one gap. A DMA copy wants a copy-source and copy-destination state, which the graph cannot currently declare, so `AddCopyTexturePass` and `AddCopyBufferPass` are rejected by validation and unused by the engine. For now, blit with `AddBlitPass(src, dst, shader)` — a raster pass with a fullscreen blit shader — or copy from a compute pass through UAVs.

## Keeping a Pass from Being Culled

A pass is removed when nothing observable consumes its output. That is usually what you want, but a pass whose real effect the graph cannot see — writing persistent memory through a raw call, initializing a work-graph program — must opt out:

```csharp
using var builder = renderGraph.AddComputeRenderPass<MeshletCullPass1Data>("MeshletCull_Pass1");

// This pass carries the one-time backing memory initialization, so it must never be culled.
builder.AllowPassCulling(false);
```

Writing an imported or extracted resource already grants this protection, since the graph treats a write outside itself as a side effect and never culls such a pass.

## Asking for Async Compute

```csharp
builder.EnableAsyncCompute(true);
```

This is a hint, not a command. The graph moves the pass to the compute queue only when it finds graphics work that can run alongside it — some independent raster pass between this one and the first pass that reads its results. Otherwise the pass stays on the graphics queue and the frame is correct either way.

A pass is only eligible if it is a compute pass with no attachments and no side effects. To confirm what happened in a given frame, read `QueueDecision` from a dump — see [Debugging and Validation](debugging.md).

## Common Mistakes

- **Forgetting a declaration.** The symptom is garbage in a resource that "obviously" was written earlier. Declare every read and write.
- **Skipping the `using`.** The pass is never finalized or validated, and the next `Add*RenderPass` abandons it mid-construction.
- **A non-static render lambda.** Costs an allocation per frame and breaks the compilation cache.
- **Declaring a write on a raster pass.** Rejected; use an attachment or `UseRandomAccess*`.
- **Attachment plus UAV on the same resource in one pass.** Rejected; one usage class per resource per pass.
- **Caching an identifier across frames.** Identifiers die at `Reset()`. Import or extract instead — see [Resources](resources.md).
- **Assuming async compute happened.** Check `QueueDecision`.

## Next

- [Resources](resources.md) — transient, imported and extracted, and sizing for the viewport.
- [Synchronization](synchronization.md) — what the graph derives from what you declared.
