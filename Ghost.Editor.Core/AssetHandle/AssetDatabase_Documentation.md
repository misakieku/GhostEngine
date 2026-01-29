# Asset Database Documentation

The Asset Database is a core component of the Ghost Editor responsible for managing the lifecycle, storage, import, and retrieval of project assets. It provides a unified API for interacting with assets, ensuring that metadata (GUIDs, tags, settings) stays synchronized with files on disk.

## Key Features

- **GUID-based Asset Identification**: Every asset is uniquely identified by a stable GUID, stored in a sidecar `.gmeta` file.
- **Automatic Importing**: Monitors the file system for changes and automatically imports assets using registered importers.
- **Dependency Tracking**: Tracks dependencies between assets to ensure validity and trigger re-imports when dependencies change.
- **Caching**: Implements an LRU (Least Recently Used) cache for loaded assets to optimize performance.
- **SQLite Backed**: Uses a local SQLite database for fast lookups (Path <-> GUID) and metadata queries.
- **Metadata Management**: Handles `.gmeta` files automatically, including generation, validation, and cleanup.

## usage

### Initialization
The Asset Database must be initialized after the project is loaded.
```csharp
await AssetDatabase.Initialize(cancellationToken);
```

### Loading Assets
Assets can be loaded by GUID or by Path.

```csharp
// Load by Path
var result = AssetDatabase.LoadAssetAtPath<TextureAsset>("Assets/Textures/my_texture.png");
if (result.IsSuccess)
{
    var texture = result.Value;
}

// Load by GUID
var guid = ...;
var result = AssetDatabase.LoadAsset<TextureAsset>(guid);
```

### File Operations
Always use the `AssetDatabase` API for file operations to ensure metadata is preserved.

```csharp
// Create
await AssetDatabase.CreateAssetAsync("Assets/Data/config.json", dataBytes);

// Move
await AssetDatabase.MoveAssetAsync("Assets/Old/file.txt", "Assets/New/file.txt");

// Copy
await AssetDatabase.CopyAssetAsync("Assets/template.txt", "Assets/instance.txt");

// Delete
await AssetDatabase.DeleteAssetAsync("Assets/garbage.tmp");
```

### Searching
Find assets using wildcards or tags.

```csharp
// Find all PNGs
var guids = await AssetDatabase.FindAssetsByNameAsync("*.png");

// Find assets with a specific tag
var enemyAssets = await AssetDatabase.FindAssetsByTagAsync("Enemy");
```

### Tags
Manage asset tags for organization.

```csharp
// Get tags
var tagsResult = await AssetDatabase.GetAssetTagsAsync(guid);

// Set tags
await AssetDatabase.SetAssetTagsAsync(guid, new List<string> { "Level1", "Prop" });
```

### Opening Assets
Open an asset using its registered handler or the system default.
```csharp
AssetDatabase.OpenAsset("Assets/Docs/readme.txt");
```

## Extending the Asset Database

### Creating a New Importer
To support a new file type, create a class that inherits from `AssetImporter<T>` and decorate it with the `[AssetImporter]` attribute.

```csharp
[AssetImporter(".myfmt")]
internal class MyFormatImporter : AssetImporter<MyFormatSettings>
{
    public override async Task<Result> ImportAsync(string assetPath, AssetMeta meta)
    {
        var settings = GetSettings(meta);
        
        // 1. Read source file
        // 2. Process data
        // 3. Save imported data using AssetDatabase.SaveImportedAsset
        
        var myAsset = new MyAsset(meta.Guid) { ... };
        return AssetDatabase.SaveImportedAsset(meta.Guid, myAsset);
    }
}

internal class MyFormatSettings : ImporterSettings
{
    public float Scale { get; set; } = 1.0f;
}
```

### Creating an Open Handler
To define custom behavior when an asset is opened (e.g., double-clicked in the editor), use the `[AssetOpenHandler]` attribute.

```csharp
internal static class MyHandlers
{
    [AssetOpenHandler(".myfmt")]
    private static void OpenMyFormat(string path)
    {
        // Open custom editor window
    }
}
```

## Internal Architecture

- **AssetDatabase.cs**: Core initialization and event coordination.
- **AssetDatabase.SQLite.cs**: Database table management and queries.
- **AssetDatabase.Meta.cs**: `.gmeta` file handling and file system watcher events.
- **AssetDatabase.Importer.cs**: Importer discovery and execution.
- **AssetDatabase.Loader.cs**: Asset loading and caching logic.
