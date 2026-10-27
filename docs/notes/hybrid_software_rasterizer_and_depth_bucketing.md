# Hybrid Software Rasterizer & Zero-Overhead Depth Bucketing Pipeline

This document details the architectural design, mathematical foundations, low-level shader implementation, and debugging case studies for GhostEngine's dedicated Compute Shader Software Rasterizer and Zero-Overhead Depth Bucketing pipeline.

---

## 1. Architectural Overview & Motivation

### 1.1 The Micro-Triangle Problem on Modern GPUs
As scene complexity scales toward cinematic fidelity (e.g., 100,000 instances $\times$ 274,000 triangles $\approx$ **27.4 billion triangles**), geometric clusters project into sub-pixel or micro-pixel scales ($< 8$ to $16$ pixels across). 

On fixed-function hardware rasterizers (DirectX 12 Mesh Shaders / Rasterizer Stages):
1. **Catastrophic Quad Over-Shading**: Hardware rasterization operates in $2 \times 2$ pixel quads for screen-space derivative evaluation (`ddx`/`ddy`). When triangles are smaller than a single pixel or span 1–4 pixels, up to **75% to 90%** of hardware pixel shader lane executions are wasted running on inactive helper pixels outside triangle boundaries.
2. **Fixed-Function Bottlenecks**: Hardware primitive assemblers and rasterizers have strict limits on primitive throughput per clock cycle. Dense micro-meshlets choke the hardware frontend.

### 1.2 Dedicated Compute Architecture vs. Work Graph Co-Location
In an earlier experiment, software rasterization nodes were placed directly inside the Work Graph (`MeshletCullGraph`). This caused a **~1.0 ms regression**:
- **VGPR Bloat & Occupancy Starvation**: The hierarchical culling and DAG traversal logic require substantial scalar/vector register state. Adding vertex transformation, subpixel fixed-point setup, and triangle traversal into the same shader bumped register usage significantly, crashing wavefront occupancy per SM.
- **Divergence**: Threads in the Work Graph wave would diverge between traversing hierarchy children and rasterizing triangles.

**The Solution**: A **hybrid decoupled pipeline**:
- **Work Graph (`MeshletCullGraph.ggraph`)**: Pure, high-occupancy culling, LOD evaluation, and classification.
- **HW Mesh Shader Pass**: Dispatched for coarse/large triangles ($\ge 32\text{ px}$) and alpha-tested/masked materials where hardware texture derivatives are required.
- **Dedicated SW Rasterizer (`RasterizeSw.gcomp`)**: Dispatched as an independent indirect compute shader pass with 64 threads per wavefront, 100% focused on rasterizing opaque micro-meshlets.

---

## 2. Hybrid Classification & Mathematical Foundations

### 2.1 Projected Triangle Edge Length Estimation
In Unreal Engine 5's Nanite (`NaniteClusterCulling.usf`), clusters are dispatched to hardware or software rasterization based on whether the projected triangle edge length is $\ge 32\text{ px}$ (hardware) or $< 32\text{ px}$ (software):

$$\text{CVarNaniteMinPixelsPerEdgeHW} = 32.0\text{f}$$

Because GhostEngine's `Meshlet` structure stores cluster-level bounding spheres and triangle counts rather than precomputed per-meshlet max edge lengths, we derive the average projected edge length analytically:

Given:
- Meshlet bounding sphere world radius $R_{\text{world}}$ and camera-relative distance $D$.
- Triangle count $T = (\text{meshlet.packedCounts} \gg 8) \& 0\text{xFF}$.
- Screen height $H$ and vertical projection scale $P_{11}$.

The projected screen-space cluster radius in pixels is:
$$R_{\text{px}} = \frac{R_{\text{world}} \cdot P_{11} \cdot 0.5 \cdot H}{\max(D - R_{\text{world}}, 0.001)}$$

For a triangulated manifold cluster of diameter $2 R_{\text{px}}$ containing $T$ triangles, approximately $\sqrt{T}$ triangles span across the cluster diameter. Thus, the average projected triangle edge length in screen pixels is:

$$E_{\text{px}} \approx \frac{2 R_{\text{px}}}{\sqrt{T}} = R_{\text{px}} \cdot \left(2.0 \cdot \text{rsqrt}(\max(T, 1))\right)$$

```hlsl
// Implemented in MeshletCullGraph.ggraph:
float dist = max(length(meshletWorldCenter), 0.001f);
float clusterRadiusPx = (meshletWorldRadius * g_ViewData.projectionMatrix[1][1] * g_ViewData.screenSize.y * 0.5f) / max(dist - meshletWorldRadius, 0.001f);
uint triangleCount = (meshlet.packedCounts >> 8u) & 0xFFu;
float projectedEdgePx = clusterRadiusPx * (2.0f * rsqrt(max((float)triangleCount, 1.0f)));
bool isSoftware = (projectedEdgePx < SW_MAX_EDGE_PIXELS);
```

### 2.2 Zero-Overhead Front-to-Back Depth Bucketing
Both hardware and software rasterizers output 64-bit visibility keys into `visBuffer` (`RWTexture2D<uint64_t>`) via `InterlockedMax`. When thousands of overlapping fragments compete for the same pixel, atomic memory contention degrades performance.

To maximize speculative Early-Z rejection before the atomic write, meshlets are binned into **32 logarithmic depth buckets**:

$$\text{Bucket}(Z) = \text{clamp}\left(\left\lfloor \frac{\log_2(Z) - \log_2(Z_{\min})}{\log_2(Z_{\max}) - \log_2(Z_{\min})} \cdot 32 \right\rfloor, 0, 31\right)$$

#### Buffer Bin Partitioning (1024 Variants Pre-allocated)
| Bin Range | Purpose | Execution Model |
| :--- | :--- | :--- |
| `0 .. 31` | **HW Opaque Meshlets** (Sorted Front-to-Back) | Unified single dispatch via Mesh Shader Bin 0 |
| `32 .. 63` | **SW Opaque Meshlets** (Sorted Front-to-Back) | Indirect dispatch via `RasterizeSw.gcomp` |
| `64 .. 1023` | **Alpha / Masked Variants** | Dispatched individually per material variant |

Because `CullCommon.DEFAULT_MAX_VARIANTS = 1024` was already allocated for the variant counters and indirect argument buffers, depth bucketing introduces **0 new buffers, 0 new passes, and 0 CPU sorting overhead**.

---

## 3. Shader Implementation Details

### 3.1 Software Rasterizer (`RasterizeSw.gcomp`)
The compute rasterizer launches with `[numthreads(64, 1, 1)]` (1 wave/warp on NVIDIA/AMD).

1. **Phase 1: Vertex Transform & LDS Projection**:
   - Up to 64 vertices per meshlet are transformed in parallel into camera-relative clip space:
     $$P_{\text{clip}} = M_{\text{worldViewProj}} \cdot \begin{pmatrix} P_{\text{local}} \\ 1 \end{pmatrix}$$
   - Projected into 8-subpixel fixed-point coordinates and cached in shared memory (`s_VertexPositions[64]`, consuming only 1024 bytes of LDS):
     $$\text{subpixel}_{xy} = \left\lfloor \frac{P_{\text{clip}.xy}}{P_{\text{clip}.w}} \cdot \text{viewportScale} + \text{viewportBias} \right\rfloor$$
   - `GroupMemoryBarrierWithGroupSync()` ensures all vertices are ready.

2. **Phase 2: Parallel Triangle Rasterization**:
   - 64 threads loop over `triangleCount` with stride 64.
   - **Near-Plane Rejection**: Triangles crossing behind the near plane ($\min(w_0, w_1, w_2) \le 0$) are discarded immediately.
   - **Setup & Backface Culling**: `SetupTriangle<8, true>` calculates edge equations and scissor bounds.
   - **Adaptive Rasterization**:
     - Narrow triangles ($\Delta X \le 4\text{ px}$): evaluated with `RasterizeTri_Rect`.
     - Wider triangles ($\Delta X > 4\text{ px}$): evaluated with `RasterizeTri_Scanline`.

### 3.2 Visibility Buffer Write & Speculative Early-Z
Both HW and SW rasterizers write identical 64-bit packed keys:
- **Bits 32..63**: Integer depth `asuint(depth)`. (In Reversed-Z, closer fragments have strictly greater values).
- **Bits 24..31**: Primitive ID (triangle index within the meshlet).
- **Bits 0..23**: Visible Meshlet Index (`binnedSlot`).

```hlsl
void WritePixel(uint2 pixelPos, float3 c3, RasterTriangle tri, TEXTURE2D visBufferIndex, uint visibleMeshletIndex, uint primitiveID)
{
    float depth = tri.depthPlane.x + tri.depthPlane.y * c3.y + tri.depthPlane.z * c3.z;
    RWTexture2D<uint64_t> visBuffer = GET_TEXTURE2D(visBufferIndex);
    
    // Non-atomic speculative Early-Z check
    uint64_t currentPacked = visBuffer[pixelPos];
    uint currentDepthInt = (uint)(currentPacked >> VBUFFER_DEPTH_SHIFT);
    uint depthInt = asuint(depth);

    if (depthInt <= currentDepthInt)
    {
        return; // Discard occluded fragment without atomic write
    }

    uint64_t newPacked = PackVisibility64(depth, visibleMeshletIndex, primitiveID);
    InterlockedMax(visBuffer[pixelPos], newPacked);
}
```

---

## 4. Key Case Studies & Debugging Log

### 4.1 Case Study 1: The Winding Order Inversion
**Symptom**: SW rasterization resulted in severe visual tearing where entire front shells of geometry were missing, exposing the concave interior cavity of meshes.

**Root Cause Analysis**:
1. GhostEngine targets DirectX 12 with `FrontCounterClockwise = false` (front-facing triangles have **Clockwise (CW)** winding in clip space).
2. The Hardware Mesh Shader (`Visibility.template.hlsl`) verifies front-facing geometry using:
   $$\det\begin{pmatrix} v_{0.xyw} \\ v_{1.xyw} \\ v_{2.xyw} \end{pmatrix} < 0.0\text{f}$$
3. Nanite's `SetupTriangle` was authored for **Counter-Clockwise (CCW)** screen-space winding:
   $$\text{detXY} = \text{edge01}.y \cdot \text{edge20}.x - \text{edge01}.x \cdot \text{edge20}.y$$
   $$\text{tri.backface} = (\text{detXY} \ge 0.0\text{f})$$
4. Screen-space projection inverts the $Y$ axis ($\text{viewportScale}.y = -0.5 \cdot H \cdot 8$). This negation makes $\text{detXY} = -\det_{\text{clip}}$.
5. For front-facing geometry ($\det_{\text{clip}} < 0$), $\text{detXY} > 0$, causing `SetupTriangle` to mark **all front-facing triangles as backfaces** and cull them! Conversely, back-facing triangles had $\text{detXY} < 0$ and were rendered.

**Resolution**:
Reversed winding order in [`RasterizeSw.gcomp`](file:///f:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshPipeline/RasterizeSw.gcomp) by swapping $v_1 \leftrightarrow v_2$ for standard instances, while preserving parity for negative-scale instances:
```hlsl
float det3x3 = determinant((float3x3)localToRWS);
bool reverseWinding = (det3x3 < 0.0f);

float4 verts[3];
if (reverseWinding) {
    verts[0] = v0; verts[1] = v1; verts[2] = v2;
} else {
    verts[0] = v0; verts[1] = v2; verts[2] = v1; // Swap for CCW rasterizer
}
```

---

### 4.2 Case Study 2: Cluster Radius vs. Triangle Edge Threshold
**Symptom**: Only 167 meshlets were being routed to software rasterization, taking $7.36\ \mu\text{s}$, while hardware rasterization took the rest of the time.

**Root Cause Analysis**:
The classification logic originally compared the projected **cluster bounding sphere radius** against 32 pixels. Because a meshlet contains ~100 triangles, its cluster diameter is $5\times$ to $10\times$ larger than its triangles. This required the entire cluster to be smaller than 32 pixels on screen, effectively demanding triangle edge lengths under $3\text{ px}$.

**Resolution**:
Incorporated triangle count into the projected edge length equation ($2 R_{\text{px}} / \sqrt{T}$). Clusters with screen diameters up to $320\text{ px}$ whose individual triangles are smaller than 32 pixels are now correctly routed to the software rasterizer.

---

### 4.3 Case Study 3: LDS Direct Scan Read vs. VRAM Race Condition
**Symptom**: Potential race condition where thread 0 in [`PrepareMeshletIndirectArgs.gcomp`](file:///f:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshPipeline/PrepareMeshletIndirectArgs.gcomp) read `binOffsets.Load(32u * 4u)` from VRAM without an explicit global UAV barrier after thread 32 stored its value.

**Resolution**:
Thread 0 now reads the inclusive scan results directly from groupshared memory:
```hlsl
// s_scan was already fully synchronized via Hillis-Steele prefix sum:
uint totalHwOpaque = s_scan[31];
uint totalSwOpaque = s_scan[63] - s_scan[31];
```

---

## 5. Performance Benchmarks

Tested on **NVIDIA GeForce RTX 4070 Ti**:
- **Scene**: 100,000 instances $\times$ 274,000 triangles = **27.4 Billion triangles**.

### 5.1 Geometry Pass Breakdown
| Metric | Pure Hardware Mesh Shader | Hybrid HW + SW Rasterizer |
| :--- | :--- | :--- |
| **HW Visibility Pass** | 2.15 ms | **0.16 ms** |
| **SW Visibility Pass** | — | **1.41 ms** |
| **Total Geometry Time** | **2.15 ms – 2.20 ms** | **1.57 ms** (including barrier) |
| **Net Geometry Speedup** | — | **+27% faster** (0.58 ms reduction) |

### 5.2 Full Frame Breakdown (Including All Post/Lighting Passes)
| Pipeline Stage | GPU Duration |
| :--- | :--- |
| **Work Graph Culling & HZB Evaluation** | 0.75 ms |
| **Visibility Buffer (HW 0.16 ms + SW 1.41 ms)** | 1.57 ms |
| **Depth Export, Tile Classification & HZB Build** | 0.35 ms |
| **Deferred Texturing (Analytic Barycentrics)** | 0.52 ms |
| **Clustered Lighting, Shadows & IBL** | 0.55 ms |
| **Post-Processing & Presentation Blit** | 0.15 ms |
| **TOTAL FRAME TIME** | **3.89 ms (~257 FPS)** |
