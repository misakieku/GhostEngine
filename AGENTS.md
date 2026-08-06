# GhostEngine — Agent Guidelines

## Repository Overview

GhostEngine is a C# game engine targeting .NET 10 / Windows, built around:

- **ECS runtime** (`Ghost.Entities`, `Ghost.Core`) — high-performance, AOT-compatible
- **Graphics** (`Ghost.Graphics`, `Ghost.Graphics.RHI`, `Ghost.Graphics.D3D12`) — D3D12 RHI
- **Editor** (`Ghost.AssetForge`, `Ghost.AssetForge.Core`, `Ghost.DSL`) — WinUI 3 (WindowsAppSDK)
- **Third-party bindings** (`Ghost.FMOD`, `Ghost.MeshOptimizer`, `Ghost.Nvtt`, `Ghost.Ufbx`, `Ghost.DXC`, `Ghost.StbI`)
- **Tools** (`Ghost.NativeWrapperGen`, `Ghost.ShaderMetadataTool`, `Ghost.AssetForge.CLI`)

Solution file: `src/GhostEngine.slnx`
All commands below should be run from the `src/` directory unless noted.

---

## Build &amp; Config

```shell
# Build entire solution (x64, Debug_Editor)
dotnet build GhostEngine.slnx -c Debug -p:Platform=x64

# Build entire solution (Release_Editor)
dotnet build GhostEngine.slnx -c Release -p:Platform=x64

# Build a single project (uses Debug/Release; Editor configs handled by Directory.Build.props)
dotnet build Runtime/Ghost.Entities/Ghost.Entities.csproj -c Release

# Clean
dotnet clean GhostEngine.slnx
```

**4 build configs** (defined in `src/Directory.Build.props`):


| Configuration | Defines                           | Use         |
| ------------- | --------------------------------- | ----------- |
| `Debug`       | DEBUG, TRACE, GHOST_SAFETY_CHECKS | Default dev |
| `Release`     |                                   | Optimized   |


Editor projects require `net10.0-windows10.*` and Windows App SDK. They only build on Windows with the correct SDK. Use an `*_Editor` config to build them.

**Ghost.Core** also defines `MHP_ENABLE_MIMALLOC;MHP_FASTMATH` (all configs) and `MHP_ENABLE_SAFETY_CHECKS` (Debug only).

### Global Constants


| Define                | Scope              | Meaning                                      |
| --------------------- | ------------------ | -------------------------------------------- |
| `GHOST_EDITOR`        | `*_Editor` configs | Editor build (extra validation, reflection)  |
| `GHOST_SAFETY_CHECKS` | All configs        | Runtime safety validation in Result<T>, etc. |
| `MHP_ENABLE_MIMALLOC` | Ghost.Core only    | Use mimalloc allocator                       |
| `MHP_FASTMATH`        | Ghost.Core only    | Fast math intrinsics                         |


### Build System Quirks

`GhostEngine.targets` (imported by `Ghost.Engine` and `TestGame`) defines two MSBuild targets:

- **GenerateShaderMetadata** — runs before `CoreCompile`, extracts shader metadata into `shader_properties.json` using `Ghost.ShaderMetadataTool`
- **BakeAndPackAssets** — runs after `Build`, triggered by `<IsGameProject>true</IsGameProject>` (set in `TestGame.csproj`). Bakes assets using `Ghost.AssetForge.CLI`

---

## Test Commands

Two test frameworks, no shared infrastructure:

### MSTest — `Ghost.UnitTest` + `Ghost.AssetForge.Test`

Runs via `dotnet test`. Uses `Microsoft.Testing.Platform` (configured in `global.json`).

```shell
# Run all MSTest tests
dotnet test Test/Ghost.UnitTest/Ghost.UnitTest.csproj -c Debug -p:Platform=x64

# Single test method
dotnet test Test/Ghost.UnitTest/Ghost.UnitTest.csproj --filter "FullyQualifiedName~TestAutoMetaGeneration_WhenFileCreated"

# Single test class
dotnet test Test/Ghost.UnitTest/Ghost.UnitTest.csproj --filter "ClassName~AssetDatabaseIntegrationTest"

# AssetForge tests (uses MSTest.Sdk meta-package)
dotnet test Test/Ghost.AssetBaker.Test/Ghost.AssetForge.Test.csproj -c Debug -p:Platform=x64
```

`Ghost.UnitTest` targets `net10.0-windows10.0.22621.0` (Windows-only). Editor integration tests are `#if false`-guarded until asset service is fully wired.

### Custom TestRunner — `Ghost.MicroTest`

Console executable with internal `ITest`/`TestRunner` (no shared `Ghost.Test.Core` library). Run directly:

```shell
dotnet run --project Test/Ghost.MicroTest/Ghost.MicroTest.csproj
```

To run a specific test class, edit `Program.cs` to call `TestRunner.Run<YourTest>()`. The project has `PublishAot=true` and references BenchmarkDotNet.

### TestGame — AOT Entry Point

`TestGame` is a `WinExe` with `PublishAot=true`, `<IsGameProject>true</IsGameProject>`, and `InvariantGlobalization=true`. It serves as a buildable entry point that exercises the full engine pipeline (ECS → graphics → asset baking).

---

## Code Style

### EditorConfig (enforced — `src/.editorconfig`)

- Max line length: **200**
- Opening braces always on a **new line** for all C# constructs
- Single-line statements/blocks **preserved**
- **No** primary constructors (`csharp_style_prefer_primary_constructors = false`)
- `System.*` using directives are **not** sorted first
- Import directive groups are **not** separated by blank lines
- Collection expressions and collection initializer **disabled**

### Language

- C# `latest` (runtime projects); C# 14 for source generators (`Ghost.Generator`) and tools
- Nullable reference types: **enabled** everywhere
- Implicit usings: **enabled**
- Unsafe blocks: **enabled** where needed (ECS, graphics, native bindings)

### Namespaces &amp; File Layout

- One type per file; file name matches type name exactly
- Namespace matches folder structure: `Ghost.<Module>[.<SubFolder>]`
- `partial` classes split across files named `TypeName.Purpose.cs`
- `AssemblyInfo.cs` holds `[assembly: InternalsVisibleTo(...)]` and assembly attributes only; do not scatter these

### Naming Conventions


| Symbol                                   | Convention                    | Example                                  |
| ---------------------------------------- | ----------------------------- | ---------------------------------------- |
| Private fields                           | `_camelCase`                  | `_jobScheduler`                          |
| Private static fields                    | `s_camelCase`                 | `s_worlds`, `s_logger`                   |
| Constants (public/private)               | `UPPER_SNAKE_CASE`            | `ASSET_EXTENSION`, `ASSETS_FOLDER_NAME`  |
| Properties &amp; public members          | `PascalCase`                  | `EntityManager`, `IsSuccess`             |
| Local variables / params / public fields | `camelCase`                   | `entityCapacity`, `signatureHash`        |
| Interfaces                               | `I` prefix                    | `IComponent`, `ISystem`, `ITest`         |
| Generic type parameters                  | `T`, `TKey`, `TValue`, `E`    |                                          |
| Type-tagged structs (handles)            | Generic param encodes context | `Handle<T>`, `Identifier<T>`, `Key64<T>` |


Use public fields only for data-container structs. Prefer private field + public property for classes.

### Design &amp; Architecture

- DOD (data-oriented-design) in mind. This is not a hard restrictions, OOP is allowed when it's better or raw performance is not the primary objective.
- Multi-threaded programming as second nature.
- Unsafe and pointer as second nature.
- Native memory over managed memory when it's necessary and possible.
- Prefer stateless over stateful in classes like compiler, utility, processor, etc.

### Types &amp; Structs

- Prefer `readonly struct` for immutable value types
- Prefer `ref struct` / `readonly ref struct` for stack-only types (`RefResult<T,E>`, `SystemAPI`)
- Use `partial class` to split large classes by concern
- Use the `field` keyword for auto-property backing fields when a dedicated private field is unnecessary

### Imports

- `using` at the top of each file, before `namespace`
- No blank line between `using` groups
- `System.*` namespaces in any order alongside project namespaces
- Prefer specific imports over global usings in performance-critical files

### Error Handling

- **Return `Result` / `Result<T>` / `Result<T,E>` / `RefResult<T,E>` instead of throwing** for expected failures
- Use `Result.Success()` / `Result.Failure(message)` / `Result.Failure(Error.XXX)`
- Use `result.ThrowIfFailed()` / `result.GetValueOrThrow()` for throw-on-failure at call sites
- **Throw exceptions** only for programming errors / invariant violations
- In performance-critical paths, guard validation behind `#if GHOST_SAFETY_CHECKS`
- `Logger.Error()` / `Logger.Warning()` for non-fatal issues; no `Console.WriteLine` in library code

### Performance Patterns

- Annotate hot paths with `[MethodImpl(MethodImplOptions.AggressiveInlining)]`
- Annotate log/assert helpers with `[StackTraceHidden]`
- Prefer `stackalloc` + `Span<T>` over heap allocation for small temporary arrays
- Use `Misaki.HighPerformance.*` allocation APIs (`AllocationManager`, `UnsafeList<T>`, `UnsafeHashMap<T,V>`, etc.) for long-lived unmanaged buffers
- All runtime/ECS types must be AOT-compatible and trimmable (`<IsAotCompatible>True</IsAotCompatible>`, `<IsTrimmable>True</IsTrimmable>` in Release)
- Avoid LINQ in hot paths; use `for` loops or `foreach` over `Span<T>`

### Source Generators &amp; Attributes

- `Ghost.Generator` targets `netstandard2.0` (Roslyn source generator). Included as analyzer in `Ghost.Engine` and `TestGame` projects.
- Active generators: `ComponentRegistrationGenerator`, `EntryPointGenerator`, `SoaGenerator`, `ShaderPropertiesGenerator`
- `[SoaGenerate(bool unmanaged)]` — tag structs/classes for SOA layout generation
- `[RuntimeInitializeAttribute]` — tag static void methods taking `EngineCore` for auto-registration (consumed by `EntryPointGenerator`)
- `[UpdateAfter<T>()]` / `[UpdateBefore<T>()]` — declare `ISystem` ordering; `SystemGroup` topologically sorts them at startup
- `[SystemGroup(typeof(X))]` — assign system to a group
- `Ghost.Entities` uses **T4 text templates** (`.tt` files) for code generation (ForEach, QueryBuilder, EntityQuery). Re-run T4 when editing templates.

### XML Documentation

- All public API surface should have `<summary>` doc-comments
- Use `<remarks>` for non-obvious behavior or threading constraints
- Document thread-safety expectations explicitly

---

## Project Structure

```
src/
  GhostEngine.slnx              # Solution (MSBuild 2024+ .slnx format)
  .editorconfig                 # Formatting rules
  global.json                   # Test runner: Microsoft.Testing.Platform
  Directory.Build.props         # Shared build config (4 configs)
  GhostEngine.targets           # Shader metadata + asset baking build targets
  Runtime/
    Ghost.Core/                 # Result, Handle, Logger, math, Attributes (EngineAssembly, Inspector, SoaGenerate)
    Ghost.Engine/               # Engine entry point & loop, Components, Systems, RenderPipeline, Streaming
    Ghost.Entities/             # ECS: World, Entity, Component, System (has T4 templates)
    Ghost.Generator/            # Roslyn source generators (netstandard2.0)
    Ghost.Graphics/             # High-level graphics API
    Ghost.Graphics.RHI/         # Render hardware interface abstraction
    Ghost.Graphics.D3D12/       # D3D12 backend
  Editor/
    Ghost.AssetForge/           # WinUI 3 shell (WinExe, net10.0-windows10.0.19041.0)
    Ghost.AssetForge.Core/      # Editor services, asset baking pipeline
    Ghost.DSL/                  # Shader DSL compiler (ANTLR4 grammar in Grammar/*.g4)
  ThridParty/                   # Native binding wrappers (actual folder name, typo preserved)
    Ghost.DXC/                  # DirectXCompiler binding (dxcompiler.dll, dxil.dll)
    Ghost.FMOD/                 # FMOD audio binding (fmod.dll, fmodstudio.dll)
    Ghost.MeshOptimizer/        # meshoptimizer binding
    Ghost.Nvtt/                 # NVIDIA Texture Tools binding
    Ghost.StbI/                 # stb_image binding
    Ghost.Ufbx/                 # ufbx FBX loader binding
  Test/
    Ghost.UnitTest/             # MSTest integration tests (dotnet test)
    Ghost.AssetBaker.Test/      # MSTest (uses MSTest.Sdk 4.0.2) for asset baking tests
    Ghost.MicroTest/            # Native binding smoke tests (console, internal ITest/TestRunner)
    TestGame/                   # AOT game entry point (WinExe, PublishAot, IsGameProject)
  Tools/
    Ghost.NativeWrapperGen/     # Code-gen tool for native C bindings (Exe)
    Ghost.ShaderMetadataTool/   # Shader metadata extraction (Exe, consumed by GhostEngine.targets)
    Ghost.AssetForge.CLI/       # Asset baking CLI (Exe, consumed by GhostEngine.targets)
```

### Key Dependency Flow

```
TestGame ──> Ghost.Engine ──> Ghost.Generator (analyzer)
                              ├─> Ghost.Entities ──> Ghost.Core
                              ├─> Ghost.Graphics ──> Ghost.Core
                              │                    └─> Ghost.Graphics.RHI ──> Ghost.Core
                              │                    └─> Ghost.Graphics.D3D12 ──> Ghost.Core, Ghost.Graphics.RHI
                              └─> Ghost.FMOD
 Ghost.MicroTest ──> Ghost.DSL, Ghost.Core, Ghost.DXC, Ghost.Nvtt, Ghost.StbI, Ghost.Ufbx
 Ghost.AssetForge ──> Ghost.AssetForge.Core ──> Ghost.Core, Ghost.DSL, Ghost.DXC, Ghost.MeshOptimizer, Ghost.Nvtt, Ghost.StbI, Ghost.Ufbx
```

