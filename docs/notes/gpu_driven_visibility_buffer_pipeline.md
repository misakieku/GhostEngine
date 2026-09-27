# GPU-Driven Visibility Buffer & Dual-Path Work Graph Pipeline

This document records the architectural design, and implementation details during the development of GhostEngine's modern GPU-driven rendering pipeline.

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
