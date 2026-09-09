# Shader System Architecture & Developer Guide

**Modules**: `Ghost.AssetForge.Core`, `Ghost.DSL`, `Ghost.Engine.Streaming`, `Ghost.Graphics`, `Ghost.Graphics.RHI`  
**Target Platform**: .NET 10 / Windows / DirectX 12  
**Primary locations**: `src/Editor`, `src/Runtime/Ghost.Engine/Streaming`, `src/Runtime/Ghost.Graphics`, `src/Runtime/Ghost.Core`

---

## 1. Purpose and design goals

GhostEngine treats shaders as a combination of:

1. **Persistent asset identity** used by project files, scenes, and the asset manager.
2. **Immutable metadata** needed to create materials and classify work before shader bytecode is resident.
3. **Baked DXIL bytecode** imported into the runtime shader library.
4. **Pass-specific pipeline state** resolved against the current material and render-target formats.
5. **Runtime variant scheduling** used by GPU-driven rendering and indirect dispatch.

The important consequence is that shader loading is not a single boolean transition. A shader can be known to the runtime and usable for material construction before its DXIL or every required PSO is available.

The system separates these concerns so that:

- startup and scene loading do not scan source DSL or compile shaders;
- worker threads can validate and stage asset data without mutating render-thread-owned GPU state;
- render code can iterate dense semantic rosters without resolving virtual paths per frame;
- a missing shader or PSO cannot silently select an unrelated fallback pipeline;
- compatible shader reloads preserve stable handles and retire old GPU data safely.

---

## 2. Shader asset kinds

### 2.1 Graphics/material shader: `.gshdr`

A `.gshdr` asset describes one graphics shader variant. It can contain multiple passes, including compute passes that are part of the material family. Typical pass semantics are:

- `Forward`
- `Visibility`
- `Shadow`
- `DeferredTexturing`
- `Custom`

A graphics pass records its stage topology. The current graphics path supports mesh-shader pipelines containing amplification, mesh, and pixel stages. A compute pass embedded in a graphics asset is represented by `ShaderStageMask.Compute` and is selected by semantic.

A graphics asset is represented at runtime by `Handle<Shader>` and is registered in `ShaderVariantRegistry`.

### 2.2 Standalone compute shader: `.gcomp`

A `.gcomp` asset is a standalone compute shader. It is not a graphics material variant and is not inserted into the graphics semantic rosters.

It is represented at runtime by `Handle<ComputeShader>` and registered in the standalone compute-shader registry. Its entry points are selected by entry index:

```csharp
computeContext.SetActiveCompute(computeShader, entryIndex);
computeContext.DispatchCompute(x, y, z);
```

Do not confuse these two cases:

- `DeferredTexturing` as a compute pass inside a `.gshdr` material shader;
- a standalone `.gcomp` shader used directly by a compute pass.

---

## 3. Baked shader format

The baker writes a versioned, self-describing `ShaderContentHeader` followed by one serialized block per pass:

```text
[ShaderContentHeader]
for each pass:
    [PassHeader]
    [EntryPointHeader * entryPointCount]
    [DXIL bytecode]
```

The current format version is `4`. The top-level header contains:

- `ShaderType`;
- pass count;
- reflected property-buffer size;
- shader model;
- persistent shader ID;
- shader family ID;
- reflected property-layout hash;
- shader name offset and size.

Each `PassHeader` contains:

- entry-point count;
- explicit `PassSemantic`;
- explicit `ShaderStageMask`;
- deterministic pass ID;
- serialized local `PipelineState`;
- pass name range;
- pass data range.

Each `EntryPointHeader` contains the stage and byte range of one DXIL blob.

The format is defined in [`ShaderContentHeader`](../../src/Runtime/Ghost.Core/AssetHeader.cs). The baker writes it in [`ShaderBaker`](../../src/Editor/Ghost.AssetForge.Core/Bakers/ShaderBaker.cs). Pass semantics are assigned by the DSL/template layer rather than inferred later from arbitrary pass-name strings.

### 3.1 Example source

`src/Test/TestGame/Assets/Shaders/test.gshdr` declares one graphics variant:

```text
shader "MyShader/Standard"
{
    pass "Forward"
    {
        pipeline
        {
            ztest = less_equal;
            zwrite = on;
            cull = back;
            blend = opaque;
            color_mask = all;
        }

        ...

        as "hlsl_block" : "ASMain";
        ms "hlsl_block" : "MSMain";
        ps "hlsl_block" : "PSMain";
    }
}
```

This produces one `Forward` pass with amplification, mesh, and pixel bytecode and the declared local pipeline state.

---

## 4. Runtime identity model

The runtime intentionally maintains multiple identities.

### 4.1 Asset ID

The asset GUID is the persistent identity used by `AssetManager`, manifests, scene data, and virtual-path resolution.

### 4.2 Shader ID

`ShaderContentHeader.shaderId` is the persistent shader identity used by the compiled shader and runtime cache. It is also used to locate compiled bytecode in `ShaderLibrary`.

### 4.3 Family ID

`familyId` groups variants that share a shader family. For example, `StandardLit : Lit` and `MyLit : Lit` can be separate variants with one family ID. Family identity is useful for material classification and compatibility checks, but it is not a replacement for the per-variant shader ID.

### 4.4 Dense `ShaderVariantIndex`

`ShaderVariantIndex` is a compact runtime-only index. It is used for:

- semantic variant rosters;
- classification buffers;
- indirect argument/count slots;
- allocation-free render-loop iteration.

It is intentionally not a serialized identity. It can change when the runtime catalog changes. Persistent data must store the asset GUID or persistent shader ID and resolve the dense index during loading.

`ShaderVariantRegistry` maps both asset GUID and shader ID to the dense index and creates the stable `Handle<Shader>` for each catalog entry.

---

## 5. Startup catalog and asset loading

Packing reads shader metadata from the baked binary payload and writes compact catalog entries into `manifest.json`. The packer does not reconstruct metadata from source DSL a second time; the catalog and payload therefore originate from the same baked representation.

The startup sequence is:

```text
RuntimeContentProvider loads manifest.json
    -> exposes Manifest.Shaders
AssetManager is created
    -> constructs ShaderVariantRegistry and standalone compute registry
ShaderVariantRegistry registers metadata-complete Handle<Shader> values
    -> builds immutable semantic rosters
Runtime initialization runs
    -> materials may be created before DXIL streaming finishes
```

When code resolves a graphics shader asset:

```text
AssetManager.ResolveAsset("Shaders/test")
    -> ShaderAssetEntry is created
    -> entry receives the existing stable Handle<Shader>
    -> ReadAssetData returns that handle immediately
    -> asynchronous loading validates and stages the baked payload
```

The handle must not change when bytecode becomes ready. Bytecode generations are published behind the existing handle.

The same flow applies to `.gcomp`, except the entry and handle types are `ComputeShaderAssetEntry` and `Handle<ComputeShader>`.

---

## 6. Readiness states

### 6.1 Metadata ready

Metadata readiness means that the runtime knows the shader's:

- stable handle;
- name and persistent IDs;
- family ID;
- property-buffer size and layout hash;
- pass count and pass semantics;
- pass IDs;
- stage topology;
- local pipeline states.

Materials require metadata readiness, not bytecode readiness.

### 6.2 Bytecode ready

Bytecode readiness means the complete validated payload for the current generation has been imported into `ShaderLibrary`. The registry publishes this state only after every pass and entry point in the generation has been committed.

A worker thread may read, validate, and stage the payload. It must not mutate live `Shader`, `ShaderLibrary`, or pipeline-library state.

### 6.3 Pipeline ready

Pipeline readiness is specific to the exact use site. It depends on:

- shader bytecode generation or compiled hash;
- pass ID/index;
- material pipeline overrides;
- RTV format set;
- DSV format.

A shader can be bytecode-ready while a requested graphics or compute PSO does not exist yet.

There is no safe universal fallback PSO. If the bytecode, requested pass, or exact pipeline is unavailable, skip the operation or leave its indirect count at zero. Binding an unrelated fallback can render with the wrong material family, property layout, or attachment contract.

---

## 7. Thread ownership and commit boundary

Shader bytecode is CPU data. It does not need to be copied through the GPU copy queue, so shader loading must not pretend that DXIL is a texture or mesh upload.

The safe boundary is:

```text
Worker thread: OnLoadContent
    -> read baked payload
    -> validate identity, ranges, pass metadata, and stage topology
    -> retain private staged payload
    -> enqueue shader commit

Render-thread frame prelude
    -> import staged bytecode into ShaderLibrary
    -> publish one complete bytecode generation
    -> mark the variant BytecodeReady
    -> make the generation visible to render-graph recording

Render-graph recording
    -> resolve exact pass and PSO
    -> bind and dispatch/draw only when resolution succeeds
```

This prevents a render thread from observing a partially imported pass table or a worker thread from mutating data while command recording is reading it.

For reloads, the new generation is built off to the side and atomically published as one complete generation. If validation or publication fails, the previous generation remains active. Replaced DXIL and PSOs are retired only after in-flight frames can no longer reference them.

---

## 8. Pass and PSO selection

### 8.1 Graphics material pass

Use the semantic API rather than a numeric pass index when selecting a material pass:

```csharp
rasterContext.SetActiveMaterialPass(material, PassSemantic.Forward);
```

The runtime performs:

```text
Material -> Shader handle
    -> Shader.GetPassIndex(semantic)
    -> ShaderPass
    -> material pass override
    -> exact graphics PSO key
    -> pipeline-library lookup or creation
    -> SetPipelineState
```

`SetActiveMaterialPass` currently returns `void`. It returns without binding when the semantic is absent or the exact PSO cannot be resolved. Therefore a caller cannot distinguish a successful bind from a skipped bind through this API alone. Production indirect graphics rendering must use a result-aware resolver, or prove readiness and exact pipeline availability before calling it; otherwise a following draw can accidentally reuse stale command-buffer state. The compute path already avoids this ambiguity with `TrySetActiveShaderPass`.

### 8.2 Standalone compute pass

For a standalone compute shader:

```csharp
computeContext.SetActiveCompute(computeShader, entryIndex);
computeContext.DispatchCompute(x, y, z);
```

For a compute pass embedded in a graphics shader variant, use the semantic resolver:

```csharp
if (computeContext.TrySetActiveShaderPass(shader, PassSemantic.DeferredTexturing))
{
    computeContext.DispatchCompute(x, y, z);
}
```

The resolver checks the pass topology and exact compute PSO before binding.

### 8.3 Pass state and attachment formats

The DSL's local pipeline state is not the complete graphics PSO key. The final key also includes the current attachment formats and material overrides. Consequently, one shader pass can produce multiple PSOs for different render targets or material states.

---

## 9. Variants and GPU-driven rendering

The render loop should not scan assets or resolve virtual paths. It should iterate the immutable semantic roster prepared by `ShaderVariantRegistry`:

```csharp
var variants = assetManager.ShaderVariants.GetVariants(PassSemantic.DeferredTexturing);
for (var i = 0; i < variants.Length; i++)
{
    var variantIndex = variants[i];
    ref readonly var variant = ref assetManager.ShaderVariants.GetVariant(variantIndex);

    if (!assetManager.ShaderVariants.IsBytecodeReady(variantIndex))
    {
        continue;
    }

    // Resolve the semantic pass and exact PSO, then execute its range.
}
```

Classification data should store `ShaderVariantIndex`, not a managed shader path or a shader handle. The dense index selects the variant's indirect argument/count slot.

The current allocation-free compute helper demonstrates the intended policy:

```csharp
var executeCount = ShaderVariantRendering.ExecuteIndirectCompute(
    variantSource,
    computeContext,
    PassSemantic.DeferredTexturing,
    commandSignature,
    maxCommandCount,
    argumentBuffer,
    argumentBaseOffset,
    argumentRangeStride,
    countBuffer,
    countBaseOffset,
    countStride);
```

For every roster entry it:

1. checks bytecode readiness;
2. resolves the requested semantic pass;
3. skips unavailable variants;
4. maps the dense index to its argument and count range;
5. records `ExecuteIndirect` with the per-variant maximum command count.

The same policy applies to a future graphics material roster: classify work by dense variant index, resolve the semantic pass, bind the exact graphics PSO, and execute only that variant's non-empty indirect range.

---

## 10. Test shader walkthrough

The test setup resolves and stores the shader handle as follows:

```csharp
var shaderAsset = engineCore.AssetManager.ResolveAsset("Shaders/test");

var shaderHandle = default(Handle<Shader>);
shaderAsset.ReadAssetData(ref shaderHandle);

var material = engineCore.RenderEngine.ResourceManager.CreateMaterial(shaderHandle);
var materialPalette = engineCore.RenderEngine.ResourceManager.GetOrCreateMaterialPalette([material]);
```

`ReadAssetData` returns the stable catalog handle. It does not mean that the DXIL has already been imported.

The current raster binding call is:

```csharp
context.SetGlobalData(data.frameBufferIndex, data.viewBufferIndex);
context.SetInstanceIndex(data.instanceIndex);
context.SetActiveMaterialPass(data.material, PassSemantic.Forward);
context.SetActiveMesh(data.mesh);
```

Do not unconditionally call `DispatchMesh` after this sequence in production code yet. `SetActiveMaterialPass` does not report whether it bound a pipeline. The graphics variant path needs a result-aware bind operation, analogous to `TrySetActiveShaderPass`, so dispatch occurs only after the exact `Forward` PSO was successfully resolved.

For `test.gshdr`, `PassSemantic.Forward` resolves pass index `0`. The pass uses:

- amplification shader `ASMain`;
- mesh shader `MSMain`;
- pixel shader `PSMain`;
- `ztest = less_equal`;
- `zwrite = on`;
- `cull = back`;
- `blend = opaque`;
- `color_mask = all`.

The shader can be loaded and its material can be created before bytecode streaming finishes. The actual dispatch must still occur only after the bytecode generation and exact PSO are available.

The `Setup` path currently creates the material palette and mesh instance. The production render pipeline must still connect that scene data to a raster pass that calls `SetActiveMaterialPass`, `SetActiveMesh`, and `DispatchMesh` (or the corresponding indirect path).

---

## 11. Material palettes and classification

A material palette is an ordered collection of material handles associated with a mesh instance. `MaterialPaletteStore` owns the palette and uploads the palette's material-index data for GPU scene use.

The palette is not the shader variant roster:

- **material palette**: instance-local ordered material slots;
- **shader variant registry**: global catalog of graphics shader variants and semantic rosters;
- **dense variant index**: global runtime slot used for classification and indirect dispatch.

A GPU-driven renderer can therefore use both values:

```text
GPU instance
    -> material palette ID
    -> local material slot
    -> material handle / shader identity
    -> dense ShaderVariantIndex
    -> semantic indirect range
```

Do not write a virtual asset path into GPU classification buffers. Resolve persistent identities on the CPU and write compact runtime indices into GPU data.

---

## 12. Reload compatibility and lifetime safety

A compatible reload keeps stable:

- asset GUID;
- persistent shader ID;
- shader family identity;
- `Handle<Shader>` or `Handle<ComputeShader>`;
- catalog pass semantics and compatible topology;
- dense index within the active catalog generation.

Before publication, reload validation checks the metadata that existing materials depend on:

- property-layout hash and property-buffer size;
- shader model;
- pass count and ordering;
- pass IDs;
- pass semantic;
- local pipeline state;
- stage topology and entry-point count.

If the property layout or pass topology is incompatible, reject the new generation and keep the previous generation active. Never replace live metadata underneath existing materials when their buffer layout or pass contract would change.

When a compatible generation is published:

1. publish it at the render-thread prelude;
2. invalidate or lazily replace PSO keys for the old compiled hash;
3. retain old DXIL and PSOs until the last in-flight frame retires;
4. then release retired storage.

---

## 13. Developer rules

1. Use `PassSemantic` for pass selection. Do not duplicate pass-name string comparisons in render code.
2. Treat `ShaderVariantIndex` as runtime-only. Persist asset GUIDs or persistent shader IDs instead.
3. Separate metadata readiness, bytecode readiness, and exact PSO readiness.
4. Never mutate live shader-library or pipeline state from worker-thread asset loading.
5. Do not route DXIL through a fake GPU upload operation.
6. Skip a variant when its bytecode, semantic pass, or exact PSO is unavailable.
7. Never bind an unrelated fallback PSO.
8. Keep render-loop iteration on prebuilt semantic rosters; do not resolve asset paths per frame.
9. Include pass identity in every graphics and compute PSO key.
10. Retire replaced shader generations and PSOs behind frame-fence completion.
11. Keep `.gcomp` standalone compute shaders separate from compute passes embedded in `.gshdr` material variants.
12. For render-graph callbacks, prefer static allocation-free functions and declare every resource access to the graph.

---

## 14. Relevant source files

- [`ShaderContentHeader`](../../src/Runtime/Ghost.Core/AssetHeader.cs)
- [`ShaderDescriptor` and `PassSemantic`](../../src/Runtime/Ghost.Core/Graphics/ShaderDescriptor.cs)
- [`ShaderBaker`](../../src/Editor/Ghost.AssetForge.Core/Bakers/ShaderBaker.cs)
- [`ShaderAssetEntry`](../../src/Runtime/Ghost.Engine/Streaming/ShaderAssetEntry.cs)
- [`ComputeShaderAssetEntry`](../../src/Runtime/Ghost.Engine/Streaming/ComputeShaderAssetEntry.cs)
- [`ShaderVariantRegistry`](../../src/Runtime/Ghost.Engine/Streaming/ShaderVariantRegistry.cs)
- [`ComputeShaderRegistry`](../../src/Runtime/Ghost.Engine/Streaming/ComputeShaderRegistry.cs)
- [`ShaderVariantRendering`](../../src/Runtime/Ghost.Graphics/ShaderVariantRendering.cs)
- [`RenderGraphContext`](../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphContext.cs)
- [`MaterialPaletteStore`](../../src/Runtime/Ghost.Graphics/Services/MaterialPaletteStore.cs)
- [`test.gshdr`](../../src/Test/TestGame/Assets/Shaders/test.gshdr)
- [`Setup`](../../src/Test/TestGame/Setup.cs)
