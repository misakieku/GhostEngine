# Components Overview

In GhostEngine, components are strict, unmanaged data structures. They must implement the empty interface `IComponent` (or its derivations). This ensures AOT compatibility and zero GC allocation overhead when iterating over millions of entities.

## Defining a Component

To define a standard component, create an unmanaged struct implementing `IComponent`:

```csharp
using Ghost.Entities;
using System.Numerics;

public struct TransformComponent : IComponent
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}
```

## Enableable Components

Sometimes you need to toggle a component on or off without incurring the heavy structural cost of removing and re-adding the component from an entity. GhostEngine supports this via `IEnableableComponent`.

```csharp
public struct Renderable : IEnableableComponent
{
    public int MeshID;
    public int MaterialID;
}
```

When iterating over an archetype chunk, you can check the bitmask to see if an entity's enableable component is currently active:

```csharp
// During a query chunk iteration:
var renderables = chunkView.GetComponentData<Renderable>();
var enableBits = chunkView.GetEnableBits<Renderable>();

for (int i = 0; i < chunkView.EntityCount; i++)
{
    if (chunkView.IsComponentEnabled<Renderable>(i))
    {
        // Process active renderable
    }
}
```

## Component Type IDs

The engine assigns a unique runtime ID to each unmanaged component type. You can retrieve this identifier using the `ComponentTypeID<T>.Value` generic cache:

```csharp
Identifier<IComponent> id = ComponentTypeID<TransformComponent>.Value;
```

This ID is heavily used under the hood for query masking, archetype matching, and memory offset calculations inside a chunk.

## RequireComponent Attribute

You can enforce dependencies between components using the `[RequireComponentAttribute<T>]`. While this is mostly semantic, it helps structure dependencies for complex systems.

```csharp
[RequireComponent<TransformComponent>]
public struct PhysicsCollider : IComponent
{
    public float Radius;
}
```
