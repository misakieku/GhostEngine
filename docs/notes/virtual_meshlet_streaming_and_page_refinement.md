# Virtual Meshlet Streaming, GPU Page Tables & Refinement-Gated Continuous LOD

This tech note breaks down the architectural design, mathematical mechanics, and practical implementation of **Virtual Meshlet Streaming** in GhostEngine.

It explains how to stream gigabytes (or billions of triangles) of continuous LOD geometry from disk into a fixed GPU memory pool with **zero LOD popping, zero geometric holes, and microsecond-level page translation**.

---

## 1. Motivation: Why Virtual Geometry Streaming?

### 1.1 The VRAM Wall
In classical rendering, models are loaded completely into GPU VRAM before rendering.
- Consider a modern asset: a single statue with 1,000,000 triangles takes ~40–50 MB of raw vertex and index data.
- If an open world has 1,000 unique high-fidelity assets, holding all LOD 0 meshes in GPU memory requires **40–50 GB of VRAM**—far exceeding consumer GPU limits.
- In reality, 95% of those triangles are either far away (sub-pixel), occluded, or behind the camera. Holding fine vertices in VRAM when a distant object only spans 10 pixels is pure waste.

### 1.2 Virtual Textures vs. Virtual Geometry
Game engines solved the exact same problem for 2D textures using **Virtual Texturing (Sparse Virtual Textures)**:
- Break huge 16K textures into small, uniform $128 \times 128$ pixel **Tiles / Pages**.
- Keep a small fixed GPU **Physical Tile Pool** (e.g. 1 GB).
- Use an indirection **Page Table** to map `VirtualPageID` $\to$ `PhysicalTileSlot`.
- When the GPU rasterizer discovers a missing tile, it queues a streaming request; until the high-res tile loads, the GPU samples a coarser resident mip level.

GhostEngine applies this exact philosophy to 3D geometry:

```
[ Virtual Meshlet Asset on Disk ] (All LOD 0..N Meshlets grouped into 128KB Pages)
                  │
                  ▼ Asynchronous Disk Streaming
[ GPU Page Pool Buffer ] (Fixed size: e.g. 512 MB to 1 GB of 128KB physical slots)
                  ▲
                  │ Indirection Address Translation
[ GPU Page Table Buffer ] (maps globalPageID -> physicalSlot)
```

---

## 2. Page Architecture & Baking Pipeline

### 2.1 Why Stream Pages Instead of Individual Meshlets?
A single meshlet (64 vertices, 124 triangles) is only about 2 to 3 KB.
- **Disk I/O**: Operating systems and NVMe SSDs are notoriously inefficient at issuing thousands of 2 KB read operations.
- **Page Table Overhead**: 10 million meshlets would require tens of megabytes just for page table tracking.
- **Memory Fragmentation**: Allocating thousands of 2 KB slots in GPU buffers creates catastrophic memory fragmentation.

GhostEngine bundles meshlets into **128 KB Pages** (`MESHLET_PAGE_SIZE = 131,072 bytes`):
- A single 128 KB page holds roughly **30 to 45 complete meshlets** along with their packed vertices and packed triangle indices.
- Disk reads are issued as contiguous 128 KB blocks, perfectly saturating NVMe and DirectStorage transfer queues.

### 2.2 Root Pages vs. Streamable Pages
In [`PagePacker.cs`](file:///f:/csharp/GhostEngine/src/Editor/Ghost.AssetForge.Core/Bakers/PagePacker.cs), the asset baker divides each model's meshlet DAG into two categories:

```
                                [ Meshlet DAG Hierarchy ]
                                            │
               ┌────────────────────────────┴────────────────────────────┐
               ▼                                                         ▼
    [ Root Pages (Coarsest LODs) ]                        [ Streamable Pages (Finer LODs) ]
  • Stored in raw buffer / base mesh                    • Stored in .gasset streamable archive
  • Loaded into GPU memory at startup                   • Streamed on demand from disk
  • Guarantees every mesh can always render             • Dynamic residency via GPU Page Table
```

1. **Root Pages**:
   - Contain the coarsest representations (LOD $N$ down to the root of the DAG).
   - Always resident in GPU memory from the moment the entity is spawned.
   - **Guarantees that a mesh will NEVER disappear or leave an empty hole**, even if disk streaming is slow or saturated.
2. **Streamable Pages**:
   - Contain the finer LOD levels (LOD 0, LOD 1, etc.).
   - Packed into a separate streamable file payload (`.gasset`).
   - Loaded and evicted dynamically by [`MeshletStreamingManager.cs`](file:///f:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Streaming/MeshletStreamingManager.cs).

---

## 3. GPU Page Table & Memory Indirection

### 3.1 Two-Level Hardware Address Translation
To render a meshlet, the GPU shader needs to know the exact byte offset of the vertex and triangle index buffers.

When virtual page streaming is enabled, GhostEngine binds two global buffers:
1. `g_FrameData.pagePoolBuffer` (`ByteAddressBuffer`): A preallocated GPU buffer containing $N$ physical 128 KB slots (e.g. 8,192 slots = 1 GB).
2. `g_FrameData.pageTableBuffer` (`ByteAddressBuffer`): A 32-bit uint array indexed by `globalPageID`.

The address translation logic in [`Mesh.hlsl`](file:///f:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/Mesh.hlsl#L102-L122) is:

```hlsl
void GetMeshletPageBufferAndBase(
    in MeshData meshData,
    uint pageIndex,
    out BYTE_ADDRESS_BUFFER pageBuffer,
    out uint pageBase)
{
    if (IS_VALID_BUFFER(g_FrameData.pagePoolBuffer) && IS_VALID_BUFFER(g_FrameData.pageTableBuffer))
    {
        ByteAddressBuffer pageTable = GET_BUFFER(g_FrameData.pageTableBuffer);
        uint globalPageID = meshData.globalPageTableOffset + pageIndex;
        uint physicalSlot = pageTable.Load(globalPageID * 4u);

        if (physicalSlot != 0xFFFFFFFFu)
        {
            // Page is resident in the streaming pool!
            pageBuffer = g_FrameData.pagePoolBuffer;
            pageBase = physicalSlot * MESHLET_PAGE_SIZE;
            return;
        }
    }

    // Fallback: Non-streamed asset or baked base buffer
    pageBuffer = meshData.rawBuffer;
    pageBase = meshData.meshletVerticesBufferOffset;
}
```

```
[ MeshData.globalPageTableOffset + pageIndex ]
                     │
                     ▼
       ┌───────────────────────────┐
       │   GPU Page Table Buffer   │
       └───────────────────────────┘
                     │
                     ├── physicalSlot == 0xFFFFFFFFu ──> [ Non-Resident (Streaming in flight) ]
                     │
                     ▼ physicalSlot != 0xFFFFFFFFu
       ┌───────────────────────────┐
       │   GPU Page Pool Buffer    │
       │  Slot 0: [ 128 KB Page ]  │
       │  Slot 1: [ 128 KB Page ]  │
       │  Slot K: [ 128 KB Page ] <──── Base Address = physicalSlot * 131,072
       └───────────────────────────┘
```

---

## 4. The Critical Pitfall: Why Naive Streaming Breaks

During early implementation, a naive approach was used:
When the camera approached an object, the continuous LOD cut algorithm selected fine LOD 0 clusters. If a cluster's page was not yet resident in `GPUPageTable`, the shader tried to fall back to older clusters, or dropped the cluster entirely while queuing a streaming request.

This caused two catastrophic bugs:
1. **Geometric "Explosion" / Tearing**: Falling back to arbitrary coarse meshlets while neighboring meshlets rendered at fine detail violated the DAG boundary invariants, cracking seams wide open across the model.
2. **Low-LOD Popping**: Coarse LOD representations would abruptly pop into view over an already high-detail surface, or vice versa, completely ruining visual continuity.

### The Root Cause: Breaking the Dual-Error Cut Condition
In Nanite-style continuous LOD, a cluster is rendered **if and only if** its parent's error is too large, but its own cluster error is small enough:

$$\text{ParentError} > \text{Threshold} \quad \text{AND} \quad \text{ClusterError} \le \text{Threshold}$$

If you discard a coarse cluster because its $\text{ClusterError} > \text{Threshold}$ **before** the finer child page has finished streaming into VRAM, you have nothing to render! The cluster is thrown away, but its replacement does not exist yet $\to$ **a gaping hole appears in the mesh**.

---

## 5. The Solution: Refinement-Gated Continuous Streaming

To eliminate popping and holes completely, GhostEngine enforces **Refinement Gating**:

> [!IMPORTANT]
> **The Refinement Invariant**:
> A coarse, resident cluster is **NEVER allowed to refine (be discarded in favor of finer geometry)** unless the finer child page is **already 100% resident in GPU memory**.

### 5.1 Step 1: Pre-Baking the Contiguous Child Page Span (`childPageRange`)
In standard METIS / edge-collapse LOD DAGs, multiple coarse groups merge from multiple finer child groups ($M:N$ relationship). If child clusters cross 128 KB page boundaries (e.g. child group A is in Page 10, child group B in Page 11), checking only a single page index would allow refinement when only half the child geometry is resident, producing instant mesh cracks along page borders.

In [`PagePacker.cs`](file:///f:/csharp/GhostEngine/src/Editor/Ghost.AssetForge.Core/Bakers/PagePacker.cs), groups are packed level-by-level, ensuring all child groups of a coarse group reside in a narrow, contiguous page range:
- For LOD 0 meshlet groups: `childPageIndex = 0xFFFF`, `childPageCount = 0` (finest detail possible).
- For LOD $> 0$ meshlet groups: `childPageIndex` and `childPageCount` encode the full child span:
  `childPageRange = (uint)childPageIndex | ((uint)childPageCount << 16)`.

```csharp
// Pack 64-byte MeshletGroup struct (childPageRange fits into 4 bytes)
writer.Write(group.MeshletStartIndex);
writer.Write(group.MeshletCount);
writer.Write(group.PageIndex);
writer.Write(group.PackedChildPageRange); // low 16 bits: start page, high 16 bits: count
```

### 5.2 Step 2: GPU Hierarchy Traversal & Bitmask Deduplicated Requests
In [`MeshletCullGraph.ggraph`](file:///f:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshPipeline/MeshletCullGraph.ggraph#L196-L245), `HierarchyTraverseNode` walks down the DAG until it hits a leaf group:
1. It checks if the leaf group's own page is resident in `pageTableBuffer`.
2. **Bitmask Deduplication:** If 100 instances of the same rock or tree move into view simultaneously, thousands of threads would detect missing pages in the same frame. Instead of flooding `streamingRequestsUav` with duplicate page IDs and overflowing the request budget, the GPU tests an 8 KB bitmask buffer (`RequestBitfield`):
   ```hlsl
   uint wordIndex = globalPageID / 32u;
   uint bitMask = 1u << (globalPageID % 32u);
   uint prevMask;
   bitmaskBuf.InterlockedOr(wordIndex * 4u, bitMask, prevMask);
   if ((prevMask & bitMask) == 0u) // Only the FIRST thread to set this bit emits the request
   {
       reqBuf.InterlockedAdd(0u, 1u, reqSlot);
       if (reqSlot < props.maxStreamingRequests)
       {
           reqBuf.Store((1u + reqSlot) * 4u, globalPageID);
       }
   }
   ```
3. If the page is **resident**, it forwards the record to `MeshletCullNode` with `childGlobalPageID` and `childPageCount`.

### 5.3 Step 3: Multi-Page Refinement Gating in `MeshletCullNode`
In [`MeshletCullNode`](file:///f:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshPipeline/MeshletCullGraph.ggraph#L317-L343), the shader verifies that **all** child pages spanning the refined cluster are resident before allowing refinement:

```hlsl
// Dual-Error Cut Condition 1: Parent Error check (Lower bound)
bool isParentDetailSufficient = (meshlet.parentError < 3.4e38f) && EvaluateLODDetailSufficient(...);

// Dual-Error Cut Condition 2: Cluster Error test (Upper bound)
// Multi-page refinement gate: ALL child pages in the span must be resident!
bool canRefine = false;
if (groupRec.childGlobalPageID != 0xFFFFFFFFu && IS_VALID_BUFFER(g_FrameData.pageTableBuffer))
{
    ByteAddressBuffer pageTable = GET_BUFFER(g_FrameData.pageTableBuffer);
    canRefine = true;
    for (uint cp = 0; cp < groupRec.childPageCount; ++cp)
    {
        if (pageTable.Load((groupRec.childGlobalPageID + cp) * 4u) == 0xFFFFFFFFu)
        {
            canRefine = false;
            break;
        }
    }
}

// IF refinement is impossible (any child page is still streaming from disk):
// The cluster CANNOT be discarded for being too coarse!
// It remains visible, seamlessly preventing holes and low-LOD pop-in.
bool isClusterDetailSufficient = !canRefine || (meshlet.clusterError <= 0.0f) || EvaluateLODDetailSufficient(...);

if (isParentDetailSufficient || !isClusterDetailSufficient)
{
    isValid = false; // Discard meshlet
}
```

### 5.4 Lifecycle of an Asset Transition

```
Frame N (Camera moves closer):
  1. HierarchyTraverseNode detects finer page P_fine is needed.
  2. P_fine is not in GPU Page Table (physicalSlot == 0xFFFFFFFF).
  3. HierarchyTraverseNode writes request for P_fine into streamingRequestsUav.
  4. MeshletCullNode checks canRefine for current resident page P_coarse:
     canRefine == false because P_fine is not yet resident!
  5. P_coarse remains visible and renders smoothly. Zero popping, zero holes.

Between Frames (Asynchronous CPU/Disk):
  1. MeshletStreamingManager reads streamingRequestsUav via readback buffer.
  2. Allocates a free 128KB slot in pagePoolBuffer (or evicts oldest LRU page).
  3. Asynchronously reads P_fine from .gasset on disk and copies to GPU slot K.
  4. Updates GPU pageTableBuffer[P_fine] = K.

Frame N + 2 (Page Loaded):
  1. MeshletCullNode evaluates canRefine for P_coarse:
     pageTable.Load(P_fine) == K != 0xFFFFFFFF -> canRefine == true!
  2. P_coarse is now allowed to refine. It yields to P_fine.
  3. Visual transition occurs seamlessly without a single missing triangle!
```

---

## 6. GPU Performance & Memory Optimization

Profiling the virtual streaming pipeline with **NVIDIA Nsight Graphics** highlighted two major bottlenecks:

### 6.1 Eliminating 254 Redundant Page Table Lookups per Meshlet
In [`Visibility.template.hlsl`](file:///f:/csharp/GhostEngine/src/Editor/Ghost.DSL/Templates/Common/Visibility.template.hlsl#L61-L155):
- A mesh shader threadgroup (`MSMain`) has 64 threads processing 1 meshlet (up to 64 vertices and 124 triangles).
- Originally, every thread independently called `LoadMeshletPositionOnly`, `LoadMeshletUVOnly`, and `LoadPackedIndices`.
- Each function called `GetMeshletPageBufferAndBase`, resulting in **~254 global memory loads from `g_FrameData.pageTableBuffer` per meshlet**!
- This choked the SM texture pipe (`TEX Throttle: 8.09%`) and inflated warp register pressure, crushing occupancy to 5.01%.

**The Fix:**
Cache the resolved page address in a 108-byte `groupshared` struct populated by `groupThreadID == 0`:
```hlsl
struct SharedMeshletState
{
    float4x4 worldViewProj;
    uint pageBufferIndex;
    uint pageBase;
    uint vertexOffset;
    uint triangleOffset;
    uint vertexCount;
    uint triangleCount;
    uint visibleMeshletIndex;
    uint localMaterialIndex;
    uint materialBufferIndex;
    uint variantIndex;
    float doubleSided;
};
groupshared SharedMeshletState s_Meshlet;
```
- Page table lookups dropped from **254 $\to$ 1** (99.6% reduction).
- 4x4 matrix multiplies dropped from **64 $\to$ 1**.
- Threads 1..63 read positions and indices directly from `pageBuffer.Load3` without touching the page table.

### 6.2 Wave-Uniform Register Broadcasting
In [`MeshletCullNode`](file:///f:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshPipeline/MeshletCullGraph.ggraph#L274-L320):
- 32 lanes in each wave process the same `groupRec`.
- Lane 0 loads `InstanceData` and queries `pageTableBuffer` for `childGlobalPageID`, then broadcasts results across the wave using `WaveReadLaneFirst`:
```hlsl
localToRWS_row0 = WaveReadLaneFirst(localToRWS_row0);
localToRWS_row1 = WaveReadLaneFirst(localToRWS_row1);
localToRWS_row2 = WaveReadLaneFirst(localToRWS_row2);
localToRWS_row3 = WaveReadLaneFirst(localToRWS_row3);
maxScale = WaveReadLaneFirst(maxScale);
canRefine = WaveReadLaneFirst(canRefine);
```
- On modern NVIDIA architectures (Turing/Ampere/Ada), values broadcasted via `WaveReadLaneFirst` are placed in **Uniform Registers (URs)** instead of consuming Vector Registers (VGPRs).
- This cut memory requests to `sceneBuffer` and `pageTableBuffer` by **32x**, freeing up 30+ VGPRs per thread and directly eliminating `CS Warp Launch Stalled Register Allocation`.

### 6.3 GPU Memory Hazard Protection & Page Pool Fencing
In [`MeshletStreamingManager.cs`](file:///f:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Streaming/MeshletStreamingManager.cs#L255-L345):
- Streaming page uploads and initial root page uploads write directly to `_pagePool.Buffer` via DMA copy engines (`UploadBufferRange`).
- If vertex/meshlet data is copied without state transitions or pipeline barriers, subsequent mesh shader invocations in the visibility buffer pass could read uncommitted PCIe memory or stale L2 cache lines.
- The manager maintains state tracking `_pagePoolInShaderResource`:
  1. Before any uploads occur in the frame, if the buffer was in `ShaderResource`, it is transitioned:
     `ShaderResource -> CopyDest` with `BarrierSync.AllShading -> BarrierSync.Copy`.
  2. After root and streaming page copies complete, an execution barrier transitions it:
     `CopyDest -> ShaderResource` with `BarrierSync.Copy -> BarrierSync.AllShading`.
  3. This completely eliminates data races between DMA transfer engines and GPU mesh/compute shaders.

---

## 7. Summary

By combining:
1. **128 KB Grouped Meshlet Pages** (for saturating disk transfer rates),
2. **GPU Page Table Indirection** (for dynamic physical slot allocation and LRU eviction),
3. **Refinement-Gated Continuous LOD with Multi-Page DAG Boundary Fencing** (preventing coarse clusters from refining until all child pages in the DAG span are resident),
4. **GPU Bitmask Request Deduplication** (preventing duplicate streaming storms across thousands of visible instances),
5. **Group-Shared & Wave-Uniform Page Translation** (eliminating redundant address translation in shaders), and
6. **Explicit DMA Pipeline Barriers** (preventing read-before-write PCIe copy hazards),

GhostEngine achieves a completely robust, pop-free virtual geometry pipeline capable of navigating tens of billions of triangles within a fixed, deterministic GPU memory footprint.
