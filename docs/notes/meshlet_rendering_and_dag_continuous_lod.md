# A Plain-English Guide to Meshlet Rendering, Continuous LOD & Work Graph Dual-Error Culling

This document explains the concepts, mathematical foundation, and implementation details behind GhostEngine's **Continuous Meshlet Level-of-Detail (CLOD)** and **DirectX 12 Work Graph Dual-Error Culling** pipeline.

If you have never worked with meshlets, DAGs, or GPU-driven geometry culling before, this guide will walk you from first principles to the exact architecture used in engines like Unreal Engine 5 (Nanite), Nyx, and GhostEngine.

---

## 1. From Triangles to Meshlets: Why Chunk Geometry?

### 1.1 The Classical Mesh Representation
Historically, 3D meshes are stored as two giant arrays:
1. **Vertex Buffer**: An array of `(Position, Normal, Tangent, UV, Color)` structs.
2. **Index Buffer**: A flat list of vertex indices `[0, 1, 2,  0, 2, 3, ...]` where every 3 indices form a triangle.

While simple, this representation has major flaws on modern GPUs:
- **All-or-nothing culling**: A mesh with 1,000,000 triangles is submitted as one or a few draw calls. If only 1% of the mesh is on screen, the GPU must still fetch and transform all vertices in vertex/geometry shaders before hardware clipping discards them.
- **Poor cache locality**: Triangles far apart in the index list jump randomly around the vertex buffer, thrashing vertex caches.
- **Micro-polygon penalty**: When triangles become smaller than a pixel, hardware rasterizers waste massive compute doing quad over-shading (running 2×2 pixel shader footprints where only 1 pixel is covered).

### 1.2 What is a Meshlet?
A **Meshlet** (also called a **Cluster**) is a small, bounded, self-contained sub-mesh carved out of a larger model:
- **Maximum 64 vertices**
- **Maximum 124 triangles**
- **A tightly-fitted local bounding sphere and bounding box**

```
                  [ Original 1,000,000 Triangle Mesh ]
                                   │
      ┌──────────────┬─────────────┼─────────────┬──────────────┐
      ▼              ▼             ▼             ▼              ▼
 [Meshlet 0]    [Meshlet 1]   [Meshlet 2]   [Meshlet 3]   [Meshlet N]
 (<=64 verts)   (<=64 verts)  (<=64 verts)  (<=64 verts)  (<=64 verts)
 (<=124 tris)   (<=124 tris)  (<=124 tris)  (<=124 tris)  (<=124 tris)
 [ Bounding ]   [ Bounding ]  [ Bounding ]  [ Bounding ]  [ Bounding ]
 [  Sphere  ]   [  Sphere  ]  [  Sphere  ]  [  Sphere  ]  [  Sphere  ]
```

### 1.3 Why Chunk into Meshlets?
1. **GPU Threadgroup Alignment**: A modern GPU compute or mesh shader workgroup typically has 32 or 64 threads. Exactly 1 threadgroup can process 1 meshlet in local on-chip shared memory (`groupshared`) with zero global memory churn.
2. **Granular Per-Cluster Culling**: Before a single triangle is rasterized, compute shaders can test each meshlet's bounding sphere against the camera frustum, backfaces (cone culling), and the Hierarchical Z-Buffer (HZB occlusion culling). If 90% of a model is offscreen or behind a wall, 90% of its meshlets are dropped with zero rasterization cost.

---

## 2. The Level of Detail (LOD) Problem

### 2.1 Discrete LODs (Traditional)
Traditionally, artists or tools bake 3–5 discrete versions of a mesh (LOD 0 = 100k triangles, LOD 1 = 30k, LOD 2 = 5k). The game engine measures the distance from the camera to the mesh origin, and switches the whole object from LOD 0 to LOD 1.
- **Flaws**:
  1. **Popping**: Obvious visual popping as the whole model switches.
  2. **Varying Depth**: For a large object (e.g., a 100-meter dragon or a terrain chunk), the head might be right in front of the camera (needs LOD 0) while the tail is 100 meters away (needs LOD 3). Whole-mesh discrete LODs cannot handle this.

### 2.2 Continuous Cluster LOD (CLOD)
Instead of switching the *entire mesh* to a lower LOD, we want each *individual cluster* to adapt its resolution based on how far that specific cluster is from the camera.

### 2.3 The Boundary Cracking Dilemma
Why can't we just simplify each meshlet independently?
Imagine two neighboring meshlets, A and B, sharing an edge:
```
 Meshlet A (Simplified to 2 tris)      Meshlet B (Still fine: 10 tris)
          *                                      *
         / \                                    /|\
        /   \  <--- T-Junction Gap! --->       / | \
       /     \                                /  |  \
      *-------*                              *---*---*
```
If Meshlet A simplifies its boundary vertices, but Meshlet B keeps its fine vertices, their shared edge no longer matches! This causes **T-junctions**, **cracks**, and visible **holes** in the geometry surface.

---

## 3. How Cluster LOD Simplification Works

To prevent holes while simplifying geometry, Unreal Engine 5 (Nanite) and `meshoptimizer` use **Locked Boundary Group Simplification**.

### Step 1: Partition into Clusters (LOD 0)
The source mesh is partitioned into fine meshlets (clusters) with $\le 64$ vertices.

### Step 2: Group Adjacent Clusters
Clusters that are physically adjacent to each other in 3D space are grouped together (typically 4 to 16 clusters per **Group**):
```
Group G = { Cluster 1, Cluster 2, Cluster 3, Cluster 4 }
```

### Step 3: Lock the External Boundary & Simplify Interior Only
Notice that:
- The **interior edges** (where Cluster 1 meets Cluster 2 inside the group) can be simplified freely.
- The **exterior boundary edges** (where the group touches neighboring groups) are **LOCKED** and cannot be moved or collapsed!

```
     Locked External Boundary (untouched)
   ┌─────────────────────────────────────┐
   │ Cluster 1        │ Cluster 2        │
   │        \ Interior Boundary /        │
   │         \  (Freely simplified)     │
   │──────────────────┼──────────────────│
   │ Cluster 3        │ Cluster 4        │
   └─────────────────────────────────────┘
     Locked External Boundary (untouched)
```

### Step 4: Split into Coarser Clusters (LOD 1)
The simplified geometry of the group (now having ~50% fewer triangles) is partitioned into 2 new clusters. These become the **coarser (LOD 1)** clusters!

Because the exterior boundary was completely locked:
- The new LOD 1 clusters stitch **seamlessly** with adjacent LOD 0 clusters!
- No T-junctions, no cracks, and no holes can ever form across the locked boundary.

This process is repeated hierarchically:
`LOD 0 clusters` $\to$ `Group` $\to$ `LOD 1 clusters` $\to$ `Group` $\to$ `LOD 2 clusters` $\dots$ until a single cluster remains.

---

## 4. Why This Creates a DAG, Not a Tree

In a simple tree, every child node has **exactly one parent**.
However, in cluster simplification, adjacent clusters get merged in different combinations across levels. A fine cluster at LOD 0 may share parts of its simplified geometry with **multiple** coarse clusters at LOD 1.

Furthermore, groups of clusters form a **Directed Acyclic Graph (DAG)**:
```
           [ Coarse Cluster C0 (LOD 2) ]
                   /          \
                  /            \
    [ Cluster B0 (LOD 1) ]   [ Cluster B1 (LOD 1) ]
         /        \               /         \
        /          \             /           \
  [ Cluster A0 ]  [ Cluster A1 ] [ Cluster A2 ]  [ Cluster A3 ] (LOD 0)
```
Notice that `Cluster B0` and `Cluster B1` may both depend on parts of `Cluster A1`.

---

## 5. The Root of All Evil: What Happened When We Forced a DAG into a Tree?

Before our fix, [`MeshBaker.Meshlet.cs`](file:///F:/csharp/GhostEngine/src/Editor/Ghost.AssetForge.Core/Bakers/MeshBaker.Meshlet.cs) attempted to build a runtime hierarchy by forcing this DAG into a single-parent tree:
```csharp
// The flawed logic:
if (parentOfGroup[childGroup] == -1)
{
    parentOfGroup[childGroup] = parentGroup; // First parent takes full ownership!
}
```

### Consequence 1: Triangle Overlapping (Double Rendering)
If `Cluster A1` had two parent groups `P0` and `P1`:
1. `P0` claimed ownership of `A1`.
2. `P1` was left with missing children or `childCount == 0`.
3. At runtime, the GPU culling shader traversed the hierarchy:
   - For `P0`: It refined downwards and rendered the fine LOD 0 clusters (`A0`, `A1`).
   - For `P1`: Because its children were stolen, it thought it had no children, so it **also** rendered its coarse LOD 1 meshlet!
4. **Result**: Both the coarse LOD 1 meshlet AND the fine LOD 0 meshlets rendered simultaneously at the exact same location, causing severe Z-fighting and overlapping triangles!

### Consequence 2: Surface Holes & Cracks
To rescue "orphan" groups, the old baker tried to find a spatial fallback parent (`bestParent`) by searching for nearest bounding spheres across unrelated geometry.
This completely destroyed the boundary vertex locking! Clusters were being turned on/off without matching their geometric neighbors, tearing open holes on the surface.

---

## 6. The Mathematical Solution: The Dual-Error Cut

Instead of forcing a single-parent tree, modern geometry engines use the **Dual-Error Cut** principle over the DAG.

### 6.1 Screen-Space Error Metric ($\tau$)
When geometry is simplified, `meshoptimizer` calculates the maximum geometric displacement distance in 3D object space:
- $E_{cluster}$: The geometric error between this cluster and the original LOD 0 mesh (for LOD 0, $E_{cluster} = 0$).
- $E_{parent}$: The geometric error of the parent representation (the error incurred if we stopped here and didn't refine).

When rendering, the camera has a field-of-view, screen height, and distance $d$ to the cluster. The screen-space projected pixel error $E_{pixel}$ is:
$$E_{pixel} = \frac{E_{object} \cdot \cot(\frac{\text{FOV}}{2}) \cdot H_{screen}}{2 \cdot d}$$

In [`CullCommon.hlsl`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/Includes/CullCommon.hlsl#L281), this is implemented as:
```hlsl
static inline bool EvaluateLODDetailSufficient(
    float objectError,
    float3 sphereCenter,
    float sphereRadius,
    float3 cameraPos,
    float proj11,
    float screenHeight,
    float errorThreshold)
{
    float dist = max(length(sphereCenter - cameraPos) - sphereRadius, 0.001f);
    float pixelError = (objectError * proj11 * screenHeight) / (2.0f * dist);
    return pixelError <= errorThreshold;
}
```
If `pixelError <= errorThreshold`, we say **detail is sufficient** (the error is so small that the human eye cannot see the difference on screen).

---

### 6.2 The Dual-Error Cut Condition
For any cluster $C$ at any level of the DAG, when should it be drawn?

$$\boxed{\text{Render Cluster } C \iff \text{ParentError} > \tau \quad \land \quad \text{ClusterError} \le \tau}$$

Where $\tau$ is the target screen error threshold (typically 1.0 pixel).

Let's dissect this intuitive formula:

| Condition | Meaning | What Happens If Violated? |
| :--- | :--- | :--- |
| **$\text{ParentError} > \tau$** | The coarser parent representation has an error **greater than 1 pixel** (i.e., parent is not detailed enough; we MUST NOT use the parent). | If $\text{ParentError} \le \tau$, the coarser level is already good enough. We **prune** this cluster and let the coarser level render. |
| **$\text{ClusterError} \le \tau$** | This cluster's own error is **within 1 pixel** (i.e., this cluster is detailed enough to look visually lossless). | If $\text{ClusterError} > \tau$, this cluster is too blurry/coarse. We **discard** it because a finer cluster below it must render instead. |

### 6.3 Why This Guarantees Zero Overlap & Zero Holes
1. **Mathematical Partition (The "Cut")**: Along any path through the DAG from root to leaves, there is **exactly one** cluster where the error transitions from $> \tau$ to $\le \tau$. It is mathematically impossible for both a parent and its child to satisfy both conditions simultaneously.
2. **Watertight Seams**: Neighboring clusters from different groups evaluate the exact same continuous world-space distance metric. Because cluster boundaries were locked during baking, when neighbor A switches from LOD 1 to LOD 0, neighbor B's boundary vertices match identically.

---

### 6.4 The Bounding Sphere Alignment Invariant (Why Spheres Must Match Across the Cut)

Even if the mathematical formula $\text{ParentError} > \tau \land \text{ClusterError} \le \tau$ is correctly implemented in HLSL, there is a subtle trap in the offline baker that causes **catastrophic LOD flickering and snapping back to LOD 0**.

Recall how projected error is evaluated:
$$\text{dist} = \max(\|\mathbf{C}_{\text{sphere}} - \mathbf{P}_{\text{camera}}\| - R_{\text{sphere}}, 0.001)$$
$$E_{\text{pixel}} = \frac{E \cdot \cot(\frac{\text{FOV}}{2}) \cdot H_{\text{screen}}}{2 \cdot \text{dist}}$$

Notice that projected error depends **directly on the sphere center $\mathbf{C}$ and radius $R$**!

When a fine group $G_k$ is simplified into coarse clusters $M_{k+1}$, geometric error $E_k$ represents the maximum displacement between the two surfaces:
- Fine group $G_k$ prunes itself when: $\text{EvaluateLODDetailSufficient}(E_k, \mathbf{C}_{\text{parent}}, R_{\text{parent}}) == \text{true}$
- Coarse cluster $M_{k+1}$ renders when: $\text{EvaluateLODDetailSufficient}(E_k, \mathbf{C}_{\text{cluster}}, R_{\text{cluster}}) == \text{true} \land \dots$

#### The Pitfall
If the baker computes:
1. $\mathbf{C}_{\text{parent}}, R_{\text{parent}}$ from the **merged group of fine clusters** $G_k$ ($R \approx 1.5$).
2. But sets $\mathbf{C}_{\text{cluster}}, R_{\text{cluster}}$ from an individual meshlet's tight bounding sphere via `meshopt_computeMeshletBounds` ($R \approx 0.4$).

Because $R_{\text{group}} \gg R_{\text{meshlet}}$, the calculated surface distance $\text{dist}$ differs by **30% to 50%**!
- At the transition distance, the fine group decides the parent is sufficient and prunes itself.
- But the coarse cluster, using its smaller radius, decides its detail is NOT sufficient and also prunes itself (creating a hole or dead zone).
- Worse: because LOD 0 has $E_{\text{cluster}} = 0.0$ (it always passes detail sufficiency), whenever camera jitter causes the fine group's parent check to wobble across the boundary, LOD 0 abruptly snaps back on and off.
- **Visual symptom**: When moving the camera, the mesh does not transition smoothly. Instead, it violently oscillates and strobes between LOD 0 and the target LOD until moving well past the transition threshold.

#### The Golden Rule
$$\boxed{\text{For any transition between level } k \text{ and } k+1:\quad S_{\text{parent}}(G_k) \equiv S_{\text{cluster}}(M_{k+1}) \quad \text{and} \quad E_{\text{parent}}(G_k) \equiv E_{\text{cluster}}(M_{k+1})}$$
The coarser cluster $M_{k+1}$ MUST inherit the exact bounding sphere of the finer group $G_k$ it was simplified from.

---

## 7. GPU Execution: DirectX 12 Work Graphs Pipeline

GhostEngine implements this traversal entirely on the GPU inside [`MeshletCullGraph.ggraph`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshletCullGraph.ggraph).

```
 [ CPU ]
    │ DispatchGraph(InstanceCount)
    ▼
┌─────────────────────────────────────────────────────────────┐
│ Node 1: InstanceCullNode (Broadcasting, 64 threads)         │
│   • Frustum culling against instance bounding box           │
│   • Dispatches Root Node 0 to HierarchyTraverseNode         │
└──────────────────────────────┬──────────────────────────────┘
                               │ HierarchyNodeRecord
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ Node 2: HierarchyTraverseNode (Broadcasting, 1 thread)      │
│   • Frustum cull node bounding sphere                       │
│   • Test ParentError:                                       │
│       if (EvaluateLODDetailSufficient(node.error))          │
│           return; // PRUNED: Coarser LOD is sufficient!     │
│   • if (node.groupIndex < 0):                               │
│       Dispatch children to HierarchyTraverseNode (Max 8)    │
│   • else:                                                   │
│       Dispatch meshlets to MeshletCullNode (Max 64 chunks)  │
└──────────────────────────────┬──────────────────────────────┘
                               │ MeshletCandidateRecord
                               ▼
┌─────────────────────────────────────────────────────────────┐
│ Node 3: MeshletCullNode (Coalescing, 32 threads)            │
│   • Test ClusterError:                                      │
│       if (!EvaluateLODDetailSufficient(meshlet.clusterError))│
│           isValid = false; // DISCARD: Too coarse!          │
│   • Frustum Culling (Meshlet AABB)                          │
│   • Pass 1 HZB Occlusion Culling                            │
│   • Wave Compaction (WaveActiveCountBits / WavePrefixCount) │
│   • Emits Visible Meshlet Draw Payloads (Opaque / Masked)   │
└─────────────────────────────────────────────────────────────┘
```

### 7.1 How the BVH Accelerates Traversal
In [`MeshBaker.Meshlet.cs`](file:///F:/csharp/GhostEngine/src/Editor/Ghost.AssetForge.Core/Bakers/MeshBaker.Meshlet.cs#L962), we group the MeshletGroups into an 8-ary Bounding Volume Hierarchy (BVH):
- Every internal BVH node contains a bounding sphere enclosing its children, and an `error = max(children.error)`.
- In `HierarchyTraverseNode`, if an internal node satisfies $\text{ParentError} \le \tau$, the **entire subtree** (thousands of candidate meshlets) is pruned in a single GPU clock cycle!

### 7.2 Why 1 Thread for HierarchyTraverseNode?
Work Graphs enforce strict record allocation limits: `[MaxRecords(8)] NodeOutput<HierarchyNodeRecord>` and `[MaxRecords(64)] NodeOutput<MeshletCandidateRecord>`.
By using `[NumThreads(1, 1, 1)]` and `[NodeDispatchGrid(1, 1, 1)]`, exactly 1 thread executes per traversed node. This:
- Completely avoids allocating idle threads.
- Eliminates threadgroup synchronization overhead.
- Satisfies D3D12 output allocation memory budgets safely.

### 7.3 Wave Compaction in MeshletCullNode
In `MeshletCullNode`, 32 threads cooperate as a single SIMD Wave. Instead of 32 threads each firing an atomic add (`InterlockedAdd`) to global VRAM:
1. `WaveActiveCountBits(isOpaque)` calculates how many meshlets in the wave survived culling.
2. `WavePrefixCountBits(isOpaque)` computes each surviving thread's local index.
3. The wave leader (`WaveIsFirstLane()`) performs a **single atomic add** for the entire wave.
This provides a **32× reduction in memory bus contention**.

---

## 8. Critical Production Pitfalls & Hard-Learned Lessons

### Pitfall 1: Bounding Sphere Divergence Across LOD Levels (Flickering to LOD 0)
- **Symptom**: Moving the camera across LOD distances causes violent strobe-like flickering between LOD 0 and LOD 1/2.
- **Root Cause**: Bounding sphere radii differed between the fine group's `parentBoundingSphere` and the coarse cluster's `boundingSphere`.
- **Fix**: In [`MeshBaker.Meshlet.cs`](file:///F:/csharp/GhostEngine/src/Editor/Ghost.AssetForge.Core/Bakers/MeshBaker.Meshlet.cs), preserve `cluster.bounds` during simplification and assign it to the coarse meshlet's `boundingSphere`. Ensure LOD 0 explicitly has `clusterError = 0.0f`.

### Pitfall 2: RenderGraph `AccessFlags.WriteAll` (Discard) in Multi-Pass Geometry Rendering
- **Symptom**: In debug modes (or multi-pass geometry rendering), running the game directly produces garbled screen tearing / "花屏", yet running inside PIX appears completely normal!
- **Root Cause**: In [`GhostRenderPipeline.cs`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/RenderPipeline/GhostRenderPipeline.cs), Pass 2 (Late-Z) called `builder.SetColorAttachment(colorTarget, 0, AccessFlags.WriteAll)`.
  - `AccessFlags.WriteAll` is defined as `AccessFlags.Write | AccessFlags.Discard`.
  - The RenderGraph builder inferred `attachment.loadOp = AttachmentLoadOp.DontCare`.
  - In D3D12, this translates to `D3D12_RENDER_PASS_BEGINNING_ACCESS_TYPE_DISCARD`.
  - When Pass 2 began, the GPU discarded the backbuffer pixels already drawn by Pass 1 (Early-Z)! Only late-visible meshlets were written, leaving the rest of the screen filled with uninitialized VRAM garbage.
  - **Why PIX masked it**: PIX's instrumentation layer and capture engine often override or initialize render targets to preserve pass history for debugger overlays and draw call inspection.
- **Fix**: Use `builder.SetColorAttachment(colorTarget, 0)` (defaulting to `AccessFlags.Write`) for multi-pass geometry rendering. Pass 1 clears the target, while Pass 2 preserves it with `AttachmentLoadOp.Load`. Reserve `AccessFlags.WriteAll` strictly for full-screen passes (like Blit) that unconditionally overwrite every pixel.

### Pitfall 3: Terminal Node Error Overflow in DAG Roots
- **Symptom**: Automated unit tests assert `!float.IsPositiveInfinity(node.error) && node.error < 3.4e38f`.
- **Root Cause**: Setting the terminal coarsest root nodes' parent error to `float.MaxValue` broke floating-point sanity checks in unit tests.
- **Fix**: Use `Math.Max(bounds.error * 2.0f, bounds.radius * 2.0f)` for terminal nodes. It is guaranteed to be strictly greater than any child error without triggering floating-point overflow.

---

## 9. Summary of Relevant Source Files

| Component | File Path | Key Functions / Structures |
| :--- | :--- | :--- |
| **Baker & BVH Generator** | [`MeshBaker.Meshlet.cs`](file:///F:/csharp/GhostEngine/src/Editor/Ghost.AssetForge.Core/Bakers/MeshBaker.Meshlet.cs) | `BuildMeshlets`, `BuildClusterLodHierarchy`, `EncloseSphere` |
| **Culling Work Graph** | [`MeshletCullGraph.ggraph`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshletCullGraph.ggraph) | `InstanceCullNode`, `HierarchyTraverseNode`, `MeshletCullNode` |
| **Culling Functions** | [`CullCommon.hlsl`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/Includes/CullCommon.hlsl) | `EvaluateLODDetailSufficient`, `BBoxIntersectFrustum`, `HZBVisible` |
| **Shader Structs** | [`Common.hlsl`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/Includes/Common.hlsl) | `struct Meshlet`, `struct MeshletGroup`, `struct MeshletHierarchyNode` |
| **C# Runtime Structs** | [`Meshlet.cs`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Core/Graphics/Meshlet.cs) | `Meshlet`, `MeshletGroup`, `MeshletHierarchyNode`, `MeshletMeshData` |
| **Render Pipeline & Debug Pass** | [`GhostRenderPipeline.cs`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/RenderPipeline/GhostRenderPipeline.cs) | `AddMeshletDebugPass`, `AddVisibilityBufferPass`, Load/Store Op handling |
| **Unit & Integration Tests** | [`MeshBakerTests.cs`](file:///F:/csharp/GhostEngine/src/Test/Ghost.AssetBaker.Test/MeshBakerTests.cs) | `TestBunnyContinuousLodHierarchy`, Monotonicity & BVH Containment |

---

## 10. TL;DR Cheat Sheet

- **What is a meshlet?** A mini-mesh of $\le 64$ vertices and $\le 124$ triangles that fits in a GPU threadgroup.
- **Why group clusters?** To simplify interior geometry while locking exterior boundary vertices, preventing holes.
- **Why is it a DAG?** Clusters merge in different combinations across levels, creating multiple parents per child.
- **Why did triangles overlap before?** The baker forced the DAG into a single-parent tree, leaving sibling parents with 0 children so they rendered coarse LODs simultaneously with fine LODs.
- **How does Dual-Error solve it?** 
  - $\text{ParentError} > \tau$: Ensures the parent is not good enough (forces refinement).
  - $\text{ClusterError} \le \tau$: Ensures this cluster is good enough (discards over-coarse clusters).
  - Together, they select an exact, continuous, watertight slice across the entire geometry DAG.
- **Why must bounding spheres match?** If the fine group's parent sphere and the coarse cluster's sphere differ in size, their distance and error calculations diverge, causing violent flickering back to LOD 0.
- **Why did debug mode tear/corrupt outside PIX?** `AccessFlags.WriteAll` triggered D3D12 RenderPass Discard on Pass 2 Late-Z, wiping out Pass 1 Early-Z rendering. Use `AccessFlags.Write` for multi-pass geometry targets.

