# Getting Started with Ghost.Entities

GhostEngine's Entity Component System (ECS) is a high-performance, Data-Oriented architecture designed for C# .NET 10. The ECS runtime is strictly AOT-compatible and focuses on unmanaged data structures to maximize cache locality and performance.

## Initialization

The core of the ECS is the `World`. A `World` contains an `EntityManager`, `ComponentManager`, and `SystemManager`. 

To get started, you must create a `World`:

```csharp
using Ghost.Entities;

// Create a new World with a default entity capacity of 16
World world = World.Create();
```

## Creating Entities and Adding Components

Entities are simply IDs that point to a combination of components. You use the `EntityManager` to create and modify them.

First, define your unmanaged components:

```csharp
public struct Position : IComponent
{
    public float X, Y, Z;
}

public struct Velocity : IComponent
{
    public float X, Y, Z;
}
```

Then, create entities and attach components:

```csharp
using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;

// Creating a ComponentSet for fast archetype initialization
using var scope = AllocationManager.CreateStackScope();
var componentSet = new ComponentSet(
    scope.AllocationHandle, 
    ComponentTypeID<Position>.Value, 
    ComponentTypeID<Velocity>.Value
);

// Create 1000 entities with both Position and Velocity
world.EntityManager.CreateEntities(1000, componentSet);
```

## Adding Systems and Updating

Systems contain the logic that operates on the component data. You add systems to the `SystemManager` and run them inside a loop.

```csharp
// Get the DefaultSystemGroup to attach systems
var group = world.SystemManager.GetSystem<DefaultSystemGroup>();

// Add custom systems
group.AddSystem<MovementSystem>();

// Sort systems based on their dependencies
group.SortSystems();

// Initialize all systems
world.SystemManager.InitializeAll(new TimeData());

// Update loop
while (running)
{
    world.SystemManager.UpdateAll(timeData);
    world.PlaybackEntityCommandBuffers();
}

// Cleanup at the end
world.Dispose();
```

## Summary

In GhostEngine ECS:
1. **World** manages the lifecycle of your ECS data.
2. **Components** (`IComponent`) are unmanaged data structures.
3. **Entities** are logical containers combining those components into archetypes.
4. **Systems** (`SystemBase`) execute logic over arrays of components using `EntityQuery`.
