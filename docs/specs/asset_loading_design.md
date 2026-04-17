# Asset Loading & Runtime Resolution Architecture

## Design Principles

1. **Editor handlers are translators** — they convert diverse source formats into a small set of uniform cooked binary representations. They never ship.
2. **Runtime loaders are consumers** — they read cooked binary data into runtime objects. They're tiny, AOT-safe, and explicitly registered.
3. **The `.imported` file is the contract** — it's the boundary between editor and runtime. Both sides agree on the binary format; neither needs to know about the other.

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────────────────────┐
│                                Ghost.Core (runtime)                                  │
│                                                                                      │
│  AssetRef<T>           — Type-safe GUID reference (like TSoftObjectPtr)              │
│  RuntimeAsset          — Base class for all runtime asset objects                    │
│  IContentProvider      — Abstract: "give me bytes for this GUID"                     │
│  IRuntimeAssetLoader   — Abstract: "decode cooked bytes into a RuntimeAsset"         │
│  AssetState            — Unloaded / Loading / Loaded / Ready / Failed                │
└───────────────────────────────────────────────────────────┬──────────────────────────┘
                                                            │
                ┌───────────────────────────────────────────┼───────────────────────── ┐
                │                                           │                          │
  ┌─────────────▼──────────────┐              ┌─────────────▼──────────────┐           │
  │  Ghost.Editor.Core         │              │  Ghost.Engine              │           │
  │  (editor only — never      │              │  (ships in builds)         │           │
  │   ships in builds)         │              │                            │           │
  │                            │              │  RuntimeLoaderRegistry     │           │
  │  IImportableAssetHandler   │              │  AssetManager              │           │
  │  AssetHandlerRegistry      │              │  EditorContentProvider     │           │
  │  ImportCoordinator         │              │  PackedContentProvider     │           │
  │  AssetCatalog (SQLite)     │              │                            │           │
  │  FileSystemWatcher         │              │  CookedTextureLoader       │           │
  │                            │              │  CookedMeshLoader          │           │
  │  TextureAssetHandler       │              │  CookedMaterialLoader      │           │
  │  MaterialHandler           │              │  CookedShaderLoader        │           │
  │  MaterialXHandler          │              │  CookedAudioLoader         │           │
  │  FBXHandler                │              └────────────────────────────┘           │
  │  ShaderGraphHandler        │                                                       │
  └────────────────────────────┘                                                       │
                │                                                                      │
                │    ImportAsync() writes                                              │
                │    cooked binary data                                                │
                │           │                                                          │
                │           ▼                                                          │
                │    Library/Imports/<guid>.imported  ─────────────────────────────────│
                │    (cooked binary = the contract between editor and runtime)         │
                └──────────────────────────────────────────────────────────────────────┘
```

### The Funnel: Many Source Formats → Few Cooked Formats

```
Editor handlers (translators):           Cooked format:              Runtime loaders (consumers):

  .png ─┐                                                            
  .jpg  ├─→ TextureAssetHandler ──→   CookedTexture            ←── CookedTextureLoader
  .tga  ┤                             (header + DDS blob)           (reads header + DDS)
  .hdr  ┘                                                            
                                                                      
  .gmat ───→ MaterialHandler ────→    CookedMaterial            ←── CookedMaterialLoader
  .mtlx ──→ MaterialXHandler ──→     (property buffer +             (reads props + refs)
                                       AssetRef[] deps)              
                                                                      
  .fbx ────→ FBXHandler ────────→     CookedMesh                ←── CookedMeshLoader
  .gltf ──→ GLTFHandler ───────→     (vertices + indices +          (reads vertex/index data)
                                       meshlets + bounding box)      
                                                                      
  .ghostshader → ShaderHandler ─→     CookedShader              ←── CookedShaderLoader
                                       (compiled DXIL bytecode)      (reads bytecode)
```

> [!IMPORTANT]
> The runtime never needs to know that a texture came from a `.png` vs `.tga`, or that a material 
> was authored as MaterialX vs a JSON `.gmat`. It only sees the cooked output. **Adding a new source 
> format (e.g., OpenEXR) only requires a new editor handler — the runtime is untouched.**

---

## Layer 1: Core Types (`Ghost.Core`)

### `AssetRef<T>` — Type-safe GUID reference

Replaces raw `Guid` fields everywhere. This is your equivalent of Unreal's `TSoftObjectPtr` / Unity's `AssetReference`.

```csharp
namespace Ghost.Core;

/// <summary>
/// A serializable, type-safe reference to an asset.
/// The GUID is resolved at load time by the AssetManager.
/// This is data only — it doesn't know how to load anything.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct AssetRef<T> : IEquatable<AssetRef<T>>
{
    public readonly Guid Guid;

    public AssetRef(Guid guid) => Guid = guid;

    public bool IsValid => Guid != Guid.Empty;
    public static AssetRef<T> Null => default;

    public bool Equals(AssetRef<T> other) => Guid == other.Guid;
    public override int GetHashCode() => Guid.GetHashCode();
    public override bool Equals(object? obj) => obj is AssetRef<T> r && Equals(r);
    public override string ToString() => $"AssetRef<{typeof(T).Name}>({Guid:N})";

    public static bool operator ==(AssetRef<T> a, AssetRef<T> b) => a.Equals(b);
    public static bool operator !=(AssetRef<T> a, AssetRef<T> b) => !a.Equals(b);
}
```

Usage in serialized data:

```csharp
// In a material's serialized properties — just GUIDs, not loaded objects
public struct MaterialProperties
{
    public AssetRef<TextureAsset> AlbedoMap;
    public AssetRef<TextureAsset> NormalMap;
    public AssetRef<TextureAsset> MetallicMap;
    // ...
}
```

### `RuntimeAsset` — Base class

```csharp
namespace Ghost.Core;

/// <summary>
/// Base class for all runtime asset objects. 
/// Produced by IRuntimeAssetLoader from cooked binary data.
/// </summary>
public abstract class RuntimeAsset
{
    public Guid ID { get; }

    /// <summary>
    /// The cooked format type ID. Used to find the right loader.
    /// Each RuntimeAsset subclass has exactly one ID.
    /// </summary>
    public abstract Guid TypeID { get; }

    protected RuntimeAsset(Guid id) => ID = id;
}
```

### `IContentProvider` — Where bytes come from

```csharp
namespace Ghost.Core;

/// <summary>
/// Abstracts the storage backend for cooked asset data.
/// Editor: reads from Library/Imports/ (loose .imported files)
/// Runtime: reads from .gpak (packed archive with manifest)
/// </summary>
public interface IContentProvider
{
    /// <summary>
    /// Returns true if cooked data exists for this asset.
    /// </summary>
    bool HasAsset(Guid guid);

    /// <summary>
    /// Open a read stream positioned at the start of the cooked payload.
    /// Caller disposes the stream.
    /// </summary>
    ValueTask<Result<Stream>> OpenReadAsync(Guid guid, CancellationToken token = default);

    /// <summary>
    /// Read the dependency list for an asset.
    /// Editor: reads from SQLite catalog (mirrors .gmeta).
    /// Runtime: reads from pack manifest.
    /// </summary>
    Guid[] GetDependencies(Guid guid);

    /// <summary>
    /// Get the cooked type ID for an asset. 
    /// This determines which IRuntimeAssetLoader to use.
    /// </summary>
    Guid GetCookedTypeId(Guid guid);
}
```

### `IRuntimeAssetLoader` — The decoder interface

```csharp
namespace Ghost.Core;

/// <summary>
/// Reads a cooked asset blob and produces a RuntimeAsset.
/// Each cooked format type has exactly one loader implementation.
/// Implementations live in Ghost.Engine, are tiny, and have zero editor dependencies.
/// </summary>
public interface IRuntimeAssetLoader
{
    /// <summary>
    /// The cooked format type ID this loader handles.
    /// Must match the TypeID of the RuntimeAsset subclass it produces.
    /// </summary>
    Guid CookedTypeId { get; }

    /// <summary>
    /// Read cooked binary data → RuntimeAsset.
    /// Stream is positioned at the start of the cooked payload.
    /// </summary>
    ValueTask<Result<RuntimeAsset>> LoadAsync(
        Stream cookedData, Guid assetId, CancellationToken token = default);
}
```

### `AssetState` — Lifecycle tracking

```csharp
namespace Ghost.Core;

public enum AssetState : byte
{
    /// <summary>Not in memory.</summary>
    Unloaded = 0,

    /// <summary>IO in progress (background thread).</summary>
    Loading = 1,

    /// <summary>CPU data ready, GPU resources not yet created.</summary>
    Loaded = 2,

    /// <summary>GPU upload complete, fully usable for rendering.</summary>
    Ready = 3,

    /// <summary>Load or upload failed. Check error message.</summary>
    Failed = 4,
}
```

---

## Layer 2: Editor Handlers (`Ghost.Editor.Core`) — Translators

These are unchanged from your current implementation. They're the cook step.

### `IImportableAssetHandler` — The translator interface

```csharp
namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// Editor-only. Translates a source file into cooked binary format.
/// The cooked output is written to Library/Imports/<guid>.imported.
/// 
/// Each handler understands ONE source format family and produces 
/// ONE cooked format that a runtime loader can consume.
/// </summary>
public interface IImportableAssetHandler
{
    /// <summary>Create default import settings for this source format.</summary>
    IAssetSettings? CreateDefaultSettings();

    /// <summary>
    /// Translate source → cooked binary.
    /// sourceStream: raw source file (.png, .fbx, .gmat, etc.)
    /// targetStream: cooked output (.imported file)
    /// settings: from .gmeta sidecar (JSON-serialized)
    /// </summary>
    ValueTask<Result> ImportAsync(
        Stream sourceStream, Stream targetStream, Guid id,
        IAssetSettings? settings, CancellationToken token = default);
}

/// <summary>
/// Editor-only. Handles saving modified assets back to source format.
/// Not needed at runtime — the runtime only reads cooked data.
/// </summary>
public interface IAssetHandler
{
    /// <summary>
    /// Save a modified asset back to its source file format.
    /// Used by the editor when the user edits properties in the inspector.
    /// </summary>
    ValueTask<Result> SaveAsync(
        RuntimeAsset asset, Stream targetStream, CancellationToken token = default);
}
```

### `AssetHandlerRegistry` — Editor-only discovery

Uses `TypeCache` (reflection/assembly scan). Only exists in the editor.

```csharp
namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// Editor-only. One-time scan at startup → O(1) lookups.
/// Maps file extensions → import handlers.
/// Uses TypeCache (reflection), NOT AOT-safe — but that's fine,
/// because this never ships in builds.
/// </summary>
internal sealed class AssetHandlerRegistry
{
    private readonly Dictionary<string, IImportableAssetHandler> _byExtension;
    private readonly Dictionary<Guid, IImportableAssetHandler> _byTypeId;
    private readonly Dictionary<Guid, int> _versionByTypeId;

    public AssetHandlerRegistry()
    {
        // Scan assemblies via TypeCache — editor-only, reflection-based
        foreach (var typeInfo in TypeCache.GetTypes())
        {
            // ... existing scan logic unchanged ...
        }
    }

    public IImportableAssetHandler? GetByExtension(string ext) { /* ... */ }
    public IImportableAssetHandler? GetByTypeId(Guid typeId) { /* ... */ }
    public int GetVersionByTypeId(Guid typeId) { /* ... */ }
}
```

### Example: `TextureAssetHandler` (editor translator)

```csharp
namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// Editor-only translator.
/// Reads: .png, .jpg, .tga, .hdr (via ImageMagick)
/// Writes: ImageContentHeader + NVTT-compressed DDS data
/// The runtime CookedTextureLoader reads this output.
/// </summary>
[CustomAssetHandler(ID = TextureAsset._TYPE_ID, 
    SupportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".hdr" })]
internal class TextureAssetHandler : IImportableAssetHandler
{
    public IAssetSettings? CreateDefaultSettings() => new TextureAssetSettings();

    public async ValueTask<Result> ImportAsync(
        Stream sourceStream, Stream targetStream, Guid id, 
        IAssetSettings? settings, CancellationToken token = default)
    {
        var textureSettings = settings as TextureAssetSettings ?? new TextureAssetSettings();

        // 1. Decode source format (ImageMagick — editor-only dependency)
        using var image = new MagickImage(sourceStream);
        var bytes = image.ToByteArray();

        // 2. Write cooked header
        var header = new ImageContentHeader
        {
            width = image.Width,
            height = image.Height,
            depth = image.Depth,
            colorComponents = image.ChannelCount,
        };
        targetStream.Write(MemoryMarshal.AsBytes(new Span<ImageContentHeader>(ref header)));

        // 3. Write NVTT-compressed DDS data (editor-only dependency)
        await TextureProcessor.CompressToStreamAsync(
            targetStream, id, bytes, image.Width, image.Height, 
            image.Depth, textureSettings, token);

        return Result.Success();
    }
}
```

> [!NOTE]
> Notice the handler pulls in `ImageMagick` and `TextureProcessor` (NVTT) — these are heavy 
> editor-only dependencies. The runtime never touches them.

---

## Layer 3: Runtime Loaders (`Ghost.Engine`) — Consumers

### `RuntimeLoaderRegistry` — Explicit, AOT-safe registration

```csharp
namespace Ghost.Engine;

/// <summary>
/// Maps cooked format type IDs to their runtime loaders.
/// Registered explicitly at startup — no reflection, no assembly scanning.
/// Fully AOT-compatible and trimmable.
/// </summary>
public sealed class RuntimeLoaderRegistry
{
    private readonly Dictionary<Guid, IRuntimeAssetLoader> _loaders = new();

    /// <summary>
    /// Register a runtime loader for a cooked format type.
    /// Call this at engine startup before any assets are loaded.
    /// </summary>
    public void Register(IRuntimeAssetLoader loader)
    {
        _loaders[loader.CookedTypeId] = loader;
    }

    /// <summary>
    /// Get the loader for a given cooked type ID.
    /// Returns null if no loader is registered (unknown asset type).
    /// </summary>
    public IRuntimeAssetLoader? GetLoader(Guid cookedTypeId)
    {
        _loaders.TryGetValue(cookedTypeId, out var loader);
        return loader;
    }
}
```

Startup registration — explicit, no reflection:

```csharp
// In Ghost.Engine startup (EngineCore.Initialize or similar)
var loaders = new RuntimeLoaderRegistry();
loaders.Register(new CookedTextureLoader());
loaders.Register(new CookedMeshLoader());
loaders.Register(new CookedMaterialLoader());
loaders.Register(new CookedShaderLoader());
// That's it. ~5 lines for all asset types the runtime will ever need.
```

### `CookedTextureLoader` — Runtime texture reader

```csharp
namespace Ghost.Engine.Assets;

/// <summary>
/// Reads cooked texture data written by TextureAssetHandler.
/// Format: ImageContentHeader (64 bytes) + DDS blob (rest of stream).
/// Produces a TextureAsset with CPU-side data ready for GPU upload.
/// </summary>
internal sealed class CookedTextureLoader : IRuntimeAssetLoader
{
    public Guid CookedTypeId => TextureAsset.s_typeGuid;

    public async ValueTask<Result<RuntimeAsset>> LoadAsync(
        Stream cookedData, Guid assetId, CancellationToken token = default)
    {
        try
        {
            // Read the fixed-size header
            var header = new ImageContentHeader();
            cookedData.ReadExactly(
                MemoryMarshal.AsBytes(new Span<ImageContentHeader>(ref header)));

            // Read remaining DDS data
            var dataSize = (int)(cookedData.Length - cookedData.Position);
            var imageData = new byte[dataSize];
            await cookedData.ReadExactlyAsync(imageData, token).ConfigureAwait(false);

            return new TextureAsset(imageData, header, assetId);
        }
        catch (Exception ex)
        {
            return Result.Failure<RuntimeAsset>($"Failed to load cooked texture: {ex.Message}");
        }
    }
}
```

### `CookedMeshLoader` — Runtime mesh reader

```csharp
namespace Ghost.Engine.Assets;

internal sealed class CookedMeshLoader : IRuntimeAssetLoader
{
    public Guid CookedTypeId => MeshAsset.s_typeGuid;

    public async ValueTask<Result<RuntimeAsset>> LoadAsync(
        Stream cookedData, Guid assetId, CancellationToken token = default)
    {
        // Read: vertex count (uint) + index count (uint)
        //       + raw Vertex[] data + raw uint[] indices
        //       + bounding box + meshlet data
        // Exact layout matches what FBXHandler/GLTFHandler ImportAsync writes.
        // ...
    }
}
```

### `CookedMaterialLoader` — Runtime material reader

```csharp
namespace Ghost.Engine.Assets;

internal sealed class CookedMaterialLoader : IRuntimeAssetLoader
{
    public Guid CookedTypeId => MaterialAsset.s_typeGuid;

    public async ValueTask<Result<RuntimeAsset>> LoadAsync(
        Stream cookedData, Guid assetId, CancellationToken token = default)
    {
        // Read: shader reference (GUID)
        //       + property buffer (raw bytes matching shader's property layout)
        //       + texture slot bindings (array of AssetRef<TextureAsset>)
        //
        // Note: the material doesn't care if it was authored as .gmat, 
        // MaterialX, or Shader Graph. All of those editor formats cook 
        // down to the same binary layout.
        // ...
    }
}
```

---

## Layer 4: `AssetManager` — The Heart (`Ghost.Engine`)

Central runtime service. Manages the full lifecycle: resolve → load → upload.

```csharp
namespace Ghost.Engine;

/// <summary>
/// Central asset lifecycle manager. Works identically in editor and shipped builds.
/// The only difference is which IContentProvider is injected.
///
/// Responsibilities:
/// - Resolve AssetRef<T> → loaded, GPU-ready RuntimeAsset
/// - Load dependencies before dependents (materials wait for textures)
/// - Schedule GPU uploads per-frame (bounded, non-blocking)
/// - Reference counting for unloading (future)
/// </summary>
public sealed class AssetManager : IDisposable
{
    private readonly IContentProvider _contentProvider;
    private readonly RuntimeLoaderRegistry _loaders;

    // ── Per-asset tracking ──
    private readonly Dictionary<Guid, AssetEntry> _entries = new();
    private readonly Lock _lock = new();

    // ── Upload queue — drained once per frame by render thread ──
    private readonly Queue<Guid> _pendingUploads = new();

    private struct AssetEntry
    {
        public RuntimeAsset? Asset;
        public AssetState State;
        public int RefCount;
        public Task? LoadTask;
        public string? Error;
    }

    public AssetManager(IContentProvider contentProvider, RuntimeLoaderRegistry loaders)
    {
        _contentProvider = contentProvider;
        _loaders = loaders;
    }

    // ────────────────────────────────────────────────────────────
    //  Public API
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Non-blocking resolve. Returns the asset if Ready, null otherwise.
    /// If the asset hasn't been requested yet, kicks off async loading.
    /// Callers should use fallback resources when this returns null.
    /// </summary>
    public T? Resolve<T>(AssetRef<T> assetRef) where T : RuntimeAsset
    {
        if (!assetRef.IsValid) return null;

        lock (_lock)
        {
            if (_entries.TryGetValue(assetRef.Guid, out var entry))
            {
                return entry.State == AssetState.Ready ? entry.Asset as T : null;
            }

            // First request — start loading
            BeginLoad(assetRef.Guid);
            return null;
        }
    }

    /// <summary>
    /// Blocking load. Returns when the asset reaches at least Loaded state.
    /// GPU upload still happens via ProcessUploads on the render thread.
    /// Use for loading screens or synchronous initialization.
    /// </summary>
    public async ValueTask<T?> LoadAsync<T>(
        AssetRef<T> assetRef, CancellationToken token = default) where T : RuntimeAsset
    {
        if (!assetRef.IsValid) return null;

        Task? loadTask;
        lock (_lock)
        {
            if (!_entries.TryGetValue(assetRef.Guid, out var entry))
            {
                BeginLoad(assetRef.Guid);
                entry = _entries[assetRef.Guid];
            }

            if (entry.State >= AssetState.Loaded)
                return entry.Asset as T;

            loadTask = entry.LoadTask;
        }

        if (loadTask is not null)
            await loadTask.WaitAsync(token).ConfigureAwait(false);

        lock (_lock)
        {
            return _entries.TryGetValue(assetRef.Guid, out var e) ? e.Asset as T : null;
        }
    }

    /// <summary>
    /// Query the current state of an asset.
    /// </summary>
    public AssetState GetState(Guid guid)
    {
        lock (_lock)
        {
            return _entries.TryGetValue(guid, out var e) ? e.State : AssetState.Unloaded;
        }
    }

    // ────────────────────────────────────────────────────────────
    //  GPU Upload (called from render thread)
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Process queued GPU uploads. Must be called once per frame from 
    /// within a RenderContext scope (render thread, active command buffer).
    /// 
    /// maxUploadsPerFrame bounds GPU work per frame to avoid stalls.
    /// </summary>
    public void ProcessUploads(RenderContext ctx, int maxUploadsPerFrame = 4)
    {
        for (int i = 0; i < maxUploadsPerFrame; i++)
        {
            Guid guid;
            lock (_lock)
            {
                if (!_pendingUploads.TryDequeue(out guid))
                    break;
            }

            lock (_lock)
            {
                if (!_entries.TryGetValue(guid, out var entry) || entry.Asset is null)
                    continue;

                // Dispatch upload based on runtime asset type
                var success = entry.Asset switch
                {
                    TextureAsset tex => UploadTexture(tex, ctx),
                    // MeshAsset mesh => UploadMesh(mesh, ctx),
                    // MaterialAsset mat => ResolveMaterialBindings(mat, ctx),
                    _ => true, // No GPU upload needed for this type
                };

                if (success)
                {
                    entry.State = AssetState.Ready;
                    _entries[guid] = entry;
                }
                else
                {
                    // Re-queue for next frame (e.g., GPU memory pressure)
                    _pendingUploads.Enqueue(guid);
                }
            }
        }
    }

    // ────────────────────────────────────────────────────────────
    //  Internal: Load pipeline
    // ────────────────────────────────────────────────────────────

    private void BeginLoad(Guid guid)
    {
        // Must be called under _lock
        var entry = new AssetEntry
        {
            State = AssetState.Loading,
            RefCount = 1,
        };

        entry.LoadTask = Task.Run(async () => await ExecuteLoadAsync(guid));
        _entries[guid] = entry;
    }

    private async Task ExecuteLoadAsync(Guid guid)
    {
        try
        {
            // ── Step 1: Load dependencies first (recursive, depth-first) ──
            var deps = _contentProvider.GetDependencies(guid);
            foreach (var dep in deps)
            {
                // Ensure each dependency is loaded before we proceed
                Task? depTask = null;
                lock (_lock)
                {
                    if (!_entries.TryGetValue(dep, out var depEntry))
                    {
                        BeginLoad(dep);
                        depEntry = _entries[dep];
                    }
                    depTask = depEntry.LoadTask;
                }

                if (depTask is not null)
                    await depTask.ConfigureAwait(false);
            }

            // ── Step 2: Find the right runtime loader ──
            var cookedTypeId = _contentProvider.GetCookedTypeId(guid);
            var loader = _loaders.GetLoader(cookedTypeId);
            if (loader is null)
            {
                SetFailed(guid, $"No runtime loader for cooked type {cookedTypeId:N}");
                return;
            }

            // ── Step 3: Read cooked data via content provider ──
            var streamResult = await _contentProvider.OpenReadAsync(guid);
            if (streamResult.IsFailure)
            {
                SetFailed(guid, streamResult.Message ?? "Failed to open asset");
                return;
            }

            // ── Step 4: Decode cooked binary → RuntimeAsset ──
            RuntimeAsset asset;
            await using (var stream = streamResult.Value)
            {
                var loadResult = await loader.LoadAsync(stream, guid);
                if (loadResult.IsFailure)
                {
                    SetFailed(guid, loadResult.Message ?? "Loader failed");
                    return;
                }
                asset = loadResult.Value;
            }

            // ── Step 5: Mark loaded + queue for GPU upload ──
            lock (_lock)
            {
                var entry = _entries[guid];
                entry.Asset = asset;
                entry.State = AssetState.Loaded;
                _entries[guid] = entry;

                _pendingUploads.Enqueue(guid);
            }
        }
        catch (Exception ex)
        {
            SetFailed(guid, ex.Message);
        }
    }

    private void SetFailed(Guid guid, string error)
    {
        lock (_lock)
        {
            var entry = _entries.GetValueOrDefault(guid);
            entry.State = AssetState.Failed;
            entry.Error = error;
            _entries[guid] = entry;
        }
        Logger.LogError($"Asset {guid:N} load failed: {error}");
    }

    // ────────────────────────────────────────────────────────────
    //  Internal: GPU upload dispatchers
    // ────────────────────────────────────────────────────────────

    private bool UploadTexture(TextureAsset tex, RenderContext ctx)
    {
        if (tex.IsUploaded) return true;

        var desc = new TextureDesc
        {
            Width = tex.Width,
            Height = tex.Height,
            // Format determined from cooked header / settings
        };

        var handle = ctx.CreateTexture(in desc, tex.TextureData.Span, $"Tex_{tex.ID:N}");
        tex.SetGPUHandle(handle);
        return true;
    }

    // private bool UploadMesh(MeshAsset mesh, RenderContext ctx) { /* ... */ }

    /// <summary>
    /// Resolve material bindings:
    /// looks up AssetRef<TextureAsset> fields → already-uploaded Handle<GPUTexture>.
    /// </summary>
    // private bool ResolveMaterialBindings(MaterialAsset mat, RenderContext ctx)
    // {
    //     // Dependencies loaded first, so textures should be Ready by now
    //     var albedo = Resolve(mat.Properties.AlbedoMap);
    //     var normal = Resolve(mat.Properties.NormalMap);
    //
    //     var gpuMat = ctx.ResourceManager.CreateMaterial(mat.ShaderHandle);
    //     ref var matRef = ref ctx.ResourceManager
    //         .GetMaterialReference(gpuMat).GetValueOrThrow();
    //
    //     matRef.SetTexture("_AlbedoMap", albedo?.GPUTexture ?? _fallbackTexture);
    //     matRef.SetTexture("_NormalMap", normal?.GPUTexture ?? _fallbackNormalMap);
    //
    //     mat.GPUMaterial = gpuMat;
    //     return true;
    // }

    public void Dispose()
    {
        // TODO: Release all GPU resources via ResourceManager
    }
}
```

---

## Layer 5: Content Providers (`Ghost.Editor.Core` / `Ghost.Engine`)

### `EditorContentProvider` — Loose files in Library/

```csharp
namespace Ghost.Editor.Core.Services;

/// <summary>
/// Editor-time content provider.
/// Reads cooked data from Library/Imports/<guid>.imported (loose files).
/// Reads metadata from the SQLite AssetCatalog (which mirrors .gmeta).
/// </summary>
internal sealed class EditorContentProvider : IContentProvider
{
    private readonly AssetCatalog _catalog;
    private readonly string _libraryRoot;

    public EditorContentProvider(AssetCatalog catalog, string libraryRoot)
    {
        _catalog = catalog;
        _libraryRoot = libraryRoot;
    }

    public bool HasAsset(Guid guid)
    {
        var path = Path.Combine(_libraryRoot, "Imports", $"{guid:N}.imported");
        return File.Exists(path);
    }

    public async ValueTask<Result<Stream>> OpenReadAsync(
        Guid guid, CancellationToken token = default)
    {
        var path = Path.Combine(_libraryRoot, "Imports", $"{guid:N}.imported");
        if (!File.Exists(path))
            return Result.Failure<Stream>("Asset not imported yet");

        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Result.Success<Stream>(stream);
    }

    public Guid[] GetDependencies(Guid guid) => _catalog.GetDependencies(guid);
    public Guid GetCookedTypeId(Guid guid) => _catalog.GetHandlerTypeId(guid);
}
```

### `PackedContentProvider` — Packed archive for shipped builds

```csharp
namespace Ghost.Engine;

/// <summary>
/// Runtime content provider for shipped builds.
/// Reads cooked data from a single .gpak archive.
/// All metadata (dependencies, type IDs) comes from an in-memory manifest.
/// </summary>
internal sealed class PackedContentProvider : IContentProvider, IDisposable
{
    private readonly PackManifest _manifest;
    private readonly FileStream _pakStream;

    public PackedContentProvider(string pakPath, string manifestPath)
    {
        _pakStream = new FileStream(pakPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        _manifest = PackManifest.Load(manifestPath);
    }

    public bool HasAsset(Guid guid) => _manifest.Contains(guid);

    public ValueTask<Result<Stream>> OpenReadAsync(
        Guid guid, CancellationToken token = default)
    {
        if (!_manifest.TryGetEntry(guid, out var entry))
            return new(Result.Failure<Stream>("Asset not in pack"));

        // Bounded sub-stream — reads only [offset, offset+size) from the .gpak
        Stream slice = new SubStream(_pakStream, entry.Offset, entry.Size);
        return new(Result.Success(slice));
    }

    public Guid[] GetDependencies(Guid guid) => _manifest.GetDependencies(guid);
    public Guid GetCookedTypeId(Guid guid) => _manifest.GetCookedTypeId(guid);

    public void Dispose() => _pakStream.Dispose();
}
```

### `PackManifest` — Flat lookup table

```csharp
namespace Ghost.Engine;

/// <summary>
/// Binary manifest for packed builds.
/// Format: header (asset count) + array of PackEntry sorted by GUID.
/// Loaded once at startup into memory. Binary search for O(log n) lookups.
/// </summary>
internal sealed class PackManifest
{
    public readonly struct PackEntry
    {
        public readonly Guid Guid;
        public readonly long Offset;
        public readonly int Size;
        public readonly Guid CookedTypeId;
        public readonly int DependencyOffset;  // index into _allDependencies
        public readonly int DependencyCount;
    }

    private readonly PackEntry[] _entries;     // sorted by GUID
    private readonly Guid[] _allDependencies;  // flat pool, referenced by offset+count

    public bool Contains(Guid guid) => FindEntry(guid) >= 0;

    public bool TryGetEntry(Guid guid, out PackEntry entry)
    {
        var idx = FindEntry(guid);
        if (idx < 0) { entry = default; return false; }
        entry = _entries[idx];
        return true;
    }

    public Guid GetCookedTypeId(Guid guid)
    {
        var idx = FindEntry(guid);
        return idx >= 0 ? _entries[idx].CookedTypeId : Guid.Empty;
    }

    public Guid[] GetDependencies(Guid guid)
    {
        var idx = FindEntry(guid);
        if (idx < 0) return [];
        var e = _entries[idx];
        return _allDependencies.AsSpan(e.DependencyOffset, e.DependencyCount).ToArray();
    }

    private int FindEntry(Guid guid)
    {
        // Binary search over sorted entries
        // ...
    }

    public static PackManifest Load(string path)
    {
        // Read binary manifest file
        // ...
    }
}
```

---

## Build Pipeline

The build pipeline runs in the editor and produces the `.gpak` + manifest:

```
Build steps:
  1. Enumerate all .gmeta files under Assets/
  2. For each: read GUID, handler type ID, dependencies from .gmeta
  3. Concatenate all corresponding Library/Imports/<guid>.imported files
     into a single content.gpak, recording each asset's offset+size
  4. Write content.manifest with the PackEntry[] and dependency pool

Output:
  GameData/
    content.gpak        ← concatenated cooked binary blobs
    content.manifest    ← GUID → { offset, size, cookedTypeId, deps[] }
```

```csharp
// Ghost.Editor.Core (future)
internal static class BuildPipeline
{
    public static void BuildPack(string assetsRoot, string libraryRoot, string outputDir)
    {
        var entries = new List<PackManifest.PackEntry>();
        var allDeps = new List<Guid>();

        using var pakStream = File.Create(Path.Combine(outputDir, "content.gpak"));

        foreach (var metaPath in Directory.EnumerateFiles(
            assetsRoot, "*.gmeta", SearchOption.AllDirectories))
        {
            var meta = AssetMetaIO.ReadAsync(metaPath).AsTask().Result;
            if (meta is null) continue;

            var importedPath = Path.Combine(libraryRoot, "Imports", $"{meta.Guid:N}.imported");
            if (!File.Exists(importedPath)) continue;

            var offset = pakStream.Position;
            using (var src = File.OpenRead(importedPath))
            {
                src.CopyTo(pakStream);
            }
            var size = (int)(pakStream.Position - offset);

            entries.Add(new PackManifest.PackEntry
            {
                Guid = meta.Guid,
                Offset = offset,
                Size = size,
                CookedTypeId = meta.HandlerTypeId ?? Guid.Empty,
                DependencyOffset = allDeps.Count,
                DependencyCount = meta.Dependencies.Length,
            });

            allDeps.AddRange(meta.Dependencies);
        }

        // Sort entries by GUID for binary search
        entries.Sort((a, b) => a.Guid.CompareTo(b.Guid));

        // Write manifest
        PackManifest.Write(Path.Combine(outputDir, "content.manifest"), entries, allDeps);
    }
}
```

---

## Dependency Resolution: Material → Texture Example

### In the editor (`.gmeta` tracks dependencies)

When the user creates a material and assigns `hero_albedo.png` as the albedo map:

```json
// hero_material.gmat.gmeta
{
  "guid": "AAAA-...",
  "handlerTypeId": "MATERIAL-HANDLER-GUID",
  "dependencies": [
    "BBBB-...",  // hero_albedo.png's GUID
    "CCCC-..."   // hero_normal.png's GUID
  ],
  "settings": { /* material settings */ }
}
```

### At runtime (AssetManager resolves the chain)

```
1. Game code:  assetManager.Resolve(materialRef)             // GUID-A
2. AssetManager sees GUID-A is Unloaded → BeginLoad(GUID-A)
3. ExecuteLoadAsync(GUID-A):
   a. GetDependencies(GUID-A) → [GUID-B, GUID-C]
   b. BeginLoad(GUID-B) → CookedTextureLoader reads DDS → TextureAsset
   c. BeginLoad(GUID-C) → CookedTextureLoader reads DDS → TextureAsset
   d. await both dependency tasks
   e. CookedMaterialLoader reads property buffer → MaterialAsset
   f. Queue all three for GPU upload
4. ProcessUploads (next frame):
   a. Upload GUID-B (texture) → Handle<GPUTexture> stored on TextureAsset
   b. Upload GUID-C (texture) → Handle<GPUTexture> stored on TextureAsset
   c. Upload GUID-A (material) → resolves AssetRef fields to already-uploaded textures
   d. All three marked Ready
5. Next Resolve(materialRef) → returns MaterialAsset with working GPU bindings
```

> [!NOTE]
> Between steps 1 and 5, `Resolve()` returns `null`. The renderer uses a fallback material 
> (e.g., solid magenta or checkerboard) until the real material is Ready. This is the same 
> behavior as Unreal's streaming system.

---

## Startup: Editor vs Runtime

### Editor startup

```csharp
// Ghost.Editor.Core — full editor with import, FSW, catalog
var catalog = new AssetCatalog(dbPath);
var contentProvider = new EditorContentProvider(catalog, libraryRoot);

// Editor-only: import pipeline + file watching
var editorHandlerRegistry = new AssetHandlerRegistry();  // TypeCache scan
var importCoordinator = new ImportCoordinator(catalog, editorHandlerRegistry, assetsRoot, libraryRoot);
var assetRegistry = new AssetRegistry(assetsRoot, catalog, importCoordinator, ...);

// Shared runtime: load + resolve + upload
var runtimeLoaders = new RuntimeLoaderRegistry();
runtimeLoaders.Register(new CookedTextureLoader());
runtimeLoaders.Register(new CookedMeshLoader());
runtimeLoaders.Register(new CookedMaterialLoader());
runtimeLoaders.Register(new CookedShaderLoader());

var assetManager = new AssetManager(contentProvider, runtimeLoaders);
```

### Runtime startup (shipped build)

```csharp
// Ghost.Engine — no editor, no import, no FSW, no SQLite
var contentProvider = new PackedContentProvider("GameData/content.gpak", "GameData/content.manifest");

var runtimeLoaders = new RuntimeLoaderRegistry();
runtimeLoaders.Register(new CookedTextureLoader());
runtimeLoaders.Register(new CookedMeshLoader());
runtimeLoaders.Register(new CookedMaterialLoader());
runtimeLoaders.Register(new CookedShaderLoader());

var assetManager = new AssetManager(contentProvider, runtimeLoaders);
// Same AssetManager class, same runtime loaders — only IContentProvider differs
```

---

## Streaming Roadmap

### Level 1: Demand loading (implement now)

```
Resolve(ref) → Unloaded → Loading (background IO) → Loaded → upload queued → Ready
```

- Returns `null` while loading — callers use fallback resources
- `ProcessUploads` bounds GPU work per frame (default: 4 uploads/frame)

### Level 2: Priority-based loading queue (implement later)

```csharp
// Replace Queue<Guid> with PriorityQueue<Guid, float>
private readonly PriorityQueue<Guid, float> _loadQueue = new();

// Priority driven by renderer: distance, screen-space coverage, visibility
public void RequestWithPriority(Guid guid, float priority)
{
    _loadQueue.Enqueue(guid, priority);
}
```

### Level 3: Texture mip streaming (implement much later)

- Store each mip level as a separate chunk in `.gpak`
- Load mip 0 (tiny) immediately, stream higher mips on demand
- `IContentProvider.OpenReadAsync` extended with mip-level parameter
- Requires extending `TextureAsset` to support partial/progressive GPU upload

---

## Implementation Order

```
Phase 1 (now):     AssetRef<T> in Ghost.Core
                   RuntimeAsset base class in Ghost.Core
                   IRuntimeAssetLoader interface in Ghost.Core
                   IContentProvider interface in Ghost.Core
                   
Phase 2 (next):    CookedTextureLoader in Ghost.Engine
                   RuntimeLoaderRegistry in Ghost.Engine
                   EditorContentProvider in Ghost.Editor.Core
                   AssetManager (basic load + upload) in Ghost.Engine
                   ProcessUploads integration into render loop

Phase 3 (soon):    Dependency resolution (material → texture)
                   Fallback resources (checkerboard, default normal, error material)
                   Reference counting / unloading

Phase 4 (later):   Build pipeline (cook → .gpak + manifest)
                   PackedContentProvider in Ghost.Engine
                   PackManifest binary format

Phase 5 (future):  Priority-based streaming
                   Mip streaming
                   Asset bundles / split packs for DLC
```

---

## Open Design Questions

> [!IMPORTANT]
> 1. **`RenderContext` is a `ref struct`** — you can't store it or pass it to async methods.
>    `ProcessUploads` must be called synchronously within a `RenderContext` scope (inside a 
>    frame's command recording). Does your current render loop have a clear synchronous point 
>    where this can be called? (e.g., beginning of frame, before render graph execution)

> [!IMPORTANT]
> 2. **`RuntimeAsset` vs your current `Asset`** — should `RuntimeAsset` replace the existing 
>    `Asset` base class entirely, or should `Asset` in `Ghost.Editor.Core` remain separate?
>    My recommendation: rename the existing `Asset` to `RuntimeAsset` and move it to `Ghost.Core`,
>    since the runtime needs it too. The current `Asset` is already close to what's needed.

> [!WARNING]
> 3. **Thread safety of `AssetManager._entries`** — the current design uses a single `Lock` for 
>    all operations. This is simple but could become a bottleneck if many systems call `Resolve()` 
>    simultaneously. Consider `ReaderWriterLockSlim` or a `ConcurrentDictionary` with state 
>    transitions protected by per-entry locks. This can be optimized later.
