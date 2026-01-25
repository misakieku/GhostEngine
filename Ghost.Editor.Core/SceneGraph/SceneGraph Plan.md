# Architecture Plan: Scene Graph and Scene Representation

The Scene Graph is a hierarchical structure that represents all the objects and entities within a 3D scene in the Ghost Editor.

## Scene Graph (Editor representation of runtime data)

There should be two main types of nodes in the Scene Graph:
1. **Entity Node**: Represents an individual entity within a scene. Name stored here, not runtime component.
2. **Scene Node**: Represents a Scene object, which can contain multiple entities. Name stored here not runtime data.

### Editor World

Editor contains a different world compares to the runtime world. When user click the Play button, we will create a runtime world and load the scene data from the editor world to the runtime world.
This allows us to
 1. Unload the runtime only systems like physics, rendering, etc when user stop playing.
 2. Load editor only systems like gizmos, debug, etc when user stop playing.
 3. Allow editor only entities like editor camera, editor lights, etc to exist in the editor world without affecting the runtime world.

### Editor Hierarchy

The Scene Graph should be represented as a tree structure in the editor (TreeView in WinUI 3), where:
 - The top level nodes represents the loaded Scenes in the editor world.
 - Levels below the Scene nodes represents the Entity nodes that belong to that scene.
 - Each Entity node can have child Entity nodes representing parent-child relationships between entities.

An example hierarchy could look like this:
```
- Scene 1
  - Entity A
    - Entity B
  - Entity C
- Scene 2
    - Entity D
```

## Scene (The runtime representation)

A Scene is a collection of entities with SceneID component from a world that are grouped together. There can be multiple scenes in a world.

### Save a Scene
When save a scene, all entities with the SceneID component matching the scene's ID should be included in the saved data.
When an Entity references another Entity in the same scene, we should store the file local id instead of the global entity id.
For example, if Entity A (id: 10, 5th in scene) references Entity B (id: 20, 50th in scene) in the same scene, in the saved data for Entity A,
we should store 50 (the file local id) as the reference to Entity B instead of 20 (the global entity id).

 > We does not allow cross-scene references for now because ideally it's not a good practice to have cross-scene references.
   We can use query or singleton pattern to access entities from other scenes if needed because they are in the same world.

### Load a Scene
When loading a scene, we need to reconstruct the entities and their relationships based on the saved data.

 1. We allocate the entities in the world and assign them new global entity IDs.
 2. We remap the file local IDs to the new global entity IDs and change the references accordingly.
    For example if Entity A (file local id: 5) references Entity B (file local id: 50) in the saved data,
    we need to find the new global entity IDs for both entities after loading and update the reference in Entity A to point to the new global entity ID of Entity B.

### Data format
The scene data should be stored in a structured format (e.g., JSON or binary) that includes:
 - Scene metadata (e.g., name, ID. Note that name of entity and scene are editor only data, should be included inside SceneNode and EntityNode, not runtime data)
 - List of entities with their components and properties (Entities must in the order that file local id directly maps to the index in the list)
 - References between entities using file local IDs

JSON should only be used in the editor and JSON serialization/deserialization logic should also only exist in the editor codebase (Ghost.Editor.Core). Reflection is allowed here.
Binary format should be used in the runtime for better performance. The runtime codebase (Ghost.Engine) must be aot compatible.

Currently we strict the IComponent to must be unmanaged and blittable types.
However, we also support ManagedEntity and ManagedEntityRef with ScriptComponent to allow OOP like logic for common gameplay logic that DOD pattern is not suitable for.
Serializing/deserializing with those components will be tricky. We can use MemoryPack for binary serialization/deserialization because it supports both unmanaged and managed types.