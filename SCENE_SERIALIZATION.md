# Scene Serialization Implementation Summary

## Overview
Implemented a dual-format scene serialization system for GhostEngine:
- **Binary format** for runtime (AOT-compatible, fast)
- **JSON format** for editor (reflection-based, human-readable)

Both formats support automatic Entity reference remapping.

## Architecture

### Two Serialization Paths

#### **Runtime Path (Ghost.Engine)** - AOT-Compatible
```
SceneManager → SceneBinarySerializer → SerializationContext
```
- Binary format using direct memory operations (`memcpy`)
- No reflection, no System.Text.Json dependency
- Fast, compact, suitable for shipping builds
- Synchronous operations

#### **Editor Path (Ghost.Editor.Core)** - Reflection-Based
```
EditorSceneManager → SceneSerializer → EntityJsonConverter → SerializationContext
```
- JSON format using System.Text.Json with reflection
- Human-readable, debuggable
- Automatic Entity remapping via custom converter
- Async operations

### Core Components

#### 1. **SerializationContext.cs** (Ghost.Engine/IO)
- **Shared by both runtime and editor**
- Thread-safe context using `AsyncLocal<T>` for managing Entity ID remapping
- Maps file-local IDs (0, 1, 2...) to runtime Entity instances
- Bidirectional mapping for both serialization and deserialization
- Usage pattern:
  ```csharp
  using var context = SerializationContext.Create();
  context.RegisterEntity(fileId, runtimeEntity);
  ```

#### 2. **SceneBinarySerializer.cs** (Ghost.Engine/IO)
- **Runtime binary serialization** - AOT-compatible
- Static utility class with synchronous methods
- **Serialize**: Writes entities to BinaryWriter using raw memory operations
  - Format: Magic number (0x47534345 "GSCE"), version, entity count, component data
  - Uses `memcpy` for component data - zero reflection
  - Implements Entity reference remapping for Hierarchy component
- **Deserialize**: Two-pass loading strategy
  - **Pass 1**: Create all entities, build ID mapping
  - **Pass 2**: Read and copy component data, remap Entity references
- **RemapEntityReferences**: Manual remapping for components with Entity fields (currently Hierarchy)

#### 3. **EntityJsonConverter.cs** (Ghost.Editor.Core/Serializer/Converters)
- **Editor-only** custom `JsonConverter<Entity>`
- Automatically remaps Entity references during JSON serialization/deserialization
- During **serialization**: Writes file-local ID from SerializationContext
- During **deserialization**: Reads file-local ID and translates to runtime Entity
- Enables deep Entity reference remapping in nested components (e.g., Hierarchy)

#### 4. **SceneSerializer.cs** (Ghost.Editor.Core/Serializer)
- **Editor-only** static utility class for JSON scene file I/O
- **SaveSceneAsync**: Queries entities by SceneID, serializes components using reflection
- **LoadSceneAsync**: Two-pass loading strategy with automatic Entity remapping
  - **Pass 1**: Create all entities, build ID mapping
  - **Pass 2**: Deserialize components with automatic Entity remapping via EntityJsonConverter
- File format: JSON with entities array containing component type names and data

#### 5. **Scene.cs** (Ghost.Engine/Core)
- Lightweight handle class with World reference, SceneID, and Name
- No longer owns the World - respects "database pattern"
- Constructor: `Scene(World world, string name)`
- Implements IDisposable and IEquatable

#### 6. **SceneManager.cs** (Ghost.Engine/Services)
- **Runtime scene lifecycle manager** - uses binary serialization
- **LoadScene**: Synchronous, loads from binary file, supports Single/Additive modes
- **SaveScene**: Synchronous, saves scene to binary file
- **UnloadScene**: Efficiently destroys all entities with matching SceneID
- Maintains registry of loaded scenes per World

#### 7. **EditorSceneManager.cs** (Ghost.Editor.Core/SceneGraph)
- **Editor scene lifecycle manager** - uses JSON serialization
- **SaveSceneAsync**: Asynchronous, saves scene to JSON file
- Integrates with editor workflows and UI

## Key Design Decisions

### 1. Dual Serialization Formats
- **Binary for Runtime**: Fast, compact, AOT-compatible for shipping builds
  - No reflection or System.Text.Json dependency in Ghost.Engine
  - Direct memory operations using unsafe pointers
  - Synchronous operations suitable for runtime loading
- **JSON for Editor**: Human-readable, debuggable, reflection-based
  - Located in Ghost.Editor.Core (not in runtime path)
  - Async operations for editor workflows
  - Automatic Entity remapping via custom JsonConverter

### 2. World-Centric Architecture
- World is the data container (the "database")
- Scene is a lightweight handle/view into that data
- SceneManager orchestrates the I/O and entity management
- Respects separation of concerns: World doesn't know about scenes

### 3. Component Tagging
- Uses `SceneID` component (currently IComponent, ready for ISharedComponent upgrade)
- Each entity stores its scene membership
- Enables efficient querying and batch operations

### 4. Entity Reference Remapping
- "Smart Serializer" strategy with two-pass loading
- File uses sequential IDs (0, 1, 2...)
- Runtime creates new Entities with different IDs
- SerializationContext handles the translation
- **Binary format**: Manual remapping in `RemapEntityReferences` method
- **JSON format**: Automatic remapping via `EntityJsonConverter`
- Works for Hierarchy and any other component with Entity fields

### 5. AOT Compatibility
- Ghost.Engine has zero reflection-based serialization
- All JSON/reflection code isolated to Ghost.Editor.Core
- Binary serializer uses only unsafe pointers and memcpy
- Suitable for IL2CPP and NativeAOT compilation

## Binary Format Specification

```
Header:
  4 bytes: Magic number (0x47534345 "GSCE")
  4 bytes: Version number (int32)
  4 bytes: Entity count (int32)

For each entity:
  4 bytes: File ID (int32)
  4 bytes: Component count (int32)
  
  For each component:
    4 bytes: Component Type ID (int32)
    4 bytes: Component Size (int32)
    N bytes: Raw component data (memcpy from archetype)
```

## Usage Examples

### Runtime Usage (Binary)
```csharp
// Create a world
var world = World.Create();

// Load a scene additively (synchronous)
var scene = SceneManager.LoadScene(world, "path/to/scene.bin", SceneLoadMode.Additive);

// Save the scene (synchronous)
SceneManager.SaveScene(scene, "path/to/scene.bin");

// Unload the scene
SceneManager.UnloadScene(scene);
```

### Editor Usage (JSON)
```csharp
// In editor code
var world = World.Create();

// Save scene to JSON (async)
await EditorSceneManager.SaveSceneAsync(scene, "path/to/scene.json");

// JSON is human-readable and can be version-controlled
```

## Future Optimizations

### When ISharedComponent is Available
- Change `SceneID` from `IComponent` to `ISharedComponent`
- Entities with same SceneID will be grouped in same chunks
- Unloading becomes O(chunks) instead of O(entities)
- Can free entire memory blocks instead of individual entities

### Entity Remapping Source Generator
- Currently `RemapEntityReferences` in `SceneBinarySerializer` manually handles Hierarchy
- Could implement a source generator to automatically detect Entity fields in all components
- Would eliminate need for manual per-component remapping code
- Pattern: `[SerializableEntity]` attribute on fields containing Entity references

### Compression
- Binary format is uncompressed raw data
- Could add optional compression (LZ4, Zstandard) for smaller file sizes
- Trade-off: loading time vs disk space

## Files Modified/Created

### Created in Ghost.Engine (Runtime)
- `Ghost.Engine/IO/SerializationContext.cs` - Shared ID remapping context
- `Ghost.Engine/IO/SceneBinarySerializer.cs` - AOT-compatible binary serialization
- `Ghost.Engine/Components/SceneID.cs` - Scene tagging component

### Created in Ghost.Editor.Core (Editor)
- `Ghost.Editor.Core/Serializer/SceneSerializer.cs` - JSON serialization (moved from Ghost.Engine)
- `Ghost.Editor.Core/Serializer/Converters/EntityJsonConverter.cs` - Entity remapping for JSON (moved from Ghost.Engine)

### Modified
- `Ghost.Engine/Core/Scene.cs` - Refactored to lightweight handle
- `Ghost.Engine/Services/SceneManager.cs` - Runtime scene lifecycle with binary serialization
- `Ghost.Editor.Core/SceneGraph/EditorSceneManager.cs` - Editor scene lifecycle with JSON serialization

### Deleted
- `Ghost.Engine/IO/SerializerRegistry.cs` - Obsolete ComponentSerializerRegistry
- `Ghost.Editor.Core/Serializer/SceneNodeSerializer.cs` - Obsolete

## Implementation Notes

### Binary Serialization Details
- Uses `BinaryWriter`/`BinaryReader` for primitive types (int, etc.)
- Component data copied with `Unsafe.CopyBlock` (memcpy equivalent)
- Stackalloc buffer reused for zero-filled missing components (prevents stack overflow)
- Entity remapping performed after all entities created (two-pass loading)

### JSON Serialization Details
- Uses `System.Text.Json` with `JsonSerializerOptions`
- `EntityJsonConverter` registered as custom converter
- Automatic Entity field detection and remapping during deserialization
- Human-readable format suitable for version control

### Thread Safety
- `SerializationContext` uses `AsyncLocal<T>` for thread-safe context isolation
- Binary serializer is not thread-safe (single-threaded runtime loading)
- JSON serializer uses async methods but should not be called concurrently for same World

### Error Handling
- Missing components write zero-filled data (graceful degradation)
- Unknown component types in JSON are skipped with warning
- Invalid Entity references remap to Entity.Null
- File format version checked on load (future-proofing)

## Known Limitations

1. **Manual Entity Remapping in Binary Format**
   - Currently only Hierarchy component is remapped
   - Other components with Entity fields need manual handling
   - Solution: Implement source generator for automatic detection

2. **Component Size Limit**
   - Binary serializer uses 4KB stackalloc buffer for zero-fills
   - Components larger than 4KB will throw exception if missing
   - Solution: Increase MaxComponentSize constant if needed

3. **SceneNode Integration**
   - Legacy SceneNode class in Ghost.Editor.Core still exists
   - May need integration with new Scene/SceneSerializer system
   - Future work: Decide on SceneNode vs Scene unification

4. **No Compression**
   - Binary format is uncompressed
   - Large scenes may have bigger file sizes than necessary
   - Future optimization: Add LZ4/Zstandard compression layer

5. **Managed Components**
   - Current implementation assumes all IComponent types are unmanaged
   - ScriptComponent and ManagedEntity may need separate handling
   - Future work: Add managed reference serialization

## Testing Recommendations

1. **Binary Format Round-Trip**
   - Create entities with various components
   - Save to binary file
   - Load into new World
   - Verify all component data matches

2. **Entity Reference Remapping**
   - Create parent-child hierarchies
   - Serialize and deserialize
   - Verify parent/child Entity references updated correctly

3. **Additive Loading**
   - Load multiple scenes into same World
   - Verify SceneID tagging works correctly
   - Unload specific scenes and verify entities destroyed

4. **JSON Compatibility**
   - Save same scene to both JSON and binary
   - Verify both formats produce equivalent results when loaded
   - Test JSON editing by hand (human-readable requirement)

5. **AOT Compatibility**
   - Build Ghost.Engine with NativeAOT or IL2CPP
   - Verify no reflection or dynamic code generation warnings
   - Test binary serialization in AOT-compiled build
