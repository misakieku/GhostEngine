# GhostEngine — Agent Guidelines

## Repository Overview

GhostEngine is a C# game engine targeting .NET 10 / Windows, built around:
- **ECS runtime** (`Ghost.Entities`, `Ghost.Core`) — high-performance, AOT-compatible
- **Graphics** (`Ghost.Graphics`, `Ghost.Graphics.RHI`, `Ghost.Graphics.D3D12`) — D3D12 RHI
- **Editor** (`Ghost.Editor`, `Ghost.Editor.Core`, `Ghost.DSL`, `Ghost.Data`) — WinUI 3 (WindowsAppSDK)
- **Third-party bindings** (`Ghost.FMOD`, `Ghost.MeshOptimizer`, `Ghost.Nvtt`, `Ghost.Ufbx`)
- **Tools** (`Ghost.NativeWrapperGen`)

Solution file: `src/GhostEngine.slnx` 
All commands below should be run from the `src/` directory unless noted.

---

## Build Commands

```shell
# Build entire solution (x64, Debug)
dotnet build GhostEngine.slnx -c Debug -p:Platform=x64

# Build entire solution (Release)
dotnet build GhostEngine.slnx -c Release -p:Platform=x64

# Build a single project
dotnet build Runtime/Ghost.Entities/Ghost.Entities.csproj

# Clean
dotnet clean GhostEngine.slnx
```

> **Note:** Editor projects (`Ghost.Editor`, `Ghost.Editor.Core`) require
> `net10.0-windows10.0.22621.0` and the Windows App SDK. They will only build
> on a Windows machine with the correct SDK installed. Platform-agnostic runtime
> and test projects target plain `net10.0`.

---

## Test Commands

There are two test frameworks in use:

### MSTest — `Ghost.UnitTest`
Standard `dotnet test` runner. Tests are parallelized at method level by default.
Currently the integration test file is `#if false`-guarded until the asset
service is fully wired.

```shell
# Run all MSTest tests
dotnet test Test/Ghost.UnitTest/Ghost.UnitTest.csproj -c Debug -p:Platform=x64

# Run a single test method by name
dotnet test Test/Ghost.UnitTest/Ghost.UnitTest.csproj \
 --filter "FullyQualifiedName~TestAutoMetaGeneration_WhenFileCreated"

# Run a single test class
dotnet test Test/Ghost.UnitTest/Ghost.UnitTest.csproj \
 --filter "ClassName~AssetDatabaseIntegrationTest"
```

### Custom TestRunner — `Ghost.MicroTest` / `Ghost.Entities.Test`
These are console executables driven by `Ghost.Test.Core.TestRunner`. There is
no `dotnet test` integration; run them directly:

```shell
# Micro tests (native binding smoke tests)
dotnet run --project Test/Ghost.MicroTest/Ghost.MicroTest.csproj

# ECS benchmarks / manual tests
dotnet run --project Test/Ghost.Entities.Test/Ghost.Entities.Test.csproj -c Release
```

To run a specific `ITest` implementation, edit `Program.cs` in the respective
project and call `TestRunner.Run<YourTestClass>()`.

---

## Code Style

### EditorConfig (enforced — `src/.editorconfig`)
- Max line length: **200**
- Opening braces always on a **new line** for all C# constructs
- Single-line statements and blocks are **preserved** (not force-expanded)
- **No** primary constructors (`csharp_style_prefer_primary_constructors = false`)
- `System.*` using directives are **not** sorted first
- Import directive groups are **not** separated by blank lines
- Collection expressions and collection initializer syntax are **disabled**
 (`dotnet_style_prefer_collection_expression = false`)

### Language
- C# `latest` (runtime/test projects) or `preview` (editor projects, for `field`
 keyword support in .NET 10)
- Nullable reference types: **enabled** everywhere (`<Nullable>enable</Nullable>`)
- Implicit usings: **enabled** (`<ImplicitUsings>enable</ImplicitUsings>`)
- Unsafe blocks: **enabled** where needed (ECS, graphics, native bindings)

### Namespaces & File Layout
- One type per file; file name matches type name exactly
- Namespace matches folder structure: `Ghost.<Module>[.<SubFolder>]`
- `partial` classes are split across files named `TypeName.Purpose.cs`
 (e.g. `EntityManager.cs`, `EntityManager.Managed.cs`)
- `AssemblyInfo.cs` holds `[assembly: InternalsVisibleTo(...)]` and assembly
 attributes; do not scatter these across regular source files

### Naming Conventions
| Symbol | Convention | Example |
|--------|-----------|---------|
| Private fields | `_camelCase` | `_jobScheduler` |
| Private static fields | `s_camelCase` | `s_worlds`, `s_logger` |
| Constants (public/private) | `UPPER_SNAKE_CASE` | `ASSET_EXTENSION`, `ASSETS_FOLDER_NAME` |
| Properties & public members | `PascalCase` | `EntityManager`, `IsSuccess` |
| Local variables / params | `camelCase` | `entityCapacity`, `signatureHash` |
| Interfaces | `I` prefix | `IComponent`, `ISystem`, `ITest` |
| Generic type parameters | `T`, `TKey`, `TValue`, `E` | |
| Type-tagged structs (handles) | Generic param encodes context | `Handle<T>`, `Identifier<T>`, `Key64<T>` |

### Types & Structs
- Prefer `readonly struct` for value types that are logically immutable.
- Prefer `ref struct` / `readonly ref struct` for stack-only types
 (`RefResult<T,E>`, `SystemAPI`, `ChunkView`).
- Use `partial class` to split large classes by concern.
- Avoid primary constructors (disabled by editorconfig).
- Use the `field` keyword (preview feature) for auto-property backing fields
 where it simplifies code — only in editor projects that opt in via
 `<langversion>preview</langversion>`.

### Imports
- `using` directives at the top of each file, before the `namespace` declaration.
- No blank line between `using` groups (enforced by editorconfig).
- `System.*` namespaces may appear in any order alongside project namespaces.
- Prefer specific `using` imports over global usings for clarity in low-level
 performance-critical files.

### Error Handling
- **Return `Result` / `Result<T>` instead of throwing** for expected failures
 (file-not-found, invalid args, etc.). 
 `Result.Success()` / `Result.Failure(message)` or `Result.Failure(Error.XXX)`.
- Use the typed `Error` enum (`Ghost.Core.Error`) for structured error codes.
- Use `result.ThrowIfFailed()` / `result.GetValueOrThrow()` extension methods at
 call sites that want throw-on-failure semantics.
- **Throw exceptions** only for programming errors / invariant violations
 (corrupt state, null-ref on internal APIs).
- In performance-critical paths, guard validation behind `#if DEBUG || GHOST_EDITOR`
 to eliminate overhead in release builds.
- `Logger.LogError(...)` / `Logger.LogWarning(...)` for non-fatal operational
 issues; do not use `Console.WriteLine` in production library code.

### Performance Patterns
- Annotate hot paths with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`.
- Annotate log/assert helpers with `[StackTraceHidden]`.
- Prefer `stackalloc` + `Span<T>` over heap allocation for small temporary arrays.
- Use the `Misaki.HighPerformance.*` allocation APIs (`AllocationManager`,
 `UnsafeList<T>`, `UnsafeHashMap<T,V>`, etc.) for long-lived unmanaged buffers.
- All runtime/ECS types must be AOT-compatible and trimmable (set
 `<IsAotCompatible>True</IsAotCompatible>` and `<IsTrimmable>True</IsTrimmable>`
 in Release config).
- Avoid LINQ in hot paths; use `for` loops or `foreach` over `Span<T>`.

### Attributes & Extensibility
- Custom attributes for editor extension points inherit from
 `DiscoverableAttributeBase` (discovered at startup via `TypeCache`).
- Use `[UpdateAfter(typeof(X))]` / `[UpdateBefore(typeof(X))]` to declare
 `ISystem` ordering dependencies; `SystemGroup.SortSystems()` topologically sorts
 them at startup.
- Use `[EditorInjection(ServiceLifetime.Singleton)]` to register editor services
 via DI without manual wiring.

### XML Documentation
- All public API surface should have `<summary>` doc-comments.
- Use `<remarks>` for non-obvious behavior or threading constraints.
- Document thread-safety expectations explicitly (see `EntityCommandBuffer` /
 `AssetRegistry` as reference).

### Preprocessor Defines
| Define | Meaning |
|--------|---------|
| `DEBUG` | Standard debug build |
| `GHOST_EDITOR` | Editor build (extra validation, reflection helpers) |
| `PLATEFORME_WIN64` | Windows 64-bit platform target |

---

## Project Structure

```
src/
 GhostEngine.slnx # Solution
 .editorconfig # Formatting rules
 Runtime/
 Ghost.Core/ # Core types: Result, Handle, Logger, math helpers
 Ghost.Engine/ # Engine entry point & loop
 Ghost.Entities/ # ECS: World, Entity, Component, System
 Ghost.Generator/ # Source generators
 Ghost.Graphics/ # High-level graphics API
 Ghost.Graphics.RHI/ # Render hardware interface abstractions
 Ghost.Graphics.D3D12/ # D3D12 backend
 Editor/
 Ghost.Editor/ # WinUI 3 shell
 Ghost.Editor.Core/ # Editor services, asset registry, inspector
 Ghost.DSL/ # Shader DSL compiler
 Ghost.Data/ # Serialization / project data models
 ThridParty/ # Native binding wrappers (FMOD, MeshOptimizer, Nvtt, Ufbx)
 Test/
 Ghost.Test.Core/ # Shared ITest / TestRunner infrastructure
 Ghost.UnitTest/ # MSTest integration tests
 Ghost.MicroTest/ # Native binding smoke tests (console app)
 Ghost.Entities.Test/ # ECS benchmarks (BenchmarkDotNet, console app)
 Ghost.Shader.Test/ # Shader DSL manual tests (console app)
 Tools/
 Ghost.NativeWrapperGen/ # Code-gen tool for native wrappers
```
