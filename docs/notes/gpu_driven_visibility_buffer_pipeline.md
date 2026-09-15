# GPU-Driven Visibility Buffer & Dual-Path Work Graph Pipeline

This document records the architectural design, implementation details, and critical debugging lessons learned during the development of GhostEngine's modern GPU-driven rendering pipeline (Phases 1 through 3.2).

---

## 1. Architectural Overview & Motivation

### 1.1 The Classical Pipeline Bottleneck
Traditional forward/deferred rendering pipelines suffer from:
1. **CPU Overhead**: Thousands of individual draw calls (`DrawIndexedInstanced`), state changes, and descriptor table updates per frame.
2. **GPU Overdraw & Quad Over-Shading**: Small triangles (sub-pixel geometry) cause massive quad over-shading in pixel shaders (2x2 pixel helper lanes calculated and thrown away).
3. **Bandwidth Saturation**: Classical G-Buffers require 4–6 wide render targets (Albedo, Normal, Roughness/Metallic, Motion Vectors, Depth), totaling 32–64 bytes per pixel read/written multiple times.

### 1.2 Modern GPU-Driven Pipeline Design
GhostEngine adopts a **Nanite / Frostbite-style GPU-Driven Visibility Buffer** architecture:

```
[GPU Scene Data] (Instances, Meshlets, Bounds, Materials)
       │
       ▼
[Pass 1: Early-Z Work Graph] (Instance Cull -> Hierarchy Node Cull -> Meshlet Cull)
       ├──> Wave Compaction: Opaque Meshlets ──> visibleMeshletsPass1
       └──> Wave Compaction: Masked Meshlets ──> visibleMaskedMeshletsPass1
       │
       ▼
[Prepare Indirect Args Pass 1] (Generates D3D12_DISPATCH_MESH_ARGUMENTS for Opaque & Masked)
       │
       ▼
[Visibility Buffer Pass 1]
       ├── Draw 1 (ExecuteIndirect): Opaque Meshlets (VisibilityBuffer.gshdr - Conservative Early-Z)
       └── Draw 2 (ExecuteIndirect): Masked Meshlets (VisibilityBufferMasked.gshdr - clip() test)
       │
       ▼ Output: VisibilityBuffer (R32G32_UINT) + SceneDepth (D32_FLOAT)
       │
       ▼
[Build HZB Mip Pyramid] (Compute passes downsampling SceneDepth using 2x2 min-reduction)
       │
       ▼
[Pass 2: Late-Z Work Graph] (Tests occluded meshlets against current-frame HZB)
       ├──> Wave Compaction: Opaque Meshlets ──> visibleMeshletsPass2
       └──> Wave Compaction: Masked Meshlets ──> visibleMaskedMeshletsPass2
       │
       ▼
[Prepare Indirect Args Pass 2] (Generates D3D12_DISPATCH_MESH_ARGUMENTS for Opaque & Masked)
       │
       ▼
[Visibility Buffer Pass 2] (Rasterizes newly visible geometry into existing VBuffer + SceneDepth)
       ├── Draw 1: Late Opaque Meshlets
       └── Draw 2: Late Masked Meshlets
       │
       ▼
[Downstream Passes (Phases 3.3+)] (Tile Classification -> Compute Deferred Texturing -> Lighting)
```

---

## 2. Key Technical Implementations

### 2.1 Meshlet Representation & Data Structures
Each meshlet is bounded to a maximum of 64 vertices and 124 triangles:
```hlsl
struct Meshlet
{
    float4 boundingSphere;
    float4 parentBoundingSphere;
    float3 boundingBoxMin;
    float3 boundingBoxMax;
    uint vertexOffset;
    uint triangleOffset;
    uint groupIndex;
    float clusterError;
    float parentError;
    uint packedCounts; // byte 0: vertexCount, byte 1: triangleCount, byte 2: localMaterialIndex, byte 3: lodLevel
};
```

### 2.2 Material Classification via Packed Descriptor Index
In [`ResourceManager.cs`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Graphics/Services/ResourceManager.cs), the upper 8 bits of the 32-bit material descriptor index store the surface render type:
```csharp
// Bits 0..23: Bindless CBuffer descriptor index
// Bits 24..31: MaterialRenderType (0 = Opaque, 1 = Masked / Alpha-Clip, 2 = Transparent)
uint packedEntry = (cbufferIndex & 0x00FFFFFFu) | ((renderType & 0xFFu) << 24);
```

### 2.3 Work Graph Wave Compaction
Instead of serializing atomics or running monolithic CPU/compute binning, [`MeshletCullGraph.ggraph`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshletCullGraph.ggraph) uses HLSL Wave Intrinsics to compact visible meshlets within each 32-lane wave:

```hlsl
bool isOpaque = isVisible && (renderType != 1);
bool isMasked = isVisible && (renderType == 1);

uint waveOpaqueCount  = WaveActiveCountBits(isOpaque);
uint localOpaqueOffset = WavePrefixCountBits(isOpaque);

uint waveMaskedCount  = WaveActiveCountBits(isMasked);
uint localMaskedOffset = WavePrefixCountBits(isMasked);

uint waveOpaqueBase = 0;
if (WaveIsFirstLane() && waveOpaqueCount > 0)
{
    counterBuf.InterlockedAdd(opaqueCounterOffset, waveOpaqueCount, waveOpaqueBase);
}
waveOpaqueBase = WaveReadLaneFirst(waveOpaqueBase);

uint waveMaskedBase = 0;
if (WaveIsFirstLane() && waveMaskedCount > 0)
{
    counterBuf.InterlockedAdd(maskedCounterOffset, waveMaskedCount, waveMaskedBase);
}
waveMaskedBase = WaveReadLaneFirst(waveMaskedBase);

if (isOpaque)
{
    uint slot = waveOpaqueBase + localOpaqueOffset;
    visibleBuffer[slot] = entry;
}
else if (isMasked)
{
    uint slot = waveMaskedBase + localMaskedOffset;
    maskedBuffer[slot] = entry;
}
```
**Benefits**:
- Atomic contention on memory is cut down by 32x.
- Memory coalescing is maximized because consecutive threads write to consecutive buffer slots.

### 2.4 Dual Indirect Arguments Buffer Layout
The indirect argument buffer holds 4 dispatches (`D3D12_DISPATCH_MESH_ARGUMENTS` = 12 bytes each, aligned to 16 bytes):
- **Offset 0** (0 bytes): Pass 1 Opaque
- **Offset 16** (16 bytes): Pass 1 Masked
- **Offset 32** (32 bytes): Pass 2 Opaque
- **Offset 48** (48 bytes): Pass 2 Masked

[`PrepareMeshletIndirectArgs.gcomp`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/PrepareMeshletIndirectArgs.gcomp) runs with `[numthreads(2, 1, 1)]`, preparing both Opaque and Masked draw parameters in a single compute threadgroup dispatch.

### 2.5 Visibility Buffer Payload Packing
Visibility Buffer render target format is `R32G32_UINT`:
- **Target 0 (`.x`)**: `[InstanceIndex (24 bits)] | [LocalMaterialIndex (8 bits) << 24]`
- **Target 1 (`.y`)**: `[MeshletIndex (24 bits)]  | [PrimitiveID (8 bits) << 24]`
- **Depth Target**: Standard 32-bit hardware floating point depth (`D32_FLOAT`).

### 2.6 Batched 4-Mip Subgroup Tree Reduction for HZB Generation
Rather than dispatching 10+ individual compute passes with global pipeline barriers between every single mip level, [`BuildHZB.gcomp`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/BuildHZB.gcomp) generates **up to 4 mip levels in a single dispatch** using a Morton Z-curve mapping and threadgroup shared memory:
- **LDS Footprint**: Only `groupshared float s_minDepth[32];` (128 bytes total), ensuring zero occupancy penalty on GPU compute units.
- **Morton Z-Curve Swizzle (`CoordInTileByIndex`)**: Maps 64 linear threads into an $8 \times 8$ pixel footprint where every 4 threads form a $2 \times 2$ pixel quad.
- **Hierarchical Reduction**:
  1. All 64 threads sample source depth and write Mip 0 ($8 \times 8$).
  2. Binary reduction (`SubgroupMergeDepths`) merges 4 values into 1; 16 threads write Mip 1 ($4 \times 4$).
  3. Binary reduction merges 16 values into 1; 4 threads write Mip 2 ($2 \times 2$).
  4. Final reduction merges 64 values into thread 0; 1 thread writes Mip 3 ($1 \times 1$).
- **Dynamic Mip Count (`minDstCount`)**: The CPU dynamically loops `Math.Min(remainingMips, 4u)`, gracefully handling arbitrary mip counts (e.g. 10 or 11 mips across just 3 batches) with zero deadlocks and zero unused writes.
- **Performance Impact**: Collapses ~20 compute passes per frame down to ~6 compute passes per frame, eliminating over 70% of GPU pipeline sync barriers.

---

## 3. Important Bugs Encountered & Solutions

### Bug 1: Intermediate Build Cache Divergence (Dotnet CLI vs Visual Studio)
- **Root Cause**:
  Building via `dotnet build GhostEngine.slnx -c Debug -p:Platform=x64` generated baked assets and shader metadata into `TestGame/obj/x64/Debug/net10.0/AssetCache`. However, launching from Visual Studio defaulted to the `AnyCPU` configuration, looking in `TestGame/obj/Debug/net10.0/AssetCache`.
  This resulted in Visual Studio running an outdated shader pack (`pack_0000.pack`) containing old bytecode while the source files had already been modified.
- **Solution & Takeaway**:
  Always ensure both `Platform=x64` and default intermediate paths are synchronized, or configure `Directory.Build.props` to standardize output paths. When testing runtime shader updates, verify the timestamp of `pack_0000.pack` in `bin/` and `obj/`.

---

### Bug 2: GPU Warp Serialization from Naive Branching Appends
- **Problem**:
  When separating visible meshlets into different surface queues (Opaque, Masked, Transparent), naive implementations use `if (isOpaque) AppendOpaque(); else if (isMasked) AppendMasked();` where each thread calls `InterlockedAdd`.
  This creates two major bottlenecks:
  1. **Atomic Contention**: Up to 32 threads in the same warp hammer the same atomic counter address.
  2. **Warp Divergence**: Divergent memory writes across the warp degrade memory bus utilization.
- **Solution**:
  Wave compaction using `WaveActiveCountBits` and `WavePrefixCountBits`. Only the first active lane in the wave performs a single `InterlockedAdd` on behalf of the entire warp, and the base index is broadcast using `WaveReadLaneFirst`.

---

### Bug 3: Hardcoded Template Override Defines
- **Problem**:
  In [`TemplateStitcher.cs`](file:///F:/csharp/GhostEngine/src/Editor/Ghost.DSL/ShaderCompiler/Templates/TemplateStitcher.cs), the preprocessor injection points (`GHOST_OVERRIDE_GET_ALPHA_COVERAGE`, `GHOST_OVERRIDE_GET_COLOR`, etc.) were hardcoded in a `switch` statement for specific template names. Adding new template override functions or new templates required editing compiler internals.
- **Solution**:
  Introduced `TemplateOverridePoint` on [`IShaderTemplate`](file:///F:/csharp/GhostEngine/src/Editor/Ghost.DSL/ShaderCompiler/Templates/IShaderTemplate.cs). Each template now declaratively returns its supported override points (`OverridePoints`). The compiler dynamically loops through `template.OverridePoints`, inspecting user code with regex and defining the corresponding override macros cleanly.

---

### Bug 4: Decoupling Producer vs Consumer in Visibility Buffer Pipelines
- **Question**:
  Does having a distinct Masked (Alpha-Clip) rasterization path risk breaking the rest of the render pipeline or require changes across the entire render graph if bugs occur?
- **Key Insight & Architectural Guarantee**:
  - The Visibility Buffer passes are purely **geometric producers**. Their output is strictly a 2D image of identifiers (`uint2`) and a depth buffer (`float`).
  - Downstream passes (HZB generation, tile classification, deferred compute texturing, clustered lighting) consume the 2D texture directly. They do not know or care whether a pixel came from an opaque or masked meshlet.
  - If alpha cutoff, mip bias, or dithered transparency needs adjustment later, the fix is **100% isolated** to [`VisibilityBufferMasked.gshdr`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/VisibilityBufferMasked.gshdr) and the classification bit check in [`MeshletCullGraph.ggraph`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshletCullGraph.ggraph). Downstream logic remains completely untouched.

---

### Bug 5: Zero-Cost Hardware Execution via `ExecuteIndirect`
- **Technique**:
  When a scene contains zero masked meshlets (`maskedCount == 0`), `PrepareMeshletIndirectArgs.gcomp` writes `ThreadGroupCountX = 0` into the indirect argument buffer.
  Under DirectX 12, when `ExecuteIndirect` is called with `ThreadGroupCountX == 0`, the GPU hardware/driver discards the work immediately at the front-end before scheduling any threadgroups. There is virtually zero runtime overhead for executing the second indirect draw when no masked geometry is present.

---

### Bug 6: Multi-Pass Geometry Pass Corrupting Screen via `AccessFlags.WriteAll` (D3D12 Discard)
- **Problem**:
  In multi-pass raster pipelines (such as Pass 1 Early-Z and Pass 2 Late-Z writing to the same render target), setting the color attachment with `builder.SetColorAttachment(colorTarget, 0, AccessFlags.WriteAll)` in Pass 2 caused severe screen tearing and uninitialized memory corruption ("花屏") when launched directly outside PIX, while running inside PIX appeared deceptively normal.
- **Root Cause**:
  `AccessFlags.WriteAll` is defined as `AccessFlags.Write | AccessFlags.Discard`. In `RenderGraphNativePassBuilder.InferLoadStoreOps`, specifying `Discard` infers `AttachmentLoadOp.DontCare`, which translates directly in D3D12 to `D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD`.
  When Pass 2 executed, the GPU driver treated existing pixels written in Pass 1 as disposable/discarded, erasing Pass 1's geometry.
  PIX masked the problem because PIX's instrumentation layer, HUD overlay, and capture engine initialize or override discard states to preserve pass history for debugger replay.
- **Solution & Takeaway**:
  Never use `AccessFlags.WriteAll` on multi-pass geometry render targets. Use `AccessFlags.Write` (default), ensuring Pass 1 performs the initial clear, and Pass 2 executes with `AttachmentLoadOp.Load` to preserve previously rasterized contents. Reserve `AccessFlags.WriteAll` strictly for full-screen passes (like Blit) that unconditionally overwrite every pixel on screen.

---

## 4. Source File Reference Map

| Component | File Path | Responsibility |
| :--- | :--- | :--- |
| **Culling Work Graph** | [`MeshletCullGraph.ggraph`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshletCullGraph.ggraph) | Hierarchical occlusion culling & wave-compacted dual routing |
| **Indirect Args Compute** | [`PrepareMeshletIndirectArgs.gcomp`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/PrepareMeshletIndirectArgs.gcomp) | Formats `D3D12_DISPATCH_MESH_ARGUMENTS` for both queues |
| **Opaque V-Buffer Shader** | [`VisibilityBuffer.gshdr`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/VisibilityBuffer.gshdr) | Ultra-fast meshlet rasterizer with conservative Early-Z |
| **Masked V-Buffer Shader** | [`VisibilityBufferMasked.gshdr`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/VisibilityBufferMasked.gshdr) | Meshlet rasterizer with texture sampling and `clip()` |
| **Culling Setup & Passes** | [`GhostRenderPipeline.Culling.cs`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/RenderPipeline/GhostRenderPipeline.Culling.cs) | Allocates culling buffers, binds UAVs/SRVs, dispatches Work Graphs |
| **Render Graph Pipeline** | [`GhostRenderPipeline.cs`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/RenderPipeline/GhostRenderPipeline.cs) | Coordinates 2-phase culling, HZB mip generation, and dual VBuffer draws |
| **Template Extensibility** | [`IShaderTemplate.cs`](file:///F:/csharp/GhostEngine/src/Editor/Ghost.DSL/ShaderCompiler/Templates/IShaderTemplate.cs) | Declarative metadata for shader template injection points |
