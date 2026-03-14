# GhostEngine Meshlet Architecture

This document explains the meshlet system planned for GhostEngine before we implement it. The goal is not just to describe what the engine will do, but why the design looks this way, how the pieces connect, and what tradeoffs we are intentionally making.

The intended result is a GPU-driven meshlet pipeline that is:

- high performance
- data oriented
- compatible with the current GhostEngine resource model
- friendly to bindless material evaluation
- understandable enough that future changes are deliberate instead of accidental

This is a design document, not a promise that every detail is final forever. But it is the architecture we should implement unless we discover a concrete reason to deviate.

## 1. What problem are we solving?

Traditional mesh rendering usually looks like this:

1. CPU decides which objects to draw.
2. CPU submits one or more draw calls per mesh or submesh.
3. GPU fetches vertices and indices for the selected draw calls.
4. LOD is usually selected per object, not per localized part of the mesh.

That model becomes limiting when scene complexity grows:

- CPU draw submission becomes expensive.
- Large meshes are hard to cull efficiently because only the whole object is considered.
- Per-submesh material splitting adds authoring and runtime complexity.
- Object-level LOD wastes detail: one visible corner of a huge object may force the whole object to render at a high LOD.

Meshlets solve a different granularity problem.

A meshlet is a small cluster of triangles with a small local vertex set. Instead of treating a mesh as a single index buffer plus a few submeshes, we treat it as many tiny, spatially bounded clusters that can be culled, selected, and dispatched more precisely.

For GhostEngine, meshlets are attractive because they line up with the rest of the engine direction:

- GPU-driven rendering
- bindless resources
- visibility buffer first, material shading later
- mesh shader capable pipeline

The meshlet system is not just "smaller draw calls". It becomes the unit of:

- visibility testing
- LOD selection
- material lookup
- mesh shader dispatch
- future streaming

## 2. Pipeline at a glance

At a high level the full flow looks like this:

```text
Imported mesh
    |
    v
Split triangles by material
    |
    v
Build LOD0 meshlets with meshoptimizer
    |
    v
Group nearby meshlets
    |
    v
Simplify each group into a coarser representation
    |
    v
Rebuild coarser meshlets
    |
    v
Repeat until reduction becomes negligible
    |
    v
Build hierarchy nodes over groups for each LOD
    |
    v
Connect all LOD hierarchies into one DAG-like refinement structure
    |
    v
Upload meshlet data + hierarchy + lookup buffers to GPU
    |
    v
GPU culling pass traverses hierarchy
    |
    v
Visible meshlets emitted to visible meshlet list
    |
    v
Mesh shader rasterizes visible meshlets into VBuffer
    |
    v
GBuffer resolve reads material through palette indirection
    |
    v
Deferred lighting
```

There are really two major phases:

- offline or asset-build phase: turn a mesh into meshlet data
- runtime phase: decide which meshlets to render this frame

Those two phases should be designed together. If the runtime needs fast decisions, the build phase must produce data that makes those decisions cheap.

## 3. Why GhostEngine uses meshlets the way it does

Some design choices are already settled.

### 3.1 Meshlet size

GhostEngine will use:

- max 64 vertices per meshlet
- max 124 triangles per meshlet

These are meshoptimizer's common defaults and are a better starting point for this engine than Nyx's 128/128 choice.

Why this matters:

- 64 unique vertices fits naturally in a byte-addressable local index space.
- 124 triangles keeps the meshlet compact while leaving room for efficient packing.
- Smaller clusters improve culling granularity.
- These limits are already well-supported by meshoptimizer APIs.

This does mean more meshlets than a larger cluster size, but that is acceptable because GhostEngine is explicitly building a GPU-driven pipeline where fine-grained culling is a feature, not a problem.

### 3.2 Meshlets are material-local

Each meshlet belongs to exactly one material slot and stores:

- `localMaterialIndex`

This replaces the need for a `SubMesh` concept in the new pipeline.

Instead of saying "this mesh has N submeshes", we say:

- this mesh has a material palette
- each meshlet points to one entry in that palette

That means material boundaries matter during meshlet generation. Triangles from two different materials must not be merged into the same meshlet.

### 3.3 Full hierarchy from the start

We are not building only LOD0 first and layering hierarchy later. The target design starts with the full refinement hierarchy.

Reason:

- the runtime culling logic depends on more than per-meshlet visibility
- LOD selection is fundamentally part of the data structure, not just a later optimization
- future streaming also wants group and hierarchy metadata

If we postponed hierarchy design, we would likely create temporary formats that have to be thrown away.

### 3.4 Visibility buffer pipeline compatibility

GhostEngine's render plan is:

1. VBuffer pass writes visibility
2. GBuffer resolve classifies materials and evaluates them through bindless resources
3. Deferred lighting consumes the resolved data

That means meshlet rendering should output enough information to identify:

- object or instance
- primitive or triangle identity if needed
- material identity through indirection

The meshlet stage should stay focused on visibility and primitive emission. Material evaluation belongs later.

## 4. Existing engine context this design must fit

The meshlet system does not exist in a vacuum. It has to fit the current GhostEngine architecture.

### 4.1 Resource ownership and handles

GhostEngine uses `Handle<T>` backed by `UnsafeSlotMap<T>`. This means:

- resources are identified indirectly
- validity uses `ID + Generation`
- stale handles can be detected

That is good for mesh and material lifetime, but it also means runtime GPU data must not rely on direct managed object references.

### 4.2 Current mesh representation

Current `Mesh` lives in `src/Runtime/Ghost.Graphics/Core/Mesh.cs` and already stores:

- CPU vertex data
- CPU index data
- mesh bounding box
- GPU vertex/index buffers
- stub meshlet data

The existing `MeshLet` struct is only a placeholder. It is not enough for the planned runtime because it lacks:

- sphere bounds
- group linkage
- hierarchy linkage
- LOD metadata
- parent error data

So the current meshlet stub should be treated as disposable scaffolding.

### 4.3 Root signature and object data

Current root constants and object data are simple:

- `PushConstantsData` in `src/Runtime/Ghost.Graphics.RHI/RootSignatureLayout.cs`
- `PerObjectData` includes `localToWorld`, object bounds, vertex buffer index, index buffer index

This is enough for the current direct mesh path, but the meshlet path will eventually need additional GPU-readable data, likely through structured buffers rather than larger push constants.

That is the correct direction. Push constants should remain tiny and hot.

### 4.4 Material palette system

GhostEngine already has the right CPU-side material indirection direction via `src/Runtime/Ghost.Graphics/Core/MaterialPaletteStore.cs`.

This was an important architectural decision.

We rejected:

- ECS shared component material lists because they fragment chunks
- per-instance full material arrays because they waste memory

We chose:

- deduplicated material palettes
- one `materialPaletteIndex` per instance

This is exactly what the meshlet pipeline wants.

## 5. Core data model

The runtime data should be designed first from the GPU's perspective, then mirrored cleanly in C#.

The proposed structures are intentionally compact, unmanaged, and GPU-friendly.

### 5.1 `Meshlet`

Planned size: 64 bytes

```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct Meshlet
{
    public SphereBounds boundingSphere;   // 16 bytes
    public AABB boundingBox;              // 24 bytes
    public uint vertexOffset;             // offset into meshlet vertex index array
    public uint triangleOffset;           // offset into packed triangle array
    public byte vertexCount;              // max 64
    public byte triangleCount;            // max 124
    public byte localMaterialIndex;       // mesh-local material slot
    public byte lodLevel;                 // this meshlet's LOD level
    public uint groupIndex;               // owning group
    public float parentError;             // geometric refinement error carried into runtime LOD tests
}
```

What this struct needs to answer quickly:

- where are my local vertices?
- where are my local triangles?
- how many are there?
- what material do I use?
- what bounds do I test?
- what LOD level am I part of?
- what group owns me?
- how much geometric error does this representation introduce?

Why both sphere and AABB?

- sphere is cheap and stable for LOD/error calculations
- AABB is useful for frustum and occlusion tests

Why store `groupIndex` in each meshlet if groups also store meshlet ranges?

- reverse lookup is sometimes cheaper than reconstructing ownership
- the GPU often prefers direct indexing over inference

### 5.2 `MeshletGroup`

Planned size: 64 bytes

```csharp
[StructLayout(LayoutKind.Sequential, Size = 64)]
public struct MeshletGroup
{
    public SphereBounds boundingSphere;   // 16 bytes
    public AABB boundingBox;              // 24 bytes
    public float parentError;             // error of refining to the previous level
    public uint meshletStartIndex;        // contiguous meshlet range
    public uint meshletCount;             // number of meshlets in the group
    public uint lodLevel;                 // group LOD level
}
```

Groups are important because we do not simplify single meshlets independently. We simplify collections of neighboring meshlets.

That gives us:

- better local continuity
- a meaningful refinement unit
- a more stable hierarchy than treating every meshlet in isolation

Conceptually:

- a meshlet is a renderable unit
- a group is a simplification and refinement unit

### 5.3 `MeshletHierarchyNode`

Planned size: 48 bytes

```csharp
[StructLayout(LayoutKind.Sequential, Size = 48)]
public struct MeshletHierarchyNode
{
    public SphereBounds boundingSphere;   // 16 bytes
    public AABB boundingBox;              // 24 bytes
    public float maxParentError;          // maximum error in this subtree
    public uint nodeData;                 // packed leaf/internal metadata
}
```

`nodeData` follows the Nyx-style packed convention because it is compact and GPU-friendly.

Internal node encoding:

- bit 0 = 0
- bits 1..27 = child start index
- bits 28..31 = child count

Leaf node encoding:

- bit 0 = 1
- bits 1..24 = group index
- bits 25..31 = meshlet count minus one

Why pack it?

- smaller memory footprint
- fewer GPU loads
- predictable layout
- easy to mirror in HLSL

Why is `maxParentError` stored on the node?

Because hierarchy traversal should be able to reject an entire subtree if its current LOD is already good enough. That requires a coarse upper bound on the subtree's refinement error.

### 5.4 `MeshletMeshData`

Planned role: CPU-side container for all meshlet-related arrays belonging to one mesh

```csharp
public struct MeshletMeshData : IDisposable
{
    public UnsafeList<Meshlet> meshlets;
    public UnsafeList<MeshletGroup> groups;
    public UnsafeList<MeshletHierarchyNode> hierarchyNodes;
    public UnsafeList<uint> meshletVertices;
    public UnsafeList<byte> meshletTriangles;
    public int lodLevelCount;
    public int materialSlotCount;
}
```

This is not just "some extra arrays". It is the authored runtime representation of the mesh in meshlet form.

The important separation is:

- `Vertices` and `Indices` still describe the original mesh data
- `MeshletMeshData` describes how the runtime consumes that mesh for clustered rendering

### 5.5 Why `SphereBounds` and `AABB`

GhostEngine should use the existing math library types from `Misaki.HighPerformance.Mathematics.Geometry`:

- `SphereBounds` = 16 bytes
- `AABB` = 24 bytes

This avoids inventing yet another geometry representation and keeps math behavior consistent across the engine.

## 6. Material model: why no `SubMesh`

In many engines, a mesh is split into submeshes so each material can be drawn separately.

That model is awkward for a meshlet pipeline because:

- submesh boundaries are coarse
- draw-call-oriented organization leaks into GPU-driven rendering
- authoring concepts become runtime constraints

GhostEngine's material palette model is better.

The intended lookup chain is:

```text
Mesh instance
    -> materialPaletteIndex
    -> PaletteOffsetBuffer[materialPaletteIndex]
    -> base offset into MaterialIndexBuffer
    -> MaterialIndexBuffer[baseOffset + meshlet.localMaterialIndex]
    -> bindless material / material buffer index
```

This means:

- many instances can share the same material palette
- each meshlet only needs a tiny local material index
- no ECS chunk fragmentation from large shared material lists
- no need for a separate submesh draw path

The key rule during build is simple:

- meshlet generation must never combine triangles with different local material indices

So the build pipeline is free to partition inside a material region, but not across material boundaries.

## 7. Build pipeline in detail

This is the most important part to understand because runtime behavior is shaped by how the asset is built.

### 7.1 Input assumptions

A source mesh provides at least:

- vertex buffer
- index buffer
- triangle-to-material assignment or equivalent material ranges

The meshlet builder must transform that into:

- LOD0 meshlets
- progressively coarser meshlet groups and meshlets
- a hierarchy for traversal
- per-level error metrics

### 7.2 Step 1: split by material

Before anything else, the builder conceptually partitions triangle sets by `localMaterialIndex`.

This does not necessarily mean creating physically separate mesh objects. It means the cluster builder treats triangles from different materials as incompatible.

If we fail this step, we break the whole material indirection model because one meshlet would need more than one material.

### 7.3 Step 2: generate a position remap

Use `meshopt_generatePositionRemap` from `Ghost.MeshOptimizer`.

Purpose:

- detect vertices that share the same position even if attributes differ
- later lock simplification boundaries so cracks do not appear

Why position remap matters:

In real meshes, one logical corner may appear as multiple vertices because normals, tangents, UVs, or colors differ. If simplification ignores this relationship, one side of a seam can collapse differently than the other, producing holes or cracks.

Position remap tells us which duplicated vertices are spatially the same point.

### 7.4 Step 3: build LOD0 meshlets

For each material-local triangle set, build meshlets using `meshopt_buildMeshletsFlex`.

Why `buildMeshletsFlex` instead of only `buildMeshlets`:

- more control over triangle fill behavior
- better fit for tuning cluster quality
- matches the studied Nyx direction

Then optimize each meshlet with `meshopt_optimizeMeshlet`.

For each generated meshlet compute:

- sphere bounds
- AABB

Use:

- `meshopt_computeMeshletBounds` where useful
- engine-side AABB construction from referenced vertices

At the end of this step, we have the finest renderable representation.

Important: LOD0 meshlets are not enough. They are only the starting layer.

### 7.5 Step 4: create attribute protection locks

This step marks vertices that should not be simplified across attribute discontinuities.

Typical discontinuity causes:

- hard normals
- UV seams
- tangent basis changes
- duplicated vertices sharing position but not full attributes

This lock data feeds `meshopt_simplifyWithAttributes`.

Conceptually, we are saying:

- some vertices may occupy the same place
- but they are not interchangeable for surface appearance

### 7.6 Step 5: group neighboring meshlets

Meshlets are grouped into local neighborhoods before simplification.

Use `meshopt_partitionClusters` to create spatially coherent meshlet groups.

Target size should start around the Nyx-like range of roughly a few dozen meshlets per group, then be tuned later.

Why group first?

- simplification needs enough local context to create a meaningful coarser representation
- simplifying one meshlet at a time would produce unstable transitions
- groups become refinement units and future streaming units

For each group, compute merged:

- bounding sphere
- bounding box

### 7.7 Step 6: build group boundary locks

This is one of the most subtle steps.

When simplifying a group, vertices shared across group boundaries should be locked appropriately. Otherwise one group may simplify differently than its neighbor, which creates discontinuities at the boundary.

Position remap is used again here:

- if the same logical position appears in multiple groups
- and simplification in one group would move or remove it inconsistently
- we lock it

This is a major reason the position-remap pass exists.

### 7.8 Step 7: simplify each group

For each group:

1. Gather unique global vertices referenced by that group's meshlets.
2. Build a local working index buffer.
3. Build local attribute streams.
4. Run `meshopt_simplifyWithAttributes`.
5. If the result is too poor or reduction is too aggressive, fall back to `meshopt_simplifySloppy`.
6. Convert simplified local indices back to global mesh indices.
7. Rebuild meshlets from the simplified result.

This yields the next coarser LOD level.

The output meshlets must remember what they refine from. In Nyx this is tracked as refine-group linkage; GhostEngine should keep the same concept even if naming differs slightly.

That linkage is what turns a plain LOD stack into a refinement structure.

### 7.9 Step 8: assign and propagate error metrics

Each coarser representation introduces geometric error relative to the finer representation it came from.

That error becomes the core input for runtime LOD decisions.

Important property: error must be monotonic enough for hierarchy traversal.

In practice this means:

- child refinement should never look "more expensive" to refine than its parent in a way that breaks traversal assumptions
- group or node error often stores the maximum relevant error in its subtree

If needed, error values should be clamped or merged conservatively so traversal can safely say:

- if this node is already good enough, the subtree is also good enough

### 7.10 Step 9: repeat until diminishing returns

The builder repeats:

- group
- simplify
- rebuild meshlets
- assign errors

Until one of the stop conditions is hit:

- reduction is too small to matter
- no valid coarser meshlets are produced
- the mesh has become trivially small

This creates multiple LOD levels, not as separate full meshes, but as a connected refinement chain.

### 7.11 Step 10: build hierarchy nodes

Once groups exist for each LOD, build hierarchy nodes over them.

Nyx uses a BVH-like structure with up to 8 children per node. That is a good model for GhostEngine too.

Desirable properties:

- each node bounds its descendants
- each node stores a conservative max error
- leaves reference meshlet groups
- nodes are flattened into arrays for GPU traversal

Use `meshopt_spatialClusterPoints` or equivalent spatial ordering to cluster group centers before bottom-up node construction.

### 7.12 Step 11: flatten for GPU use

The final asset format should become contiguous arrays suitable for GPU upload:

- `Meshlet[]`
- `MeshletGroup[]`
- `MeshletHierarchyNode[]`
- `uint[] meshletVertices`
- `byte[] meshletTriangles`

Future streaming may serialize these in chunked blobs, but the in-memory model should already assume a flattened GPU-friendly layout.

## 8. Why this is described as a DAG even though the traversal looks like a BVH

This is worth explaining carefully.

The hierarchy nodes form a BVH-style tree over groups for culling.

But the full refinement relationship across LODs behaves more like a DAG-style representation because:

- meshlets at one level are derived from groups at a finer level
- traversal makes decisions based on refinement eligibility, not just parent-child culling links
- the chosen rendered set is a cut through multiple linked levels of representation

So when we say "meshlet DAG" in this document, we do not mean a generic arbitrary graph with random fan-in and fan-out. We mean:

- culling hierarchy is tree-like
- refinement relationships across LODs create a more general dependency structure than a single flat LOD chain

The important mental model is not the exact graph theory label. The important mental model is:

- the runtime chooses one valid frontier of detail through linked coarse and fine representations

## 9. Runtime culling flow

At runtime, the GPU should decide which meshlets are visible and at what detail level.

The broad idea is inspired by Nyx's `DAGCullCS.hlsl` and `CullCommon.hlsli`.

### 9.1 Inputs to runtime culling

Per frame, the culling pass needs at least:

- view/projection data
- object transform
- object world-space bounds
- hierarchy buffer
- group buffer
- meshlet buffer
- HZB or similar occlusion structure
- residency information if streaming is enabled

### 9.2 Node queue phase

Traversal starts from the root hierarchy nodes for a mesh or instance.

For each node:

1. Frustum test the node bounds.
2. Occlusion test the node bounds.
3. Evaluate whether the node's represented detail is already sufficient.
4. If not sufficient and visible, enqueue children.
5. If leaf and visible, emit candidate meshlets or candidate groups.

This is the first big optimization point.

If a coarse node is invisible or already detailed enough, the entire subtree is skipped.

### 9.3 Meshlet candidate phase

Candidate meshlets from visible leaf groups go through a finer pass:

1. Per-meshlet frustum test
2. Per-meshlet occlusion test
3. Refinement test against finer group information
4. Emit visible meshlet if it is the right representative for the current cut

This second stage matters because leaf-group visibility does not guarantee every meshlet inside the group should render.

### 9.4 Two-pass occlusion idea

Nyx uses two-pass HZB logic:

- pass 0 uses previous-frame HZB
- pass 1 uses current-frame HZB to recover cases that were conservatively hidden before

This is useful because occlusion data always lags a little.

GhostEngine should adopt the same conceptual model later, even if the first implementation starts simpler.

The reason is practical:

- previous-frame HZB is immediately available and cheap
- current-frame HZB can improve correctness for newly visible regions

## 10. LOD selection: the part that usually feels confusing

This is the part most people trip over on the first read.

The runtime is not asking:

- "which numbered LOD mesh do I choose for this object?"

Instead it is asking:

- "is this representation already good enough on screen, or should I refine further?"

That is a refinement question, not a classic object-LOD switch.

### 10.1 Error-driven refinement

Each group or meshlet carries an error measure that describes how wrong the current coarser representation could be compared to the next finer representation.

At runtime we project that error into screen space.

Conceptually:

```text
projectedError = f(distance, bounds, projection)
```

If the projected error is small enough, the coarser version is acceptable.

If the projected error is too large, we should refine.

### 10.2 Why sphere bounds help

Using a sphere for LOD calculation is common because it gives a stable scalar distance proxy.

Instead of reasoning about the whole box orientation, we can say:

- how far is the camera from the sphere surface?
- how much screen-space error would the stored geometric error create at that distance?

This is one reason every meshlet and group stores a sphere.

### 10.3 The "cut through the DAG"

The rendered set for a frame should form a valid frontier.

That means:

- we do not render both a coarse group and all of its refined descendants at the same time
- we do not stop too early where detail is needed
- we do not refine too far where the extra detail is invisible

The chosen set of visible, detail-appropriate meshlets is often described as a cut through the refinement DAG.

This is a good term because it captures the idea that:

- some nearby parts of the same model may refine deeper
- some distant parts may stay coarse
- the final set is mixed across levels but still logically consistent

### 10.4 Mental model for one branch

Imagine one branch of a large rock formation:

- very far away: coarse group is acceptable, stop early
- medium distance: refine once, use mid-detail meshlets
- very near: refine again, use fine meshlets

Now imagine the left side of the rock is near the camera but the right side is distant or occluded.

With meshlets, those branches can diverge naturally. That is the power of local refinement.

## 11. Frustum and occlusion testing

The visibility tests should be conservative and cheap.

### 11.1 Frustum testing

The hierarchy can use AABB-based frustum testing because it is conservative and maps well to clip-space checks.

The exact implementation can evolve, but the principle is:

- reject only when definitely outside
- keep nodes when uncertain

False positives are acceptable. False negatives are not.

### 11.2 HZB occlusion testing

The occlusion test asks:

- is this bound fully behind already-known depth at an appropriate mip level?

The hierarchy level matters here:

- coarse nodes should use conservative footprint-aware tests
- meshlets can use tighter tests

The reason to use HZB at node level first is obvious once you picture cost:

- rejecting one node can remove dozens or hundreds of downstream meshlets

## 12. Mesh shader path

Once visible meshlets are selected, the runtime dispatches mesh shaders instead of issuing classic indexed draw calls per submesh.

The mesh shader's job is roughly:

1. load the meshlet record
2. load meshlet-local vertex indices
3. gather source vertices from the mesh vertex buffer
4. load packed triangle indices
5. emit primitives
6. write visibility payload for later passes

The mesh shader should not do expensive material evaluation. That belongs to the later resolve stage.

### 12.1 Why this matches the VBuffer pipeline

GhostEngine already wants:

- VBuffer first
- materials later

That is ideal here because the mesh shader only needs to know enough to produce visibility.

The later resolve pass can turn visibility information plus material indices into actual shaded surface data.

This separation is important:

- geometry stage focuses on coverage and visibility
- shading stage focuses on material work

## 13. Material evaluation path at runtime

The material lookup for a visible meshlet should conceptually work like this:

```text
Instance provides materialPaletteIndex
Meshlet provides localMaterialIndex
GPU resolves palette entry to actual material handle/index
Resolve pass reads material constants/textures bindlessly
```

This keeps material identity compact in the geometry stage.

The meshlet only needs a local slot, not a full material object reference.

That is exactly what we want in a data-oriented renderer.

## 14. Proposed GPU buffer families

Exact buffer ownership may evolve, but the runtime likely wants at least these logical buffers:

- meshlet metadata buffer
- meshlet vertex remap buffer
- meshlet triangle buffer
- group buffer
- hierarchy node buffer
- visible meshlet output buffer
- instance-to-palette buffer or palette offset buffer
- material index indirection buffer

One useful way to think about them is by update frequency.

Mostly static per mesh:

- meshlet metadata
- remap buffer
- triangle buffer
- groups
- hierarchy

Dynamic per frame:

- visible meshlet list
- indirect dispatch args
- culling queues

Mostly static but shared across instances:

- material palette indirection data

## 15. Streaming implications

Streaming is not the first implementation goal, but the architecture should not block it.

Groups are the natural future streaming unit because they already bundle:

- local meshlet ranges
- bounds
- refinement meaning

Nyx uses `GroupDataLocation`-style indirection so group payload can be resident or missing independently.

GhostEngine does not have to build full streaming support on day one, but the design should leave room for:

- checking group residency during refinement
- rendering a coarser resident representation when a finer one is unavailable
- chunking serialized meshlet data by groups or LOD ranges

This is another reason the refinement model matters. A coarse group can stand in for a missing finer group without inventing a separate fallback system.

## 16. Why the material palette decision was so important

This deserves its own section because it connects rendering architecture to ECS architecture.

If material lists had been stored as ECS shared components:

- chunk fragmentation would increase
- instance organization would depend on material palette uniqueness
- the CPU-side ECS layout would be distorted by a GPU-side lookup problem

If material lists had been copied per instance:

- memory cost would scale badly
- updates would become noisy

The chosen `MaterialPaletteStore` model avoids both problems.

That means the meshlet renderer gets a clean contract:

- instance owns one palette index
- meshlet owns one local material index
- runtime resolves the pair

This is one of the strongest architectural wins in the whole design.

## 17. Build responsibilities vs runtime responsibilities

It helps to state this explicitly.

### Build phase responsibilities

- split triangles by material
- generate LOD0 meshlets
- detect position-equivalent vertices
- create protection locks
- group nearby meshlets
- simplify groups into coarser levels
- compute bounds and errors
- build hierarchy nodes
- flatten all arrays for runtime upload

### Runtime responsibilities

- upload meshlet data to GPU buffers
- transform object bounds to world space
- traverse hierarchy per view
- perform frustum and occlusion culling
- choose the appropriate refinement frontier
- emit visible meshlets
- dispatch mesh shader work
- resolve materials in later passes

This split is important because it keeps frame-time logic simple. Runtime should consume prepared data, not rebuild meaning on the fly.

## 18. Planned implementation order

The implementation should happen in a sequence that preserves understanding and debuggability.

### Phase 1: data structures

Add new types for:

- `Meshlet`
- `MeshletGroup`
- `MeshletHierarchyNode`
- `MeshletMeshData`

Also update `Mesh` so the old `MeshLet` stub is replaced by the real structure.

### Phase 2: meshoptimizer integration usage

No native wrapper work is needed because `Ghost.MeshOptimizer` already exists.

We only need to add the project reference and write the engine-side builder that uses the wrapper.

### Phase 3: CPU builder

Implement `MeshletBuilder` that:

- reads a mesh
- partitions by material
- builds LOD0 meshlets
- builds groups and coarser levels
- emits final `MeshletMeshData`

### Phase 4: GPU upload path

Create and upload buffers for:

- meshlets
- meshlet vertices
- meshlet triangles
- groups
- hierarchy
- palette indirection if not already mirrored on GPU

### Phase 5: GPU culling path

Implement compute-based traversal and visible meshlet list generation.

Start simple if needed, but the target direction is the hierarchy-driven refinement model described here.

### Phase 6: mesh shader VBuffer path

Use visible meshlets to drive mesh shader dispatch and write the VBuffer.

### Phase 7: refinement and occlusion improvements

Add:

- better LOD tuning
- two-pass HZB behavior
- residency logic for future streaming

## 19. Common failure modes to avoid

This section is here because meshlet systems are easy to get mostly right but subtly wrong.

### 19.1 Mixing materials inside one meshlet

This breaks the material model immediately.

Rule: one meshlet, one `localMaterialIndex`.

### 19.2 Treating meshlets like tiny submeshes

If we only use meshlets as a different way to package triangles but keep a CPU draw-submission mindset, we lose most of the benefit.

Meshlets should be part of a GPU-driven visibility and refinement system.

### 19.3 Ignoring boundary locks during simplification

This often causes visible cracks and is one of the classic simplification mistakes.

### 19.4 Using non-conservative bounds

If bounds are too tight and a visible node is rejected, the error is catastrophic. Conservative false positives are far safer than false negatives.

### 19.5 Letting error metrics become inconsistent

If hierarchy error values do not behave monotonically enough, refinement traversal becomes unstable and hard to reason about.

### 19.6 Overloading push constants

The current root signature is tiny for a reason. Meshlet rendering should scale through structured buffers, not through ever-growing push constant payloads.

## 20. Glossary

### Meshlet

A small cluster of triangles and the local vertex set needed to render them.

### Group

A set of nearby meshlets treated as a simplification and refinement unit.

### LOD0

The finest generated meshlet representation.

### Parent error

The geometric error introduced by using a coarser representation instead of refining to a finer one.

### Refinement

Replacing a coarse representation with a finer one because projected error is too large.

### DAG cut

The set of meshlets selected across the refinement structure that forms the final rendered detail frontier for the frame.

### HZB

Hierarchical Z buffer used for conservative occlusion testing.

### Material palette

A deduplicated list of materials shared across instances, referenced by `materialPaletteIndex`.

### `localMaterialIndex`

A mesh-local slot inside the material palette used by one meshlet.

## 21. Final mental model

If you only keep one model in your head, keep this one.

GhostEngine is moving from:

- draw whole object or submesh

to:

- build many small triangle clusters
- organize them into local refinement groups
- build a hierarchy over those groups
- let the GPU decide what is visible and how much detail is necessary
- shade materials later through bindless palette indirection

So the system is really three ideas working together:

1. meshlets give us fine-grained geometry units
2. hierarchy plus error metrics give us scalable visibility and LOD selection
3. material palettes let those units stay compact and GPU-friendly without polluting ECS layout

If we implement those three ideas consistently, the result should be a renderer that is both fast and understandable.

## 22. Concrete next steps after this document

After this document, the next code work should be:

1. add the real meshlet structs
2. replace the placeholder `MeshLet` path in `src/Runtime/Ghost.Graphics/Core/Mesh.cs`
3. reference `Ghost.MeshOptimizer` from `src/Runtime/Ghost.Graphics/Ghost.Graphics.csproj`
4. implement a CPU-side `MeshletBuilder`
5. upload meshlet data to GPU buffers
6. implement hierarchy-driven culling
7. integrate mesh shader VBuffer rendering

That order keeps the learning path clear: define the data, build the data, consume the data.
