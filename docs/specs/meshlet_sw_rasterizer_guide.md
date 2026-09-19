# Nanite-Style Work Graph Software Rasterizer & 64-bit Visibility Buffer Guide

This guide is an in-depth learning manual and architectural blueprint for implementing a **Nanite-style software rasterizer** for micro-meshlets in GhostEngine. It covers the mathematical foundations, GPU memory layout, Work Graph node execution, and the unified 0-RTV / 0-DSV hardware rasterizer integration.

---

## 1. Why Software Rasterize Small Meshlets?

Modern graphics hardware rasterizes primitives using fixed-function units that process triangles in **$2 \times 2$ pixel quads**. 

### The Quad Inefficiency Problem
When geometry is densely tessellated or viewed at distance:
- A triangle may only cover **1 or 2 pixels**.
- The hardware rasterizer still allocates an entire $2 \times 2$ quad (4 pixel shader lanes) to compute finite differences (`ddx`, `ddy`).
- The remaining 2–3 lanes are **helper lanes**—they execute the shader instructions to calculate gradients but their outputs are discarded.
- In extreme cases with micro-triangles, hardware ALU and rasterizer efficiency drops below **10–15%**.

### The Software Compute Rasterizer Solution
By shifting micro-meshlets (projected bounding radius $< 16$ pixels) to a Compute / Work Graph shader:
1. Triangles are evaluated analytically against pixel centers using integer arithmetic.
2. Pixel coverage is emitted directly without allocating quad footprints or helper lanes.
3. Pixels are committed into a **64-bit Visibility Buffer** using hardware 64-bit atomics (`InterlockedMax64`).
4. Large meshlets ($\ge 16$ pixels) remain on the hardware fixed-function rasterizer (Mesh Shader $\to$ Pixel Shader), where hardware rasterization is already peak-efficiency.

---

## 2. The Unified 64-Bit Visibility Buffer

Both the Software Rasterizer and the Hardware Rasterizer write to the **same 64-bit UAV buffer** using identical bit packing and atomic operations.

### Bit Layout

$$\mathbf{VBuffer64} = (\text{Depth} \ll 32) \mid (\text{VisibleSlot} \ll 8) \mid \text{PrimitiveID}$$

```
63                             32 31                    8 7            0
+--------------------------------+-----------------------+--------------+
|     Depth (asuint(depth))      |      VisibleSlot      | PrimitiveID  |
|            32 bits             |        24 bits        |    8 bits    |
+--------------------------------+-----------------------+--------------+
```

- **Depth (Bits 32–63)**: 32-bit floating point depth reinterpreted as an unsigned integer via `asuint(saturate(depth))`.
- **VisibleSlot (Bits 8–31)**: 24-bit index into the global `visibleMeshlets` buffer (identifies `instanceIndex` and `meshletIndex`). Up to 16.7 million visible meshlets.
- **PrimitiveID (Bits 0–7)**: 8-bit local triangle index inside the meshlet ($0 \dots 123$).

### The Reverse-Z Atomic Invariance

GhostEngine uses a **Reverse-Z** projection with `greater_equal` depth testing (Near $= 1.0$, Far $= 0.0$):
$$\text{Closer surface} \implies \text{Larger float value}$$

Because positive IEEE 754 floating-point numbers preserve their ordering when reinterpreted as unsigned 32-bit integers:
$$\text{depth}_A > \text{depth}_B \iff \text{asuint}(\text{depth}_A) > \text{asuint}(\text{depth}_B)$$

When placing the depth in the **upper 32 bits** of a 64-bit integer:
```hlsl
uint64_t packedValue = (((uint64_t)asuint(depth)) << 32) | (((uint64_t)visibleSlot) << 8) | ((uint64_t)primitiveID);
vbuffer.InterlockedMax64(byteAddress, packedValue);
```

#### Why this works without race conditions:
1. If the incoming depth is **closer** than the existing depth in the buffer, the upper 32 bits are strictly greater. `InterlockedMax64` updates both the depth and the payload in one atomic instruction.
2. If the incoming depth is **farther**, the upper 32 bits are smaller. `InterlockedMax64` ignores the value.
3. If depths are equal, the tie is broken deterministically by the lower 32 bits.
4. **Buffer Clear**: The buffer is cleared to `0`. Under Reverse-Z, `depth = 0.0` represents the far plane / background.

### Buffer Memory Addressing
For a screen with dimensions $W \times H$:
- Each pixel is 8 bytes (`uint64_t`).
- Total buffer size: $W \times H \times 8$ bytes.
- Linear byte address for pixel $(x, y)$:
  $$\text{byteAddress} = (y \times W + x) \times 8$$

---

## 3. High-Level Dataflow

```
                   [Scene Instances]
                           │
                           ▼
                 [InstanceCullNode]
                           │
                           ▼
               [HierarchyTraverseNode]
                           │
                           ▼
                  [MeshletCullNode]
                           │
         ┌─────────────────┴─────────────────┐
         │ (Frustum & HZB Visible)           │
         ▼                                   ▼
[R_pixel < 16 && !nearPlane]           [Large or near plane]
         │                                   │
         ▼                                   ▼
 [swRasterQueue Record]           [visibleMeshlets Buffer]
         │                                   │
         ▼                                   ▼
  [SWRasterizeNode]                       [MSMain]
 (Work Graph Broadcast)                 (Mesh Shader)
         │                                   │
         ▼                                   ▼
[SetupTriangle / Scanline]                [PSMain]
         │                          (0 RTV / 0 DSV)
         │                                   │
         └─────────────────┬─────────────────┘
                           │ InterlockedMax64
                           ▼
              [(Unified 64-bit VBuffer)]
```

---

## 4. Meshlet Classification & The Near-Plane Guard

Inside `MeshletCullNode` (in [`MeshletCullGraph.ggraph`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshletCullGraph.ggraph)):

### 1. Screen-Space Projected Radius
For each meshlet that survives frustum and HZB occlusion culling, calculate its bounding sphere in world space:
```hlsl
float3 meshletWorldCenter = mul(instance.localToWorld, float4(meshlet.boundingSphere.xyz, 1.0f)).xyz;
float meshletWorldRadius  = meshlet.boundingSphere.w * maxScale;
```

Compute the distance along the camera view direction ($Z_{\text{view}}$):
$$Z_{\text{view}} = \text{dot}(\text{meshletWorldCenter} - \text{view.cameraPosition}, \text{view.viewForward})$$

Project the radius to screen pixels:
$$R_{\text{pixel}} = \frac{\text{meshletWorldRadius} \times \text{proj}_{11} \times \text{screenHeight}}{2.0 \times Z_{\text{view}}}$$

### 2. The Near-Plane Clipping Trap (Nanite Rule)
> **Crucial Engine Insight**:
> If any portion of a triangle crosses behind the camera near plane ($W \le Z_{\text{near}}$), dividing by $W$ produces negative or infinite screen coordinates, causing geometry to mirror or explode across the screen.
>
> Hardware rasterizers have dedicated silicon to clip triangles against the near plane (producing 1 or 2 new triangles). In a compute shader, implementing homogeneous near-plane polygon clipping for arbitrary triangles adds severe divergence and overhead.
>
> **Nanite's Strategy**: If a meshlet's bounding sphere touches or crosses the near plane, **force it into the Hardware Rasterizer path**!

```hlsl
bool touchesNearPlane = (Z_view - meshletWorldRadius) <= view.nearPlane;
bool isSmallMeshlet   = (R_pixel < 16.0f) && !touchesNearPlane;
```

### 3. Slot Allocation
Every visible meshlet (whether HW or SW) requires an entry in `visibleMeshlets` so that downstream shading and material evaluation passes can look up `(instanceIndex, meshletIndex)` using `VisibleSlot`.

Allocate a slot atomically (using wave compaction):
```hlsl
// visibleSlot is the index into visibleMeshlets buffer
visibleMeshlets[visibleSlot] = entry;

if (isSmallMeshlet)
{
    // Emit record to Work Graph SW raster queue
    ThreadNodeOutputRecords<SWMeshletRecord> outRec = swRasterQueue.GetThreadNodeOutputRecords(1);
    outRec.Get().instanceIndex = rec.instanceIndex;
    outRec.Get().meshletIndex  = rec.meshletIndex;
    outRec.Get().visibleSlot   = visibleSlot;
    outRec.OutputComplete();
}
else
{
    // Allocate into HW draw list
}
```

---

## 5. Software Rasterizer Work Graph Node (`SWRasterizeNode`)

### Work Graph Node Signature
In [`MeshletCullGraph.ggraph`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshletCullGraph.ggraph):

```hlsl
struct SWMeshletRecord
{
    uint instanceIndex;
    uint meshletIndex;
    uint visibleSlot;
};

[Shader("node")]
[NodeLaunch("broadcasting")]
[NodeDispatchGrid(1, 1, 1)]
[NumThreads(64, 1, 1)]
void SWRasterizeNode(
    DispatchNodeInputRecord<SWMeshletRecord> inputRecord,
    uint groupThreadId : SV_GroupThreadID)
```

#### Why Broadcasting with Grid `(1, 1, 1)` and 64 Threads?
- Each record represents exactly **one meshlet**.
- A dispatch grid of `(1, 1, 1)` launches a single threadgroup of 64 threads for that meshlet.
- The GPU Work Graph runtime batches these threadgroups automatically, saturating compute units without CPU involvement or indirect argument buffer overhead.

### LDS (groupshared) Memory Layout
A GhostEngine meshlet has at most **64 vertices** and **124 triangles**.
```hlsl
groupshared float4 s_GroupVerts[64];
```
Each entry stores:
- `.xy`: Subpixel fixed-point screen coordinates.
- `.z`: Device depth in $[0.0, 1.0]$.
- `.w`: Perspective reciprocal $1 / W$.

---

## 6. Subpixel Precision and Fixed-Point Math

### Why Fixed-Point Subpixels?
Floating-point rasterization can suffer from precision jitter at triangle edges, leading to pixel gaps (cracks) or double-rasterization between adjacent triangles sharing an edge.

Fixed-point math solves this:
- Screen space is subdivided into subpixel units (e.g. $256$ subpixels per pixel = 8 bits of fractional precision).
- Triangle vertices snap to exact subpixel integers.
- Edge equations evaluate to exact integers without floating-point rounding errors.

### Viewport Transformation Formula
Given `clipPos = mul(worldViewProj, float4(pos, 1.0f))`:

1. Normalize to NDC:
   $$\text{ndc} = \frac{\text{clipPos.xyz}}{\text{clipPos.w}}$$
2. Map NDC to Subpixel Integer Coordinates:
   - NDC $X \in [-1, 1]$ maps to $X_{\text{pixel}} \in [0, W]$.
   - NDC $Y \in [-1, 1]$ maps to $Y_{\text{pixel}} \in [H, 0]$ (DirectX screen $Y$ points downwards).
   - Multiply by $256$ (`subpixelSamples`):
   $$\text{ViewportScale} = \left( \frac{W}{2} \times 256.0, \; -\frac{H}{2} \times 256.0 \right)$$
   $$\text{ViewportBias}  = \left( \frac{W}{2} \times 256.0, \;  \frac{H}{2} \times 256.0 \right)$$
   $$\text{Subpixel}_{xy} = \text{floor}(\text{ndc}_{xy} \times \text{ViewportScale} + \text{ViewportBias})$$
3. Depth and $1/W$:
   $$\text{Subpixel}_z = \text{ndc}_z = \frac{\text{clipPos.z}}{\text{clipPos.w}}$$
   $$\text{Subpixel}_w = \frac{1.0}{\text{clipPos.w}}$$

---

## 7. Inside the Triangle Rasterization Loop

### Thread-to-Triangle Mapping
Because a meshlet has up to 124 triangles and the threadgroup has 64 threads, threads loop in strides of 64:

```hlsl
for (uint triIdx = groupThreadId; triIdx < triangleCount; triIdx += 64)
{
    // 1. Fetch indices
    uint packedIndices = meshletTrianglesBuffer.Load((meshlet.triangleOffset + triIdx) * 4);
    uint3 indices = uint3(packedIndices & 0xFF, (packedIndices >> 8) & 0xFF, (packedIndices >> 16) & 0xFF);

    // 2. Fetch transformed vertices from LDS
    float4 verts[3] = {
        s_GroupVerts[indices.x],
        s_GroupVerts[indices.y],
        s_GroupVerts[indices.z]
    };

    // 3. Triangle Setup (Rasterizer.hlsl)
    RasterTriangle tri = SetupTriangle<256, true>(scissorRect, verts);

    // 4. Rasterization & Atomic Write
    if (tri.isValid)
    {
        uint pixelValue = (record.visibleSlot << 8) | (triIdx & 0xFFu);
        RasterizeTri_Adaptive(tri, vbuffer, pixelValue, renderWidth);
    }
}
```

### Triangle Setup Mechanics (`SetupTriangle`)
In [`Rasterizer.hlsl`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/Includes/Meshlet/Rasterizer.hlsl):

1. **Backface Culling**:
   $$\text{detXY} = \text{edge01.y} \times \text{edge20.x} - \text{edge01.x} \times \text{edge20.y}$$
   If $\text{detXY} \ge 0$, the triangle is back-facing.
2. **Bounding Box**:
   Finds `minPixel` and `maxPixel` from vertex subpixels, divided by 256 and clamped to the scissor rectangle. If `minPixel > maxPixel`, triangle covers no pixels and is discarded.
3. **Half-Edge Equations ($C_0, C_1, C_2$)**:
   Evaluates 2D cross products for edges.
4. **Top-Left Fill Rule**:
   Nudges edge constants by Direct3D top-left tie-breaking rule:
   ```hlsl
   tri.c0 -= saturate(tri.edge12.y + saturate(1.0f - tri.edge12.x));
   tri.c1 -= saturate(tri.edge20.y + saturate(1.0f - tri.edge20.x));
   tri.c2 -= saturate(tri.edge01.y + saturate(1.0f - tri.edge01.x));
   ```
5. **Analytical Depth Plane**:
   Computes plane gradients $\frac{\partial Z}{\partial x}$ and $\frac{\partial Z}{\partial y}$ so depth at any pixel can be evaluated with two multiply-adds:
   $$\text{depth}(x, y) = \text{depthPlane.x} + \text{depthPlane.y} \cdot C_1 + \text{depthPlane.z} \cdot C_2$$

### Rasterization Traversal: Rect vs Scanline
- **`RasterizeTri_Rect`**:
  Nested loop over $[x_{\min}, x_{\max}] \times [y_{\min}, y_{\max}]$ with incremental additions of edge derivatives. Ideal for small triangles ($1 \times 1$ to $4 \times 4$ pixels) due to zero setup overhead.
- **`RasterizeTri_Scanline`**:
  Calculates exact start and end $x$ coordinates per scanline by dividing edge constants by edge slopes. Prevents wasting iterations on long, skinny sliver triangles.
- **`RasterizeTri_Adaptive`**:
  Dynamically checks if triangle bounding width $> 4$ pixels (`WaveActiveAnyTrue(width > 4)`). If yes, uses Scanline; otherwise, uses Rect.

---

## 8. Hardware Rasterizer Integration (0 RTV / 0 DSV)

To keep the pipeline 100% unified, [`VisibilityBuffer.gshdr`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/VisibilityBuffer.gshdr) is configured with **zero render targets and zero depth-stencil views**.

### Pipeline Configuration
```hlsl
pipeline
{
    ztest  = off;    // No hardware DSV! Depth test is performed via InterlockedMax64
    zwrite = off;
    cull   = back;   // Hardware front-end still culls backfaces
    blend  = off;
}
```

### Mesh Shader (`MSMain`)
Transfers vertices to clip space and passes `visibleSlot` down as flat parameter:
```hlsl
struct PixelInput
{
    float4 position : SV_POSITION;
    nointerpolation uint visibleSlot : TEXCOORD0;
};

// Inside MSMain:
outVerts[groupThreadID.x].position    = mul(worldViewProj, float4(pos, 1.0f));
outVerts[groupThreadID.x].visibleSlot = visibleSlot;
```

### Pixel Shader (`PSMain`)
Returns `void`, binds the 64-bit V-Buffer UAV, and uses hardware interpolated depth:
```hlsl
void PSMain(PixelInput input, uint primitiveID : SV_PrimitiveID)
{
    uint2 pixelPos = (uint2)input.position.xy;
    float depth    = saturate(input.position.z); // Interpolated by HW rasterizer

    uint byteAddress     = (pixelPos.y * renderWidth + pixelPos.x) * 8;
    uint64_t packedValue = (((uint64_t)asuint(depth)) << 32) |
                           (((uint64_t)input.visibleSlot) << 8) |
                           ((uint64_t)(primitiveID & 0xFFu));

    vbuffer.InterlockedMax64(byteAddress, packedValue);
}
```

---

## 9. Resolving the Visibility Buffer in Later Passes

In downstream deferred shading passes (e.g. Material Shading or G-Buffer export):

1. **Load 64-bit Pixel**:
   ```hlsl
   uint64_t rawPixel = vbuffer.Load<uint64_t>(pixelByteAddress);
   if (rawPixel == 0)
   {
       // Background / Sky
       return;
   }

   float depth      = asfloat((uint)(rawPixel >> 32));
   uint visibleSlot = (uint)((rawPixel >> 8) & 0x00FFFFFFu);
   uint primitiveID = (uint)(rawPixel & 0xFFu);
   ```
2. **Look Up Meshlet & Instance**:
   ```hlsl
   VisibleMeshletEntry entry = visibleMeshlets[visibleSlot];
   InstanceData instance     = instanceBuffer[entry.instanceIndex];
   Meshlet meshlet           = meshletBuffer[entry.meshletIndex];
   ```
3. **Reconstruct Barycentrics & Attributes**:
   - Fetch the 3 triangle vertex indices using `meshletTrianglesBuffer.Load(meshlet.triangleOffset + primitiveID)`.
   - Reconstruct perspective-correct barycentrics from screen coordinates and vertex clip positions.
   - Interpolate UVs, normals, and material indices without ever having stored them in a fat G-Buffer.

---

## 10. Checklist for Implementation

### File 1: [`Rasterizer.hlsl`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/Includes/Meshlet/Rasterizer.hlsl)
- [x] Fix `WritePixel`:
  - Change line 134 from `(y * 64 + x) * 16` to `(pixelPos.y * renderWidth + pixelPos.x) * 8`.
  - Pack `uint64_t packedValue = (((uint64_t)asuint(depth)) << 32) | (uint64_t)pixelValue`.
- [x] Pass `renderWidth` as parameter through `RasterizeTri_Adaptive`, `RasterizeTri_Rect`, and `RasterizeTri_Scanline`.

### File 2: [`MeshletCullGraph.ggraph`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/MeshletCullGraph.ggraph)
- [ ] Add `SWMeshletRecord` struct (`instanceIndex`, `meshletIndex`, `visibleSlot`).
- [ ] In `MeshletCullNode`:
  - Calculate `Z_view` and `R_pixel`.
  - Guard near-plane: `(Z_view - meshletWorldRadius) <= view.nearPlane`.
  - Route small meshlets to `swRasterQueue`.
- [ ] Implement `SWRasterizeNode`:
  - `[Shader("node")]`, `[NodeLaunch("broadcasting")]`, `[NodeDispatchGrid(1, 1, 1)]`, `[NumThreads(64, 1, 1)]`.
  - Cache transformed subpixel vertices in `groupshared float4 s_GroupVerts[64]`.
  - Stride over triangles, run `SetupTriangle`, and call `RasterizeTri_Adaptive`.

### File 3: [`VisibilityBuffer.gshdr`](file:///F:/csharp/GhostEngine/src/Runtime/Ghost.Engine/Assets/EngineResources/Shaders/VisibilityBuffer.gshdr)
- [ ] Update pipeline block (0 RTV, 0 DSV, `ztest = off`, `zwrite = off`).
- [ ] Update `PixelInput` to carry `nointerpolation uint visibleSlot`.
- [ ] Change `PSMain` return type to `void` and execute `vbuffer.InterlockedMax64`.
