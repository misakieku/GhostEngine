# GhostEngine Asset Registry — Detailed Design Document

> **Audience:** Anyone working on this system alongside me.  
> **Scope:** Full rewrite of the asset pipeline — sidecar metadata, SQLite catalog, import queue, cooked cache, and updated handler API.  
> **Existing code references** are linked throughout so you can trace every decision back to the current implementation.

---

## Table of Contents

1. [Design Overview](#1-design-overview)
2. [Project Folder Layout](#2-project-folder-layout)
3. [The `.gmeta` Sidecar File](#3-the-gmeta-sidecar-file)
4. [SQLite Catalog (`AssetDB`)](#4-sqlite-catalog-assetdb)
5. [Handler Registration & TypeCache Integration](#5-handler-registration--typecache-integration)
6. [Import Pipeline](#6-import-pipeline)
7. [Asset Loading (Runtime Path)](#7-asset-loading-runtime-path)
8. [FileSystemWatcher & Change Detection](#8-filesystemwatcher--change-detection)
9. [Dependency Graph](#9-dependency-graph)
10. [Cooked Binary Format](#10-cooked-binary-format)
11. [AssetRegistry Class Redesign](#11-assetregistry-class-redesign)
12. [Handler API Redesign](#12-handler-api-redesign)
13. [Migration Path from Current Code](#13-migration-path-from-current-code)
14. [Thread Safety Model](#14-thread-safety-model)
15. [Error Handling Strategy](#15-error-handling-strategy)
16. [Open Decisions & Trade-offs](#16-open-decisions--trade-offs)

---

## 1. Design Overview

### Core Principle: Separate source truth from derived data

```
┌─────────────────────────────────────────────────────────────────┐
│                    USER'S PROJECT                               │
│                                                                 │
│  Assets/            ← Source files + sidecar .gmeta (in git)    │
│  Library/           ← Derived cache (NOT in git, regenerable)   │
│    AssetDB.sqlite   ← Fast GUID↔path index + dep graph          │
│    Imports/         ← Cooked binary blobs                       │
│  Sources/           ← C# user scripts                           │
│  Config/            ← Project config                            │
└─────────────────────────────────────────────────────────────────┘
```

**Three files per asset, three distinct roles:**

| File | Location | In VCS? | Role |
|------|----------|---------|------|
| `hero.png` | `Assets/Textures/` | ✅ | Source of truth — never modified by the engine |
| `hero.png.gmeta` | `Assets/Textures/` | ✅ | Identity (GUID) + import settings (JSON) |
| `<guid>.imported` | `Library/Imports/` | ❌ | Cooked/processed binary — the runtime loads this |

The current design ([AssetRegistry.cs](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs)) merges all three concerns into a single `.gasset` file. The rewrite splits them.

---

## 2. Project Folder Layout

The existing `EditorApplication` ([EditorApplication.cs](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/EditorApplication.cs)) already defines the project folder constants. We add one:

```csharp
public const string LIBRARY_FOLDER_NAME = "Library";
public static string LibraryFolderPath => Path.Combine(ProjectPath, LIBRARY_FOLDER_NAME);
```

Full project tree at runtime:

```
MyProject/
├── Assets/                          ← EditorApplication.AssetsFolderPath
│   ├── Textures/
│   │   ├── hero_diffuse.png         ← source file
│   │   ├── hero_diffuse.png.gmeta   ← sidecar metadata
│   │   ├── hero_normal.png
│   │   └── hero_normal.png.gmeta
│   ├── Models/
│   │   ├── character.fbx
│   │   └── character.fbx.gmeta
│   └── Materials/
│       ├── hero_material.gasset     ← engine-native assets (no source file)
│       └── hero_material.gasset.gmeta
│
├── Library/                         ← EditorApplication.LibraryFolderPath
│   ├── AssetDB.sqlite               ← catalog
│   ├── Imports/
│   │   ├── 0906f4eb-c3f0431b-bcea132c88ab0c3f.imported
│   │   └── ...
│   └── Thumbnails/
│       └── ...
│
├── Sources/                         ← EditorApplication.SourcesFolderPath
├── Config/                          ← EditorApplication.ConfigFolderPath
└── .gitignore                       ← must include Library/ and Caches/
```

> [!IMPORTANT]
> **Engine-native assets** (materials, prefabs, scenes — things that have no external source file) still need a `.gmeta`. For these, the `.gasset` file IS the source, and the `.gmeta` sits beside it. The `Library/Imports/` entry is either a copy or not needed (the `.gasset` is already in a loadable format).

---

## 3. The `.gmeta` Sidecar File

### 3.1 Why JSON over YAML/binary

- `System.Text.Json` ships with .NET — zero new dependencies
- JSON diffs are readable in any git client
- Add/remove fields without breaking existing files (missing keys → defaults)
- `JsonSerializerOptions.WriteIndented` makes it human-readable
- We can use source generators (`System.Text.Json.SourceGeneration`) for AOT-safe serialization in the future

### 3.2 File naming convention

The `.gmeta` file name is always `<source_file_name_including_extension>.gmeta`:

```
hero_diffuse.png       → hero_diffuse.png.gmeta
character.fbx          → character.fbx.gmeta
hero_material.gasset   → hero_material.gasset.gmeta
```

This is the Unity convention. It guarantees a 1:1 mapping and lets `FileSystemWatcher` easily pair source ↔ meta.

### 3.3 Data model

```csharp
namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// Persisted as a JSON sidecar (.gmeta) next to every source asset.
/// This is the single source of truth for asset identity and import settings.
/// </summary>
public sealed class AssetMeta
{
    /// <summary>
    /// Globally unique identifier for this asset. Generated once, never changes.
    /// Even if the file is moved/renamed, this GUID follows it via the .gmeta.
    /// </summary>
    public required Guid Guid { get; init; }

    /// <summary>
    /// The Guid that identifies which IAssetHandler processes this asset.
    /// Maps to CustomAssetHandlerAttribute.ID.
    /// Null for auto-detection from file extension.
    /// </summary>
    public Guid? HandlerTypeId { get; set; }

    /// <summary>
    /// Version of the handler that last imported this asset.
    /// Used to detect when a handler upgrade requires re-import.
    /// </summary>
    public int HandlerVersion { get; set; }

    /// <summary>
    /// xxHash64 of the source file content at last successful import.
    /// Stored as hex string for JSON readability.
    /// Used to skip re-import when source hasn't changed.
    /// </summary>
    public string? ContentHash { get; set; }

    /// <summary>
    /// xxHash64 of the serialized import settings at last successful import.
    /// If settings change, we know to re-import even if source didn't change.
    /// </summary>
    public string? SettingsHash { get; set; }

    /// <summary>
    /// UTC timestamp of last successful import.
    /// </summary>
    public DateTime? LastImportedUtc { get; set; }

    /// <summary>
    /// GUIDs of other assets this asset depends on.
    /// For example, a material might reference texture assets.
    /// </summary>
    public Guid[] Dependencies { get; set; } = [];

    /// <summary>
    /// Optional user-facing labels for search/filtering in the editor.
    /// </summary>
    public string[] Labels { get; set; } = [];

    /// <summary>
    /// Handler-specific import settings. Stored as a polymorphic JSON object.
    /// The concrete type is determined by HandlerTypeId at deserialization time.
    /// </summary>
    public IAssetSettings? Settings { get; set; }
}
```

### 3.4 Example on disk

```json
{
  "guid": "0906f4eb-c3f0-431b-bcea-132c88ab0c3f",
  "handlerTypeId": "0906f4eb-c3f0-431b-bcea-132c88ab0c3f",
  "handlerVersion": 1,
  "contentHash": "A1B2C3D4E5F67890",
  "settingsHash": "1234567890ABCDEF",
  "lastImportedUtc": "2026-04-14T07:00:00Z",
  "dependencies": [],
  "labels": ["environment", "hero"],
  "settings": {
    "$type": "TextureAssetSettings",
    "basic": {
      "textureType": "Default",
      "textureShape": "Texture2D",
      "isSRGB": true
    },
    "advanced": {
      "generateMipmaps": true,
      "compressionLevel": "Normal",
      "mipmapFilter": "Kaiser"
    },
    "sampler": {
      "maxSize": 2048,
      "filterMode": "Anisotropic",
      "wrapMode": "Repeat"
    }
  }
}
```

### 3.5 Settings serialization — the polymorphism problem

The `IAssetSettings` property is polymorphic — each handler has its own settings type (`TextureAssetSettings`, future `MeshAssetSettings`, etc.). We solve this with `System.Text.Json`'s built-in polymorphism:

```csharp
// Mark IAssetSettings for polymorphic serialization
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextureAssetSettings), "TextureAssetSettings")]
// [JsonDerivedType(typeof(MeshAssetSettings), "MeshAssetSettings")]  ← add as you create new types
public interface IAssetSettings;
```

> [!NOTE]
> This replaces the current approach of `Unsafe.WriteUnaligned` raw struct bytes 
> ([TextureAsset.cs:229-253](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/AssetHandler/TextureAsset.cs#L229-L253)).
> The old way breaks whenever you add a field to `TextureAssetSettings.BasicSettings`.
> JSON handles field addition/removal gracefully — missing fields get default values.

### 3.6 Reading/writing `.gmeta` files

```csharp
namespace Ghost.Editor.Core.AssetHandler;

internal static class AssetMetaIO
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Read a .gmeta sidecar file. Returns null if the file doesn't exist.
    /// </summary>
    public static async ValueTask<AssetMeta?> ReadAsync(string metaPath, CancellationToken token = default)
    {
        if (!File.Exists(metaPath))
        {
            return null;
        }

        await using var stream = new FileStream(metaPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return await JsonSerializer.DeserializeAsync<AssetMeta>(stream, s_options, token).ConfigureAwait(false);
    }

    /// <summary>
    /// Write a .gmeta sidecar file atomically (write to .tmp, then rename).
    /// </summary>
    public static async ValueTask WriteAsync(string metaPath, AssetMeta meta, CancellationToken token = default)
    {
        var tempPath = metaPath + ".tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(stream, meta, s_options, token).ConfigureAwait(false);
        }

        File.Move(tempPath, metaPath, overwrite: true);
    }

    /// <summary>
    /// Given a source file path, returns the expected .gmeta path.
    /// </summary>
    public static string GetMetaPath(string sourceFilePath)
    {
        return sourceFilePath + ".gmeta";
    }

    /// <summary>
    /// Given a .gmeta path, returns the source file path.
    /// </summary>
    public static string GetSourcePath(string metaPath)
    {
        // "hero.png.gmeta" → "hero.png"
        return metaPath[..^".gmeta".Length];
    }
}
```

> [!TIP]
> The atomic write (write to `.tmp` + `File.Move`) prevents corruption if the editor crashes mid-write. `File.Move` with `overwrite: true` is atomic on NTFS.

---

## 4. SQLite Catalog (`AssetDB`)

This replaces the current in-memory `ConcurrentDictionary<string, Guid>` + `ConcurrentDictionary<Guid, string>` ([AssetRegistry.cs:42-43](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L42-L43)) and the current `Dictionary<Guid, HashSet<Guid>>` dependency graph ([AssetRegistry.cs:48-49](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L48-L49)).

### 4.1 Why SQLite

- `Microsoft.Data.Sqlite` is already in [Ghost.Editor.Core.csproj](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Ghost.Editor.Core.csproj#L18) — **zero additional dependencies**
- Startup is O(1) — open file, tables are already indexed
- Survives editor crashes (WAL mode + transactions)
- Queryable — "find all textures with label X" becomes a SQL query
- Eliminates the current full disk scan on startup ([AssetRegistry.cs:99-134](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L99-L134))

### 4.2 Schema

```sql
-- Database: Library/AssetDB.sqlite
-- Journal mode: WAL (concurrent readers, single writer)
PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

-- Schema version for migrations
PRAGMA user_version = 1;

-- ─── Core tables ───────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS assets (
    -- Asset GUID stored as 16-byte BLOB (matches sizeof(Guid))
    guid            BLOB(16) PRIMARY KEY NOT NULL,
    
    -- Relative path from Assets/ root to the SOURCE file (not .gmeta)
    -- e.g. "Textures/hero_diffuse.png"
    source_path     TEXT NOT NULL,
    
    -- Handler type GUID (matches CustomAssetHandlerAttribute.ID)
    handler_type_id BLOB(16),
    
    -- Handler version that last imported this asset
    handler_version INTEGER NOT NULL DEFAULT 0,
    
    -- xxHash64 of source file content (hex string)
    content_hash    TEXT,
    
    -- xxHash64 of import settings (hex string)  
    settings_hash   TEXT,
    
    -- Unix timestamp (ms) of last successful import
    imported_at_ms  INTEGER,
    
    -- Asset state: 0 = needs import, 1 = imported, 2 = import failed
    state           INTEGER NOT NULL DEFAULT 0,
    
    -- Error message if state = 2
    error_message   TEXT,

    UNIQUE(source_path)
);

-- Fast path→guid lookup (most common query during FSW events)
CREATE INDEX IF NOT EXISTS idx_assets_path ON assets(source_path);

-- ─── Dependency edges ──────────────────────────────────────────

CREATE TABLE IF NOT EXISTS dependencies (
    -- The asset that depends on another
    from_guid   BLOB(16) NOT NULL REFERENCES assets(guid) ON DELETE CASCADE,
    -- The asset being depended upon
    to_guid     BLOB(16) NOT NULL REFERENCES assets(guid) ON DELETE CASCADE,
    PRIMARY KEY (from_guid, to_guid)
);

-- Reverse lookup: "what depends on asset X?"  (for invalidation cascades)
CREATE INDEX IF NOT EXISTS idx_dep_reverse ON dependencies(to_guid);

-- ─── Labels ────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS labels (
    guid    BLOB(16) NOT NULL REFERENCES assets(guid) ON DELETE CASCADE,
    label   TEXT NOT NULL,
    PRIMARY KEY (guid, label)
);

CREATE INDEX IF NOT EXISTS idx_labels_label ON labels(label);
```

### 4.3 The `AssetCatalog` class

This is a thin C# wrapper. It lives in `AssetRegistry.Backend.cs` (replacing the current empty placeholder):

```csharp
namespace Ghost.Editor.Core.Services;

using Microsoft.Data.Sqlite;

/// <summary>
/// Thread-safe SQLite-backed asset catalog. 
/// All public methods synchronize internally — callers need no locks.
/// 
/// Replaces the in-memory ConcurrentDictionary approach with persistent storage
/// that survives editor restarts.
/// </summary>
internal sealed class AssetCatalog : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Lock _writeLock = new();

    // ─── Prepared statement cache ──────────────────────────────
    // Prepared once, reused on every call — avoids SQL parsing overhead.
    
    private readonly SqliteCommand _cmdGetGuid;
    private readonly SqliteCommand _cmdGetPath;
    private readonly SqliteCommand _cmdUpsert;
    private readonly SqliteCommand _cmdDelete;
    private readonly SqliteCommand _cmdSetState;
    private readonly SqliteCommand _cmdGetReferencers;
    private readonly SqliteCommand _cmdGetDependencies;
    private readonly SqliteCommand _cmdInsertDep;
    private readonly SqliteCommand _cmdClearDeps;

    public AssetCatalog(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        
        var connString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
        
        _connection = new SqliteConnection(connString);
        _connection.Open();
        
        // Enable WAL for concurrent reads
        using var pragma = _connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;";
        pragma.ExecuteNonQuery();

        CreateSchema();
        PrepareStatements();
    }

    // ─── Lookup ────────────────────────────────────────────────

    /// <summary>
    /// Get the asset GUID for a source path. Returns Guid.Empty if not found.
    /// Path is relative to Assets/ root.
    /// </summary>
    public Guid GetGuid(string sourcePath) { /* bind + ExecuteScalar */ }

    /// <summary>
    /// Get the source path for an asset GUID. Returns null if not found.
    /// </summary>
    public string? GetSourcePath(Guid guid) { /* bind + ExecuteScalar */ }

    // ─── Mutation ──────────────────────────────────────────────

    /// <summary>
    /// Insert or update an asset entry. Called after reading/creating a .gmeta file.
    /// </summary>
    public void Upsert(AssetMeta meta, string sourcePath) { /* INSERT OR REPLACE */ }

    /// <summary>
    /// Remove an asset entry. Called when a source file is deleted.
    /// CASCADE deletes dependencies and labels automatically.
    /// </summary>
    public bool Remove(string sourcePath) { /* DELETE, return rows affected > 0 */ }
    public bool Remove(Guid guid) { /* DELETE, return rows affected > 0 */ }

    /// <summary>
    /// Mark an asset as needing re-import (state = 0).
    /// </summary>
    public void MarkDirty(Guid guid) { /* UPDATE state = 0 */ }

    /// <summary>
    /// Mark an asset as successfully imported.
    /// Updates content_hash, settings_hash, imported_at_ms, state = 1.
    /// </summary>
    public void MarkImported(Guid guid, string contentHash, string settingsHash) { /* UPDATE */ }

    /// <summary>
    /// Mark an asset import as failed (state = 2) with error message.
    /// </summary>
    public void MarkFailed(Guid guid, string error) { /* UPDATE */ }

    // ─── Dependencies ──────────────────────────────────────────

    /// <summary>
    /// Replace all forward dependencies for an asset (transaction).
    /// </summary>
    public void SetDependencies(Guid assetId, ReadOnlySpan<Guid> dependencies)
    {
        lock (_writeLock)
        {
            using var tx = _connection.BeginTransaction();
            
            _cmdClearDeps.Parameters[0].Value = assetId.ToByteArray();
            _cmdClearDeps.Transaction = tx;
            _cmdClearDeps.ExecuteNonQuery();
            
            foreach (var dep in dependencies)
            {
                _cmdInsertDep.Parameters[0].Value = assetId.ToByteArray();
                _cmdInsertDep.Parameters[1].Value = dep.ToByteArray();
                _cmdInsertDep.Transaction = tx;
                _cmdInsertDep.ExecuteNonQuery();
            }
            
            tx.Commit();
        }
    }

    /// <summary>
    /// Get all assets that depend on the given asset (reverse lookup).
    /// Used for invalidation cascade — when asset X changes, all its referencers are dirty.
    /// </summary>
    public List<Guid> GetReferencers(Guid guid) { /* SELECT from_guid WHERE to_guid = ? */ }

    /// <summary>
    /// Get all assets that the given asset depends on (forward lookup).
    /// </summary>
    public List<Guid> GetDependencies(Guid guid) { /* SELECT to_guid WHERE from_guid = ? */ }

    // ─── Queries ───────────────────────────────────────────────

    /// <summary>
    /// Get all assets in state 0 (needs import).
    /// Used on startup to queue pending imports.
    /// </summary>
    public List<(Guid guid, string sourcePath)> GetDirtyAssets() { /* SELECT WHERE state = 0 */ }

    /// <summary>
    /// Enumerate all known assets. Used for full consistency checks.
    /// </summary>
    public IEnumerable<(Guid guid, string sourcePath)> EnumerateAll() { /* SELECT */ }

    public void Dispose()
    {
        _cmdGetGuid.Dispose();
        _cmdGetPath.Dispose();
        // ... dispose all prepared commands ...
        _connection.Dispose();
    }
}
```

### 4.4 Guid storage in SQLite

SQLite doesn't have a native GUID type. We store them as 16-byte `BLOB`:

```csharp
// Writing:
parameter.Value = guid.ToByteArray();   // Guid → byte[16]

// Reading:
var bytes = (byte[])reader["guid"];
var guid = new Guid(bytes);             // byte[16] → Guid
```

This keeps GUIDs compact (16 bytes vs 36+ chars for a string) and equality checks fast (BLOB comparison).

---

## 5. Handler Registration & TypeCache Integration

### 5.1 The current problem

The current code scans **all assemblies × all types** on every handler lookup ([AssetRegistry.cs:326-338](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L326-L338) and [AssetRegistry.cs:342-354](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L342-L354)). This is O(N×M) and runs from `FileSystemWatcher` callbacks.

### 5.2 The fix: `AssetHandlerRegistry` (build once at startup)

We already have `TypeCache` ([TypeCache.cs](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Utilities/TypeCache.cs)) that scans assemblies marked with `[EngineAssembly]` at startup. But `TypeCache` currently only caches method-level attributes, not class-level ones. Two options:

**Option A** — Extend `TypeCache` to also cache class-level `DiscoverableAttributeBase` subclasses. This is a broader change that benefits the whole editor.

**Option B** — Build a dedicated `AssetHandlerRegistry` that does its own scan once. Simpler, self-contained.

I recommend **Option B** for now (less blast radius), with Option A as a future cleanup:

```csharp
namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// One-time scan at editor startup → two dictionaries.
/// All lookups are O(1) after construction.
/// </summary>
internal sealed class AssetHandlerRegistry
{
    // ".png" → handler instance
    private readonly Dictionary<string, IAssetHandler> _byExtension;
    // TypeId GUID → handler instance
    private readonly Dictionary<Guid, IAssetHandler> _byTypeId;
    // TypeId GUID → handler version (from attribute or const on the handler)
    private readonly Dictionary<Guid, int> _versionByTypeId;

    public AssetHandlerRegistry()
    {
        _byExtension = new Dictionary<string, IAssetHandler>(StringComparer.OrdinalIgnoreCase);
        _byTypeId = new Dictionary<Guid, IAssetHandler>();
        _versionByTypeId = new Dictionary<Guid, int>();
        
        // Scan once using TypeCache's already-loaded types
        foreach (var typeInfo in TypeCache.GetTypes())
        {
            if (typeInfo.IsAbstract || typeInfo.IsInterface)
                continue;
            if (!typeof(IAssetHandler).IsAssignableFrom(typeInfo))
                continue;

            var attr = typeInfo.GetCustomAttribute<CustomAssetHandlerAttribute>(false);
            if (attr is null)
                continue;

            var handler = (IAssetHandler)Activator.CreateInstance(typeInfo)!;
            var typeId = new Guid(attr.ID);

            _byTypeId[typeId] = handler;

            foreach (var ext in attr.SupportedExtensions)
            {
                _byExtension[ext] = handler;
            }
        }
    }

    public IAssetHandler? GetByExtension(string extension)
    {
        _byExtension.TryGetValue(extension, out var handler);
        return handler;
    }

    public IAssetHandler? GetByTypeId(Guid typeId)
    {
        _byTypeId.TryGetValue(typeId, out var handler);
        return handler;
    }

    public IEnumerable<string> GetSupportedExtensions() => _byExtension.Keys;
}
```

This replaces the `_cachedHander` dictionary and the two `GetAssetHandlerFor*` methods in the current `AssetRegistry`.

> [!NOTE]
> We also have `AssetImporterAttribute` in [Attributes.cs:24-34](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Attributes.cs#L24-L34) which serves a similar purpose. As part of this rewrite, we should unify it with `CustomAssetHandlerAttribute` to avoid two parallel discovery systems.

---

## 6. Import Pipeline

This is the biggest change from the current design. Currently, import happens inline in `OnFileSystemOp` ([AssetRegistry.cs:189-255](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L189-L255)) and `ImportAssetAsync`. The rewrite separates detection from execution.

### 6.1 Architecture

```
        FileSystemWatcher                     Manual "Reimport" button
              │                                        │
              ▼                                        ▼
     ┌─────────────────────────────────────────────────────┐
     │            ImportCoordinator                        │
     │  (channels FSW events → determines what to import)  │
     └────────────────────────┬────────────────────────────┘
                              │ writes ImportJob to
                              ▼
                    Channel<ImportJob>
                    (bounded, backpressure)
                              │
              ┌───────────────┼──────────────┐
              ▼               ▼              ▼
        ImportWorker     ImportWorker    ImportWorker
        (thread pool)    (thread pool)   (thread pool)
              │
              ▼
     ┌─────────────────┐
     │  IAssetHandler  │
     │  .ImportAsync() │
     └────────┬────────┘
              │ writes
              ▼
     Library/Imports/<guid>.imported     ← cooked binary
     AssetCatalog.MarkImported()         ← update DB
     AssetMeta contentHash/settingsHash  ← update .gmeta
              │
              ▼
     OnAssetChanged event (marshalled to UI thread via DispatcherQueue)
```

### 6.2 ImportJob and ImportCoordinator

```csharp
namespace Ghost.Editor.Core.Services;

internal enum ImportReason
{
    NewAsset,           // .gmeta just created
    SourceChanged,      // source file content changed
    SettingsChanged,    // user edited import settings in inspector
    HandlerUpgraded,    // handler version bumped
    ManualReimport,     // user clicked "Reimport" in context menu
    Startup,            // found dirty assets on editor startup
}

internal readonly record struct ImportJob(
    Guid AssetGuid,
    string SourcePath,      // relative to Assets/ root
    string MetaPath,        // absolute path to .gmeta
    ImportReason Reason
);
```

```csharp
internal sealed class ImportCoordinator : IDisposable
{
    private readonly Channel<ImportJob> _importChannel;
    private readonly AssetCatalog _catalog;
    private readonly AssetHandlerRegistry _handlers;
    private readonly string _assetsRoot;
    private readonly CancellationTokenSource _cts;
    private readonly Task[] _workers;

    public event EventHandler<IAssetRegistry, AssetChangedEventArgs>? OnAssetChanged;

    public ImportCoordinator(AssetCatalog catalog, AssetHandlerRegistry handlers, string assetsRoot, int workerCount = 2)
    {
        _catalog = catalog;
        _handlers = handlers;
        _assetsRoot = assetsRoot;
        _cts = new CancellationTokenSource();

        // Bounded channel provides backpressure if imports pile up
        _importChannel = Channel.CreateBounded<ImportJob>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });

        // Start N workers reading from the channel
        _workers = new Task[workerCount];
        for (var i = 0; i < workerCount; i++)
        {
            _workers[i] = Task.Run(() => WorkerLoop(_cts.Token));
        }
    }

    /// <summary>
    /// Enqueue an import job. Non-blocking (unless channel is full).
    /// </summary>
    public ValueTask EnqueueAsync(ImportJob job, CancellationToken token = default)
    {
        return _importChannel.Writer.WriteAsync(job, token);
    }

    /// <summary>
    /// Queue all assets in state=0 (dirty) from the catalog.
    /// Called once at startup after catalog sync.
    /// </summary>
    public async ValueTask EnqueueDirtyAssetsAsync(CancellationToken token = default)
    {
        foreach (var (guid, sourcePath) in _catalog.GetDirtyAssets())
        {
            var metaPath = AssetMetaIO.GetMetaPath(Path.Combine(_assetsRoot, sourcePath));
            await EnqueueAsync(new ImportJob(guid, sourcePath, metaPath, ImportReason.Startup), token);
        }
    }

    private async Task WorkerLoop(CancellationToken token)
    {
        await foreach (var job in _importChannel.Reader.ReadAllAsync(token))
        {
            try
            {
                await ProcessImportAsync(job, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.Error($"Import failed for {job.SourcePath}: {ex.Message}");
                _catalog.MarkFailed(job.AssetGuid, ex.Message);
            }
        }
    }

    private async ValueTask ProcessImportAsync(ImportJob job, CancellationToken token)
    {
        var fullSourcePath = Path.Combine(_assetsRoot, job.SourcePath);

        // 1. Read .gmeta
        var meta = await AssetMetaIO.ReadAsync(job.MetaPath, token);
        if (meta is null)
        {
            Logger.Warning($"Missing .gmeta for {job.SourcePath}, skipping import");
            return;
        }

        // 2. Resolve handler
        var ext = Path.GetExtension(job.SourcePath);
        var handler = meta.HandlerTypeId.HasValue
            ? _handlers.GetByTypeId(meta.HandlerTypeId.Value)
            : _handlers.GetByExtension(ext);

        if (handler is not IImportableAssetHandler importable)
        {
            // Not importable (engine-native asset). Nothing to do.
            _catalog.MarkImported(job.AssetGuid, "", "");
            return;
        }

        // 3. Check if import is actually needed (content + settings hash)
        var contentHash = await ComputeFileHashAsync(fullSourcePath, token);
        var settingsHash = ComputeSettingsHash(meta.Settings);

        if (job.Reason != ImportReason.ManualReimport
            && contentHash == meta.ContentHash
            && settingsHash == meta.SettingsHash)
        {
            // Nothing changed — skip
            _catalog.MarkImported(job.AssetGuid, contentHash, settingsHash);
            return;
        }

        // 4. Do the actual import
        var importedPath = GetImportedPath(job.AssetGuid);
        Directory.CreateDirectory(Path.GetDirectoryName(importedPath)!);

        await using var sourceStream = new FileStream(fullSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using var targetStream = new FileStream(importedPath, FileMode.Create, FileAccess.Write, FileShare.None);

        var result = await importable.ImportAsync(sourceStream, targetStream, job.AssetGuid, meta.Settings, token);
        if (result.IsFailure)
        {
            _catalog.MarkFailed(job.AssetGuid, result.Message ?? "Unknown error");
            return;
        }

        // 5. Update .gmeta with new hashes
        meta.ContentHash = contentHash;
        meta.SettingsHash = settingsHash;
        meta.LastImportedUtc = DateTime.UtcNow;
        await AssetMetaIO.WriteAsync(job.MetaPath, meta, token);

        // 6. Update catalog
        _catalog.MarkImported(job.AssetGuid, contentHash, settingsHash);

        // 7. Fire change event (marshal to UI thread)
        var args = new AssetChangedEventArgs(job.SourcePath, null, AssetChangeType.Modified);
        EditorApplication.DispatcherQueue.TryEnqueue(() =>
        {
            OnAssetChanged?.Invoke(/* registry */, args);
        });
    }

    private string GetImportedPath(Guid guid)
    {
        return Path.Combine(EditorApplication.LibraryFolderPath, "Imports", $"{guid:N}.imported");
    }

    private static async ValueTask<string> ComputeFileHashAsync(string filePath, CancellationToken token)
    {
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = new XxHash64();
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, token)) > 0)
            {
                hash.Append(buffer.AsSpan(0, bytesRead));
            }
            return hash.GetCurrentHashAsUInt64().ToString("X16");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static string ComputeSettingsHash(IAssetSettings? settings)
    {
        if (settings is null) return "";
        var json = JsonSerializer.SerializeToUtf8Bytes(settings);
        return XxHash64.HashToUInt64(json).ToString("X16");
    }

    public void Dispose()
    {
        _importChannel.Writer.TryComplete();
        _cts.Cancel();
        Task.WaitAll(_workers, TimeSpan.FromSeconds(5));
        _cts.Dispose();
    }
}
```

### 6.3 How this fixes the current bugs

| Current Bug | How the rewrite fixes it |
|---|---|
| `async void` FileSystemWatcher callback ([line 189](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L189)) | FSW handler is synchronous — just writes to `Channel<ImportJob>`. All async work happens in worker tasks with proper error handling. |
| Source file deleted after import ([line 224](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L224)) | Source file is never touched. Cooked data goes to `Library/Imports/`. |
| No content hash check ([TextureProcessor.cs](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/AssetHandler/TextureProcessor.cs#L172-L179)) | `ComputeFileHashAsync` hashes source content. Both content + settings hashes must match to skip import. |
| `_ignoreFileChanges` race ([line 51](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L51)) | We watch **only** the source files and `.gmeta` files; `Library/` writes don't need ignore logic because `FileSystemWatcher` is rooted at `Assets/`. |

---

## 7. Asset Loading (Runtime Path)

Loading is the hot path — this is what happens when the game/editor needs an asset at runtime.

### 7.1 Load flow

```
LoadAssetAsync(guid)
    │
    ├── Check _loadedAssets WeakReference cache (keep from current design)
    │   └── if alive → return immediately
    │
    ├── Check Library/Imports/<guid>.imported exists
    │   └── if not → return Result.Failure("Asset not imported")
    │
    ├── Resolve handler via AssetHandlerRegistry.GetByTypeId()
    │   └── O(1) lookup, no assembly scan
    │
    ├── handler.LoadAsync(importedStream, registry, token)
    │   └── reads the cooked binary format (Section 10)
    │
    └── Store in _loadedAssets with WeakReference
```

> [!IMPORTANT]
> The `WeakReference<Asset>` cache ([AssetRegistry.cs:46](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L46)) and the double-check locking pattern ([AssetRegistry.cs:414-483](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L414-L483)) are good designs — we keep them. The only change is *where* we read from (`.imported` file instead of `.gasset`).

### 7.2 Handler signature change for LoadAsync

The handler no longer needs to skip past the metadata header — the registry does that. The handler receives a stream pre-positioned at the content section:

```csharp
// Current signature (unchanged)
ValueTask<Result<Asset>> LoadAsync(Stream sourceStream, IAssetRegistry assetRegistry, CancellationToken token = default);
```

But now `sourceStream` is an `FileStream` opened on `Library/Imports/<guid>.imported`, not a `.gasset`. The cooked binary format (Section 10) is simpler — it's pure content, no metadata header.

---

## 8. FileSystemWatcher & Change Detection

### 8.1 What we watch

Only watch `Assets/` — never `Library/` (we write there, watching ourselves would cause feedback loops).

```csharp
_watcher = new FileSystemWatcher(EditorApplication.AssetsFolderPath)
{
    IncludeSubdirectories = true,
    EnableRaisingEvents = true,
    NotifyFilter = NotifyFilters.FileName 
                 | NotifyFilters.LastWrite 
                 | NotifyFilters.DirectoryName,
};
```

### 8.2 Event handling decision tree

```csharp
private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
{
    var ext = Path.GetExtension(e.FullPath);
    var relativePath = Path.GetRelativePath(_assetsRoot, e.FullPath);
    
    // Ignore .gmeta changes triggered by our own writes
    if (_ignoreMetaWrites.TryRemove(e.FullPath, out _))
        return;
    
    // Ignore temp files
    if (ext is ".tmp" or ".gtemp")
        return;
    
    // ─── A .gmeta file changed ───────────────────────────
    if (ext == ".gmeta")
    {
        if (e.ChangeType == WatcherChangeTypes.Changed)
        {
            // User edited import settings externally → re-import the source
            var sourcePath = AssetMetaIO.GetSourcePath(relativePath);
            var meta = AssetMetaIO.ReadAsync(e.FullPath).AsTask().Result; // sync read is OK here, .gmeta is tiny
            if (meta is not null)
            {
                _importCoordinator.EnqueueAsync(new ImportJob(
                    meta.Guid, sourcePath, e.FullPath, ImportReason.SettingsChanged
                )).AsTask();
            }
        }
        // .gmeta Created/Deleted are handled by the source file events below
        return;
    }

    // ─── A source file changed ───────────────────────────
    switch (e.ChangeType)
    {
        case WatcherChangeTypes.Created:
            // New source file dropped in → generate .gmeta + queue import
            HandleNewSourceFile(e.FullPath, relativePath);
            break;

        case WatcherChangeTypes.Changed:
            // Source file modified → queue re-import
            HandleModifiedSourceFile(e.FullPath, relativePath);
            break;

        case WatcherChangeTypes.Deleted:
            // Source file deleted → remove from catalog, optionally clean Library/
            HandleDeletedSourceFile(e.FullPath, relativePath);
            break;
    }
}
```

### 8.3 Auto-generating `.gmeta` for new files

When an artist drops a `hero.png` into `Assets/Textures/`:

```csharp
private void HandleNewSourceFile(string fullPath, string relativePath)
{
    var ext = Path.GetExtension(relativePath);
    var handler = _handlerRegistry.GetByExtension(ext);
    if (handler is null)
        return;  // Unknown file type — ignore
    
    var metaPath = AssetMetaIO.GetMetaPath(fullPath);
    if (File.Exists(metaPath))
        return;  // Already has .gmeta (maybe copied together)
    
    var attr = handler.GetType().GetCustomAttribute<CustomAssetHandlerAttribute>()!;
    var meta = new AssetMeta
    {
        Guid = Guid.NewGuid(),
        HandlerTypeId = new Guid(attr.ID),
        HandlerVersion = 0,
        Settings = handler.CreateDefaultSettings(),  // new method on IAssetHandler
    };
    
    // Write .gmeta (suppress FSW for this write)
    _ignoreMetaWrites[metaPath] = true;
    AssetMetaIO.WriteAsync(metaPath, meta).AsTask().Wait();
    
    // Register in catalog
    _catalog.Upsert(meta, relativePath);
    
    // Queue import
    _importCoordinator.EnqueueAsync(new ImportJob(
        meta.Guid, relativePath, metaPath, ImportReason.NewAsset
    )).AsTask();
}
```

---

## 9. Dependency Graph

### 9.1 Storage

Dependencies are stored in three places (all kept in sync):
1. **`.gmeta`** — `Dependencies: Guid[]` — source of truth, checked into git
2. **SQLite `dependencies` table** — fast queries, regenerable from `.gmeta`
3. **In-memory** — not needed anymore (SQLite is fast enough for editor use)

The current in-memory `_referencerGraph` / `_dependencyCache` dictionaries ([AssetRegistry.cs:48-49](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L48-L49)) are replaced by SQLite queries. SQLite with WAL mode handles concurrent reads efficiently.

### 9.2 Invalidation cascade

When asset A changes, all assets that depend on A may need re-import:

```csharp
public void InvalidateTransitive(Guid changedAssetGuid)
{
    var visited = new HashSet<Guid>();
    var queue = new Queue<Guid>();
    queue.Enqueue(changedAssetGuid);

    while (queue.Count > 0)
    {
        var current = queue.Dequeue();
        if (!visited.Add(current))
            continue;

        _catalog.MarkDirty(current);

        foreach (var referencer in _catalog.GetReferencers(current))
        {
            queue.Enqueue(referencer);
        }
    }

    // Re-queue all dirty assets (except the original, which is already being imported)
    foreach (var guid in visited)
    {
        if (guid != changedAssetGuid)
        {
            var sourcePath = _catalog.GetSourcePath(guid);
            if (sourcePath is not null)
            {
                var metaPath = AssetMetaIO.GetMetaPath(Path.Combine(_assetsRoot, sourcePath));
                _importCoordinator.EnqueueAsync(new ImportJob(guid, sourcePath, metaPath, ImportReason.SourceChanged)).AsTask();
            }
        }
    }
}
```

---

## 10. Cooked Binary Format

The `.imported` files in `Library/Imports/` are the cooked binary format. This is **the only place we use binary serialization** — settings are in `.gmeta` (JSON), only the processed content blob is binary.

### 10.1 File structure

We keep the `AssetMetadata` header concept ([Asset.cs:43-117](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/AssetHandler/Asset.cs#L43-L117)) but simplify it — settings and dependencies are no longer embedded:

```csharp
/// <summary>
/// Header for .imported files in Library/Imports/.
/// Simplified from the original AssetMetadata — no settings/dependencies 
/// (those live in .gmeta and SQLite).
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = SIZE)]
internal struct ImportedHeader
{
    public const int CURRENT_FORMAT_VERSION = 1;
    public const int SIZE = 64;  // enough room for future fields

    public int FormatVersion { get; set; }
    public Guid AssetGuid { get; set; }
    public Guid HandlerTypeId { get; set; }
    public int HandlerVersion { get; set; }
    public long ContentSize { get; set; }

    // Reserved padding (fills to SIZE bytes)
}
```

After the header, the handler writes whatever binary content it needs. For textures, this might be:

```
Bytes 0-63:     ImportedHeader
Bytes 64+:      ImageContentHeader (width, height, depth, channels)
                + raw pixel data (or DDS reference path)
```

### 10.2 Why keep the binary header at all?

Validation on load:
- `FormatVersion` → detect old/corrupt files, trigger re-import
- `HandlerTypeId` → resolve the correct `IAssetHandler` without touching the catalog
- `HandlerVersion` → detect when handler has been upgraded, trigger re-import

If the catalog is out of sync (e.g., user copied Library/ from another machine), the header acts as a sanity check.

---

## 11. AssetRegistry Class Redesign

### 11.1 New structure (partial class split)

```
Services/
├── AssetRegistry.cs              ← public API surface (IAssetRegistry impl)
├── AssetRegistry.Startup.cs      ← initialization, catalog sync, first-time setup
├── AssetRegistry.Watcher.cs      ← FileSystemWatcher event handling
└── AssetRegistry.Backend.cs      ← AssetCatalog class (SQLite wrapper)
```

### 11.2 Top-level class

```csharp
namespace Ghost.Editor.Core.Services;

/// <summary>
/// Central asset registry for the GhostEngine editor.
/// 
/// Owns the lifecycle of:
/// - AssetCatalog (SQLite GUID↔path mapping + dependency graph)
/// - AssetHandlerRegistry (O(1) handler lookup by extension/typeId)
/// - ImportCoordinator (background import workers + Channel-based queue)
/// - FileSystemWatcher (Assets/ directory monitoring)
/// 
/// Thread safety: see each partial file for details.
/// The public API is safe to call from any thread — the registry
/// marshals events to the UI thread via DispatcherQueue.
/// </summary>
[EditorInjection(EditorInjectionAttribute.ServiceLifetime.Singleton, 
                 ImplementationType = typeof(AssetRegistry))]
internal sealed partial class AssetRegistry : IAssetRegistry
{
    private readonly string _assetsRoot;
    private readonly AssetCatalog _catalog;
    private readonly AssetHandlerRegistry _handlerRegistry;
    private readonly ImportCoordinator _importCoordinator;
    private readonly FileSystemWatcher _watcher;

    // WeakReference cache — kept from current design, it works well
    private readonly ConcurrentDictionary<Guid, WeakReference<Asset>> _loadedAssets;
    private readonly SemaphoreSlim _loadLock;

    public event EventHandler<IAssetRegistry, AssetChangedEventArgs>? OnAssetChanged;

    // ─── IAssetRegistry implementation ─────────────────────

    public string? GetAssetPath(Guid id)
        => _catalog.GetSourcePath(id);

    public Guid GetAssetGuid(string path)
        => _catalog.GetGuid(path);

    public async ValueTask<Result<Guid>> ImportAssetAsync(
        string sourceFilePath, string targetAssetPath, CancellationToken token = default)
    {
        // 1. Copy source file to Assets/ if not already there
        // 2. Generate .gmeta with new GUID
        // 3. Upsert to catalog
        // 4. Enqueue import job
        // 5. Return the new GUID immediately (import happens in background)
    }

    public ValueTask<Result> ReimportAssetAsync(Guid assetId, string sourceFilePath, CancellationToken token = default)
    {
        // Enqueue a ManualReimport job
    }

    public async ValueTask<Result<Asset>> LoadAssetAsync(Guid id, CancellationToken token = default)
    {
        // Same double-check WeakReference pattern as current code
        // But reads from Library/Imports/<guid>.imported
    }

    public async ValueTask<Result> SaveAssetAsync(Asset asset, CancellationToken token = default)
    {
        // For engine-native assets: write to .gasset + update .gmeta
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _importCoordinator.Dispose();
        _catalog.Dispose();
        _loadLock.Dispose();
    }
}
```

### 11.3 Startup sequence (`AssetRegistry.Startup.cs`)

```csharp
internal sealed partial class AssetRegistry
{
    public AssetRegistry(string assetsRoot)
    {
        _assetsRoot = assetsRoot;

        // 1. Open or create catalog
        var dbPath = Path.Combine(EditorApplication.LibraryFolderPath, "AssetDB.sqlite");
        _catalog = new AssetCatalog(dbPath);

        // 2. Build handler registry (one-time scan)
        _handlerRegistry = new AssetHandlerRegistry();

        // 3. Sync catalog with filesystem
        SyncCatalogWithDisk();

        // 4. Start import coordinator
        _importCoordinator = new ImportCoordinator(_catalog, _handlerRegistry, _assetsRoot);
        _importCoordinator.OnAssetChanged += (_, args) => OnAssetChanged?.Invoke(this, args);

        // 5. Queue pending imports
        _importCoordinator.EnqueueDirtyAssetsAsync().AsTask().Wait();

        // 6. Start watching for changes
        _loadedAssets = new ConcurrentDictionary<Guid, WeakReference<Asset>>();
        _loadLock = new SemaphoreSlim(1, 1);
        SetupWatcher();
    }

    /// <summary>
    /// Walk Assets/ directory, diff against SQLite catalog, and reconcile.
    /// 
    /// For each .gmeta file found on disk:
    ///   - If not in catalog → insert (new asset)
    ///   - If in catalog with different path → update (file was moved)
    ///   
    /// For each entry in catalog:
    ///   - If .gmeta no longer on disk → delete (asset was removed externally)
    ///   
    /// This is O(N) where N = number of assets. Runs once at startup.
    /// </summary>
    private void SyncCatalogWithDisk()
    {
        var onDisk = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Scan all .gmeta files
        foreach (var metaFile in Directory.EnumerateFiles(_assetsRoot, "*.gmeta", SearchOption.AllDirectories))
        {
            var metaRelative = Path.GetRelativePath(_assetsRoot, metaFile);
            var sourceRelative = AssetMetaIO.GetSourcePath(metaRelative);
            onDisk.Add(sourceRelative);

            var meta = AssetMetaIO.ReadAsync(metaFile).AsTask().Result;
            if (meta is null)
                continue;

            var existingPath = _catalog.GetSourcePath(meta.Guid);
            if (existingPath is null)
            {
                // New asset — insert
                _catalog.Upsert(meta, sourceRelative);
            }
            else if (!string.Equals(existingPath, sourceRelative, StringComparison.OrdinalIgnoreCase))
            {
                // Asset was moved/renamed — update path
                _catalog.Upsert(meta, sourceRelative);
            }
        }

        // 2. Remove catalog entries whose .gmeta no longer exists
        foreach (var (guid, catalogPath) in _catalog.EnumerateAll())
        {
            if (!onDisk.Contains(catalogPath))
            {
                _catalog.Remove(guid);
            }
        }
    }
}
```

> [!IMPORTANT]
> **Startup is now O(N) filesystem enumeration + O(N) SQLite upserts**, which is much faster than the current approach of opening every `.gasset` file and reading 20 bytes from each ([AssetRegistry.cs:99-134](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L99-L134)). For a project with 10,000 assets, this goes from 10,000 file opens → ~10,000 tiny `.gmeta` reads (or even better — just stat + compare timestamps).

---

## 12. Handler API Redesign

### 12.1 Updated interfaces

```csharp
namespace Ghost.Editor.Core.AssetHandler;

public interface IAssetHandler
{
    /// <summary>
    /// Load a cooked asset from a Library/Imports/ stream.
    /// The stream is positioned at the start of handler-specific content
    /// (past the ImportedHeader).
    /// </summary>
    ValueTask<Result<Asset>> LoadAsync(Stream importedStream, IAssetRegistry registry, CancellationToken token = default);

    /// <summary>
    /// Save an engine-native asset (materials, prefabs, etc).
    /// For imported assets (textures, meshes), this is typically not called.
    /// </summary>
    ValueTask<Result> SaveAsync(Asset asset, Stream targetStream, IAssetRegistry registry, CancellationToken token = default);

    /// <summary>
    /// Create default import settings for a new asset of this type.
    /// Called when auto-generating .gmeta for a newly dropped file.
    /// Return null if this handler has no configurable settings.
    /// </summary>
    IAssetSettings? CreateDefaultSettings() => null;
}

public interface IImportableAssetHandler : IAssetHandler
{
    /// <summary>
    /// Import a source file into a cooked binary format.
    /// 
    /// sourceStream: the raw source file (PNG, FBX, etc.)
    /// targetStream: where to write the cooked binary (Library/Imports/<guid>.imported)
    /// id: the asset GUID
    /// settings: import settings from .gmeta (previously deserialized from JSON)
    /// </summary>
    ValueTask<Result> ImportAsync(Stream sourceStream, Stream targetStream, Guid id, 
                                   IAssetSettings? settings, CancellationToken token = default);

    ValueTask<Result> ExportAsync(Stream importedStream, Stream targetStream, 
                                   IAssetExportOptions? options, CancellationToken token = default);
}
```

Note the key change: `ImportAsync` now receives `IAssetSettings? settings` as a parameter. The handler no longer reads/writes settings from the binary stream — settings live in `.gmeta`.

### 12.2 TextureAssetHandler changes

```csharp
[CustomAssetHandler(ID = TextureAsset._TYPE_ID, 
                    SupportedExtensions = [".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr"])]
internal class TextureAssetHandler : IImportableAssetHandler
{
    private const int _CURRENT_VERSION = 1;

    public IAssetSettings? CreateDefaultSettings() => new TextureAssetSettings();

    public async ValueTask<Result> ImportAsync(
        Stream sourceStream, Stream targetStream, Guid id, 
        IAssetSettings? settings, CancellationToken token = default)
    {
        var texSettings = settings as TextureAssetSettings ?? new TextureAssetSettings();

        using var image = new MagickImage(sourceStream);
        var bytes = image.ToByteArray();

        // Kick off NVTT compression in background (this part stays the same)
        await TextureProcessor.CompressToCacheAsync(
            EditorApplication.CachesFolderPath, id, bytes, 
            image.Width, image.Height, image.Depth, texSettings, token);

        // Write ImportedHeader
        var header = new ImportedHeader
        {
            FormatVersion = ImportedHeader.CURRENT_FORMAT_VERSION,
            AssetGuid = id,
            HandlerTypeId = TextureAsset.s_typeGuid,
            HandlerVersion = _CURRENT_VERSION,
        };

        // Write content (ImageContentHeader + raw pixels)
        var contentHeader = new ImageContentHeader { ... };
        
        // ... (similar to current code but without settings in the binary)
    }

    public async ValueTask<Result<Asset>> LoadAsync(
        Stream importedStream, IAssetRegistry registry, CancellationToken token = default)
    {
        // Read ImportedHeader (validation)
        var header = ImportedHeader.ReadFromStream(importedStream);

        // Read image content
        var contentHeader = /* read ImageContentHeader */;
        
        // Create GPU texture handle
        // ...

        return new TextureAsset(header.AssetGuid, [], null, texture);
    }
}
```

### 12.3 TextureAssetSettings no longer needs struct layout

Since settings are now serialized as JSON (not raw bytes), we can make `TextureAssetSettings` a normal class with proper properties:

```csharp
// Before (fragile — binary struct layout)
public struct BasicSettings()
{
    public TextureType TextureType { get; set; } = TextureType.Default;
    // ... adding a field here breaks all existing .gasset files
}

// After (resilient — JSON serialization)
public sealed class BasicSettings
{
    public TextureType TextureType { get; set; } = TextureType.Default;
    // ... adding a field is safe — old .gmeta files without it get the default
}
```

The `TextureProcessor` still needs `Unsafe.SizeOf` for hashing — but that's only for the cache key hash, not for persistence. We can replace that hash computation with JSON-based hashing (or keep the struct hash as an internal optimization detail).

---

## 13. Migration Path from Current Code

If you have existing `.gasset` files, we need a migration tool. This runs once:

```csharp
internal static class AssetMigration
{
    /// <summary>
    /// Read old .gasset files and produce .gmeta + Library/Imports/ equivalents.
    /// </summary>
    public static async Task MigrateFromGassetAsync(string assetsRoot, AssetCatalog catalog)
    {
        foreach (var gassetPath in Directory.EnumerateFiles(assetsRoot, "*.gasset", SearchOption.AllDirectories))
        {
            await using var fs = new FileStream(gassetPath, FileMode.Open, FileAccess.Read);
            var oldMeta = AssetMetadata.ReadFromStream(fs);

            // Create .gmeta
            var meta = new AssetMeta
            {
                Guid = oldMeta.ID,
                HandlerTypeId = oldMeta.TypeID,
                HandlerVersion = oldMeta.HandlerVersion,
                Dependencies = ReadDependencies(fs, oldMeta),
                Settings = ReadSettings(fs, oldMeta),  // deserialize from binary
            };

            var metaPath = gassetPath + ".gmeta";
            await AssetMetaIO.WriteAsync(metaPath, meta);

            // Copy content blob to Library/Imports/
            var importedPath = Path.Combine(
                EditorApplication.LibraryFolderPath, "Imports", $"{oldMeta.ID:N}.imported");
            // ... extract content section from .gasset and write to .imported
            
            // Register in catalog
            var relativePath = Path.GetRelativePath(assetsRoot, gassetPath);
            catalog.Upsert(meta, relativePath);
        }
    }
}
```

---

## 14. Thread Safety Model

| Component | Thread Model | Mechanism |
|---|---|---|
| `AssetCatalog` (SQLite) | Single writer, multiple readers | `Lock` on writes; SQLite WAL handles concurrent reads |
| `AssetHandlerRegistry` | Read-only after construction | Immutable dictionaries, no sync needed |
| `ImportCoordinator` | Multiple worker tasks read from `Channel<T>` | Channel provides thread-safe MPSC semantics |
| `_loadedAssets` cache | Concurrent reads, rare writes | `ConcurrentDictionary` + `SemaphoreSlim` for double-check (same as current) |
| `FileSystemWatcher` callbacks | Runs on thread pool threads | Non-blocking — just writes to channel |
| `OnAssetChanged` events | Marshalled to UI thread | `DispatcherQueue.TryEnqueue` |
| `.gmeta` file I/O | One writer at a time per file | Atomic write via temp-file + rename |

---

## 15. Error Handling Strategy

Following GhostEngine conventions ([AGENTS.md](file:///f:/csharp/GhostEngine/AGENTS.md)):

| Situation | Approach |
|---|---|
| Source file not found | `Result.Failure(Error.NotFound)` |
| No handler for extension | `Result.Failure("No handler for .xyz")` |
| Import fails (NVTT crash, corrupt file) | `Logger.Error(...)` + `catalog.MarkFailed(guid, msg)` — asset shows red in editor browser |
| `.gmeta` parse error | `Logger.Warning(...)` + regenerate with defaults |
| SQLite error | Throw — programming error, should not happen |
| `.imported` file corrupt/missing | Mark dirty in catalog → re-import on next startup |

---

## 16. Open Decisions & Trade-offs

### 16.1 What to do with engine-native assets (materials, scenes)?

Engine-native assets like materials don't have a "source file" separate from the engine format. Options:

- **Option A**: The `.gasset` file IS the source. `.gmeta` sits beside it. No `.imported` file needed.
- **Option B**: Store as JSON (like `.gmeta` but with content), drop binary entirely for these types.

I lean toward **Option A** — keep `.gasset` for engine-native assets only, use `.gmeta` + `.imported` for imported assets.

### 16.2 Should `ImportAssetAsync` return immediately or wait for import?

Current design returns after import completes. New design enqueues and returns GUID immediately. Which do you prefer?

We could offer both:
```csharp
// Fire-and-forget (for drag-and-drop, batch imports)
ValueTask<Result<Guid>> ImportAssetAsync(...);

// Wait for completion (for scripted imports, tests)
ValueTask<Result<Guid>> ImportAssetAndWaitAsync(...);
```

### 16.3 Hot-reload after re-import

When an asset is re-imported, should live `Asset` objects auto-refresh? Your current code already has `Asset.RefreshAsync` ([Asset.cs:36-39](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/AssetHandler/Asset.cs#L36-L39)) and the `WeakReference` lookup in `ReimportAssetAsync` ([AssetRegistry.cs:406-409](file:///f:/csharp/GhostEngine/src/Editor/Ghost.Editor.Core/Services/AssetRegistry.cs#L406-L409)). We should keep this behavior — after import completes, check `_loadedAssets` for a live reference and call `RefreshAsync`.

### 16.4 Library/ on first clone

When someone clones the repo, `Library/` doesn't exist. Options:
1. **Full re-import on first open** — slowest but simplest
2. **Pre-built Library in git-lfs** — fast startup but large repo
3. **Asset server / CDN** — best UX but complex to build

I suggest starting with **(1)** — it's correct by construction. Optimize later with (3) if import times become painful.

---

## File Summary: What Gets Created / Modified / Deleted

### New Files

| File | Purpose |
|---|---|
| `AssetHandler/AssetMeta.cs` | `AssetMeta` data model + `AssetMetaIO` read/write |
| `AssetHandler/AssetHandlerRegistry.cs` | One-time handler scan, O(1) lookups |
| `Services/AssetCatalog.cs` | SQLite wrapper (replaces `AssetRegistry.Backend.cs` placeholder) |
| `Services/ImportCoordinator.cs` | Channel-based import queue + worker pool |

### Modified Files

| File | Changes |
|---|---|
| `Services/AssetRegistry.cs` | Rewritten — delegates to AssetCatalog + ImportCoordinator |
| `Services/AssetRegistry.Backend.cs` | Deleted or merged into `AssetCatalog.cs` |
| `AssetHandler/Asset.cs` | `AssetMetadata` simplified → `ImportedHeader` (no settings/deps in binary) |
| `AssetHandler/AssetHandler.cs` | `IImportableAssetHandler.ImportAsync` gains `IAssetSettings?` param |
| `AssetHandler/TextureAsset.cs` | Handler updated for new API; settings → class instead of struct |
| `Contracts/IAssetRegistry.cs` | Minor — add `IAssetSettings? GetSettings(Guid)` if needed by inspector |
| `EditorApplication.cs` | Add `LIBRARY_FOLDER_NAME` and `LibraryFolderPath` |
| `Attributes.cs` | Unify `AssetImporterAttribute` with `CustomAssetHandlerAttribute` |

### Deleted Files

| File | Reason |
|---|---|
| `Utilities/AssetHandlerUtility.cs` | Superseded by handler API changes |
