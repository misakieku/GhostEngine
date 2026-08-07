# Ghost.Entities — Archetype ECS Review

**Scope:** `Runtime/Ghost.Entities` (implementation) + `Test/Ghost.UnitTest/ECS` (tests)
**Date:** 2025-12
**Status:** Review — no code changes made in this pass

---

## 1. Executive Summary

The implementation is a serious, well-structured archetype ECS: 16 KB chunks with
SoA layout, alignment-sorted layouts, entity id arrays, generational slot maps,
shared-component chunk groups, enableable components with bit masks, cleanup
components, edge-cached archetype transitions, batched swap-remove, an ECB with
temp entities, thread-local ECBs, change versioning, and a parallel-for chunk
scheduler. The architecture is sound and the test suite is a good start.

However, there are **several genuine correctness bugs**, including one
(§2.1) that can corrupt the heap, and a second (in cleanup-entity handling,
§2.4/§2.5) that makes the cleanup-component feature incomplete: destroyed
cleanup entities stay alive in their old chunk. There are also notable
performance bottlenecks (a global lock on every component info lookup,
linear scans for shared groups), and a set of natural next features.

**Priority ordering:** §2.1 → §2.2 → §2.3 → §2.4 → §2.5 → §2.6 → rest of §2,
then the perf items in §3 that feed the hot paths, then tests, then features.

---

## 2. Bugs

### 2.1 CRITICAL — `Archetype.AllocateEntities` off-by-one leaves an entity with garbage coordinates

`Archetype.cs:558`:

```csharp
if (idx == rowIndex.Length - 1)
{
    return;
}
```

Must be `if (idx == rowIndex.Length)`. With the current code, when the active
chunk has exactly `N-1` free slots left and you bulk-allocate `N` entities, the
loop fills `N-1` slots and then hits the early return **before** the remaining
entity gets a `chunkIndex`/`rowIndex`. The caller (`EntityManager.CreateEntities`,
ECB playback) then calls `SetEntity(chunkIndex, rowIndex, entity)` with
**uninitialized stack garbage** — an out-of-bounds write into the chunk buffer
(possibly beyond the 16 KB allocation) and a corrupted `EntityLocation` in the
slot map.

Reproduction: create an archetype whose capacity is `K`, fill `K-1` entities,
then `CreateEntities(2)` for that archetype.

Fix: `idx == rowIndex.Length`. Also consider asserting
`Logger.DebugAssert(idx == rowIndex.Length)` after the loop.

### 2.2 HIGH — `ChunkView.GetComponentVersion` indexes the version array by component ID

`Query.cs:208` and `Query.cs:220`:

```csharp
public readonly uint GetComponentVersion(Identifier<IComponent> id) => _pVersion[id];
public readonly uint GetComponentVersion<T>() => _pVersion[ComponentTypeID<T>.Value];
```

The version array is sized by `_layouts.Count` and indexed by
`layout.versionIndex` (the **layout index**, not the component ID). These two
methods read the wrong slot (or out of bounds) whenever component IDs don't
dense-map to layout indices. Compare the correct pattern in `HasChanged`
(`Query.cs:171/186`).

Fix:

```csharp
public readonly uint GetComponentVersion(Identifier<IComponent> id)
    => _pVersion[GetLayout(id).versionIndex];
```

### 2.3 HIGH — `ChunkView.GetEnableBits<T>` indexes `_layouts` by component ID

`Query.cs` (in `ChunkView`):

```csharp
var layout = _layouts[ComponentTypeID<T>.Value];
```

`_layouts` is sorted by **alignment** (`Archetype.CalculateLayout`), so the ID
is not a valid index into it. This returns the wrong layout (wrong enable-bits
offset — silently reading garbage enablement state) or runs out of bounds.
Use the same lookup as `IsComponentEnabled<T>`:

```csharp
var layout = GetLayout(ComponentTypeID<T>.Value);
```

### 2.4 HIGH — Cleanup entities are never actually removed; location stays stale

`EntityManager.DestroyEntity` (cleanup path) allocates a slot in the cleanup
archetype, copies data, sets the entity, but then:

1. never calls `oldArchetype.RemoveEntity(...)` — the entity **stays in its
   original chunk**, and
2. never updates `_entityLocations` — the location still points at the old
   chunk/row.

Result: `Exists(entity)` keeps returning true; queries still see the entity with
its full component set; the "cleanup archetype" row is a duplicate. The
following loop in `DestroyEntities` has the same shape (see §2.5).

Additionally, **neither path handles the case where the cleanup archetype is the
same archetype** (entity already carries only cleanup components — or the empty
archetype's `_cleanupEdge == 0` trick). `GetOrCreateCleanupArchetype` is skipped
only when `_cleanupEdge == 0`; for an entity already in a cleanup-only archetype,
`_cleanupEdge >= 0` (it points to itself or was cached earlier), so
`DestroyEntity` allocates another row in the *same* archetype and copies —
permanent entity leak, and the swap-logic interplay with `RemoveEntities` makes
it worse.

Fix design:

- In `DestroyEntity`: if the entity is already in the cleanup archetype (or the
  cleanup archetype is the empty archetype), destroy it immediately via
  `DestroyEntity_Internal`.
- Otherwise: migrate (copy + `RemoveEntity` from source + `UpdateEntityLocation`
  to the cleanup archetype), and return the *cleanup archetype* location — not
  `Error.None` pretending the destroy finished.
- `DestroyEntities` should use the same per-entity "migrate into cleanup
  archetype" flow *and* batch the removals correctly (see §2.5).

### 2.5 HIGH — `DestroyEntities` uses stale captured locations after per-entity removal

In the gather loop of `DestroyEntities`, cleanup entities are migrated with
`archetype.RemoveEntity(location.chunkIndex, location.rowIndex)` **immediately**,
one at a time, while later `batchDestroy` entries still hold row indices
captured *before* those removals. Because removal is swap-with-last:

- a later captured `rowIndex` can now point at a *different* entity (the one
  swapped in), so `RemoveEntities` removes the wrong entity, and
- the `EntityLocation` update inside `RemoveEntity` re-parents the wrong entity.

The same stale-index problem exists for `batchDestroy` entries gathered from one
chunk, then executed together — *that* part is handled correctly because
`RemoveEntities` expects sorted indices and swaps internally. But the
immediate, non-batched removals in the gather loop break the invariant the
batched path relies on.

Also note: `DestroyEntities` never marks `cleanupMigrated` entities' locations
correctly (they still point at the old chunk after the immediate removal), and
the location update that *is* done (`UpdateEntityLocation(entity, newArcID, ...)`)
happens **before** `archetype.RemoveEntity(...)`, which itself calls
`UpdateEntityLocation` on the swapped entity — ordering is fragile.

Fix design: separate the two phases cleanly:

1. **Gather** — resolve locations for all entities.
2. **Migrate** — for cleanup entities, compute the target cleanup archetype and
   the shared-data blob, but **defer the actual row removal**: build per-chunk
   sorted removal lists including both destroyed and cleanup-migrated rows.
3. **Execute** — one `RemoveEntities` per (archetype, chunk) with the merged
   sorted index list (this is the standard DOTS pattern and keeps swap
   bookkeeping inside one function).
4. Update locations/remove from slot map afterwards.

### 2.6 MEDIUM — `SetEnabled` (non-generic) writes the enable mask for non-enableable components

`EntityManager.SetEnabled(entity, componentID, enabled)` looks up the layout,
then unconditionally does `maskBase[byteIndex] ...` — but for a non-enableable
component `enableBitsOffset == -1`, so `maskBase = chunkBase - 1` and the write
goes **one byte before the chunk buffer**. The generic overload
(`SetEnabled<T>`, constrained to `IEnableableComponent`) is safe. Guard:

```csharp
if (layoutResult.Value.enableBitsOffset == -1)
    return Error.InvalidArgument; // or NotFound
```

Also note the code comment `/// <param name="componentID">...` contains a broken
XML doc tag (`</` instead of `</param>`).

### 2.7 MEDIUM — `Collect()` leaks chunk slots and never reclaims dead chunks

`Archetype.Collect()` disposes empty chunks but never removes them from
`_chunks` (there is no removal path), and the `activeChunkIndex` handling is an
acknowledged TODO ("How can we set the activeChunkIndex? Backward tracing?").
Consequences:

- `_chunks` grows without bound (a world that repeatedly creates/destroys
  bursts of entities accumulates one dead chunk per burst — a classic churn
  leak pattern).
- `ChunkIterator.MoveNext` must skip empty chunks forever, adding a branch per
  chunk per iteration.
- `refCount` is decremented but never used for anything.

Fix design: when a chunk reaches `_count == 0` (in `RemoveEntity`/`RemoveEntities`),
swap-with-last it out of `_chunks` and dispose it immediately, then point
`activeChunkIndex` at the swapped-in chunk (or scan back) — or keep the
deferred `Collect()` but make it compact the list (swap-remove + fix up
`activeChunkIndex` to the swapped-in index, and update `Chunk._groupIndex`
ownership). Ideally `RemoveEntities` reports "this chunk is now empty" and the
manager handles it inline so no dead chunk is ever iterated.

### 2.8 MEDIUM — Archetype lookup is hash-only; a collision returns a wrong archetype

`ComponentManager.GetArchetypeIDBySignatureHash` / `GetEntityQueryIDByMaskHash`
return whatever archetype/query is stored under the hash. There is **no
signature-equality confirmation**. `UnsafeHashMap.Add` semantics on an existing
key are also unverified here — at minimum a collision will silently corrupt, and
at worst the `Add` throws. The code already shows awareness of this for queries
(`QueryBuilder.Build` compares masks after the hash hit) — the archetype path
needs the same treatment, and the empty signature's hash must be verified to
match between `UnsafeBitSet` (used for `Archetype._hash`) and `SpanBitSet`
(used by `ComponentRegistry.GetHashCodeForTypeIDs` / `FindOrCreateArchetype`) —
if those two hash functions ever diverge, **every** archetype lookup breaks.

Fix: store the signature bitset in the lookup value (`UnsafeHashMap<int,
(Identifier<Archetype>, UnsafeBitSet)>` or a second parallel array), and verify
`signature.Equals` before returning; on mismatch, insert (and log a collision
warning under `GHOST_SAFETY_CHECKS`).

### 2.9 LOW — `ComponentSet.Equals` / `ComponentSetView.Equals` compare cached hashes instead of contents

```csharp
return _hashCode == other._hashCode && _sharedHashCode == other._sharedHashCode
    && _components.AsSpan().SequenceEqual(...) && _sharedData.AsSpan().SequenceEqual(...);
```

`_hashCode == -1` means "not yet computed". Comparing an uncomputed cache value
(`-1`) against a computed hash makes two *equal* sets compare unequal. The
sequence comparisons are already there — just drop the hash short-circuit (or
compare lazily-computed hashes only when both are non-`-1`).

### 2.10 LOW — `EntityLocation.CompareTo` compares `chunkIndex` twice, never `archetypeID`

`EntityManager.cs` (`EntityLocation`):

```csharp
var archComp = chunkIndex.CompareTo(other.chunkIndex);   // ← duplicated
...
var chunkComp = chunkIndex.CompareTo(other.chunkIndex);  // ← same comparison
```

The second comparison is dead code, and archetype ID is missing from the key.
It currently works only because `DestroyEntities` flushes on
`chunkIndex != prevChunkIndex || archetypeID != prevArchetypeID` — but the sort
order itself is wrong: two different archetypes with equal chunk indices are
ordered arbitrarily, and the batch logic leans on that ordering. Fix:

```csharp
var archComp = archetypeID.CompareTo(other.archetypeID);
if (archComp != 0) return archComp;
var chunkComp = chunkIndex.CompareTo(other.chunkIndex);
if (chunkComp != 0) return chunkComp;
return rowIndex.CompareTo(other.rowIndex);
```

### 2.11 LOW — ECB `DestroyEntities` ignores the batch path and ignores temp-entity maps

`EntityCommandBuffer.Playback` handles `ECBOpCode.DestroyEntities` by looping
`entityManager.DestroyEntity(...)` per entity (no batching), and `MapEntity`
never remaps temp entities for the batch. If users record
`DestroyEntities` with temp entities from the same ECB, those entities were
mapped to *real* entities by the earlier `CreateEntity` playback, but
`MapEntity` on a real entity with a positive ID returns it unchanged — so the
temp entities that were created in the same ECB are **never destroyed**. The
single `DestroyEntity` path *does* map; the batch path must map each element too
(and should use `DestroyEntities` on the manager for performance).

### 2.12 LOW — `World.Reset` leaks queries/archetypes created before the reset

`ComponentManager.Clear()` disposes and re-creates archetypes and queries, but
the **edge caches** inside entities' archetypes (`_edgesAdd`/`_edgesRemove`,
`_cleanupEdge`) are per-archetype so they go with the archetype — however any
`Identifier<EntityQuery>` / `Identifier<Archetype>` the user cached outside the
world (e.g. in a system) now points at fresh, unrelated data. The test
`TestWorld_Reset_SharedComponents` only checks entity state. Consider either
documenting "identifiers are invalidated by Reset" or making `Clear()` rebuild
with stable IDs (clear archetype *contents* rather than the archetype list).

---

## 3. Performance Optimizations

### 3.1 Lock on every `ComponentRegistry.GetComponentInfo` call (hot path)

`ComponentRegistry` guards *every* lookup (including `GetComponentInfo`, called
from `SetComponentData`, ECB playback per-op, `QueryBuilder.BuildQueryMask`, …)
with `s_registerLock`. `Lock` is a heavy monitor; this serializes all ECS work
on one OS lock and adds contention in multithreaded systems.

Fix: registration happens during module init (the code even has a NOTE about
this). Make `s_registeredComponents` an **append-only** structure (`UnsafeList`
or `List` that is only *read* after init) and switch read paths to lock-free
array reads: `s_registeredComponents[typeId]`. Keep the lock only in the
registration path, or use `ImmutableArray`-style swap semantics. Optionally add
a `static readonly` snapshot for the (rare) runtime registration case. This is
the single biggest systemic win.

### 3.2 Linear scans for shared groups and shared layouts

- `Archetype.AllocateEntity` / `AllocateEntities` scan `_chunkGroups` linearly
  and do a `SequenceEqual` per candidate — O(groups) per allocation, which
  matters when entities with distinct shared values are created in bulk.
  Fix: per-archetype `UnsafeHashMap<int hash, int groupIndex>` (hash →
  group). Verify equality only on hash hit (already the pattern).
- `GetSharedLayout` and `ChunkView.GetSharedComponent` scan `_sharedLayouts`
  linearly per call. Fix: reuse `_componentIDToLayoutIndex`-style lookup or a
  parallel array for shared layouts (archetypes typically have few shared
  components, so this is minor — but it's free to fix).

### 3.3 Bulk creation is not actually bulk

- `CreateEntities(int count, set)` and `CreateEntities(Span<Entity>, set)` call
  `AllocateEntity` per entity; only the empty-entity path uses
  `AllocateEntities`. For `count == 10_000` this is 10k `_chunkGroups` scans +
  chunk lookups. Route all creation through `AllocateEntities` (which is
  already written and — after fixing §2.1 — correct).
- ECB `CreateEntities` correctly batches, good; keep it that way.

### 3.4 `stackalloc` from user-controlled count

`CreateEntities(int count)` does `stackalloc int[count]` ×2 (plus the ECB
path's `CreateEntities(int, set)` has no stackalloc, but `EntityManager`
does). `count = 1_000_000` blows the 1 MB default stack. Fix: use a
`AllocationManager.CreateStackScope()` + `UnsafeArray<int>`/`UnsafeList` when
`count > threshold` (e.g. 256), or better, allocate slots in-place by chunk
without materializing index arrays: fill one chunk at a time and append
`EntityLocation`s directly.

### 3.5 ForEach version-marking before the callback + no change filtering

`EntityQuery.ForEach.gen.cs` marks chunks as changed (`MarkChanged`) *before*
invoking the callback, for every chunk of every matching archetype. Two
problems:

- False positives: a "changed" stamp is written even when the callback only
  reads.
- Worse: only components in `_mask.writeAccess` are stamped, and
  `QueryBuilder.WithAll` **never populates** `writeAccess` — the generated
  `ForEach<T0>(ref T0 ...)` callbacks therefore never mark anything, so change
  tracking is silently broken for the most common query form (see also the
  test-coverage gap "Versioning" in §4).
- No `HasChanged` filtering exists at all — there is no way to skip chunks that
  didn't change (the `HasChanged` API exists on `ChunkView` but ForEach never
  uses it).

Fix design: stamp only components the user could have written (already the
intent via `writeAccess` — make it *actually* populated for `ref` parameters
and add an opt-in `WithChanged<T>`/`WithChangedOnly` filter that skips chunks
via `HasChanged`); stamp after the callback *or* accept "at most one frame of
false positive" the way DOTS does, but never stamp for pure-read queries.

### 3.6 `IsEntityValid` re-fetches layouts per entity

`EntityQuery.IsEntityValid` calls `archetype.GetLayout(id)` (bounds check +
index indirection) for every bit in every mask, **per entity**, in the
`CalculateEntityCount` slow path. The `ForEach` template already hoists this
into per-chunk offset arrays (`reqOffsets[16]`) — apply the same hoisting to
`CalculateEntityCount` / `HasMatchingEntity` / the entity iterators.

### 3.7 JobChunk gathers raw pointers into an `UnsafeList` from the main thread

`ScheduleChunkParallel` snapshots `(Archetype*, Chunk*)` pointers into a
TempJob list. That's fine *only* if no structural change happens between
schedule and completion — which the API does not enforce or document. Add:

- a structural-version stamp check before scheduling (fail fast if
  `World.Version` moved), or better,
- schedule against chunk **indices** and resolve through the (stable) archetype
  list on the worker, keeping only `(archetypeID, chunkIndex)` pairs.

Also: `DisposeJobChunk` leaks the list if the job chain is canceled — handle
disposal on cancellation paths.

### 3.8 `GetWorldUncheck` / version reads

`World.Version` uses `Volatile.Read` on every chunk iteration (good), but
`ChunkView._currentVersion` is captured at view construction — fine. Consider
caching `_world` pointer in `Archetype` instead of `World.GetWorldUncheck`
(dictionary/list lookup) per `AllocateEntity` call.

### 3.9 Minor

- `Chunk._data` is a fixed 16 KB `UnsafeArray<byte>` per chunk — 16 KB is a
  great default, but consider making it configurable per-world (some games want
  32–64 KB chunks for fewer allocation stalls).
- `_layouts` per-entity `MemCpy` loops in `RemoveEntity`/`CopyData` could use
  `Unsafe.CopyBlock` on a precomputed combined stride — negligible, but the
  per-layout bounds checks are worth hoisting (they already are in the hot
  iterator paths).
- `Archetype.CreateNewChunk` memsets enable-bitmasks to `0xFF` — good, but it
  does it per enableable component; combine into one memset if offsets are
  contiguous (they are, per layout order).

---

## 4. Test Coverage Gaps

Current suite (5 files, ~1100 lines) covers: basic lifecycle, generational IDs,
add/remove/set component, singletons, migrate, query all/any/none, enableable
filters, ECB basics + temp entities, shared grouping + migration. **Missing:**

| Area | What's missing | Why it matters |
| --- | --- | --- |
| Bulk creation | `CreateEntities(2)` at a chunk boundary; multi-chunk bulk; `CreateEntities(1000+)` | Directly reproduces Bug §2.1 |
| Cleanup components | destroy with `ICleanupComponent`; destroy twice; cleanup archetype reached by multiple paths; cleanup entity *finally* destroyed | Bugs §2.4/§2.5 are entirely untested |
| Versioning | `GetComponentVersion`; `HasChanged`; `ChunkView.GetComponentDataRW` stamping; writeAccess marking through ForEach | Bugs §2.2, §3.5 |
| Enable bits | `GetEnableBits<T>`; `SetEnabled` on a **non-enableable** component (must not corrupt); disabled bit set after swap-remove | Bugs §2.3, §2.6 |
| Chunk recycling | create/destroy churn over many frames; `Collect()`; assert chunk list doesn't grow | Bug §2.7 |
| ECB edge cases | destroy a temp entity created in the same ECB; `DestroyEntities` with temp IDs; AddComponent on already-destroyed temp; ECB playback twice without Reset | Bug §2.11 |
| Migrate | `MigrateEntity` with shared components; same-archetype + different-shared-value migration (the `return Error.None` shortcut in `MigrateEntity` needs a test — see §2.12 note on shared-group short-circuits) | Logic paths with no tests |
| Stress/randomized | fuzz: random add/remove/set/enable/destroy across archetypes, verify invariants (EntityCount, chunk counts, location consistency) after each op | Catches swap/removal index bugs like §2.5 |
| Jobs | `ScheduleChunkParallel` with a real `JobScheduler`, verify every chunk visited exactly once | Untested API surface |
| Multiple worlds | parallel worlds with the same component set; world slot reuse after `Destroy` (the `s_freeWorldSlots` path) | `WorldTests` only tests 2 worlds |

Also worth adding: a `GHOST_SAFETY_CHECKS` build-mode test run (the code is
littered with `#if` guards that are never exercised by the unit tests), and a
deterministic debug-assert that `EntityLocation` invariants hold after every
structural operation.

---

## 5. New Features (ordered by value)

1. **`WithChanged<T>` / change-filtered ForEach** — the single most useful
   missing system feature. Chunk-level skip via existing version arrays is
   already half-built (`HasChanged` exists); wire it into the generated ForEach
   templates and the entity iterators.

2. **`EntityCommandBuffer.SetEnabled`** — enable/disable is currently only
   available synchronously. Add the op code + playback case; ECB consumers
   (jobs) can then toggle enablement safely.

3. **Singleton API completion** — `GetSingletonEntity<T>()`,
   `DestroySingleton<T>()`, `SetSingleton<T>`; make `CreateSingleton` return
   the entity (not just `Error`), and enforce one-singleton-per-type per world.

4. **Query `WithSharedComponentValue<T>(T value)`** — filter chunks by shared
   value equality (hash compare) instead of forcing users to manually check
   `chunk.GetSharedComponent<T>()` in the callback. Cheap: chunk-group hash is
   already stored on `ChunkGroup`.

5. **`IComponentData` default values / constructor callbacks on add** — a
   `static void Create(ref T)` hook (DOTS-style `IComponentData` implicit
   default construction) so users can avoid zero-init bug class. Alternatively
   document that components are zero-initialized and let `AddComponent` take a
   factory delegate.

6. **Chunk-query cache & structural change notifications** — a
   `ComponentSystemBase`-style `OnCreateForCompiler`-like API is overkill, but
   `SystemBase.RequireQueryForUpdate` already exists; add `OnQueryChanged`
   callbacks via the existing version stamps, or a dirty-flag per query updated
   when `CreateArchetype`/`AddArchetypeIfMatch` runs.

7. **Prefab / archetype cloning** — `EntityManager.Instantiate(Entity template)`
   replicating all components (including shared) to a new entity; trivial on
   top of `CopyData` + `CreateEntities(set)` and unlocks spawn systems.

8. **Entity queries as `IEnumerable` with LINQ-compatible filtering** — the
   `ChunkIterator` exists; a `Where`-style fluent filter that uses the SIMD
   block masks from `HasMatchingEntity` would give users a safe, fast
   "collect matching entities" API without codegen.

9. **Serialization** — `Entity` already has `[JsonIgnore]` generation; add a
   world snapshot (archetype signature → component blob arrays) for save/load
   and network replication. Combined with #7 this makes the ECS usable for
   actual game content.

10. **Managed components (re-enable `ManagedComponent.cs`)** — the whole file
    is `#if false` dead code. Either finish it (hook `DestroyEntity`/cleanup
    paths to call `OnDestroy`, wire into `CopyData` for `Managed<T>` handles)
    or delete it — dead code in a core library is a maintenance trap.

11. **System-level chunk callbacks** — `IJobChunk` exists; add
    `ISystemForEach`-style automatic `Execute(ref T, ...)` dispatch generated
    by `Ghost.Generator` (the `SoaGenerator`/`ComponentRegistrationGenerator`
    already prove the source-gen pipeline exists) so users can write systems
    without hand-rolled ForEach loops.

12. **`EntityQuery` batch operations** — `query.DestroyAllEntities()` and
    `query.SetComponentAll<T>(value)` using `RemoveEntities` bulk path; avoids
    per-entity structural calls in teardown/wipes.

---

## 6. Code-Quality Notes (non-bugs)

- `SharedComponent.cs` and `ManagedComponent.cs` are fully `#if false`-disabled
  (plus a `SystemGroupRegistry` block in `System.cs`). Delete or resurrect;
  dead code hides bugs (the disabled `SharedComponentStore` has its own bugs —
  hash-key collisions on `(typeId << 32) ^ hash` with no secondary check).
- `ComponentTypeID<T>` static ctor calls `GetOrRegisterComponentID<T>()` which
  takes the global lock — first-touch cost for every component type in every
  process; fine at init, but see §3.1 for making it lock-free.
- `Entity.IsValid`'s "Temp entities have negative generation" contract is
  implicit — the ECB creates `new Entity(tempId, -1)`; a doc comment on
  `Entity` explaining the ID/generation sign conventions would prevent misuse.
- `ChunkDebugView` uses reflection + `MakeGenericMethod` per component — debug
  only, fine, but it runs under `#if DEBUG` yet the class itself is compiled in
  Release (dead weight).
- `GetHashCode` implementations that cache `-1` as "unset" conflict with real
  `-1` hashes (see §2.9); prefer `bool _hashComputed` or lazy fields.
- `CreateSingleton` returns `Error.InvalidArgument` when the singleton
  *already exists* — ambiguous error semantics; consider a dedicated
  `Error.AlreadyExists`.
- Minor: `GetWorldUncheck` name (typo), `QueryBuilder` fields `_present` vs
  `_absent` naming, `World` XML doc typo ("publicntity").

---

## 7. Suggested Fix Order (cheapest → safest)

| # | Change | File(s) | Est. effort |
| --- | --- | --- | --- |
| 1 | Fix §2.1 off-by-one | `Archetype.cs` | 1 line |
| 2 | Fix §2.2, §2.3 version/layout indexing | `Query.cs` | 4 lines |
| 3 | Fix §2.6 `SetEnabled` guard | `EntityManager.cs` | 3 lines |
| 4 | Fix §2.10 `CompareTo` | `EntityManager.cs` | 5 lines |
| 5 | Fix §2.9 `Equals` hash short-circuit | `Component.cs` | 6 lines |
| 6 | Fix §2.11 ECB batch destroy mapping | `EntityCommandBuffer.cs` | 10 lines |
| 7 | Add tests for 1–6 first (red), then fix | `Test/.../ECS` | 1 day |
| 8 | Rework cleanup-entity paths (§2.4/§2.5) with batched merged removals | `EntityManager.cs`, `Archetype.cs` | 2–3 days |
| 9 | Rework `Collect()` → inline chunk recycling (§2.7) | `Archetype.cs` | 1–2 days |
| 10 | Lock-free component registry reads (§3.1) | `Component.cs` | 1 day |
| 11 | Hash-indexed chunk groups (§3.2) + bulk `AllocateEntities` everywhere (§3.3) | `Archetype.cs`, `EntityManager.cs` | 2 days |
| 12 | `WithChanged<T>` + writeAccess population (§3.5) | templates, `QueryBuilder` | 2–3 days |
| 13 | Pointer-free job chunk scheduling (§3.7) | `EntityQuery.JobChunk.cs` | 1 day |

Items 1–6 are small, mechanical, and independently testable. Items 8–9 need
the most care because they touch the swap-removal invariants. Items 10–12 are
the ones that will show up in profiling once the correctness layer is solid.
