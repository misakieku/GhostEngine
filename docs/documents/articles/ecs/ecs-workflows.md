# ECS Workflows

This document outlines standard daily workflows for working within GhostEngine's ECS.

## Creating Entity Queries

To read or modify data, you first build an `EntityQuery`. You use the `QueryBuilder` struct to define inclusion and exclusion constraints.

```csharp
Identifier<EntityQuery> queryId = QueryBuilder.Create()
    .WithAll<Position>()           // Must have Position
    .WithAny<Player, Enemy>()      // Must have Player OR Enemy
    .WithNone<Dead>()              // Must NOT have Dead structurally
    .WithDisabled<Renderable>()    // Must have Renderable structurally, but it must be disabled
    .Build(world);
```

Queries are hashed and cached internally by the `ComponentManager`. Building the same query mask twice will yield the same `Identifier<EntityQuery>`.

## Iterating Chunk Views

Once you have a query, use `GetChunkIterator()` to traverse the unmanaged memory directly. This returns a `ChunkView` struct.

Because the data is unmanaged, you can extract `Span<T>` and modify it with extreme speed.

```csharp
ref var query = ref world.ComponentManager.GetEntityQueryReference(queryId);

foreach (var chunkView in query.GetChunkIterator())
{
    // Get Read-Only Span
    ReadOnlySpan<Velocity> vels = chunkView.GetComponentData<Velocity>();
    
    // Get Read-Write Span
    Span<Position> pos = chunkView.GetComponentDataRW<Position>();

    // For loop for cache-aligned processing
    for (int i = 0; i < chunkView.EntityCount; i++)
    {
        pos[i].Value += vels[i].Value * dt;
    }
}
```

> **Note:** Accessing data using `GetComponentDataRW<T>()` will automatically bump the internal version numbers for that component type in that specific chunk, allowing other systems to detect changes efficiently.

## Entity Command Buffers (Structural Changes)

In ECS, structural changes (adding/removing components, destroying entities) reorganize chunk memory. Doing this while iterating over chunks invalidates the memory layout. 

To safely defer structural changes, use an `EntityCommandBuffer`. 

```csharp
// Get the world's main ECB
var ecb = world.EntityCommandBuffer;

foreach (var chunk in query.GetChunkIterator())
{
    var entities = chunk.GetEntities();
    var healths = chunk.GetComponentData<Health>();

    for (int i = 0; i < chunk.EntityCount; i++)
    {
        if (healths[i].Value <= 0)
        {
            // Defer the destruction until Playback() is called
            ecb.DestroyEntity(entities[i]);
        }
    }
}
```

At the end of the frame (or the end of the system group update), you must call:
```csharp
world.PlaybackEntityCommandBuffers();
```

### Multi-threading ECBs

When using the `JobScheduler`, you can record deferred commands across multiple worker threads concurrently by requesting thread-local ECBs:

```csharp
int workerIndex = JobScheduler.CurrentThreadIndex;
var threadEcb = world.GetThreadLocalEntityCommandBuffer(workerIndex);

// Safely record structural changes from parallel jobs
threadEcb.AddComponent<Dead>(entity);
```
