# Scene Graph Implementation Guide

This document provides an overview of the scene graph system implementation and integration points.

## Architecture Overview

The scene graph system is **editor-only** and follows a clean separation between editor metadata and runtime data:

- **Editor Layer** (Ghost.Editor.Core): SceneNode, EntityNode, SceneGraph, Serialization
- **Runtime Layer** (Ghost.Engine): Minimal components - SceneID, Hierarchy, LocalToWorld

## Core Classes

### SceneNode (Editor-only)
- **Location**: `Ghost.Editor.Core/SceneGraph/SceneNode.cs`
- **Purpose**: Represents a scene in the editor hierarchy
- **Metadata**: Name, SceneId, SceneGuid, List of child EntityNodes
- **Key Methods**: FindEntityNode()

### EntityNode (Editor-only)
- **Location**: `Ghost.Editor.Core/SceneGraph/EntityNode.cs`
- **Purpose**: Represents an entity in the editor hierarchy
- **Metadata**: Name, EntityId, FileLocalId, ParentNode, Children, IsExpanded, IsSelected
- **Key Methods**: FindRecursive(), GetDepth(), GetAllDescendants()

### SceneGraph (Editor View-Model)
- **Location**: `Ghost.Editor.Core/SceneGraph/SceneGraph.cs`
- **Purpose**: Main view-model providing hierarchical access to scenes and entities
- **Key Features**:
  - Maintains scenes and entity hierarchy
  - O(1) lookup via internal caches
  - Manages parent-child relationships
  - Provides queries for UI rendering
- **Key Methods**:
  - AddScene(), RemoveScene(), GetSceneNode()
  - AddEntity(), RemoveEntity(), GetEntityNode()
  - SetEntityParent()
  - GetEntitiesInScene()
  - RebuildFromWorld()

### Serialization Classes

#### IdRemapTable
- **Location**: `Ghost.Editor.Core/SceneGraph/Serialization/IdRemapTable.cs`
- **Purpose**: Maps file-local entity IDs to runtime global entity IDs
- **Key Methods**:
  - Register(fileLocalId, globalEntityId)
  - GetGlobalId(), GetLocalId()
  - RemapReference() - throws if not found

#### SceneSerializationContext
- **Location**: `Ghost.Editor.Core/SceneGraph/Serialization/IdRemapTable.cs`
- **Purpose**: Container for serialization metadata
- **Contents**:
  - IdRemap: Remapping table
  - SceneId, EditorWorld
  - EntityOrder: List of entities in file order
  - ValidationErrors: Errors encountered during load

#### SceneAssetData (JSON model)
- **Location**: `Ghost.Editor.Core/SceneGraph/Serialization/SceneAssetData.cs`
- **Purpose**: JSON-serializable representation of a scene
- **Structure**:
  - Version, SceneGuid, Name, SceneId
  - Entities: List of EntityData (index = file-local ID)

#### ComponentData (JSON model)
- **Location**: `Ghost.Editor.Core/SceneGraph/Serialization/SceneAssetData.cs`
- **Purpose**: JSON-serializable representation of a component instance
- **Structure**:
  - ComponentTypeName: Full type name
  - Data: Dictionary of field name -> value

#### EntityData (JSON model)
- **Location**: `Ghost.Editor.Core/SceneGraph/Serialization/SceneAssetData.cs`
- **Purpose**: JSON-serializable representation of an entity
- **Structure**:
  - FileLocalId: Position in entities list
  - Name: Editor-only display name
  - ParentFileLocalId: Reference to parent (file-local)
  - Components: List of ComponentData

#### SceneSerializer
- **Location**: `Ghost.Editor.Core/SceneGraph/Serialization/SceneSerializer.cs`
- **Purpose**: Handles save/load operations
- **Key Methods**:
  - SaveScene(sceneGraph, sceneId, editorWorld) -> SceneAssetData
  - LoadScene(sceneGraph, sceneData, editorWorld, context)
  - ValidateNoInvalidReferences()

## Integration Points

### 1. Component Serialization
The system uses reflection to serialize/deserialize components. You need to:
- Implement component field reflection in `SceneSerializer.SerializeEntityComponents()`
- Handle special cases like Entity references (remap to file-local IDs)
- Support both unmanaged and managed components (via ManagedEntity/ManagedEntityRef)

**Key challenge**: Entity references must be stored as file-local IDs and remapped on load.

### 2. World Integration
The `SceneGraph.RebuildFromWorld()` method needs:
- Query entities with `SceneID` component
- Extract `Hierarchy` component data to build parent-child relationships
- Build EntityNode tree from ECS data

**Expected query**:
```csharp
var query = world.QueryBuilder()
    .With<SceneID>()
    .With<Hierarchy>()
    .Build();
```

### 3. Runtime Components Required
Add to `Ghost.Engine/Components/`:
- `SceneID`: Already exists - tags entities with scene membership
- `Hierarchy`: Already exists - stores parent/firstChild/nextSibling
- `LocalToWorld`: Already exists - for transform hierarchies (optional integration)

### 4. File I/O
You need to implement:
- JSON file loading/saving (use System.Text.Json)
- Scene asset file paths (e.g., `Assets/Scenes/SceneName.scene.json`)
- Asset database integration for scene asset tracking

## Data Flow

### Saving a Scene

```
SceneGraph (editor view-model)
    ↓
SceneSerializer.SaveScene()
    ↓
Serialize EntityNode tree to SceneAssetData
    - Build entities list in deterministic order
    - Convert Entity references to file-local IDs
    - Validate no cross-scene references
    ↓
SceneAssetData (JSON model)
    ↓
System.Text.Json serialization
    ↓
Scene file (.scene.json)
```

### Loading a Scene

```
Scene file (.scene.json)
    ↓
System.Text.Json deserialization
    ↓
SceneAssetData (JSON model)
    ↓
SceneSerializer.LoadScene()
    - Create entities in editor world
    - Build IdRemapTable (file-local -> global)
    - Remap component entity references
    - Establish parent-child relationships
    ↓
SceneGraph (editor view-model)
    ↓
UI displays hierarchy
```

## File-Local ID Remapping

**Critical concept**: Entities must maintain stable file-local IDs for serialization.

**Serialization**:
```
Entities in scene (in order):
[Entity A (global: 10), Entity B (global: 20), Entity C (global: 30)]

File-local IDs: 0, 1, 2

Entity references stored as file-local:
- If Entity A refers to Entity B: store 1 (file-local of B)
```

**Deserialization**:
```
1. Allocate new entities: Entity A' (global: 50), Entity B' (global: 51), Entity C' (global: 52)
2. Build IdRemapTable:
   - 0 -> 50 (A')
   - 1 -> 51 (B')
   - 2 -> 52 (C')
3. Remap references:
   - Entity A's reference to 1 -> becomes reference to 51 (Entity B')
```

## Next Steps for Integration

1. **Implement component reflection** in `SceneSerializer`:
   - Use `System.Reflection` to get component type and fields
   - Handle custom serialization for entity references
   - Support nullable types and managed components

2. **Implement world query integration** in `SceneGraph.RebuildFromWorld()`:
   - Query entities with SceneID and Hierarchy components
   - Build EntityNode hierarchy from Hierarchy component data
   - Handle entities without parents (root entities in scene)

3. **Implement file I/O**:
   - Create scene asset loader/saver
   - Integrate with asset database
   - Handle file paths and metadata

4. **Add UI components** (in editor UI layer):
   - TreeView binding to SceneGraph.Scenes
   - Entity selection and renaming UI
   - Drag-drop parent reassignment

5. **Test edge cases**:
   - Loading scenes with missing entities
   - Cross-scene reference validation
   - Circular parent-child relationships
   - Very large scenes with many entities

## Key Design Decisions

1. **Editor-only metadata**: Names, selection state, expansion state are stored in SceneNode/EntityNode only, not at runtime.

2. **File-local IDs**: Provide stable references for serialization independent of runtime entity allocation order.

3. **Minimal runtime**: Only SceneID, Hierarchy, LocalToWorld in runtime; no scene names, display data, etc.

4. **Reflection in editor**: Allows flexibility and OOP patterns that aren't AOT-compatible.

5. **No cross-scene references**: Enforced by validation; use queries/singletons for cross-scene access.

6. **Hierarchy as component**: Parent-child relationships use Hierarchy component, making them queryable in ECS systems.

## Common Pitfalls

- **Circular parent-child references**: Add validation in SetEntityParent()
- **File-local ID collision**: Ensure Index-based ID assignment is stable
- **Missing entity references**: Catch during load, report validation errors
- **Type name changes**: Store full namespace+typename; handle version migration if types rename
- **Managed component fields**: Require special serialization (MemoryPack recommended)
