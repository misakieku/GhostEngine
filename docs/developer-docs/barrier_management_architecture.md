# Barrier Management &amp; GPU Synchronization Architecture

**Module**: `Ghost.Graphics.RHI`, `Ghost.Graphics.D3D12`, `Ghost.Graphics.RenderGraphModule`  
**Target Platform**: .NET 10 / Windows (DirectX 12 Enhanced Barriers / Vulkan synchronization2)  
**Location**: `src/Runtime/Ghost.Graphics.RHI`, `src/Runtime/Ghost.Graphics.D3D12`, `src/Runtime/Ghost.Graphics/RenderGraphModule`

---

## 1. Overview &amp; Architecture Philosophy

Modern low-level graphics APIs (DirectX 12 Agility SDK / `ID3D12GraphicsCommandList7::Barrier`, Vulkan `VK_KHR_synchronization2`) shifted resource barrier architecture from driver-implicit/legacy resource states to fine-grained, explicit **Execution Synchronization (`Sync`)**, **Memory Access (`Access`)**, and **Texture Memory Layout (`Layout`)**.

GhostEngine adopts a **100% Stateless RHI** coupled with a **Contract-Driven Render Graph Compiler**:

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ High-Level Pass Authors & TAs                                                          │
│   • Pure Declarative Intent: builder.UseTexture(tex, AccessFlags.Read)                 │
│   • Zero raw barrier calls, zero knowledge of hardware sync flags                      │
└───────────────────────────────────────────┬────────────────────────────────────────────┘
                                            │ Compiled into DAG
                                            ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ Render Graph Compiler (Compile-Time Transient Tracking)                                │
│   • Compiles RAW/WAR/WAW dependencies and queue handoffs into binary command stream    │
│   • Enforces boundary contracts (initialState -> finalState) on imported resources     │
│   • Emits explicit (sourceState -> targetState) barrier descriptors                    │
└───────────────────────────────────────────┬────────────────────────────────────────────┘
                                            │ Binary Opcode Stream (IssueBarriers)
                                            ▼
┌────────────────────────────────────────────────────────────────────────────────────────┐
│ Stateless RHI Layer (D3D12CommandBuffer / VulkanCommandBuffer)                         │
│   • Pure command emission: converts BarrierDesc directly to D3D12_TEXTURE_BARRIER      │
│   • ZERO resource database lookups, ZERO mutable state tracking, fully thread-safe     │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

### Why Global Runtime Tracking in RHI Was Deprecated

Early D3D12 engines tracked states in a central database (`record.barrierData`). GhostEngine explicitly removed this pattern due to critical architectural flaws:

1. **Multi-Threaded Recording Race Conditions**: Parallel command list recording cannot safely mutate central database state without expensive locking and non-deterministic execution order.
2. **Memory Overhead**: 95% of GPU resources (static vertex/index buffers, immutable textures, transient G-buffers) never require dynamic state tracking. Storing state in every resource record wastes cache lines.
3. **Subresource State Explosion**: Tracking per-mip and per-slice state inside central databases requires heap allocations (`State[]`) for every texture.
4. **Leaky API Splitting**: Engines with runtime tracking often split their API into `Barrier.Texture` (implicit) vs `Barrier.TextureExplicit`, creating architectural schizophrenia.

---

## 2. Core Data Structures &amp; Types

### 2.1 `ResourceBarrierData` (`IResourceDatabase.cs`)

Represents an immutable snapshot of a resource's hardware state:

```csharp
public struct ResourceBarrierData
{
    public BarrierLayout layout;
    public BarrierAccess access;
    public BarrierSync sync;

    public ResourceBarrierData(BarrierLayout layout, BarrierAccess access, BarrierSync sync)
    {
        this.layout = layout;
        this.access = access;
        this.sync = sync;
    }
}
```

### 2.2 `BarrierDesc` (`Common.cs`)

An explicit command descriptor passed to `ICommandBuffer.Barrier(...)`. It mandates both `before` and `after` states:

```csharp
public struct BarrierDesc
{
    public BarrierType Type;                     // Global, Buffer, Texture
    public BarrierSync SyncBefore, SyncAfter;
    public BarrierAccess AccessBefore, AccessAfter;
    public BarrierLayout LayoutBefore, LayoutAfter;
    public Handle[[ORCA_RICH_MD:9ee07d75f182daa4b35fa27840d8d46a:inline-html:%3CGPUResource%3E]] Resource;
    public BarrierSubresourceRange Subresources;
    public BarrierHandoffType Handoff;           // None, Release, Acquire
    public bool Discard;                         // Discards previous contents (D3D12_TEXTURE_BARRIER_FLAG_DISCARD)
    public bool IsAliasing;                      // True if memory is reused across aliased resources
    public bool Force;                           // Forces barrier emission even if states match (e.g. UAV sync)
}
```

### 2.3 `BarrierSubresourceRange` (`Common.cs`)

Defines the mip, array slice, and plane subresource target for texture transitions:

- `BarrierSubresourceRange.AllSubresources = 0xFFFFFFFF` (`D3D12_BARRIER_SUBRESOURCE_RANGE_ALL`): Targets all subresources (whole texture).
- `BarrierSubresourceRange.Single(subresourceIndex)`: Targets an individual subresource index.
- `BarrierSubresourceRange.Range(firstMip, numMips, firstSlice, numSlices)`: Targets a specific rectangular subresource box.

---

## 3. Intra-Queue Barrier Management (Single Queue)

Within a single hardware queue (e.g. Direct / Graphics Queue), the Render Graph compiler manages transitions between consecutive passes.

### 3.1 Initial Transient State &amp; Discard Flag

Transient resources (allocated on aliased placed heaps) begin the frame in an uninitialized state.

- **Rule**: When `resourceState.isValid` is `false` (first pass using the transient resource in a frame), the compiler sets:
  ```csharp
  sourceState = new ResourceBarrierData(BarrierLayout.Undefined, BarrierAccess.NoAccess, BarrierSync.None);
  flags |= BarrierFlags.FirstUsage | BarrierFlags.Discard;
  ```
- **D3D12 Mapping**: Mapped to `D3D12_BARRIER_LAYOUT_UNDEFINED` (`0xFFFFFFFF`) with `D3D12_TEXTURE_BARRIER_FLAG_DISCARD`. This instructs the GPU and driver to discard previous memory contents, permitting a legal transition from *any* previous layout on *any* queue without state mismatch errors.

### 3.2 Memory Aliasing Barriers

When two transient resources share the same physical heap offset at non-overlapping schedule times:

1. Resource $A$ finishes execution in Pass $N$.
2. Resource $B$ begins execution in Pass $M > N$ at the same heap offset.
3. The compiler detects the overlap via `AliasingPlan` and emits an aliasing barrier:
  - `LayoutBefore = BarrierLayout.Undefined`
  - `AccessBefore = BarrierAccess.NoAccess`
  - `Discard = true`, `IsAliasing = true`
  - In D3D12, `pResource` points to Resource $B$, and the runtime ensures cache flushing between aliased memory ranges.

### 3.3 UAV Ordering &amp; Same-State Sync

When two consecutive passes write to the same UAV buffer or texture without changing layout (`UnorderedAccess` $\rightarrow$ `UnorderedAccess`):

- `hasSameState` is `true`.
- The compiler sets `flags |= BarrierFlags.Force`.
- D3D12 emits a pipeline barrier (`SyncBefore = ComputeShading, SyncAfter = ComputeShading, AccessBefore = UnorderedAccess, AccessAfter = UnorderedAccess`) ensuring back-to-back shader writes serialize correctly without data hazard races.

---

## 4. Cross-Queue Synchronization &amp; Queue Handoffs (Async Compute)

DirectX 12 requires explicit queue ownership transfer when a resource written on Queue $A$ (e.g. Compute) is read on Queue $B$ (e.g. Graphics).

```
Producer Queue (Compute)                           Consumer Queue (Graphics)
────────────────────────                           ────────────────────────
[Pass 1: UAV Write]
       │
       ▼
[Release Barrier]
 LayoutBefore: UnorderedAccess
 LayoutAfter:  Common (or Present)
 SyncAfter:    None
 AccessAfter:  NoAccess
 Handoff:      Release
       │
       ▼
[Signal Fence (Value = V)] ──────────────────────► [Wait Fence (Value = V)]
                                                          │
                                                          ▼
                                                   [Acquire Barrier]
                                                    LayoutBefore: Common (or Present)
                                                    LayoutAfter:  ShaderResource
                                                    SyncBefore:   None
                                                    AccessBefore: NoAccess
                                                    Handoff:      Acquire
                                                          │
                                                          ▼
                                                   [Pass 2: SRV Read]
```

### 4.1 Cross-Queue Barrier Rules (Direct3D 12 Enhanced Barriers)

1. **Release Barrier (on Producer Queue)**:
  - Must set `SyncAfter = D3D12_BARRIER_SYNC_NONE`.
  - Must set `AccessAfter = D3D12_BARRIER_ACCESS_NO_ACCESS`.
  - Must transition `LayoutAfter` to `D3D12_BARRIER_LAYOUT_COMMON` (or `PRESENT`).
  - `Handoff = BarrierHandoffType.Release`.
2. **GPU Fence Sync**:
  - The producer queue signals a hardware fence upon command buffer completion.
  - The consumer queue executes `queue.Wait(fence, fenceValue)` before executing its acquiring command buffer.
3. **Acquire Barrier (on Consumer Queue)**:
  - Must set `SyncBefore = D3D12_BARRIER_SYNC_NONE`.
  - Must set `AccessBefore = D3D12_BARRIER_ACCESS_NO_ACCESS`.
  - Must transition from `LayoutBefore = D3D12_BARRIER_LAYOUT_COMMON` to `targetState.layout`.
  - `Handoff = BarrierHandoffType.Acquire`.

---

## 5. Boundary State Contracts (Import &amp; Export)

To prevent pass authors from needing to know hardware sync states while guaranteeing correct pipeline boundaries, external resources use **State Contracts**.

### 5.1 The Contract Model on `ImportTexture` / `ImportBuffer`

```csharp
public Identifier<RGTexture> ImportTexture(
    Handle<GPUTexture> texture,
    ResourceBarrierData? initialBarrierState = null,
    ResourceBarrierData? finalBarrierState = null,
    Color128 clearColor = default,
    float clearDepth = 1.0f,
    byte clearStencil = 0,
    bool clearAtFirstUse = false,
    bool discardAtLastUse = false)
```

### 5.2 Common State Contracts

#### 1. Swapchain Backbuffer Contract

- Direct3D 12 presentation requires the swapchain image to be in `D3D12_BARRIER_LAYOUT_PRESENT` (`D3D12_RESOURCE_STATE_PRESENT`) before calling `IDXGISwapChain::Present`.
- **Pipeline Declaration**:
  ```csharp
  var backBuffer = rg.ImportTexture(
      backBufferHandle,
      initialBarrierState: new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None),
      finalBarrierState: new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None));
  ```
- **Execution Flow**:
  1. Pass 1 transitions `backBuffer` from `Present` $\rightarrow$ `RenderTarget`.
  2. Native render pass draws geometry into `backBuffer`.
  3. `EmitClosingBarriers` compiler phase automatically checks `currentState.state != finalBarrierState` and emits `RenderTarget` $\rightarrow$ `Present`.
  4. Swapchain presents the backbuffer with zero additional API calls.

#### 2. History Buffer Ping-Pong Contract (TAA / Motion Vectors)

- **Frame **$N$**:**
  - `History_Read` imported with `initialBarrierState: ShaderResource`. No prologue barrier needed.
  - `History_Write` imported with `finalBarrierState: ShaderResource`. Render pass writes as UAV / RenderTarget.
  - End of frame: `EmitClosingBarriers` transitions `History_Write` from `RenderTarget/UAV` $\rightarrow$ `ShaderResource`.
- **Frame **$N+1$**:**
  - Pointers ping-pong. `History_Write` from Frame $N$ is imported as `History_Read` for Frame $N+1$, already in `ShaderResource` layout.

---

## 6. Native Render Pass Integration &amp; Load/Store Invariants

When rendering with native D3D12 Render Passes (`ID3D12GraphicsCommandList4::BeginRenderPass`):

### 6.1 Layout Invariant Before `BeginRenderPass`

- Color render targets **must** be in `D3D12_BARRIER_LAYOUT_RENDER_TARGET` before calling `BeginRenderPass`.
- Depth/stencil targets **must** be in `D3D12_BARRIER_LAYOUT_DEPTH_STENCIL_WRITE` or `DEPTH_STENCIL_READ` before calling `BeginRenderPass`.
- All prologue transitions must be executed before `BeginRenderPass`. Calling barrier commands inside an active render pass is illegal in hardware.

### 6.2 Load &amp; Store Operation Inference

In `RenderGraphNativePassBuilder.InferLoadStoreOps`:

```csharp
// Store Op Inference
if (resourceOrdering.GetLastUseScheduleIndex(resource.index) == lastScheduleIndex)
{
    if (!resource.rgTextureDesc.discardAtLastUse)
    {
        attachment.storeOp = AttachmentStoreOp.Store;    // D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_PRESERVE
    }
    else
    {
        attachment.storeOp = AttachmentStoreOp.DontCare; // D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_DISCARD
    }
}
```

---

## 7. Lessons Learned &amp; Failure Mode Post-Mortem

This section documents real-world bugs encountered and resolved during engine development to prevent regressions.

### Failure 1: Incompatible Barrier Values (`D3D12 ERROR #1331`)

- **Symptom**:
  ```
  D3D12 ERROR: ID3D12CommandList::Barrier: AccessBefore bits D3D12_BARRIER_ACCESS_SHADER_RESOURCE
  are incompatible with LayoutBefore D3D12_BARRIER_LAYOUT_RENDER_TARGET.
  [ STATE_SETTING ERROR #1331: INCOMPATIBLE_BARRIER_VALUES]
  ```
- **Cause**: In D3D12 Enhanced Barriers, access flags must strictly match the permitted access masks for a given layout. Combining `AccessBefore = RenderTarget | ShaderResource` with `LayoutBefore = RenderTarget` is invalid because `ShaderResource` access cannot occur in `RenderTarget` layout.
- **Solution**: Pass strictly the valid access bit for the specified layout (`AccessBefore = RenderTarget` for `LayoutBefore = RenderTarget`).

### Failure 2: C# Enum Default Value `0` (`COMMON`) vs `UNDEFINED` (`-1`)

- **Symptom**:
  ```
  D3D12 ERROR: Barrier layout(D3D12_BARRIER_LAYOUT_COMMON) does not match expected layout
  (D3D12_BARRIER_LAYOUT_SHADER_RESOURCE) using COMPUTE command list.
  [ STATE_SETTING ERROR #1334: INCOMPATIBLE_BARRIER_LAYOUT]
  ```
- **Cause**: In C#, uninitialized structs have fields set to `0`. For `BarrierLayout`, `0` is `Common` / `Present`, while `Undefined` is `-1`. Uninitialized `resourceStates` entries evaluated to `LayoutBefore = Common` instead of `Undefined`. Because `Discard` was not set, D3D12 validated the layout against the resource's previous state from the Graphics queue.
- **Solution**: Explicitly initialize uninitialized resource states to `CompiledResourceState.Undefined` (`layout = BarrierLayout.Undefined`, `sync = None`, `access = NoAccess`) and attach `BarrierFlags.FirstUsage | BarrierFlags.Discard`.

### Failure 3: Premature Discard on Native Render Pass End

- **Symptom**:
  ```
  D3D12 ERROR: Barrier layout(D3D12_BARRIER_LAYOUT_RENDER_TARGET), pResource = 'SwapChain_BackBuffer_1',
  does not match expected layout (D3D12_BARRIER_LAYOUT_COMMON).
  [ STATE_SETTING ERROR #1334: INCOMPATIBLE_BARRIER_LAYOUT]
  ```
- **Cause**: `ImportTexture` defaulted `discardAtLastUse = true`. On the last raster pass, `InferLoadStoreOps` set `EndingAccess.Type = D3D12_RENDER_PASS_ENDING_ACCESS_TYPE_DISCARD`. In D3D12, ending a render pass with `DISCARD` resets the texture tracking layout back to `COMMON`. When `EmitClosingBarriers` subsequently tried to transition from `RenderTarget` $\rightarrow$ `Present`, D3D12 failed because the texture was already discarded to `COMMON`.
- **Solution**: Forced `attachment.storeOp = AttachmentStoreOp.Store` (`PRESERVE`) for all `resource.isImported` and `resource.isExtracted` textures, and defaulted `discardAtLastUse = false` on `ImportTexture`.

### Failure 4: Submitting Empty Command Lists (`D3D12 WARNING #1356`)

- **Symptom**:
  ```
  D3D12 WARNING: ExecuteCommandLists references command lists that have recorded only Barrier commands.
  [ EXECUTION WARNING #1356: NON_OPTIMAL_BARRIER_ONLY_EXECUTE_COMMAND_LISTS]
  ```
- **Cause**: `RenderEngine` unconditionally submitted `preludeCmd` every frame even when no streaming uploads or prelude draws occurred (`CommandCount == 0`), or submitted standalone `epilogueCmd` buffers containing only present transitions.
- **Solution**: Guard submission with `if (cmd.State.CommandCount > 0)` and let the Render Graph's internal closing barrier handle the present transition at the tail of the existing graphics submission.

---

## 8. Summary Best Practices Checklist

1. **Keep RHI Stateless**: Never add mutable tracking state to `IResourceDatabase` or `D3D12ResourceDatabase`.
2. **Use Boundary Contracts**: Always specify `initialBarrierState` and `finalBarrierState` when importing persistent resources (backbuffers, history buffers).
3. **Always Discard Ephemeral Resources on First Use**: Ensure transient resources emit `BarrierLayout.Undefined` with `BarrierFlags.Discard` on their first pass.
4. **Never Discard Imported or Extracted Textures**: Ensure `InferLoadStoreOps` selects `AttachmentStoreOp.Store` (`PRESERVE`) for any resource whose lifetime extends beyond the graph.
5. **No Empty Command List Submissions**: Check `CommandCount > 0` before submitting standalone prelude or utility command buffers to the GPU scheduler.

