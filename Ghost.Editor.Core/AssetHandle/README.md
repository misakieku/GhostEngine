# Asset Database Implementation

This is the complete implementation of the GhostEngine Asset Database system based on the plan in `AssetDBPlan.md`.

## Structure

The asset database is implemented as a partial class split across multiple files:

### Core Files

- **AssetDatabase.cs** - Main entry point with initialization and shutdown logic
- **AssetDatabase.Meta.cs** - Metadata file management and file system watching
- **AssetDatabase.SQLite.cs** - SQLite database operations for caching
- **AssetDatabase.Lookup.cs** - GUID/Path lookup and search operations
- **AssetDatabase.FileOps.cs** - File operations (create, delete, move, copy)
- **AssetDatabase.Importer.cs** - Asset importing framework
- **AssetDatabase.Open.cs** - Asset opening handlers (existing file)

### Supporting Files

- **Asset.cs** - Base class for all assets
- **AssetMeta.cs** - Metadata structure (stored in .gmeta files)
- **AssetImporter.cs** - Base class for all asset importers
- **AssetImporterAttribute.cs** - Attribute to mark importer classes
- **AssetOpenHandlerAttribute.cs** - Attribute for custom open handlers
- **ImporterSettings.cs** - Base class for importer settings

### Example Importer

- **Importers/TextImporter.cs** - Example importer for .txt and .md files

## Features Implemented

### Core API (AssetDatabase.Lookup.cs)

- ✅ `PathToGuid(string assetPath)` - Find GUID by path
- ✅ `GuidToPath(Guid guid)` - Find path by GUID
- ✅ `LoadAsset<T>(Guid guid)` - Load asset by GUID (TODO: needs asset loader)
- ✅ `GetAssetTagsAsync(Guid guid)` - Get asset tags
- ✅ `SetAssetTagsAsync(Guid guid, List<string> tags)` - Set asset tags
- ✅ `FindAssetsByName(string namePattern)` - Search by name
- ✅ `FindAssetsByTagAsync(string tag)` - Search by tag
- ✅ `GetAllAssets()` - Get all assets in database

### File Operations (AssetDatabase.FileOps.cs)

- ✅ `CreateAssetAsync(string assetPath, byte[] content)` - Create new asset
- ✅ `DeleteAssetAsync(Guid guid)` - Delete asset
- ✅ `MoveAssetAsync(Guid guid, string newPath)` - Move/rename asset
- ✅ `CopyAssetAsync(Guid guid, string newPath)` - Copy asset with new GUID
- ✅ `RefreshAsync()` - Refresh database manually
- ✅ `MarkDirtyAsync(Guid guid)` - Mark asset for re-import
- ✅ `ImportDirtyAssetsAsync()` - Import all dirty assets

### Background Services (AssetDatabase.Meta.cs)

- ✅ File system watcher for automatic change detection
- ✅ Automatic metadata generation on file creation
- ✅ Automatic metadata cleanup on file deletion
- ✅ Automatic metadata movement on file rename
- ✅ File hash comparison for change detection
- ✅ Automatic dirty marking on file modification
- ✅ Dependent asset tracking and dirty propagation

### Database (AssetDatabase.SQLite.cs)

- ✅ SQLite for persistent storage and efficient querying
- ✅ In-memory cache for fast lookups
- ✅ Automatic database creation and schema management
- ✅ Asset indexing by GUID and path
- ✅ Dirty flag tracking for re-import
- ✅ Tag-based search support

### Validation (AssetDatabase.cs)

- ✅ Validate and fix database on project load
- ✅ Check for missing/corrupted metadata files
- ✅ Regenerate metadata when necessary
- ✅ Database consistency checks

## Metadata File Format

Assets have associated `.gmeta` files stored alongside them:

```json
{
  "Guid": "123e4567-e89b-12d3-a456-426614174000",
  "Version": 1,
  "Tags": ["Environment", "Texture"],
  "FileHash": "ABC123...",
  "Dependencies": [
    "456e7890-e89b-12d3-a456-426614174001"
  ],
  "ImporterSettings": {
    "TextureImporter": {
      "MaxSize": 2048,
      "MipLevels": 1
    }
  }
}
```

## Usage Examples

### Finding Assets

```csharp
// Find by path
var guidResult = AssetDatabase.PathToGuid("Assets/Textures/logo.png");
if (guidResult.IsSuccess)
{
    var guid = guidResult.Value;
    // Use guid...
}

// Find by GUID
var pathResult = AssetDatabase.GuidToPath(myGuid);
if (pathResult.IsSuccess)
{
    var path = pathResult.Value;
    // Use path...
}

// Search by name
var results = AssetDatabase.FindAssetsByName("logo");

// Search by tag
var textureAssets = await AssetDatabase.FindAssetsByTagAsync("Texture");
```

### Creating and Managing Assets

```csharp
// Create new asset
var content = Encoding.UTF8.GetBytes("Hello, World!");
await AssetDatabase.CreateAssetAsync("Assets/test.txt", content);

// Move asset
await AssetDatabase.MoveAssetAsync(guid, "Assets/NewFolder/test.txt");

// Copy asset
var newGuid = await AssetDatabase.CopyAssetAsync(guid, "Assets/test_copy.txt");

// Delete asset
await AssetDatabase.DeleteAssetAsync(guid);
```

### Working with Tags

```csharp
// Get tags
var tagsResult = await AssetDatabase.GetAssetTagsAsync(guid);
if (tagsResult.IsSuccess)
{
    var tags = tagsResult.Value;
}

// Set tags
await AssetDatabase.SetAssetTagsAsync(guid, new List<string> { "UI", "Icon" });
```

### Asset Importing

```csharp
// Mark asset dirty for re-import
await AssetDatabase.MarkDirtyAsync(guid);

// Import all dirty assets
await AssetDatabase.ImportDirtyAssetsAsync();
```

## Creating Custom Importers

To create a custom asset importer:

1. Create a settings class inheriting from `ImporterSettings`
2. Create an importer class inheriting from `AssetImporter<TSettings>`
3. Add the `[AssetImporter]` attribute with supported extensions

Example:

```csharp
public class MyImporterSettings : ImporterSettings
{
    public bool SomeOption { get; set; } = true;
}

[AssetImporter(".myext")]
public class MyImporter : AssetImporter<MyImporterSettings>
{
    public override async Task<Result> ImportAsync(string assetPath, AssetMeta meta)
    {
        var settings = GetSettings(meta);
        
        // Validate dependencies
        var depResult = await ValidateDependenciesAsync(meta);
        if (depResult.IsFailure)
        {
            return depResult;
        }

        // Import logic here...
        
        return Result.Success();
    }
}
```

## Architecture Notes

### Source of Truth

The `.gmeta` files are the **source of truth** for asset information. The SQLite database is used only for:
- Caching for fast lookups
- Efficient querying and search operations
- Tracking dirty state

If the database becomes inconsistent, it can be regenerated from the `.gmeta` files by calling `RefreshAsync()`.

### Thread Safety

All database operations use locks to ensure thread safety. File system watcher events are handled asynchronously to avoid blocking the main thread.

### Error Handling

The system uses the `Result` pattern for railway-oriented programming. All operations return `Result` or `Result<T>` to indicate success or failure without throwing exceptions for expected failures.

## Testing

Unit tests should be added to verify:
- Metadata file generation and parsing
- Database consistency
- File operations (create, delete, move, copy)
- Asset importing
- Dependency tracking
- Tag management
- Search functionality

## Future Improvements

- Asset loader implementation (`LoadAsset<T>`)
- Asset browser UI
- More sophisticated dependency resolution
- Asset preview generation
- Asset versioning and migration
- Orphaned entry cleanup in database
- Better error reporting and logging
- Asset import progress tracking
- Parallel asset importing
- Asset thumbnail generation
