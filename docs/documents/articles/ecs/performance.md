# Performance and Memory

GhostEngine's ECS is explicitly designed around maximum CPU efficiency and AOT compatibility.

## Structure of Arrays (SoA)

Traditional OOP patterns use Arrays of Structures (AoS), which causes cache misses when looping through objects to read a single field.

GhostEngine ECS uses Structure of Arrays (SoA) at the chunk level. A 16KB unmanaged `Chunk` separates components into continuous blocks. When a `System` iterates a `ChunkView` and calls `GetComponentData<Position>()`, it receives a contiguous `Span<Position>`. 

Because modern CPUs pre-fetch continuous memory aggressively, an ECS query can be up to orders of magnitude faster than naive OOP equivalents.

## Benchmark Example

The internal `Ghost.Entities.Test` benchmarks demonstrate the raw speed comparison:

```csharp
// OOP Approach (~4000ns)
for (var i = 0; i < _gameObjects.Length; i++)
{
    _gameObjects[i].Position += new Vector4(_dt, _dt, _dt, 0);
}

// ECS Approach (~620ns)
ref var query = ref _world.ComponentManager.GetEntityQueryReference(_queryIdentifier);
foreach (var chunkView in query.GetChunkIterator())
{
    var positions = chunkView.GetComponentDataRW<Position>();
    ref var address = ref MemoryMarshal.GetReference(positions);

    for (var i = 0; i < positions.Length; i++)
    {
        Unsafe.Add(ref address, i).value += new Vector4(_dt, _dt, _dt, 0);
    }
}
```

**Results for 1,000,000 Entities:**
- Over **98.5% cache hits**
- Approximately **4x to 6x faster** execution times
- **0 Bytes** allocated per frame (Zero Garbage Collection)

## Versioning and Change Tracking

Chunks maintain atomic version counters. Whenever `GetComponentDataRW<T>()` is called, the chunk updates its component version to match the current `World.Version`. 

Systems can rapidly bypass entire chunks without iterating them by checking `chunkView.HasChanged<T>(LastSystemVersion)`. This enables dirty-flag processing at near-zero cost.

## Unmanaged Constraints

To guarantee this performance, the engine strictly enforces unmanaged types. 
- You cannot put `string`, `class`, or managed arrays inside `IComponent`.
- For bulk temporary allocations during system updates, you should use `VirtualStack.Scope` or `AllocationManager` alongside `UnsafeList<T>` to remain entirely off the .NET Garbage Collector heap.
