# Ghost Shader Concept - High Performance Material System

A modern, high-performance material and shader system designed for maximum efficiency and flexibility. Built with data-oriented design principles inspired by Unity DOTS, Unreal Engine 5, and modern rendering engines.

## Architecture Overview

### Core Design Principles

1. **Data-Oriented Design**: Cache-friendly memory layouts for optimal performance
2. **Lock-Free Where Possible**: Concurrent collections and atomic operations minimize contention
3. **Zero-Allocation Hot Paths**: Struct-based keys and value types reduce GC pressure
4. **Compile-Time Variants**: Shader permutations compiled ahead or on-demand
5. **Batch-Friendly**: Automatic PSO batching for minimal state changes

---

## System Components

### 1. Keyword System (`ShaderKeyword.cs`, `KeywordSet.cs`)

**Keywords** enable/disable shader features at compile time, creating variants.

- **Global Keywords**: Engine-wide settings (HDR, shadow quality, platform features)
- **Local Keywords**: Per-material settings (normal mapping, alpha test, etc.)

**KeywordSet**: Compact bitset (256 global + 256 local keywords) using unsafe fixed buffers
- O(1) enable/disable/query operations
- Fast hash computation for variant key generation
- Supports merging global + local keywords

```csharp
var keywords = new KeywordSet();
keywords.Enable(alphaTestKeyword);
keywords.Enable(normalMapKeyword);
ulong hash = keywords.ComputeHash(); // For variant lookup
```

### 2. Shader Variant System (`ShaderKeys.cs`)

**ShaderVariantKey**: Uniquely identifies a compiled shader variant
- Combines shader program ID + keyword hash
- Used as cache key for `IShaderCompiler`

**GraphicsPipelineKey**: Uniquely identifies a complete PSO
- Combines shader variant + render state hash + pass ID
- Used as cache key for `IPipelineLibrary`

### 3. Render State (`RenderState.cs`)

Immutable, hashable pipeline state:
- Rasterizer (cull mode, fill mode, depth bias)
- Depth-Stencil (test/write enable, compare func, stencil ops)
- Blend State (per-RT blend factors and operations)
- Topology

```csharp
var state = RenderState.Default;
state.BlendEnable = true;
state.SrcBlend = BlendFactor.SrcAlpha;
ulong hash = state.ComputeHash();
```

### 4. Shader Programs (`ShaderProgram.cs`)

A **ShaderProgram** represents a complete shader with multiple passes.

**ShaderPass**: Single rendering pass with:
- Name and ID
- Default render state
- Entry point functions (vertex/pixel)

**Builder Pattern** for clean creation:

```csharp
var shader = new ShaderProgramBuilder()
    .WithName("StandardPBR")
    .AddPass("ForwardBase", RenderState.Default)
    .AddPass("ShadowCaster", shadowState)
    .DeclareKeywords(alphaTest, normalMap)
    .Build();
```

### 5. Material Properties (`MaterialPropertyBlock.cs`)

**Thread-safe**, **linear memory layout** for GPU upload efficiency.

Supports:
- Scalars (float, int)
- Vectors (float2, float3, float4)
- Matrices (4x4)
- Textures (planned)

Properties are **16-byte aligned** for GPU compatibility and stored contiguously.

```csharp
var props = new MaterialPropertyBlock();
props.SetFloat("_Metallic", 0.5f);
props.SetVector4("_Color", 1, 0, 0, 1);
unsafe {
    props.CopyTo(gpuBufferPtr, bufferSize);
}
```

### 6. Materials (`Material.cs`)

High-level material instance combining:
- **Shader program** reference
- **Property block** for per-material data
- **Local keywords** for variant selection
- **Per-pass render state overrides**

**Thread-safe** for property updates. **Dirty tracking** for efficient GPU updates.

```csharp
var material = new Material(shaderProgram);
material.SetFloat("_Metallic", 0.8f);
material.EnableKeyword(normalMapKeyword);
material.SetPassRenderState("ForwardBase", transparentState);

// Get pipeline key for rendering
var psoKey = material.GetPipelineKey(passIndex, globalKeywords);
```

**Cloning** for material instances:
```csharp
var clone = material.Clone(); // Deep copy of properties and state
```

### 7. Global State (`GlobalKeywordState.cs`)

Singleton managing **engine-wide keywords**.
- Thread-safe keyword enable/disable
- Version tracking for cache invalidation
- Automatic merging with local keywords during rendering

```csharp
GlobalKeywordState.Instance.EnableKeyword(hdrKeyword);
var keywords = GlobalKeywordState.Instance.GetKeywordSet();
```

### 8. Batch Renderer (`MaterialBatchRenderer.cs`)

**Core rendering system** that:
1. Groups draw calls by PSO (shader variant + render state + pass)
2. Ensures shader variants are compiled
3. Gets/creates PSOs from pipeline library
4. Returns sorted batches for minimal state changes

```csharp
var batches = batchRenderer.BatchDrawCalls(drawCalls);
foreach (var batch in batches) {
    SetPipeline(batch.Pipeline);
    foreach (var draw in batch.DrawCommands) {
        // Upload material properties
        // Issue draw call
    }
}
```

**Async Warmup** for pre-compiling variants:
```csharp
await batchRenderer.WarmupVariantsAsync(shader, variantConfigs);
```

### 9. Material Pooling (`MaterialPool.cs`)

Object pool for material instances to reduce allocations.

```csharp
var material = pool.Rent(shaderProgram);
// Use material...
pool.Return(material);
```

---

## Performance Characteristics

### Memory Layout
- **KeywordSet**: 64 bytes (fixed size, stack-allocated)
- **RenderState**: ~60 bytes (stack-allocated)
- **MaterialPropertyBlock**: Variable, contiguous, 16-byte aligned

### Complexity
- **Keyword enable/disable**: O(1)
- **Hash computation**: O(1) - fixed iterations
- **Pipeline key generation**: O(1)
- **Batch sorting**: O(N log N) where N = unique PSOs (typically << draw calls)

### Concurrency
- **Lock-free**: Keyword queries, hash computation, key generation
- **Concurrent**: Variant compilation cache, PSO cache
- **Thread-safe**: Material property updates, global keyword changes

---

## Usage Example

```csharp
// 1. Setup
var registry = ShaderKeywordRegistry.Instance;
var normalMap = registry.GetOrRegister("NORMAL_MAP", KeywordScope.Local);
var hdr = registry.GetOrRegister("HDR", KeywordScope.Global);

GlobalKeywordState.Instance.EnableKeyword(hdr);

// 2. Create shader
var shader = new ShaderProgramBuilder()
    .WithName("PBR")
    .AddPass("Forward", RenderState.Default)
    .AddPass("Shadow", shadowState)
    .DeclareKeywords(normalMap)
    .Build();

// 3. Create material
var material = new Material(shader);
material.SetVector4("_BaseColor", 1, 0, 0, 1);
material.SetFloat("_Metallic", 0.8f);
material.EnableKeyword(normalMap);

// 4. Render
var batchRenderer = new MaterialBatchRenderer(compiler, pipelineLib);
var batches = batchRenderer.BatchDrawCalls(drawCommands);

foreach (var batch in batches) {
    commandList.SetPipeline(batch.Pipeline);
    foreach (var draw in batch.DrawCommands) {
        unsafe {
            draw.Material.CopyPropertiesTo(cbufferPtr, cbufferSize);
        }
        commandList.DrawIndexed(draw.IndexCount, ...);
    }
}
```

---

## Advanced Features

### Per-Pass State Overrides

Materials can override render state per-pass:

```csharp
var transparentState = RenderState.Default;
transparentState.BlendEnable = true;
transparentState.SrcBlend = BlendFactor.SrcAlpha;
transparentState.DestBlend = BlendFactor.InvSrcAlpha;

material.SetPassRenderState("Forward", transparentState);
material.SetPassRenderState("Shadow", RenderState.Default); // Opaque shadow
```

### Shader Variant Warmup

Pre-compile common variants to avoid runtime hitches:

```csharp
var variants = new[] {
    keywordSet1, // No features
    keywordSet2, // Normal map only
    keywordSet3, // Normal map + alpha test
    // ...
};

await batchRenderer.WarmupVariantsAsync(shader, variants);
```

### Material Property Inheritance

Clone materials with shared base properties:

```csharp
var baseMaterial = new Material(shader);
baseMaterial.SetVector4("_BaseColor", 1, 1, 1, 1);

var redVariant = baseMaterial.Clone();
redVariant.SetVector4("_BaseColor", 1, 0, 0, 1);

var blueVariant = baseMaterial.Clone();
blueVariant.SetVector4("_BaseColor", 0, 0, 1, 1);
```

---

## Extension Points

### Custom Property Types

Extend `MaterialPropertyBlock` for custom data:

```csharp
public void SetCustomStruct<T>(string name, T value) where T : unmanaged
{
    // Implementation
}
```

### Material Property Validation

Add validation in `Material` setters:

```csharp
public void SetFloat(string name, float value)
{
    if (value < 0 || value > 1)
        throw new ArgumentOutOfRangeException();
    _propertyBlock.SetFloat(name, value);
}
```

### Custom Batching Strategies

Subclass or compose with `MaterialBatchRenderer`:

```csharp
public class DepthSortedBatchRenderer : MaterialBatchRenderer
{
    public override MaterialBatch[] BatchDrawCalls(...)
    {
        var batches = base.BatchDrawCalls(...);
        // Custom depth sorting logic
        return batches;
    }
}
```

---

## Comparison to Other Engines

| Feature | Ghost | Unity URP | Unreal 5 | Godot 4 |
|---------|-------|-----------|----------|---------|
| Keyword System | Global + Local | Global + Local | Static + Dynamic | Static |
| Multi-pass | Native | SubShader | Material Functions | Multi-pass |
| Per-pass Override | ✓ | Limited | ✓ | ✓ |
| Variant Caching | Auto | Auto | Auto | Auto |
| Batch Optimization | PSO-based | SRP Batcher | Nanite/VSM | Clustered |
| Unsafe/Native | ✓ | ✓ (Jobs) | ✓ (C++) | Limited |

---

## Future Enhancements

1. **GPU-Driven Rendering**: Indirect draws, culling on GPU
2. **Material Graphs**: Node-based shader authoring
3. **Hot Reload**: Runtime shader recompilation
4. **Texture Support**: Bindless textures, virtual texturing
5. **Compute Shaders**: Material property animation on GPU
6. **Serialization**: Material asset loading/saving

---

## License

This is a concept/demonstration project. Adapt as needed for your engine.
