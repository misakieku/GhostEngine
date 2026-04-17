# Shader Pipeline Architecture — Proposed Design

> Presented as a design walkthrough. Take what's useful, ignore what doesn't fit your vision.

---

## 1. System Topology

The first decision: **where does each responsibility live?**

```mermaid
graph TB
    subgraph EditorProcess["Ghost.Editor Process"]
        FW["FileWatcher<br/>(monitors .ghost DSL files)"]
        AR["AssetRegistry<br/>(GUID ↔ file path mapping)"]
        EP["Editor UI<br/>(status bar, material inspector)"]
    end

    subgraph CompilerProcess["GhostShaderServer Process"]
        DSL["DSL Compiler<br/>(Ghost DSL → HLSL)"]
        DXC["DXC Compiler<br/>(HLSL → DXIL bytecode)"]
        MW["Manifest Writer<br/>(updates variant → hash mapping)"]
    end

    subgraph RuntimeGraphics["Ghost.Graphics (Runtime)"]
        SL["ShaderLibrary<br/>(reads bytecode from cache)"]
        PL["PipelineLibrary<br/>(PSO creation + double-buffer)"]
        RGC["RenderGraphContext<br/>(binds PSO per draw call)"]
        BR["IShaderCompilationBridge<br/>(interface, 2 methods)"]
    end

    subgraph SharedDisk["Shared Disk (ShaderCache/)"]
        MF["ShaderManifest.bin<br/>(GUID+variant → content hash)"]
        BC["Bytecode Files<br/>(content-addressed .bin blobs)"]
    end

    FW -- "file changed event" --> AR
    AR -- "GUID + file path<br/>(named pipe)" --> CompilerProcess
    DSL --> DXC
    DXC -- "bytecode bytes" --> MW
    MW -- "write blob" --> BC
    MW -- "update entry" --> MF

    SL -- "read blob" --> BC
    SL -- "read mapping" --> MF
    BR -- "status query<br/>(named pipe)" --> CompilerProcess

    EP -- "poll status" --> BR

    style CompilerProcess fill:#1a1a2e,stroke:#e94560,color:#eee
    style EditorProcess fill:#1a1a2e,stroke:#0f3460,color:#eee
    style RuntimeGraphics fill:#1a1a2e,stroke:#16213e,color:#eee
    style SharedDisk fill:#0f3460,stroke:#533483,color:#eee
```

### Why a Separate Process?

| Concern | In-process compiler | Separate process |
|---------|-------------------|------------------|
| DXC crash | Editor dies | Server restarts, editor lives |
| DXC memory leak | Editor bloats over time | Kill & restart server periodically |
| Parallelism | Threads compete with editor UI | Fully independent CPU budget |
| Build pipeline reuse | Need separate build-time path | Same server binary, different mode |
| Complexity | Lower (one process) | Higher (IPC needed) |

> [!TIP]
> If the separate process feels like overkill for your current stage, **start with in-process behind the `IShaderCompilationBridge` interface**, then swap the implementation to out-of-process later. The interface is the same either way.

---

## 2. Data Model — The Manifest

This is the most important data structure in the entire system. It decouples **identity** from **content**.

```mermaid
graph LR
    subgraph ShaderAsset["Shader Asset (on disk)"]
        GUID["Asset GUID<br/><i>e.g. 7f3a-...-c82b</i><br/>stable forever"]
        SRC["Source Code<br/><i>.ghost DSL file</i><br/>changes on edit"]
    end

    subgraph Manifest["ShaderManifest"]
        E1["Entry:<br/>GUID=7f3a | Pass=0 | Variant=0x00<br/>→ ContentHash=0xABCD"]
        E2["Entry:<br/>GUID=7f3a | Pass=0 | Variant=0x01<br/>→ ContentHash=0x1234"]
        E3["Entry:<br/>GUID=7f3a | Pass=1 | Variant=0x00<br/>→ ContentHash=0x5678"]
    end

    subgraph Cache["ShaderCache/ (content addressed)"]
        B1["AB/shader_cache_ABCD...bin"]
        B2["12/shader_cache_1234...bin"]
        B3["56/shader_cache_5678...bin"]
    end

    GUID --> E1
    GUID --> E2
    GUID --> E3
    E1 --> B1
    E2 --> B2
    E3 --> B3

    style ShaderAsset fill:#16213e,stroke:#0f3460,color:#eee
    style Manifest fill:#1a1a2e,stroke:#e94560,color:#eee
    style Cache fill:#0f3460,stroke:#533483,color:#eee
```

### Manifest Entry Structure

```
ManifestKey = Hash(AssetGUID + PassIndex + VariantKeywordMask)
ManifestValue = ContentHash (= Hash of compiled bytecode)
```

- **ManifestKey** is *structurally* derived — same shader, same pass, same keywords = same key, regardless of source changes.
- **ContentHash** is *content-derived* — changes every time the source code changes.
- When source changes: the ManifestKey stays the same, but the ContentHash it points to gets updated.

> [!IMPORTANT]
> The `Shader` struct in runtime only needs to know the **AssetGUID**. It never stores or cares about content hashes. The `ShaderLibrary` uses the manifest to translate `(GUID, Pass, Variant) → ContentHash → File`.

---

## 3. Compilation Flow — What Happens When You Save a Shader

```mermaid
sequenceDiagram
    participant User
    participant FileSystem
    participant Editor as Ghost.Editor
    participant Server as ShaderServer
    participant Cache as ShaderCache/

    User->>FileSystem: Save "water.ghost"
    FileSystem-->>Editor: FileWatcher event

    Editor->>Editor: Lookup GUID for "water.ghost"<br/>via AssetRegistry
    Editor->>Server: CompileRequest {<br/>  guid: 7f3a-...,<br/>  filePath: "water.ghost",<br/>  defines: [...],<br/>  platform: D3D12<br/>}

    Note over Server: Mark status = Compiling<br/>for this GUID

    Server->>Server: Read .ghost DSL file
    Server->>Server: DSL Compiler: DSL → HLSL
    
    alt DSL has syntax errors
        Server->>Server: Mark status = Error
        Server-->>Editor: CompileResult {<br/>  status: Error,<br/>  errors: [...]<br/>}
        Editor->>Editor: Show errors in<br/>console/inspector
    else DSL is valid
        Server->>Server: For each (pass, variant):<br/>DXC Compile HLSL → DXIL

        alt Any DXC error
            Server->>Server: Mark status = Error
            Server-->>Editor: CompileResult {<br/>  status: Error,<br/>  errors: [...]<br/>}
        else All variants compiled
            Server->>Cache: Write bytecode blobs<br/>(content-addressed)
            Server->>Cache: Update manifest entries:<br/>(GUID+pass+variant) → new hash
            Server->>Server: Mark status = Ready
            Server-->>Editor: CompileResult {<br/>  status: Ready,<br/>  variantCount: N<br/>}
            Editor->>Editor: Show ✓ in status bar
        end
    end
```

### Key Design Decision: Compile All Variants Upfront?

**No.** Only compile variants that are *currently referenced* by materials in the scene. The editor knows which materials reference which shader (via AssetRegistry), and which keyword combinations those materials use. Ship only what's needed.

For the edit-time hot-reload, you really only need the specific variants the viewport is currently rendering. The full permutation set is a build-time concern.

---

## 4. Runtime PSO Resolution — The Frame-by-Frame Flow

This is where most of the complexity lives. Here's what `SetActiveMaterial` does every frame:

```mermaid
flowchart TD
    A["SetActiveMaterial(material)"] --> B["Compute ManifestKey<br/>= f(shader.GUID, passIndex, variantMask)"]
    B --> C{"PipelineLibrary<br/>has PSO for<br/>ManifestKey?"}

    C -- "Yes (cache hit)" --> D["Bind existing PSO<br/>to command buffer"]
    D --> Z["Done ✓"]

    C -- "No (cache miss)" --> E{"ShaderLibrary<br/>has bytecode for<br/>ManifestKey?"}

    E -- "Yes (manifest hit)" --> F["Read bytecode<br/>from cache file"]
    F --> G["Create PSO from bytecode"]
    G --> H["Store in PipelineLibrary"]
    H --> D

    E -- "No (manifest miss)" --> I{"Is this Editor<br/>or Runtime?"}

    I -- "Runtime<br/>(shipped game)" --> J["Bind Fallback<br/>ERROR PSO ⚠️"]
    J --> K["Log error:<br/>missing shader"]
    K --> Z

    I -- "Editor" --> L{"Query Bridge:<br/>IsCompiling?"}

    L -- "Status = Compiling" --> M["Bind OLD PSO<br/>(keep previous frame's shader)"]
    M --> Z

    L -- "Status = Error" --> N["Bind ERROR PSO<br/>(magenta)"]
    N --> Z

    L -- "Status = Ready" --> O["The manifest was just updated.<br/>Re-read manifest entry."]
    O --> F

    L -- "Status = NotAvailable" --> J

    style A fill:#533483,stroke:#e94560,color:#eee
    style D fill:#16213e,stroke:#0f3460,color:#eee
    style J fill:#e94560,stroke:#1a1a2e,color:#eee
    style M fill:#0f3460,stroke:#533483,color:#eee
    style N fill:#e94560,stroke:#1a1a2e,color:#eee
    style Z fill:#16213e,stroke:#16213e,color:#eee
```

### The "Keep Old PSO" Strategy — How It Works Mechanically

This is the part that makes the UX feel seamless. The trick:

```mermaid
graph LR
    subgraph PipelineLibrary
        direction TB
        K["ManifestKey 0xAABB"]
        K --> CURRENT["current: PSO_v2 ✓<br/>(what we render with)"]
        K --> PENDING["pending: null<br/>(set during recompilation)"]
    end

    style CURRENT fill:#16213e,stroke:#0f3460,color:#eee
    style PENDING fill:#1a1a2e,stroke:#e94560,color:#eee
```

When shader source changes and recompilation starts:

```mermaid
graph LR
    subgraph PipelineLibrary_During["During Recompilation"]
        direction TB
        K2["ManifestKey 0xAABB"]
        K2 --> CURRENT2["current: PSO_v2 ✓<br/>(still rendering with this)"]
        K2 --> PENDING2["pending: COMPILING<br/>(server is working...)"]
    end

    style CURRENT2 fill:#16213e,stroke:#0f3460,color:#eee
    style PENDING2 fill:#e94560,stroke:#1a1a2e,color:#eee
```

When recompilation finishes successfully:

```mermaid
graph LR
    subgraph PipelineLibrary_After["After Swap"]
        direction TB
        K3["ManifestKey 0xAABB"]
        K3 --> CURRENT3["current: PSO_v3 ✓<br/>(new shader, rendering now)"]
        K3 --> PENDING3["pending: null<br/>(swap complete)"]
    end

    style CURRENT3 fill:#16213e,stroke:#0f3460,color:#eee
    style PENDING3 fill:#1a1a2e,stroke:#533483,color:#eee
```

> [!NOTE]
> The old `PSO_v2` is **not immediately destroyed**. It stays alive until the GPU is done with any in-flight frames referencing it (tracked by fence value). This prevents use-after-free on the GPU timeline.

---

## 5. Hot-Reload Sequence — The Complete Picture

Everything combined into one timeline:

```mermaid
sequenceDiagram
    participant User
    participant Editor
    participant Server as ShaderServer
    participant Cache as Disk Cache
    participant Runtime as RenderGraphContext
    participant GPU

    Note over Runtime,GPU: Frame N: Rendering with PSO_v2

    User->>Editor: Edit & save "water.ghost"
    Editor->>Server: CompileRequest(guid=7f3a)
    Server->>Server: status[7f3a] = Compiling

    Note over Runtime,GPU: Frame N+1
    Runtime->>Runtime: SetActiveMaterial()
    Runtime->>Runtime: ManifestKey lookup → old hash still there
    Runtime->>Runtime: PipelineLibrary has PSO → use it
    Note over Runtime: Still rendering with PSO_v2<br/>(user sees no flicker)

    Note over Server: Background: DSL→HLSL→DXC...

    Note over Runtime,GPU: Frame N+2, N+3, ...
    Runtime->>Runtime: Same as N+1, no visible change

    Server->>Cache: Write new bytecode files
    Server->>Cache: Update manifest:<br/>key(7f3a,0,0) → new_hash
    Server->>Server: status[7f3a] = Ready

    Note over Runtime,GPU: Frame N+K (compilation done)
    Runtime->>Runtime: SetActiveMaterial()
    Runtime->>Runtime: Manifest read → NEW content hash
    Runtime->>Runtime: PipelineLibrary miss for new hash
    Runtime->>Cache: Read new bytecode
    Runtime->>GPU: Create PSO_v3
    Runtime->>Runtime: PipelineLibrary: current=PSO_v3
    Runtime->>Runtime: Bind PSO_v3

    Note over Runtime,GPU: Frame N+K+1: Rendering with PSO_v3 ✓

    Runtime->>Runtime: Defer release PSO_v2<br/>(after GPU fence)
```

### What the User Sees

| Frame | Viewport | Status Bar |
|-------|----------|------------|
| N | Water renders normally | — |
| N+1 | Water renders normally (old shader) | 🔄 Compiling water.ghost... |
| N+2 | Water renders normally (old shader) | 🔄 Compiling water.ghost... |
| N+K | Water renders with new shader | ✅ water.ghost compiled (2 variants) |

**Zero flicker. Zero blocking. Zero pink frames.**

---

## 6. How the Manifest Key Replaces Your Current Hash Problem

Here's a before/after of your `Shader` struct:

### Current Design (problematic)
```mermaid
graph TD
    subgraph Current["Current: Hash = f(source code)"]
        S1["Shader struct"] --> P1["Pass[0].Key = 0xABCD<br/><i>derived from source hash</i>"]
        P1 --> V1["ShaderVariantKey = f(0xABCD, keywords)"]
        V1 --> PK1["PipelineKey = f(variant, rtv, dsv)"]
        PK1 --> PSO1["PSO lookup in PipelineLibrary"]

        EDIT["User edits source"] -.-> STALE["Pass[0].Key is now STALE ❌<br/>Still 0xABCD, but source changed"]
        STALE -.-> WRONG["Looks up OLD bytecode<br/>or worse, the old PSO"]
    end

    style STALE fill:#e94560,stroke:#1a1a2e,color:#eee
    style WRONG fill:#e94560,stroke:#1a1a2e,color:#eee
```

### Proposed Design (stable)
```mermaid
graph TD
    subgraph Proposed["Proposed: Key = f(GUID, pass index)"]
        S2["Shader struct<br/>assetGUID = 7f3a-..."] --> P2["Pass[0]: index=0<br/><i>no source hash stored</i>"]
        P2 --> MK["ManifestKey = f(7f3a, 0, keywords)"]
        MK --> MANIFEST["Manifest Lookup<br/>→ ContentHash = 0x9999"]
        MANIFEST --> SL2["ShaderLibrary<br/>→ read 99/shader_cache_9999.bin"]
        SL2 --> PSO2["Create or get PSO"]

        EDIT2["User edits source"] -.-> RECOMP["Server recompiles<br/>→ new ContentHash = 0xBBBB"]
        RECOMP -.-> MUPD["Manifest updated:<br/>same key → 0xBBBB"]
        MUPD -.-> NEXT["Next frame: manifest read<br/>picks up 0xBBBB automatically"]
    end

    style RECOMP fill:#0f3460,stroke:#533483,color:#eee
    style MUPD fill:#0f3460,stroke:#533483,color:#eee
    style NEXT fill:#16213e,stroke:#0f3460,color:#eee
```

> [!IMPORTANT]
> **The `Shader` struct never changes.** No unload, no recreate, no generation counter bump. The manifest is the *only* mutable state, and it lives on disk, outside the runtime's object graph. The runtime just reads it.

---

## 7. The Two Interfaces That Make This Work

Only two abstractions are needed in `Ghost.Graphics` to support the full pipeline:

```mermaid
classDiagram
    class IShaderCompilationBridge {
        <<interface>>
        +TryGetBytecode(manifestKey: ulong, out bytecode: ReadOnlyMemory~byte~) bool
        +IsCompiling(manifestKey: ulong) bool
    }

    class RuntimeStub {
        +TryGetBytecode() → always from ShaderLibrary cache
        +IsCompiling() → always false
    }

    class EditorImplementation {
        -NamedPipeClient _serverConnection
        +TryGetBytecode() → check manifest, read cache
        +IsCompiling() → query server status
    }

    IShaderCompilationBridge <|.. RuntimeStub : "Shipped game"
    IShaderCompilationBridge <|.. EditorImplementation : "Editor mode"

    class ShaderLibrary {
        -string _cacheDirectory
        +GetCache(contentHash: ulong) Result~bytes~
        +GetFromManifest(manifestKey: ulong) Result~bytes~
    }

    EditorImplementation --> ShaderLibrary : reads cache
    RuntimeStub --> ShaderLibrary : reads cache
```

> [!TIP]
> `RenderGraphContext` doesn't talk to the bridge directly. It talks to `ShaderLibrary`, which internally consults the bridge on cache miss. This keeps the rendering code clean — it never sees compilation status. It just gets bytecode or it doesn't.

---

## 8. Build Pipeline — How Shipped Games Work

For completeness, here's how the same architecture handles builds:

```mermaid
flowchart LR
    subgraph BuildTime["Build Pipeline"]
        SCAN["Scan all materials<br/>in scenes/assets"] --> COLLECT["Collect all referenced<br/>(GUID, pass, variant) tuples"]
        COLLECT --> COMPILE["Compile all variants<br/>via ShaderServer"]
        COMPILE --> PACK["Package manifest +<br/>bytecode blobs into<br/>game data archive"]
    end

    subgraph ShippedGame["Runtime (shipped game)"]
        LOAD["Load manifest +<br/>bytecode from archive"] --> LIB["ShaderLibrary<br/>(read-only, all variants pre-cached)"]
        LIB --> MISS{"Cache miss?"}
        MISS -- "Never<br/>(if build is correct)" --> OK["Create PSO normally"]
        MISS -- "Somehow yes<br/>(bug or modding)" --> ERR["Error PSO<br/>+ log warning"]
    end

    BuildTime --> ShippedGame

    style BuildTime fill:#1a1a2e,stroke:#0f3460,color:#eee
    style ShippedGame fill:#16213e,stroke:#533483,color:#eee
```

The beauty: **the same `ShaderLibrary` and `PipelineLibrary` code runs in both editor and shipped game**. The only difference is whether `IShaderCompilationBridge` is the editor implementation or the runtime stub.

---

## Summary of Key Design Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | Stable GUID identity, not content hash | Shader struct never needs recreation on edit |
| 2 | Content-addressed cache | Deduplication, easy invalidation, git-friendly |
| 3 | Manifest as the bridge | Decouples identity from compiled output cleanly |
| 4 | Keep old PSO during recompile | Zero flicker, seamless UX |
| 5 | Separate compiler process | Crash isolation, independent resource budget |
| 6 | Two-method interface in runtime | Minimal coupling, easy to stub for shipped game |
| 7 | Deferred PSO release via fence | Prevents GPU use-after-free |
| 8 | Same code path for editor + shipped | Fewer bugs, one pipeline to maintain |
