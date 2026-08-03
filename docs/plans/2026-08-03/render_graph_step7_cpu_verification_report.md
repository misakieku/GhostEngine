# Render Graph Step 7 CPU Verification Report

Date: 2026-08-02

## Scope

This report covers the Step 7 work that can be validated without a real rendering pipeline or a render graph executing on a D3D12 command buffer.

Completed here:

- Debug and Release build verification.
- Focused and full unit-test verification.
- Fresh-compilation versus cache-hit command-stream parity.
- Transient placement and alias-group cache parity.
- Compatible native-pass merging and cache parity.
- Cold and warm compilation benchmarks with managed-allocation diagnostics.

Deferred until a real render-graph pipeline exists:

- D3D12 debug-layer validation of render-graph barriers.
- GPU-based validation.
- PIX captures.
- Visual correctness.
- Real command-list submission and presentation validation.

## Correctness Results

- Combined render-graph tests: 22 passed, 0 failed, 0 skipped.
- Full unit suite: 83 passed, 0 failed, 0 skipped.
- Full Debug x64 solution build: succeeded with 0 errors.
- Full Release x64 solution build: succeeded with 0 errors.
- Modified runtime files passed project-aware diagnostics.

The standalone test-file scanner still cannot resolve Ghost and MSTest project references. MSBuild, Microsoft.Testing.Platform, and Visual Studio remain authoritative for those files.

## Hardening Findings

### Cache restoration lost aliasing-plan diagnostics

A perfect cache hit restored placed resources but did not restore:

- `AliasingPlan.totalHeapSize`.
- Logical-resource membership in each placed resource.
- Same-offset alias groups.

This caused a fresh dump to report a nonzero transient heap while the equivalent cache-hit dump reported zero,
and alias diagnostics were incomplete. Cache restoration now recomputes the aligned heap size and reconstructs
logical and same-offset alias membership without managed allocation.

### BenchmarkDotNet isolated builds exposed an absolute-path bug

BenchmarkDotNet supplies an absolute `IntermediateOutputPath` for generated projects. `GhostEngine.targets`
previously prefixed `MSBuildProjectDirectory`, producing an invalid path. The target now normalizes relative and
rooted intermediate paths through one `GhostIntermediateOutputPath` property. Ordinary Debug and Release builds
and BenchmarkDotNet isolated builds all pass.

## Cache And Native-Pass Verification

Equivalent fresh and cached graphs now assert equality for:

- Graph hash.
- Disassembled binary command stream.
- Native-pass assignment per logical pass.
- Total transient heap size.
- Resource heap offsets and sizes.
- Resource alias membership.

A dedicated test verifies that two compatible consecutive raster passes sharing an imported render target:

- Merge into one native pass.
- Emit one `BeginNativePass` command.
- Preserve the merged assignment and command stream on cache replay.

Existing tests continue to cover culling, extraction, texture and buffer aliasing, barrier canonicalization, async containment, and Step 6 validation.

## Benchmark Results

Command:

```shell
dotnet run -c Release -p:Platform=x64 --project src/Test/Ghost.UnitTest/Ghost.UnitTest.csproj -- --filter "*RenderGraphBenchmark*"
```

Environment:

- BenchmarkDotNet 0.15.8.
- .NET 10.0.10, X64 RyuJIT x86-64-v3.
- Intel Core i7-13700K.
- DefaultJob.
- Diagnostic dumps disabled in all benchmark methods.

| Path | Mean | StdDev | Managed allocation | GC |
| --- | ---: | ---: | ---: | ---: |
| Declaration only | 2.500 us | 0.012 us | 0 B/op | No collections |
| Cold cache miss | 16.339 us | 0.205 us | 1,288 B/op | Gen0 0.0610, Gen1 0.0305 per 1000 ops |
| Warm cache hit | 6.569 us | 0.042 us | 0 B/op | No collections |

The optimization phases produced the following DefaultJob progression:

| Phase | Cold cache miss | Warm cache hit |
| --- | ---: | ---: |
| Canonical hash sorting | 18.393 us | 9.577 us |
| Successful-validation memoization | 17.966 us | 8.905 us |
| Exact linear alias restoration | 17.711 us | 8.061 us |
| Compact declaration sets | 17.660 us | 7.472 us |
| Shipping validation gate | 16.339 us | 6.569 us |

The declaration-only benchmark measured 3.955 us with `HashSet` storage, 3.079 us with the compact resource set,
and 2.500 us after dedicated validation was compiled out of plain Release. Debug, Debug Editor, Release Dev, and
Release Editor retain validation through `GHOST_SAFETY_CHECKS`.

The original pre-remediation result supplied by the user was 14.174 us cold and 5.387 us warm, so the shipping
implementation remains approximately 15% slower cold and 22% slower warm. The correctness work has therefore
not been presented as a complete performance recovery.

The cold benchmark includes graph declaration, cache invalidation/replacement, compilation, and mock execution.
The steady-state cache-hit and declaration-only paths remain allocation-free under MemoryDiagnoser.

## Pending GPU Gate

When the minimal real pipeline is available, complete Step 7 by:

1. Confirming that the graph records into a real D3D12 command buffer and is submitted.
2. Enabling the D3D12 debug layer and breaking on validation errors.
3. Enabling GPU-based validation.
4. Exercising real RT, depth, SRV, and UAV transitions represented by the pipeline.
5. Verifying transient aliasing only if the pipeline contains non-overlapping transient resources.
6. Capturing a PIX frame and checking barrier order, resource states, command-list closure, submission, and presentation.
7. Confirming visual output against the pipeline's expected result.

Step 7 is therefore CPU-complete but not GPU-complete. Step 8 must not begin until the deferred GPU gate is either passed or explicitly waived.
