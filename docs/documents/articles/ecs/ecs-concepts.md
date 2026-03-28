# ECS Concepts

GhostEngine's ECS implementation relies on several key concepts rooted in Data-Oriented Design. Understanding these concepts is essential to writing performant engine code and gameplay logic.

## World

A `World` encapsulates all data and logic for an independent ECS simulation. It owns three core managers:
- **EntityManager:** Handles the creation, destruction, and structural modification of Entities.
- **ComponentManager:** Manages Archetypes and EntityQueries, keeping track of component memory layouts and chunk allocations.
- **SystemManager:** Manages the lifecycle and execution order of all Systems.

Multiple Worlds can exist simultaneously (e.g., one for logic, one for rendering, or server/client splits in multiplayer).

## Entity

An `Entity` in Ghost.Entities is not an object or a class; it is merely an integer ID (often accompanied by a version number). It serves as a lookup key to find a specific set of component data.

## Component

A `Component` is a pure, unmanaged data structure (`struct`). It implements `IComponent` or `IEnableableComponent`. Components contain no logic. By restricting components to unmanaged types, the engine ensures they can be packed tightly into continuous blocks of memory (Chunks), entirely avoiding Garbage Collection (GC) overhead and ensuring fast CPU cache lines.

## Archetype and Chunks

When you create an entity with a specific set of components (e.g., `Position` and `Velocity`), the ECS groups it into an **Archetype**. An Archetype represents a unique signature of components.

Entities of the same Archetype have their component data stored in **Chunks**. A Chunk is a fixed-size block of unmanaged memory (e.g., 16KB). 

Instead of storing arrays of Entities containing objects (Array of Structures), the Chunk stores independent arrays for each component type (Structure of Arrays). This means when a system queries for `Position`, it receives a contiguous unmanaged memory `Span<Position>`, allowing the CPU to aggressively prefetch data and vectorize operations.

## System

A `System` provides the logic that transforms component data from one state to the next. Systems iterate over matching Archetypes using `EntityQuery` and process component data in bulk. Systems can be managed manually or grouped into a `SystemGroup` which topologically sorts execution order based on dependencies.
