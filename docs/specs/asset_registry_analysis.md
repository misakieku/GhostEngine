# GhostEngine Asset Registry — Design Analysis & Recommendations

## 1. Your Current Design at a Glance

Your current approach is **Unreal-style packed binary** (`.gasset`):

```
┌──────────────────────────────────────────────┐
│ AssetMetadata (128 bytes, fixed)             │
│   FormatVersion ─ ID ─ TypeID ─              │
│   HandlerVersion ─ DependencyCount ─         │
│   DependenciesOffset ─ SettingsOffset/Size ─  │
│   ContentOffset/Size                         │
├──────────────────────────────────────────────┤
│ Settings blob (struct → raw bytes)           │
├──────────────────────────────────────────────┤
│ Content blob (e.g. ImageContentHeader + raw) │
├──────────────────────────────────────────────┤
│ Dependencies (Guid[])                        │
└──────────────────────────────────────────────┘
```

The AssetRegistry maintains an in-memory GUID↔path index by reading the first 20 bytes of every `.gasset` on startup, with a `FileSystemWatcher` for live updates. A planned SQLite backend (`AssetRegistry.Backend.cs`) would persist this catalog.

---

## 2. Unreal vs Unity — The Trade-Off Matrix

| Dimension | Unreal (Packed Binary `.uasset`) | Unity (Raw File + `.meta` sidecar) |
|---|---|---|
| **Source control** | Opaque blobs — merges impossible, diffs useless | Raw files are human-readable; `.meta` is text YAML — mergeable |
| **Import speed** | One file to open per asset | Two opens per asset (source + meta), but meta is tiny |
| **Runtime loading** | One `seek+read` → done (no re-import step) | Must "import" (cook) before runtime loading; raw files are editor-only |
| **Artist iteration** | Must re-import through editor | Can drop a PNG in Explorer & it auto-imports |
| **Dependency tracking** | Embedded in the binary — self-contained | External DB (`.meta` GUIDs + Library/) — can desync |
| **Asset settings versioning** | Binary struct layout is fragile | YAML/JSON → easy to add fields with defaults |
| **Corruption resilience** | One corrupted byte → whole asset lost | Source file is unaffected; re-import fixes derived data |
| **Build pipeline** | Already cooked (or close to it) | Separate cook step needed for builds |
| **Team discoverability** | "What is this .gasset?" → need editor to inspect | "It's a PNG, I can open it anywhere" |

### Key Insight

> Unreal doesn't actually store source data inside `.uasset` for most asset types. Unreal stores the **cooked/processed** representation. The source data (FBX, PSD, etc.) lives outside the engine's asset system — artists use a separate "source art" folder. The `.uasset` is a **derived artifact**, not the source of truth.

Unity's insight was: **leave source files alone, store metadata beside them, and derive everything else into a Library/ cache.** The `.meta` sidecar is tiny (GUID + import settings in YAML), version-control-friendly, and the actual imported data lives in `Library/` (a local, regenerable cache).

---

## 3. Current Design — Issues Found

### 3.1 Binary Settings Are a Versioning Nightmare

```csharp
// TextureAssetHandler — writes settings as raw struct bytes
Unsafe.WriteUnaligned(ref address, settings.Basic);
Unsafe.WriteUnaligned(ref Unsafe.Add(ref address, ...), settings.Advanced);
```

**Problem:** Adding a single field to `BasicSettings`, `AdvancedSettings`, or `SamplerSettings` changes the struct layout. Every existing `.gasset` file becomes unreadable because the byte offsets shift. You have `HandlerVersion` in the metadata, but no migration logic — and you'd need one per handler per version.

> [!CAUTION]
> This is the #1 pain point of the Unreal approach in practice. Epic has dedicated teams managing asset versioning with `FArchive` custom serialization + version tags. For a small team, this is a massive maintenance burden.

### 3.2 Source File Is Destroyed on Import

```csharp
// OnFileSystemOp — line 224
File.Delete(assetPath);  // ← deletes the original source file!
```

After import, the source `.png` is deleted and only the `.gasset` remains. If the user wants to change import settings (e.g. switch from BC7 to BC5 for a normal map), they need to find the original source file elsewhere and re-import.

### 3.3 Handler Discovery Is O(N × M) per Call

```csharp
// GetAssetHandlerForExtension — line 326-338
foreach (var handlerType in AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(assembly => assembly.GetTypes())
    .Where(type => typeof(IAssetHandler).IsAssignableFrom(type) ...))
```

This scans **every type in every loaded assembly** on each call. It's called from `OnFileSystemOp` (FileSystemWatcher callback — frequent!) and `ImportAssetAsync`. The `_cachedHandler` dictionary helps for repeat loads, but the initial scan is expensive and runs every time a new extension is encountered.

### 3.4 `async void` in FileSystemWatcher Callback

```csharp
private async void OnFileSystemOp(object sender, FileSystemEventArgs e)
```

If `ImportAsync` throws, the exception is swallowed silently (unobserved). `FileSystemWatcher` callbacks should be synchronous (queue work to a channel/queue), or at minimum wrap the body in `try/catch`.

### 3.5 Race Conditions in Path Mapping

```csharp
// ConcurrentDictionary + lock(_pathLock)
_pathToGuid = new ConcurrentDictionary<...>();  // concurrent dict
lock (_pathLock) { _pathToGuid[relativePath] = guid; }  // but manually locked
```

You're using `ConcurrentDictionary` but also taking a `Lock` for every access. These two strategies conflict — either use a plain `Dictionary<>` + lock, or use `ConcurrentDictionary` lock-free. Mixing them gives the worst of both: allocation overhead of `ConcurrentDictionary` with the contention of a lock.

### 3.6 Missing Content Hash for Cache Invalidation

The `TextureProcessor` hashes **settings** to build a cache key (`guid_settingsHash.dds`), but doesn't hash the **source content**. If you replace a PNG with a different image of the same name, the stale cache is served because only the settings hash changed (it didn't).

### 3.7 No Version Migration Path

The 128-byte `AssetMetadata` header reserves space for expansion — good! But there's no mechanism to detect "this `.gasset` was written by handler v1 and we're now at v3" and upgrade in place. Currently `HandlerVersion` is written but never read.

---

## 4. Recommendation: Hybrid Architecture

I recommend a **Unity-inspired hybrid** — keep source files untouched, use lightweight sidecar metadata, and produce a separate cooked cache. Here's the concrete design:

### 4.1 Three-Layer Architecture

```
ProjectRoot/
├── Assets/                          ← Source files (PNG, FBX, HLSL, ...)
│   ├── Textures/
│   │   ├── hero_diffuse.png         ← Source of truth (never modified)
│   │   └── hero_diffuse.png.gmeta   ← Sidecar: GUID + import settings (YAML/JSON)
│   └── Models/
│       ├── character.fbx
│       └── character.fbx.gmeta
│
├── Library/                         ← Derived data cache (local, .gitignore'd)
│   ├── AssetDB.sqlite               ← Fast GUID↔path + dependency index
│   ├── Imports/                     ← Cooked assets (DDS, compiled meshes, etc.)
│   │   ├── <guid>.imported          ← Binary cooked data (current .gasset content section)
│   │   └── ...
│   └── Thumbnails/
│       └── <guid>.thumb
│
└── .ghostignore                     ← Patterns to exclude from asset scanning
```

### 4.2 `.gmeta` Sidecar File

```yaml
# hero_diffuse.png.gmeta
guid: 0906f4eb-c3f0-431b-bcea-132c88ab0c3f
handler: TextureAssetHandler
handlerVersion: 1
settings:
  textureType: Default
  textureShape: Texture2D
  isSRGB: true
  maxSize: 2048
  filterMode: Anisotropic
  wrapMode: Repeat
  generateMipmaps: true
  compressionLevel: Normal
  # ... full settings tree
dependencies: []
labels: [environment, hero]               # optional user tags
```

**Why this is better:**

| Concern | Current `.gasset` | Proposed `.gmeta` |
|---|---|---|
| Add a field | Binary layout breaks | YAML: missing keys → default values |
| Merge conflict | Impossible (binary) | Text merge, trivial |
| Inspect settings | Need editor | Open in any text editor |
| Source file recovery | Destroyed | Untouched, always available |
| Re-import | Need original file | `Library/` rebuild from source + `.gmeta` |
| `git diff` | `Binary files differ` | Readable YAML diff |

### 4.3 SQLite Catalog (`Library/AssetDB.sqlite`)

Replace the in-memory `ConcurrentDictionary<string, Guid>` mapping with an SQLite database (you already planned this in `AssetRegistry.Backend.cs`):

```sql
-- Core asset table
CREATE TABLE assets (
    guid        BLOB PRIMARY KEY,   -- 16 bytes, exactly sizeof(Guid)
    path        TEXT NOT NULL,       -- relative path to .gmeta
    handler     TEXT NOT NULL,       -- handler type name
    content_hash TEXT,              -- xxHash64 of source file bytes
    settings_hash TEXT,             -- xxHash64 of import settings
    imported_at  INTEGER,           -- unix timestamp of last successful import
    UNIQUE(path)
);

-- Dependency edges (forward: asset → dependency)
CREATE TABLE dependencies (
    from_guid   BLOB NOT NULL REFERENCES assets(guid),
    to_guid     BLOB NOT NULL REFERENCES assets(guid),
    PRIMARY KEY (from_guid, to_guid)
);

-- Reverse index for "what depends on me?" queries
CREATE INDEX idx_dep_reverse ON dependencies(to_guid);

-- Full-text search on asset paths and labels
CREATE VIRTUAL TABLE assets_fts USING fts5(path, labels);
```

**Startup becomes:**
1. Open SQLite DB → instant GUID↔path from indexed table
2. Diff `Assets/` tree vs DB → find stale/new/deleted `.gmeta` files
3. Queue incremental re-imports only for changed assets

This is **dramatically faster** than scanning every `.gasset` header on disk (your current `LoadExistingAssets`).

### 4.4 Import Pipeline

```
Source File Changed
       │
       ▼
  FileSystemWatcher
       │
       ├─── No .gmeta exists? → Generate one (new GUID, default settings)
       │
       ▼
  Hash source + settings
       │
       ├─── Hash matches DB? → Skip (already imported)
       │
       ▼
  Queue ImportJob to background channel
       │
       ▼
  ImportWorker (background thread pool)
       │
       ├── Read source file
       ├── Run handler pipeline (e.g. NVTT compress)
       ├── Write Library/Imports/<guid>.imported
       ├── Update SQLite (content_hash, settings_hash, imported_at)
       └── Fire AssetChanged event on main thread
```

### 4.5 Handler Registration — Build Once, Cache Forever

Replace the per-call assembly scan with a startup-once TypeCache approach (you already have this pattern in the engine):

```csharp
// Startup: build lookup tables once
Dictionary<string, Type> _extensionToHandler;   // ".png" → typeof(TextureAssetHandler)
Dictionary<Guid, Type>   _typeIdToHandler;      // TypeGuid → handler type

// Populated once via TypeCache / assembly attribute scan at editor startup
foreach (var type in TypeCache.GetTypesWithAttribute<CustomAssetHandlerAttribute>())
{
    var attr = type.GetCustomAttribute<CustomAssetHandlerAttribute>();
    _typeIdToHandler[new Guid(attr.ID)] = type;
    foreach (var ext in attr.SupportedExtensions)
        _extensionToHandler[ext] = type;
}
```

---

## 5. What to Keep from Your Current Design

Your design has several things done well:

| Element | Verdict |
|---|---|
| `AssetMetadata` fixed-size header with offsets | ✅ Keep for the cooked `.imported` files — great for O(1) seeks |
| `Handle<GPUTexture>` on `TextureAsset` | ✅ Clean separation of asset data vs GPU resource handle |
| `WeakReference<Asset>` cache in registry | ✅ Elegant — auto-evicts when nothing holds the asset |
| `IAssetHandler` / `IImportableAssetHandler` split | ✅ Good separation (some assets are import-only, e.g. shaders compiled differently) |
| `AssetReference` with internal/external encoding | ✅ Clever — keeps sub-asset refs compact |
| `TextureProcessor` cache with settings hash | ✅ Great idea, just needs content hash too |
| `Result<T>` return pattern | ✅ Consistent with the rest of GhostEngine |

---

## 6. Summary Recommendation

```
┌────────────────────────────────────────────────────────────┐
│                    RECOMMENDED APPROACH                     │
│                                                            │
│   Source files       →  untouched, checked into git        │
│   .gmeta sidecars    →  GUID + settings (YAML), in git    │
│   Library/           →  derived cache, .gitignored         │
│     AssetDB.sqlite   →  fast GUID↔path index              │
│     Imports/*.imported → cooked binary (your AssetMetadata │
│                          header + content, no settings)    │
│                                                            │
│   Binary format      →  for cooked data only, not settings │
│   Settings format    →  YAML/JSON in .gmeta (human + VCS) │
│   Handler discovery  →  one-time TypeCache at startup      │
│   Watcher callbacks  →  queue to Channel<T>, no async void │
└────────────────────────────────────────────────────────────┘
```

This gives you:
- **Unreal's runtime performance** (cooked binary in Library/ → single seek+read)
- **Unity's artist workflow** (drop files in Assets/, settings are readable text)
- **Clean version control** (text `.gmeta` files merge cleanly)
- **Resilient re-import** (source is never touched; Library/ is regenerable)
- **Zero startup cost** (SQLite index instead of scanning thousands of file headers)

---

## 7. Open Questions for You

1. **Do you want `.gmeta` in YAML, JSON, or a custom text format?** YAML is more compact and human-friendly, but adds a parser dependency. JSON is built into .NET but more verbose. A custom format is more work.

2. **Should the cooked `.imported` files keep the 128-byte `AssetMetadata` header?** It's useful for validation on load, but since SQLite already knows the GUID and handler, you could simplify the binary format.

3. **Do you want hot-reload of import settings?** (Changing `.gmeta` → auto re-import and refresh live asset in editor.) Your current `WeakReference<Asset>` + `RefreshAsync` already supports this.

4. **How do you want to handle the `Library/` on first clone?** Options: (a) full re-import from source, (b) share a pre-built Library via LFS, (c) asset server that caches imports.
