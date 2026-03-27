# Systems Overview

Systems are where all your ECS logic lives. They interact with data by iterating over entities that match an `EntityQuery`.

## The `ISystem` Interface

At its core, a system implements `ISystem`, which provides three lifecycle methods:

```csharp
public interface ISystem
{
    void Initialize(ref readonly SystemAPI systemAPI);
    void Update(ref readonly SystemAPI systemAPI);
    void Cleanup(ref readonly SystemAPI systemAPI);
}
```

The `SystemAPI` struct provides access to the `World` and `TimeData` for the current frame.

## `SystemBase`

For most gameplay code, you should inherit from the abstract class `SystemBase`, which provides convenient wrappers and handles "Should Update" logic automatically.

```csharp
public class MovementSystem : SystemBase
{
    private Identifier<EntityQuery> _query;

    protected override void OnInitialize(ref readonly SystemAPI systemAPI)
    {
        // Build a query for entities that have both Position and Velocity
        _query = new QueryBuilder()
            .WithAllRW<Position>()
            .WithAll<Velocity>()
            .Build(World);

        // Tell the system base to ONLY run OnUpdate if this query has at least 1 entity
        RequireQueryForUpdate(_query);
    }

    protected override void OnUpdate(ref readonly SystemAPI systemAPI)
    {
        float dt = systemAPI.Time.DeltaTime;
        
        ref var query = ref World.ComponentManager.GetEntityQueryReference(_query);
        
        foreach (var chunk in query.GetChunkIterator())
        {
            var positions = chunk.GetComponentDataRW<Position>();
            var velocities = chunk.GetComponentData<Velocity>();

            for (int i = 0; i < chunk.EntityCount; i++)
            {
                positions[i].Value += velocities[i].Value * dt;
            }
        }
    }
}
```

### Automatic Update Triggers

Notice the `RequireQueryForUpdate(_query)` call. `SystemBase` will skip calling `OnUpdate` if the query results are completely empty, saving processing time. `SystemBase` also invokes `OnStartRunning` and `OnStopRunning` when the system transitions between having matching entities and not.

## System Execution Order

By default, systems execute in the order they are added to a `SystemGroup`. To enforce execution order regardless of initialization sequence, use the `[UpdateAfter]` and `[UpdateBefore]` attributes.

```csharp
[UpdateAfter(typeof(PhysicsSystem))]
[UpdateBefore(typeof(RenderSystem))]
public class MovementSystem : SystemBase
{
    // ...
}
```

When you call `group.SortSystems()`, the `SystemGroup` uses Kahn's algorithm to perform a topological sort and resolves these dependencies. If circular dependencies are detected, the engine will throw an exception.

## System Groups

`SystemGroup`s implement `ISystem` themselves, meaning you can nest groups within groups to build complex hierarchical update loops (e.g. `FixedUpdateGroup`, `PresentationGroup`). GhostEngine provides a `DefaultSystemGroup` that is automatically instantiated by the `SystemManager`.
