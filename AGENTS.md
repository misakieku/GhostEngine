# GhostEngine - Agent Development Guide

This guide provides essential information for AI coding agents working on the GhostEngine codebase.

## Project Overview

- **Type**: Game Engine
- **Language**: C# 
- **Target Framework**: .NET 10.0
- **Special Features**: ECS architecture, D3D12 rendering, AOT compilation, WinUI 3 editor
- **Platform**: Windows (net10.0-windows10.0.22621.0 for editor projects)

## Build Commands

### Build Entire Solution
```bash
dotnet build GhostEngine.slnx
```

### Build Specific Project
```bash
dotnet build Ghost.Entities/Ghost.Entities.csproj
dotnet build Ghost.Editor/Ghost.Editor.csproj
```

### Build with Configuration
```bash
dotnet build GhostEngine.slnx -c Release
dotnet build GhostEngine.slnx -c Debug
```

### Clean Build
```bash
dotnet clean GhostEngine.slnx
dotnet build GhostEngine.slnx
```

## Test Commands

### Run All Tests (Custom Framework)
Tests use a custom test framework (not xUnit/NUnit/MSTest). Each test project is an executable.

```bash
# Run entity tests
dotnet run --project Ghost.Entities.Test/Ghost.Entities.Test.csproj

# Run shader tests
dotnet run --project Ghost.Shader.Test/Ghost.Shader.Test.csproj
```

### Run Single Test
Tests implement `ITest` interface. To run a specific test, modify the test project's `Program.cs`:

```csharp
// In Ghost.Entities.Test/Program.cs
TestRunner.Run<EntityQueryTest>();  // Run specific test
TestRunner.Run<EntityQueryTest>(10); // Run with 10 iterations
```

### Visual Tests (Graphics)
Graphics tests use WinUI 3 and require running as packaged apps:

```bash
dotnet run --project Ghost.Graphics.Test/Ghost.Graphics.Test.csproj
```

## Code Style Guidelines

### Formatting (from .editorconfig)

- **Braces**: Allman style - all opening braces on new lines
- **Line Length**: Max 400 characters (very permissive)
- **Single-line statements**: Preserved (allowed)
- **Single-line blocks**: Preserved (allowed)

```csharp
// Correct brace style
public void Method()
{
    if (condition)
    {
        DoSomething();
    }
}
```

### Imports

- **System directives**: NOT sorted first (dotnet_sort_system_directives_first = false)
- **No grouping**: Import directives not separated by blank lines
- **Order**: Organize by project convention, not alphabetically

```csharp
using Ghost.Core;
using Ghost.Entities;
using Misaki.HighPerformance.Collections;
using System.Diagnostics;
using TerraFX.Interop.DirectX;
```

### Types and Nullability

- **Nullable**: Enabled for all projects
- **Implicit usings**: Enabled
- **Unsafe code**: Allowed in most projects (AllowUnsafeBlocks = True)
- **Primary constructors**: NOT preferred (csharp_style_prefer_primary_constructors = false)

### Naming Conventions

- **Classes/Interfaces**: PascalCase (`EntityManager`, `ICommandBuffer`)
- **Methods**: PascalCase (`CreateEntity`, `GetComponent`)
- **Properties**: PascalCase (`IsSuccess`, `Value`)
- **Fields (private)**: Camel case with underscore prefix (`_entityLocations`, `_world`)
- **Fields (public/internal)**: Camel case, no prefix for struct fields (`archetypeID`, `chunkIndex`)
- **Type parameters**: Single letter or PascalCase (`T`, `TComponent`)
- **Constants**: PascalCase (no SCREAMING_SNAKE_CASE)

```csharp
public class EntityManager
{
    private readonly World _world;              // Private field
    private UnsafeSlotMap<EntityLocation> _entityLocations;
    
    public World World => _world;               // Property
    
    public Entity CreateEntity() { }            // Method
}

internal struct EntityLocation                  // Struct
{
    public int archetypeID;                     // Public struct field
    public int chunkIndex;
}
```

### Error Handling

**Use Result Types** - Railway-oriented programming pattern:

```csharp
// Custom result types defined in Ghost.Core
public ErrorStatus DoOperation()
{
    return ErrorStatus.None; // or ErrorStatus.NotFound, etc.
}

public Result<T> GetValue()
{
    if (success)
        return Result<T>.Success(value);
    else
        return Result<T>.Failure("Error message");
}

public Result<T, ErrorStatus> GetValueWithStatus()
{
    if (success)
        return value;  // Implicit conversion
    else
        return ErrorStatus.NotFound;  // Implicit conversion
}

// Extension methods for checking results
result.ThrowIfFailed();
var value = result.GetValueOrThrow();
var value = result.GetValueOrDefault(defaultValue);
if (result.TryGetValue(out var value)) { }
```

**Error Status Values**: None, NotFound, InvalidArgument, InvalidState, InternalError, PermissionDenied, NotSupported, OutOfMemory, Timeout, Cancelled, UnknownError

### Memory and Performance

- **Use unsafe code** when needed for performance-critical paths
- **Span<T> and stackalloc**: Prefer for temporary allocations
- **ref returns**: Use for zero-copy access to internal data
- **Allocator patterns**: Use `Allocator.Persistent` for long-lived allocations
- **AllocationManager**: Create stack scopes for temporary allocations

```csharp
// Stack allocation pattern
var entities = (Span<Entity>)stackalloc Entity[1];

// Using allocation scope
using var scope = AllocationManager.CreateStackScope();
var batchDestroy = new UnsafeList<EntityLocation>(entities.Length, scope.AllocationHandle);

// Ref returns for zero-copy access
public ref T GetSingleton<T>() where T : unmanaged, IComponent
{
    var ptr = GetSingleton(ComponentTypeID<T>.Value);
    return ref *(T*)ptr;
}
```

### Type Safety Patterns

**Strongly-typed identifiers**:
```csharp
Identifier<IComponent> componentID;
Identifier<Archetype> archetypeID;
Handle<T> resourceHandle;
```

**Generic constraints**:
```csharp
public void Method<T>() where T : unmanaged, IComponent
public void Method<T, E>() where E : struct, Enum
```

### Documentation

- **XML comments**: Required for public APIs
- **Summary tags**: Describe what, not how
- **Remarks**: Add for complex behavior, thread-safety warnings, structural changes

```csharp
/// <summary>
/// Create an entity with specified components.
/// </summary>
/// <param name="set">A set of component space IDs to add to the entities.</param>
/// <returns>The created entity.</returns>
/// <remarks>
/// This method causes structural changes and is not thread-safe.
/// Use <see cref="EntityCommandBuffer"/> to defer changes.
/// </remarks>
public Entity CreateEntity(ComponentSet set) { }
```

### Common Patterns

**ECS Component Registration**:
```csharp
// Type-safe component ID
ComponentTypeID<Transform>.Value

// Component sets for archetypes
var set = new ComponentSet(ComponentTypeID<Transform>.Value, ComponentTypeID<Velocity>.Value);
```

**Disposal Pattern**:
```csharp
private bool _disposed;

~MyClass()
{
    Dispose();
}

public void Dispose()
{
    if (_disposed) return;
    
    // Cleanup code
    _disposed = true;
    GC.SuppressFinalize(this);
}
```

**Debug-only validation**:
```csharp
#if DEBUG || GHOST_EDITOR
    if (!_isSuccess)
    {
        throw new InvalidOperationException($"Error: {_message}");
    }
#endif
```

## Architecture Notes

### Entity Component System (ECS)
- Archetype-based storage (similar to Unity DOTS)
- Component data stored in chunks
- Queries use bitset signatures for fast matching
- Structural changes move entities between archetypes

### Graphics (D3D12)
- Hardware abstraction via `ICommandBuffer`
- Resource lifetime managed via handles
- Pipeline state objects (PSO) cached in library
- Native interop via TerraFX.Interop

### Custom Dependencies
- `Misaki.HighPerformance.*`: High-performance collections and utilities
- `TerraFX.Interop.*`: Native Windows/DirectX interop
- Custom source generators in `Ghost.Generator`

## Important Rules

1. **Never disable nullable warnings** - fix the root cause
2. **Use Result types** instead of throwing exceptions for expected failures
3. **Document thread-safety** in XML comments for public APIs
4. **AllowUnsafeBlocks** is enabled - use unsafe code when it improves performance
5. **Avoid collection expressions/initializers** (disabled in .editorconfig)
6. **Prefer explicit over implicit** - clarity over brevity
7. **Test changes** by running the appropriate test project executable
