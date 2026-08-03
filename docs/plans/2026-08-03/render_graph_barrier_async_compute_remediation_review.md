# Render Graph Barrier and Async Compute Remediation Review

**Status**: Proposed
**Scope**: `Ghost.Graphics.RenderGraphModule`, D3D12 command submission, render graph diagnostics, and focused render graph tests
**Related documentation**: [Render Graph Architecture and Developer Guide](../developer-docs/render_graph_architecture.md)

## 1. Executive Summary

The inspected render graph command stream exposes two classes of problems:

1. A pass can emit multiple transitions for the same whole resource, including incompatible Shader Resource View (SRV) and Unordered Access View (UAV) target states.
2. A compute pass marked for async compute can force a Graphics -> Compute -> Graphics queue round trip even when there is no independent graphics work to overlap.

The first problem is a resource-state correctness defect. The second is initially visible as a performance problem, but review of the executor and D3D12 backend found more serious correctness defects in the current multi-queue path:

- A source queue fence is signaled before the source command list is submitted.
- Command lists are submitted while still recording.
- Command buffers and allocators are not segmented or reset between queue batches.
- Fence values start at `1` in the compiler and are reused from the cached command stream.
- The graph does not explicitly submit its final queue segment.
- A graph ending on compute has no complete close and submission path.

The recommended remediation is deliberately split into two tracks.

### Immediate correctness track

- Canonicalize declarations so repeated use of one resource does not create repeated list entries.
- Resolve all declarations for a pass into one effective usage per resource before emitting barriers.
- Emit no more than one transition per resource and subresource range in a barrier batch.
- Reject incompatible simultaneous usages that cannot be represented by one state.
- Fold aliasing first-use information into the resource's resolved transition.
- Treat `EnableAsyncCompute(true)` as a scheduling request and temporarily execute all compute passes on the Graphics/Direct queue.
- Remove render-graph queue-switch opcodes while the multi-queue path is disabled.

### Follow-up async compute track

- Compile dependency-aware queue batches rather than switching queues based on adjacent pass types.
- Schedule compute asynchronously only when a real overlap window exists.
- Record each submitted queue batch into a closed command list with a valid allocator lifetime.
- Allocate monotonic fence values at execution time rather than storing absolute values in the compilation cache.
- Make final submission and presentation dependencies explicit.

The immediate track should be completed and verified before true async compute is re-enabled.

## 2. Scope and Non-Goals

### 2.1 In scope

- Pass resource declaration deduplication.
- Per-pass resource usage resolution.
- Barrier validation and serialization.
- Transient aliasing first-use transitions.
- Effective queue selection for compute passes.
- Render graph command stream diagnostics.
- Render graph unit tests and D3D12 validation coverage.
- Design requirements for a later multi-queue scheduler.

### 2.2 Not in scope for the immediate patch

- Full texture mip, array slice, or plane-level dependency tracking.
- Automatic pass splitting when one pass needs incompatible states at different times.
- Copy queue scheduling.
- Cross-frame async compute.
- Reordering passes to manufacture overlap opportunities.
- GPU performance heuristics based on measured pass duration.
- A full replacement of the binary command stream format.

Subresource-aware barriers are a prerequisite for passes that simultaneously read one mip and write another mip of the same logical texture. Until that model exists, the compiler must conservatively treat each logical resource as one whole-resource range.

## 3. Observed Command Stream

Two representative barrier batches are:

```text
IssueBarriers (3 barriers)
  Transition: VisibleBuffer -> Layout: Undefined,
              Access: ShaderResource,
              Sync: PixelShading | NonPixelShading
  Transition: VisibleBuffer -> Layout: UnorderedAccess,
              Access: UnorderedAccess,
              Sync: ComputeShading
```

```text
IssueBarriers (3 barriers)
  Transition: HDR_Lighting -> Layout: ShaderResource,
              Access: ShaderResource,
              Sync: PixelShading | NonPixelShading
  Transition: HDR_Lighting -> Layout: UnorderedAccess,
              Access: UnorderedAccess,
              Sync: ComputeShading
```

The queue bounce around `VolumetricFog_Inject` is:

```text
SignalFence -> Queue: Graphics, FenceValue: 5
SubmitQueue -> Queue: Graphics
GPUWait -> Queue: Compute, FenceValue: 5
ExecutePass #15 'VolumetricFog_Inject' [Compute]
SignalFence -> Queue: Compute, FenceValue: 6
SubmitQueue -> Queue: Compute
GPUWait -> Queue: Graphics, FenceValue: 6
```

No graphics work is submitted between launching the compute pass and waiting for it. The compute pass therefore cannot overlap graphics work in this schedule.

## 4. Diagnostic Limitation

The human-readable dump prints resource names but not logical resource identifiers or subresource ranges. A repeated name is not proof that two entries refer to the same resource because callers can create multiple resources with the same name.

For example, multiple bloom resources may all be named `Bloom_Down`. Two SRV entries with that name could be two distinct logical textures. An SRV and UAV pair produced by one `AccessFlags.ReadWrite` declaration is still a confirmed compiler defect, but future investigation should not depend on display names alone.

The dump should identify a resource as:

```text
HDR_Lighting [Texture #17, Mips: All, Slices: All]
```

For buffers:

```text
VisibleBuffer [Buffer #9, Bytes: All]
```

This change is diagnostic only and does not add subresource tracking.

## 5. Current Barrier Compilation

Barrier generation is split across these methods:

- [`RenderGraphBuilder.UseResource`](../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphBuilder.cs) records reads and writes independently.
- [`RenderGraphCompiler.EmitBarriersForPass`](../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphCompiler.Barrier.cs) serializes aliasing and implicit transitions into one `IssueBarriers` batch.
- `EmitAliasingBarriers` emits first-use alias/discard barriers.
- `EmitImplicitTransitions` independently emits generic reads, attachments, random-access usages, compute writes, and unsafe writes.
- `AddTransition` appends a `CompiledBarrier` without checking whether the resource already has a transition in the current batch.
- [`RenderGraphExecutor.ExecuteBarrierBatch`](../../src/Runtime/Ghost.Graphics/RenderGraphModule/RenderGraphExecutor.cs) translates every compiled descriptor and forwards it to the active command buffer.
- [`D3D12CommandBuffer.Barrier`](../../src/Runtime/Ghost.Graphics.D3D12/D3D12CommandBuffer.cs) groups descriptors into enhanced D3D12 barrier groups.

There is no canonical per-pass usage record between declaration and serialization.

## 6. Detailed Findings

### 6.1 Finding RG-BARRIER-001: incompatible transitions for one resource

**Severity**: High
**Category**: Correctness

`UseResource(resource, AccessFlags.ReadWrite, type)` adds the same resource to both `resourceReads` and `resourceWrites`.

For a compute pass, `EmitImplicitTransitions` then performs these independent actions:

1. The read loop emits an SRV state.
2. The compute write loop emits a UAV state.

Both descriptors appear before the same `ExecutePass` command. No GPU work separates the transitions, and both descriptors can be submitted in one D3D12 enhanced-barrier group.

#### Why this is invalid

A whole resource cannot be in both `ShaderResource` and `UnorderedAccess` layout/access states for the same pass. A shader that reads and writes an RW resource uses UAV state. `AccessFlags.ReadWrite` describes the shader's behavior, not two sequential resource layouts.

#### Required behavior

For a compute `ReadWrite` resource:

```text
Transition: Resource -> Layout: UnorderedAccess,
                        Access: UnorderedAccess,
                        Sync: ComputeShading
```

Only one transition should be emitted.

### 6.2 Finding RG-BARRIER-002: duplicate declarations are preserved

**Severity**: Medium
**Category**: Correctness robustness and command overhead

`UseResource` appends to read and write lists without checking whether the pass already declared that access. Repeated builder calls therefore create repeated DAG processing and repeated barrier candidates.

Random-access registration also calls `UseResource(..., ReadWrite, ...)` and then appends to `randomAccess`. The compiler contains ad hoc suppression logic for some read cases, but this does not establish a general uniqueness invariant.

#### Required behavior

Within one pass and resource type:

- A resource appears at most once in the read list.
- A resource appears at most once in the write list.
- A resource appears at most once in the random-access list.
- A `ReadWrite` resource remains in both read and write sets because the DAG needs both semantics.

### 6.3 Finding RG-BARRIER-003: aliasing and target-state transitions are separate

**Severity**: Medium
**Category**: Barrier correctness and simplification

A first write to an aliased transient can receive:

1. An aliasing/discard descriptor targeting `Undefined` and `NoAccess`.
2. A normal transition targeting the actual RT, depth, SRV, or UAV state.

Both are placed in the same `IssueBarriers` batch. The backend already supports an aliasing descriptor whose before-state is treated as `Undefined`/`NoAccess` and whose after-state is the descriptor target.

#### Required behavior

The first-use transition should carry:

- The final required target state.
- `FirstUsage` and `Discard` flags.
- The aliasing predecessor identity when one exists.

This permits one transition directly from the aliased before-state to the required after-state.

### 6.4 Finding RG-BARRIER-004: unsafe writes are assigned render-target state indiscriminately

**Severity**: High
**Category**: Incorrect state inference

For an unsafe pass, every generic write is currently assigned:

```text
Layout: RenderTarget
Access: RenderTarget
Sync: RenderTarget
```

This applies to both textures and buffers. A buffer cannot be a render target. A texture write is also not necessarily a render-target operation; it may be UAV, copy destination, depth, or another explicitly managed operation.

#### Required behavior

The immediate patch should not silently infer render-target state for arbitrary unsafe writes.

Recommended policy:

- `UseRandomAccessTexture` and `UseRandomAccessBuffer` resolve to UAV.
- Explicit attachment APIs resolve to attachment state.
- A generic unsafe `Write` without a usage class fails validation, or uses a new explicit unsafe usage API introduced for the intended state.

This behavior change should be made deliberately because existing unsafe passes may currently rely on the incorrect implicit render-target assumption. Call sites must be audited before enabling strict validation.

### 6.5 Finding RG-BARRIER-005: no subresource identity exists in pass usage

**Severity**: Medium
**Category**: Design limitation

Current pass declarations identify a logical texture but do not identify mip, array slice, or plane ranges. Therefore the compiler cannot distinguish:

- Reading mip N while writing mip N+1.
- Reading one array slice while rendering to another.
- Reading depth while writing stencil, when supported.

The repeated bloom sequence may represent multiple logical textures, but if it represents one mipmapped texture, the current graph cannot model it safely.

#### Immediate policy

Treat every declared resource as a whole-resource access. Reject incompatible states for that resource within a pass.

#### Future policy

Add a `RGSubresourceRange` to usage declarations, usage keys, dependency analysis, barrier compilation, and diagnostics. The uniqueness key then becomes `(resourceId, subresourceRange)` rather than only `resourceId`.

### 6.6 Finding RG-QUEUE-001: queue switching uses adjacency, not dependencies

**Severity**: Medium
**Category**: Scheduling and performance

`BuildExecutionCommands` chooses the queue from only:

```csharp
pass.asyncCompute && pass.type == RenderPassType.Compute
```

It switches queues whenever that value differs from the previous pass. This does not inspect:

- The last graphics producer required by the compute pass.
- The first graphics consumer of the compute output.
- Independent graphics passes that can run between those boundaries.
- Whether multiple compute passes can be grouped.
- Estimated submission and wait cost.

An isolated async pass therefore creates two serialized queue transitions.

### 6.7 Finding RG-QUEUE-002: fence signal precedes source submission

**Severity**: Critical
**Category**: Cross-queue correctness

The compiled order is:

```text
SignalFence(source)
SubmitQueue(source)
GPUWait(destination)
```

D3D12 queue operations execute in queue order. Signaling before submitting means the destination wait covers work before the signal, not the source command list submitted afterward.

The minimum valid queue order is conceptually:

```text
CloseCommandList(source batch)
SubmitQueue(source batch)
SignalFence(source, value)
GPUWait(destination, value)
```

This ordering is necessary but not sufficient; valid command-list and allocator lifetimes are also required.

### 6.8 Finding RG-QUEUE-003: recording command lists are submitted

**Severity**: Critical
**Category**: D3D12 API correctness

Callers begin the graphics and compute command buffers before `CompileAndExecute`. `RenderGraphExecutor` handles `SubmitQueue` by immediately calling `ICommandQueue.Submit`, but it never calls `ICommandBuffer.End`.

`D3D12CommandBuffer.End` is the operation that calls `ID3D12GraphicsCommandList::Close`. D3D12 requires an executed command list to be closed.

The mock queues accept this behavior, so the existing async test does not expose the native API violation.

### 6.9 Finding RG-QUEUE-004: command-list segments have no allocator lifecycle

**Severity**: Critical
**Category**: D3D12 resource lifetime

Switching the active context pointer is not equivalent to starting a new command-list segment. The executor does not:

- End the source command list.
- Start a destination command list with an allocator.
- Acquire another command list when returning to a previously submitted queue.
- Preserve allocator lifetime until the submitted work completes.

A single command list cannot be submitted, reset, and recorded again without an allocator lifecycle that respects GPU completion.

### 6.10 Finding RG-QUEUE-005: fence values are compiled and cached

**Severity**: Critical
**Category**: Synchronization lifetime

`BuildExecutionCommands` initializes `nextFenceValue` to `1`. Absolute values are serialized into command bytes and reused on compilation-cache hits.

Fence timelines are execution state, not graph topology. Reusing values can make waits pass immediately because a previous frame already completed the same or a greater value. It can also collide with the outer frame fence timeline.

#### Required behavior

The compiled graph should store dependency tokens or batch-edge indices. A queue timeline allocator should map those tokens to monotonic fence values at execution time.

### 6.11 Finding RG-QUEUE-006: final queue ownership is incomplete

**Severity**: Critical
**Category**: Submission ownership

The graph emits `SubmitQueue` only when changing queues. It emits no terminal submission after the pass loop.

The outer render loop closes and submits a graphics command buffer, which can work for a graph that remains entirely on graphics. It does not complete a graph whose final active queue is compute. More generally, ownership is split implicitly between the graph and outer frame loop.

The system must choose one model:

1. The graph owns all graph batch closure, submission, and cross-queue synchronization, then returns a final graphics dependency for presentation.
2. The graph returns a submission plan and recorded command lists to an outer scheduler that owns all submission.

The recommended long-term model is the second if the engine is expected to coordinate render graph, uploads, streaming, and presentation on shared queues. The immediate patch avoids the ownership problem by using the externally submitted graphics command buffer only.

### 6.12 Finding RG-TEST-001: async test assertions are incomplete

**Severity**: Medium
**Category**: Test gap

The current async test checks Graphics signal, Graphics submit, and Compute wait, then repeats Graphics submit and Compute wait assertions. It does not assert:

- Compute submit.
- Compute signal.
- Graphics wait.
- Exact operation order.
- Command-list close state at submission.
- Final submission.
- Monotonic fence values across executions and cache hits.

The mock command queue also does not reject recording command lists.

## 7. Barrier Design Invariants

The immediate implementation must establish these invariants.

### 7.1 Declaration invariants

1. A pass/resource pair is unique within each declaration set.
2. Read and write are independent dependency properties and may both be true.
3. Attachment and random-access declarations identify the concrete usage class.
4. Generic declarations do not override a more specific declaration.
5. A resource type must support the selected usage class.

### 7.2 Compilation invariants

1. Each `(resource, subresource range)` has one resolved target state per pass.
2. One barrier batch contains no duplicate keys.
3. Read plus UAV write resolves to UAV; it does not emit SRV followed by UAV.
4. Attachment helper calls may create generic read/write dependency entries, but those entries do not create extra state transitions.
5. Incompatible concrete usage classes produce a compilation error with pass and resource context.
6. Aliasing first use is represented on the resolved final transition.

### 7.3 Execution invariants

1. The executor receives an already canonical barrier list.
2. The backend may remove no-op transitions based on current state.
3. The backend must not be responsible for resolving contradictory pass declarations.
4. A barrier batch is independent of display names.

## 8. Proposed Per-Pass Usage Model

Introduce an internal usage accumulator used only during compilation. The exact collection type should follow existing allocation patterns and avoid per-frame managed allocations.

Conceptual structure:

```csharp
internal enum PassResourceUsageClass : byte
{
    None,
    ShaderRead,
    UnorderedAccess,
    ColorAttachment,
    DepthRead,
    DepthWrite,
    IndirectArgument
}

internal struct ResolvedPassResourceUsage
{
    public Identifier<RGResource> resource;
    public RGResourceType resourceType;
    public bool reads;
    public bool writes;
    public PassResourceUsageClass usageClass;
    public ResourceBarrierData targetState;
    public Identifier<RGResource> aliasingPredecessor;
    public BarrierFlags barrierFlags;
}
```

This is not intended to replace the DAG's read/write sets in the immediate patch. It canonicalizes state requirements after culling and before command serialization.

### 8.1 Usage resolution order

For each pass:

1. Register generic reads.
2. Register generic writes.
3. Apply explicit color attachment usages.
4. Apply explicit depth attachment usage.
5. Apply explicit random-access usages.
6. Resolve the final target state.
7. Apply aliasing first-use metadata.
8. Validate the final entry.
9. Emit one `CompiledBarrier` per entry.

Steps 3 through 5 are not simple last-write-wins overrides. They call a compatibility resolver that understands why generic entries exist.

### 8.2 Resolution matrix

| Declarations for one whole resource | Effective state | Result |
| --- | --- | --- |
| Generic read | SRV or indirect argument | Valid |
| Compute write | UAV | Valid |
| Compute read + write | UAV | Valid |
| Random access | UAV | Valid |
| Generic read + random access | UAV | Valid; generic read is dependency metadata |
| Color attachment write + incidental generic write | Render target | Valid |
| Depth read + incidental generic read | Depth read | Valid |
| Depth write/read-write + incidental generic entries | Depth write | Valid |
| Color attachment + random access | None | Reject for the same whole-resource range |
| Depth attachment + random access | None | Reject for the same whole-resource range |
| Color attachment + depth attachment | None | Reject |
| Buffer + render-target/depth usage | None | Reject |
| Unsafe generic write with no concrete usage | None | Reject or migrate call site to an explicit API |

### 8.3 Sync resolution

Sync masks may be combined only when the usage class and layout/access state are compatible.

Examples:

- A shader read used by pixel and non-pixel stages can combine the relevant sync bits.
- A UAV used only by a compute pass uses `ComputeShading`.
- A raster UAV may use `AllShading` unless stage-specific declaration is added.
- `ShaderResource` access and `UnorderedAccess` access must not be OR-ed to manufacture a pseudo-state.

The resolver should operate on usage classes first and produce `ResourceBarrierData` afterward.

## 9. Builder Deduplication Plan

Update `RenderGraphBuilder` registration helpers as follows:

```csharp
if (reads && !readList.Contains(resource))
{
    readList.Add(resource);
    registry.AddConsumer(resource, pass.index);
}

if (writes && !writeList.Contains(resource))
{
    writeList.Add(resource);
    registry.SetProducer(resource, pass.index);
}
```

Do the same for `randomAccess`.

Because pass lists are intentionally small, a linear `Contains` check is likely cheaper and simpler than allocating a per-pass hash set. This should be confirmed with the render graph benchmark after implementation.

Registry producer/consumer APIs must also be checked for duplicate insertion. Builder-level deduplication should not be the only defense if those APIs are used elsewhere.

## 10. Aliasing Transition Plan

Replace independent aliasing serialization with metadata lookup during usage resolution.

For each resolved resource usage:

1. Ignore imported resources.
2. Check whether this pass is the resource's first use.
3. Find its placed resource and most recent non-overlapping logical predecessor.
4. If a predecessor exists, set:
   - `aliasingPredecessor` to that resource.
   - `FirstUsage | Discard` on the resolved entry.
5. Preserve the resolved final target state.
6. Emit one descriptor.

Expected result:

```text
AliasingTransition: Previous #12 -> HDR_Lighting #17
  Before: Undefined / NoAccess
  After:  UnorderedAccess / UnorderedAccess / ComputeShading
  Discard: True
```

The D3D12 backend currently uses only the presence of an aliasing predecessor to select aliasing before-state behavior. The predecessor identity remains valuable for diagnostics and future backends even if D3D12 does not pass both resource pointers to an API-level aliasing barrier.

## 11. Immediate Async Compute Policy

### 11.1 Policy

Until the multi-queue path satisfies the invariants in Section 14:

- Every pass executes on `CommandQueueType.Graphics`.
- Compute pass callbacks still receive `IComputeRenderContext`.
- Compute dispatch commands are recorded into the graphics/direct command buffer.
- `EnableAsyncCompute(true)` is retained as requested scheduling metadata.
- No `SignalFence`, `SubmitQueue`, or `GPUWait` opcodes are emitted by the render graph.
- The outer frame loop remains responsible for closing and submitting the graphics command buffer.

D3D12 Direct queues support compute commands, so this preserves compute functionality while removing invalid cross-queue behavior.

### 11.2 Why demote all requested async passes

A rule that demotes only an async pass surrounded by graphics passes fixes the visible queue bounce but leaves the following defects active for other graph shapes:

- Signal-before-submit ordering.
- Submission of open command lists.
- Cached absolute fence values.
- Missing terminal compute submission.
- Missing allocator lifecycle.

Selective demotion would hide one symptom without making the remaining async cases correct.

### 11.3 API semantics

`EnableAsyncCompute(true)` should be documented as:

> Marks the pass as eligible for asynchronous compute scheduling. The compiler may execute the pass on the graphics/direct queue when no safe or profitable overlap window exists.

This is a compatible semantic model for future scheduling.

## 12. Diagnostics Changes

### 12.1 Barrier dump

Each transition should include:

- Logical resource name.
- Logical resource identifier.
- Resource type.
- Subresource range, currently `All`.
- Target layout, access, and sync.
- Aliasing predecessor identifier when present.
- Discard flag.

Example:

```text
Transition: HDR_Lighting [Texture #17, Subresources: All]
  -> Layout: UnorderedAccess
  -> Access: UnorderedAccess
  -> Sync: ComputeShading
  -> AliasingFrom: Texture #12
  -> Discard: True
```

### 12.2 Pass dump

Separate requested execution from effective scheduling:

```text
ExecutePass #15 'VolumetricFog_Inject'
  Type: Compute
  AsyncRequested: True
  EffectiveQueue: Graphics
  QueueDecision: AsyncExecutionTemporarilyDisabled
```

Later scheduler decisions could include:

- `NoOverlapWindow`
- `UnsupportedDependencyShape`
- `GroupedIntoComputeBatch`
- `AsyncComputeSelected`

### 12.3 Compiler validation error

Errors should identify the pass, resource, and conflicting declarations:

```text
Render graph pass 'ExamplePass' declares texture 'Lighting' (#17)
as both ColorAttachment and UnorderedAccess for the same whole-resource range.
Split the pass, use separate resources, or declare disjoint subresource ranges
when subresource tracking is supported.
```

Expected invalid declarations should return `Result`/`Error` through compilation rather than throw, consistent with repository error-handling conventions. Assertions may remain for programming invariants.

## 13. Immediate Implementation Plan

### Phase 0: Lock in reproductions

Add tests before changing behavior:

1. A compute pass declares one buffer with `AccessFlags.ReadWrite`.
2. A compute pass declares one texture with `AccessFlags.ReadWrite`.
3. A pass repeats the same read declaration.
4. A raster pass uses one random-access resource.
5. A transient aliased resource receives a first-use state transition.
6. An async-requested compute pass sits between graphics passes.

Tests should inspect compiled barrier identity and queue opcodes, not only backend barrier counts.

### Phase 1: Deduplicate declarations

Modify:

- `RenderGraphBuilder.UseResource`
- `RenderGraphBuilder.UseRandomAccessTexture`
- `RenderGraphBuilder.UseRandomAccessBuffer`
- Resource registry producer/consumer insertion if it also permits duplicates

Preserve independent read and write semantics.

### Phase 2: Add usage resolver

Modify `RenderGraphCompiler.Barrier.cs`:

- Introduce the internal usage class and resolved usage structure.
- Replace `EmitImplicitTransitions` with collection, resolution, validation, and serialization steps.
- Replace unconditional `AddTransition` calls with one emission loop over resolved entries.
- Keep the implementation allocation-conscious by using stack-scoped high-performance collections already used in the compiler.

Suggested method boundaries:

```text
ResolvePassResourceUsages
AccumulateGenericReads
AccumulateGenericWrites
ApplyAttachmentUsages
ApplyRandomAccessUsages
ResolveTargetState
ApplyAliasingMetadata
ValidateResolvedUsage
EmitResolvedBarriers
```

These boundaries make resolution rules testable without coupling every test to raw byte-stream parsing.

### Phase 3: Fold aliasing into final transitions

Modify or replace `EmitAliasingBarriers` so it enriches resolved entries rather than independently writing command bytes.

Verify both textures and buffers. Texture discard flags and buffer aliasing semantics may differ at the RHI level; tests should assert the actual `BarrierDesc` generated for each resource type.

### Phase 4: Add compiler validation

Add checks for:

- Duplicate resolved keys.
- Resource type and usage compatibility.
- Attachment conflicts.
- Whole-resource SRV/UAV conflicts not resolved by an explicit UAV declaration.
- Unsafe generic writes with unknown usage.

Audit current unsafe pass call sites before changing generic write behavior. If migration is too broad for the first patch, retain compatibility behind an explicit temporary warning and track strict validation as a required follow-up. Do not silently consider the current behavior correct.

### Phase 5: Demote async requests

Modify `BuildExecutionCommands`:

- Select Graphics as the effective queue for every current pass.
- Stop serializing graph-owned queue synchronization opcodes.
- Preserve the requested async flag in pass metadata and graph hashing.
- Add the effective queue decision to the dump.

Update `EnableAsyncCompute` XML documentation to describe eligibility rather than guaranteed queue assignment.

### Phase 6: Improve diagnostics

Modify render graph dump models and disassembly to include IDs, ranges, requested async state, effective queue, and scheduling reason.

### Phase 7: Verify immediate patch

Run focused tests, all render graph tests, and the complete unit test project. Run with the D3D12 debug layer and GPU-based validation when available.

Required command:

```shell
cd src
dotnet test Test/Ghost.UnitTest/Ghost.UnitTest.csproj -c Debug -p:Platform=x64
```

The previous external plan used `dotnet run` for the MSTest project. This repository's documented test command is `dotnet test`.

## 14. True Async Compute Design

True async compute should be implemented as a separate project after the immediate patch.

### 14.1 Scheduling concept

An async compute candidate has an overlap window defined by dependencies:

```text
Graphics producer boundary
        |
        +---- Compute batch ------------------+
        |                                      |
        +---- Independent graphics work -------+
                                               |
                                      Graphics consumer boundary
```

The compute batch is useful only if independent graphics work exists between launch and join boundaries and the expected overlap benefit exceeds synchronization cost.

### 14.2 Queue-batch model

Compile passes into explicit batches:

```csharp
internal struct CompiledQueueBatch
{
    public CommandQueueType queue;
    public ReadOnlyView<int> passIndices;
    public ReadOnlyView<int> dependencyBatchIndices;
    public bool isTerminal;
}
```

The exact storage should use existing unmanaged views and allocation ownership. The important design change is that submission boundaries become first-class compilation output rather than implicit consequences of adjacent queue types.

### 14.3 Batch construction algorithm

For each async-eligible compute pass or compatible compute group:

1. Identify all direct and transitive prerequisite passes.
2. Find the latest graphics batch that produces any required input.
3. Identify all graphics consumers of compute outputs.
4. Find the earliest graphics batch that must wait for the compute result.
5. Identify graphics passes between those boundaries that do not depend on the compute result and do not conflict through resources or aliasing.
6. If no independent work exists, assign the compute group to graphics.
7. Otherwise create a compute batch and cross-queue dependency edges.
8. Group adjacent compute candidates when grouping does not reduce overlap or introduce unnecessary waits.
9. Ensure transient aliasing does not overlap physical memory use across queues without a dependency edge.

Memory aliasing lifetimes must use scheduled batch concurrency, not only linear pass indices. Two resources whose logical pass ranges do not overlap in a linear order may still overlap in wall-clock execution across queues.

### 14.4 Submission order

For a dependency from graphics batch G0 to compute batch C0:

```text
Record G0
Close G0 command list
Submit G0 on Graphics
Signal Graphics timeline value V0
Wait Compute on V0
Record/submit C0 as permitted by its other dependencies
```

For a graphics consumer G2 of C0:

```text
Close C0 command list
Submit C0 on Compute
Signal Compute timeline value V1
Wait Graphics on V1 before G2
```

Independent graphics batch G1 can execute between G0 and G2 without waiting for C0.

### 14.5 Fence model

Do not compile absolute fence values.

Compile dependency edges such as:

```text
Batch C0 waits for completion token G0
Batch G2 waits for completion token C0
```

At execution time:

1. Allocate a monotonically increasing value from the source queue timeline.
2. Submit the source batch.
3. Signal the allocated value.
4. Enqueue waits on destination queues.
5. Associate command allocator and command list reclamation with the completion value.

Using one fence for multiple queues is possible if values come from one synchronized global allocator. Separate per-queue timeline fences are easier to reason about and make ownership explicit. The final choice should align with the engine's frame scheduler and resource pool.

### 14.6 Command allocator and list ownership

Each in-flight submitted batch needs:

- A command allocator that is not reset until that batch completes.
- A closed command list before submission.
- A command-list pool entry associated with the completion fence/value.
- A clear owner responsible for returning both objects after completion.

Returning to the graphics queue after submitting an earlier graphics batch requires a new graphics command list or another safely reusable list/allocator pair. Changing `RenderGraphContext`'s active pointer is insufficient.

### 14.7 Final frame and presentation

The scheduler must ensure that presentation waits for the final graphics batch that writes the swapchain image. If the final producer is compute, a graphics batch must wait for compute and perform the required final transition/presentation preparation.

The render graph execution result should expose the completion token needed by the frame loop, or the frame scheduler should own graph submission entirely.

### 14.8 Profitability policy

Correctness decides whether async execution is legal. Profitability decides whether it is selected.

Initial conservative heuristic:

- Require at least one independent graphics batch between launch and join.
- Reject a compute batch that immediately joins on the next graphics batch.
- Prefer grouping small compute passes to reduce submissions.
- Cap queue transitions per frame.

Later improvements can use timestamp history:

- Average pass duration.
- Queue occupancy.
- Historical overlap achieved.
- Submission and wait overhead.
- Architecture-specific policy.

## 15. File-Level Change Map

| File | Immediate change |
| --- | --- |
| `RenderGraphBuilder.cs` | Deduplicate resource declarations; preserve read/write dependency semantics; clarify async eligibility docs. |
| `RenderGraphPass.cs` | Add effective queue or queue-decision metadata if stored on the compiled/pass model. |
| `RenderGraphCompiler.Barrier.cs` | Add usage accumulation, resolution, validation, aliasing enrichment, and unique emission. |
| `RenderGraphCompiler.cs` | Demote async requests to graphics; remove invalid queue-switch emission in the immediate path. |
| `RenderGraphExecutor.cs` | Stop depending on queue opcodes in the immediate path; later execute explicit queue batches. |
| Render graph dump implementation | Print resource IDs/ranges and requested versus effective queue. |
| `RenderGraphHasher.cs` | Confirm requested async and all state-affecting declarations remain part of the graph hash. |
| `D3D12CommandQueue.cs` | Add safety validation that submitted command buffers are closed, if exposed through RHI state. |
| Mock command buffer/queue files | Reject open-list submission and record exact ordered operations. |
| `RenderGraphTest.cs` | Add focused barrier and conservative queue tests; replace incomplete async assertions. |

The long-term async implementation will additionally affect command allocator/list pools, frame scheduling, render execution results, and presentation synchronization.

## 16. Test Plan

### 16.1 Barrier unit tests

#### Test: compute read/write buffer

Setup:

```text
Compute pass:
  UseBuffer(buffer, ReadWrite)
```

Expected:

- One barrier for the buffer.
- Access is `UnorderedAccess`.
- Sync is `ComputeShading`.
- No SRV transition for the same ID in that batch.

#### Test: compute read/write texture

Expected:

- One UAV texture barrier.
- Layout is `UnorderedAccess`.
- Range is whole resource until subresource tracking exists.

#### Test: repeated read declaration

Setup:

```text
UseTexture(texture, Read)
UseTexture(texture, Read)
```

Expected:

- One read-list entry.
- One consumer registration for the pass.
- One barrier candidate.

#### Test: raster random access

Expected:

- One UAV barrier with `AllShading` or the selected raster shader sync.
- No extra generic SRV or write transition.

#### Test: attachment declaration

Expected:

- One render-target or depth transition.
- Incidental generic dependency entries do not produce extra barriers.

#### Test: incompatible attachment and UAV

Expected:

- Compilation fails with pass name, resource name, ID, and both usage classes.

#### Test: aliased first use

Expected:

- One barrier for the new resource.
- Aliasing predecessor is valid.
- Discard is set.
- Target is the actual first-use state, not `Undefined/NoAccess`.

#### Test: duplicate names

Setup two resources with the same display name.

Expected:

- Dump distinguishes IDs.
- Uniqueness checks use IDs, not names.

### 16.2 Immediate queue tests

#### Test: requested async pass

Expected:

- `AsyncRequested` remains true.
- Effective queue is Graphics.
- No graph-owned signal, submit, or wait opcodes are present.
- Compute callback records into the graphics/direct command buffer.

#### Test: graph starts with requested async pass

Expected:

- No empty graphics signal or submission.
- Pass executes on graphics.

#### Test: graph ends with requested async pass

Expected:

- Pass executes on graphics.
- Existing outer frame closure/submission owns final submission.

### 16.3 Future async queue tests

These tests gate re-enabling compute queue execution:

- Source command list is closed before submission.
- Source submission occurs before source signal.
- Destination wait occurs after the source signal is enqueued.
- Returning to a queue uses a valid new command-list segment.
- Every terminal batch is submitted.
- Fence values increase across frames.
- Fence values increase across cache hits.
- Frame fence values cannot collide with graph queue timeline values.
- A no-overlap candidate is demoted.
- A real fork/join graph produces overlap-capable queue batches.
- Physical aliasing lifetimes remain valid across concurrent batches.
- Device removal and validation-layer errors are absent.

### 16.4 Validation tools

- MSTest assertions over compiled commands and mock operations.
- D3D12 debug layer.
- GPU-based validation.
- PIX timing capture for overlap verification.
- Render graph command dump comparison.
- Existing render graph benchmarks for allocation and compile-time regressions.

## 17. Acceptance Criteria

### 17.1 Immediate patch

The immediate patch is complete when all of the following are true:

1. A barrier batch has at most one entry per logical resource and whole-resource range.
2. Compute `ReadWrite` resources resolve to one UAV transition.
3. Attachment and random-access helper declarations do not generate incidental duplicate transitions.
4. Incompatible concrete usage classes fail compilation clearly.
5. An aliased first use transitions directly to its final required state.
6. Dumps identify logical resource IDs and effective queues.
7. Async-requested compute passes execute on graphics/direct.
8. The render graph emits no cross-queue synchronization opcodes.
9. All render graph unit tests pass.
10. Existing compile/cache allocation targets do not regress materially.

### 17.2 Re-enabling true async compute

Compute queue execution must remain disabled until all of the following are true:

1. Queue batches are dependency-based.
2. Submitted command lists are closed.
3. Command allocator lifetimes are tied to GPU completion.
4. Submission precedes signaling.
5. Fence values are allocated monotonically at execution time.
6. Every final batch is explicitly submitted.
7. Presentation dependencies are explicit.
8. No-overlap candidates are demoted.
9. D3D12 debug layer and GPU-based validation report no synchronization or command-list errors.
10. A PIX capture demonstrates actual graphics/compute overlap for at least one representative graph.

## 18. Expected Command Stream After Immediate Patch

The in-place SSR pass should become:

```text
IssueBarriers (2 barriers)
  Transition: SSR_Raw [Texture #21, Subresources: All]
    -> Layout: ShaderResource
    -> Access: ShaderResource
    -> Sync: PixelShading | NonPixelShading
  Transition: HDR_Lighting [Texture #17, Subresources: All]
    -> Layout: UnorderedAccess
    -> Access: UnorderedAccess
    -> Sync: ComputeShading
ExecutePass #14 'SSR_TemporalDenoise'
  Type: Compute
  AsyncRequested: False
  EffectiveQueue: Graphics
```

The volumetric fog section should become:

```text
IssueBarriers (...)
ExecutePass #15 'VolumetricFog_Inject'
  Type: Compute
  AsyncRequested: True
  EffectiveQueue: Graphics
  QueueDecision: AsyncExecutionTemporarilyDisabled
IssueBarriers (...)
ExecutePass #16 'TAA_Resolve'
  Type: Compute
  EffectiveQueue: Graphics
```

There should be no queue signal, internal submit, or GPU wait around the isolated pass.

## 19. Risks and Mitigations

### 19.1 Strict validation exposes existing invalid pass declarations

Some existing passes may depend on the current last-transition-wins behavior.

Mitigation:

- Add diagnostics first.
- Audit all unsafe writes and mixed attachment/UAV declarations.
- Migrate call sites to explicit APIs.
- Do not weaken the final invariant to preserve ambiguous behavior.

### 19.2 Deduplication changes producer/consumer registration

Removing duplicates could expose code that accidentally relies on repeated registry entries.

Mitigation:

- Add registry-level tests.
- Verify culling and DAG behavior for read/write and multi-writer resources.
- Compare compiled pass order and culling before and after the patch.

### 19.3 Usage resolution adds compilation cost

A per-pass map adds work during fresh compilation.

Mitigation:

- Use stack-scoped unmanaged collections.
- Size from declared resource counts.
- Keep cache-hit behavior unchanged.
- Benchmark fresh compile and cache-hit paths.

### 19.4 Async demotion reduces potential GPU overlap

Any currently intended overlap is temporarily removed.

Mitigation:

- The current path cannot safely provide that overlap.
- Direct queue compute preserves rendering correctness.
- Keep requested async metadata so candidates remain identifiable.
- Re-enable only after measured, validated queue-batch scheduling exists.

### 19.5 Aliasing across future concurrent queues

Linear pass lifetimes are insufficient once queues overlap.

Mitigation:

- Treat aliasing and async scheduling as coupled in the long-term design.
- Extend lifetime analysis to account for partial-order batch execution before enabling concurrent queues.

## 20. Decisions Recommended

1. **Adopt one resolved state per resource/range per pass.**
   This is the central barrier invariant.

2. **Resolve compute read/write to UAV.**
   Do not emit separate SRV and UAV transitions and do not blindly OR incompatible layouts/access values.

3. **Reject incompatible simultaneous whole-resource usages.**
   Do not choose whichever declaration was processed last.

4. **Fold aliasing into the final first-use transition.**
   Avoid a separate undefined target transition for the same resource in the same batch.

5. **Demote all async requests in the immediate patch.**
   Selective adjacency-based demotion leaves critical synchronization defects reachable.

6. **Treat async compute as eligibility, not a guarantee.**
   This supports both conservative scheduling and future performance heuristics.

7. **Build queue batches from dependency windows before re-enabling async.**
   Adjacent pass types do not describe overlap.

8. **Allocate fence values at execution time.**
   Fence values must not be baked into cached graph bytes.

9. **Make submission ownership explicit.**
   The graph and outer frame loop must not both partially own queue submission.

10. **Add resource IDs and ranges to diagnostics.**
    Display names are insufficient for correctness investigation.

## 21. Implementation Checklist

### Immediate correctness patch

- [ ] Add failing read/write barrier tests.
- [ ] Add repeated-declaration tests.
- [ ] Add aliasing final-state test.
- [ ] Add invalid mixed-usage tests.
- [ ] Deduplicate builder read/write/random-access sets.
- [ ] Verify registry producer/consumer uniqueness.
- [ ] Implement per-pass usage accumulation.
- [ ] Implement usage compatibility resolution.
- [ ] Emit one compiled barrier per resolved key.
- [ ] Fold aliasing metadata into resolved transitions.
- [ ] Audit and migrate unsafe generic writes.
- [ ] Add compile-time usage validation.
- [ ] Include IDs and ranges in dumps.
- [ ] Include requested and effective queue in dumps.
- [ ] Demote async-requested compute to graphics.
- [ ] Remove immediate-path queue synchronization opcodes.
- [ ] Update async API documentation.
- [ ] Run focused tests and complete MSTest project.
- [ ] Run D3D12 debug layer and GPU-based validation.
- [ ] Run render graph compile/cache benchmarks.

### True async compute follow-up

- [ ] Define queue-batch compilation model.
- [ ] Define submission ownership boundary.
- [ ] Add command allocator/list pool per in-flight batch.
- [ ] Compile cross-queue dependency tokens.
- [ ] Allocate timeline values at execution time.
- [ ] Enforce close -> submit -> signal -> wait ordering.
- [ ] Submit all terminal batches.
- [ ] Integrate presentation dependency.
- [ ] Make transient aliasing concurrency-aware.
- [ ] Implement no-overlap demotion heuristic.
- [ ] Add full queue-order and fence-lifetime tests.
- [ ] Validate actual overlap in PIX.

## 22. Final Recommendation

Do not implement only barrier deduplication plus an adjacency check for isolated async passes. Barrier deduplication must resolve usage semantics, and the current multi-queue path contains correctness defects that an isolated-pass heuristic does not address.

Proceed in this order:

1. Establish failing tests and better resource diagnostics.
2. Canonicalize declarations and resolve one valid target state per resource/range.
3. Fold aliasing into the resolved transition.
4. Demote all async-requested compute to the graphics/direct queue.
5. Verify correctness and compilation performance.
6. Design and implement dependency-based queue batches as a separate change.
7. Re-enable async compute only after native validation and measured overlap succeed.

This order restores a defensible correctness baseline first and preserves a clear path to profitable async compute without retaining the current synchronization hazards.
