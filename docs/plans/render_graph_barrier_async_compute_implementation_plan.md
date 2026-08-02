We should treat this as a sequence of independently reviewable changes. Each step should leave the render graph buildable and tested before moving forward.

## Step 1: Diagnostics And Reproduction

Goal: Make the current defects observable and lock them into failing tests.

Changes:

- Add logical resource IDs and resource types to the command dump.
- Report requested async state separately from the effective queue.
- Add tests reproducing compute ReadWrite SRV/UAV conflicts.
- Add tests for repeated resource declarations and queue operation order.
- Strengthen mocks so submitting an open command buffer fails.

Gate: We can identify resources unambiguously and tests demonstrate the current failures. Production behavior remains unchanged.

---

## Step 2: Contain The Invalid Async Path

Goal: Remove immediate D3D12 synchronization risk before deeper refactoring.

Changes:

- Treat EnableAsyncCompute(true) as an eligibility hint.
- Execute all compute passes on the Graphics/Direct queue temporarily.
- Stop emitting render-graph SignalFence, SubmitQueue, and GPUWait operations.
- Preserve asyncCompute in graph metadata and hashing.
- Update the async test to expect requested async plus effective Graphics execution.

Gate: All compute passes still work, no render-graph queue switching occurs, and the outer frame loop owns the single graphics submission.

This should happen early because the current multi-queue path submits open command lists and uses invalid fence ordering.

---

## Step 3: Canonicalize Resource Declarations

Goal: Ensure each pass records clean dependency information.

Changes:

- Deduplicate entries in resourceReads.
- Deduplicate entries in resourceWrites.
- Deduplicate randomAccess.
- Ensure producer and consumer registration is unique.
- Preserve both read and write membership for ReadWrite, because dependency analysis needs both.

Gate: Repeated builder calls do not create duplicate declarations, while RAW, WAR, WAW, culling, and producer/consumer behavior remain correct.

This step cleans the input model but does not yet resolve final GPU states.

---

## Step 4: Introduce The Barrier Usage Resolver

Goal: Resolve one effective GPU usage per resource before serialization.

Changes:

- Accumulate generic reads, writes, attachments, and UAV declarations into a per-pass usage table.
- Resolve compute ReadWrite to one UAV state.
- Let explicit attachment or random-access usage absorb incidental generic entries.
- Combine sync masks only when layout and access are compatible.
- Emit at most one transition per resource and whole-resource range.
- Stop independently emitting read and write transitions.

Gate: The VisibleBuffer and HDR_Lighting examples produce one UAV barrier, and no normal pass emits duplicate resource IDs in a barrier batch.

This is the main fix for the barrier bug.

---

## Step 5: Integrate Aliasing With Final Transitions

Goal: Make the one-barrier invariant include transient aliasing.

Changes:

- Stop emitting an independent Undefined/NoAccess aliasing transition.
- Attach aliasing predecessor, first-use, and discard metadata to the resolved transition.
- Transition directly from the aliased before-state to the actual RT, depth, SRV, or UAV state.
- Verify texture and buffer behavior separately.

Gate: An aliased first use produces one transition with the correct final state and aliasing metadata.

---

## Step 6: Validate Ambiguous And Invalid Usages

Goal: Prevent the compiler from silently inventing invalid states.

Changes:

- Reject color attachment plus UAV for the same whole texture.
- Reject depth attachment plus UAV for the same whole texture.
- Reject color and depth attachment usage on the same range.
- Reject render-target or depth states for buffers.
- Audit unsafe passes, because generic unsafe writes are currently treated as render targets regardless of resource type.
- Introduce explicit unsafe usage APIs if existing passes need RT, UAV, copy, or other distinct states.

Gate: Invalid declarations fail compilation with the pass name, resource name, resource ID, and conflicting usage classes.

This step may expose existing call-site bugs, so I would review the unsafe-pass audit with you before changing its public API.

---

## Step 7: Hardening And Performance Verification

Goal: Confirm the immediate render graph is correct and has not regressed its performance goals.

Changes:

- Run all render graph and unit tests.
- Run D3D12 debug-layer and GPU-based validation.
- Verify command dumps from the representative pipeline.
- Benchmark fresh compilation and cache hits.
- Verify no unexpected managed allocations.
- Check pass culling, native-pass merging, and transient allocation results.

Gate: The immediate patch meets all acceptance criteria, with async compute still conservatively running on Graphics.

At this point, the render graph should have a defensible single-queue correctness baseline.

---

## Step 8: Design True Async Queue Batches

Goal: Model actual concurrency before touching native submission again.

Changes:

- Convert DAG dependencies into explicit Graphics and Compute batches.
- Find producer launch boundaries and consumer join boundaries.
- Detect independent graphics work within the overlap window.
- Demote candidates with no overlap opportunity.
- Group compatible compute passes.
- Make transient aliasing analysis aware of concurrent batches.
- Decide whether the graph or outer frame scheduler owns submissions.

Gate: We can inspect a compiled batch plan and prove that it represents legal dependencies and a real overlap opportunity. Native compute-queue submission remains disabled during this design step.

---

## Step 9: Implement Correct Multi-Queue Execution

Goal: Safely execute the batch plan.

Changes:

- Give each submitted batch a valid command list and allocator lifetime.
- Close command lists before submission.
- Enforce submit -> signal -> destination wait.
- Allocate monotonic fence values at execution time.
- Do not store absolute fence values in cached graph bytes.
- Explicitly submit terminal batches.
- Integrate the final graphics/presentation dependency.
- Reclaim command allocators and lists only after GPU completion.

Gate: Exact queue-order tests pass across normal compilation and cache hits, and D3D12 validation reports no errors.

---

## Step 10: Enable And Measure Async Compute

Goal: Enable async compute only where it provides real benefit.

Changes:

- Activate compute-queue execution for eligible batches.
- Capture GPU timestamps and PIX traces.
- Compare direct-queue and async schedules.
- Keep automatic demotion for non-overlapping or very small batches.
- Add architecture-specific heuristics only after measurements justify them.

Gate: PIX demonstrates actual overlap and a measurable frame-time improvement without synchronization or aliasing errors.
