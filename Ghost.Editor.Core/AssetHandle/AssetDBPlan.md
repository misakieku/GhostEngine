# Asset Database Plan

AssetDB is a core component of the Ghost Editor that manages the storage, retrieval, and organization of various assets used within the editor.
This document outlines the plan for implementing the AssetDB, including its structure, functionality, and integration with other components of the Ghost Editor.

## Data Structure

- Asset Metadata: Each asset will have associated metadata, including:
    - Unique Identifier (GUID)
    - Version (Version of the asset pipeline, not the asset. This is primarily for migration when we redesign the asset pipeline in the future)
    - Tags
    - Importer Settings

An simplified example of metadata file (filename.png.gmeta):
```json
{
    "Guid": "123e4567-e89b-12d3-a456-426614174000",
    "Version": 1,
    "Tags": ["Environment", "Texture"],
    "ImporterSettings": [
        "TextureImporter": {
            "Version": 1,
            "MaxSize": 2048,
            "MipLevels": 1
        },
        "OtherImporter": {

        }
    ]
}
```

- Asset: The base class for all assets.

- Asset Database: A centralized database that stores and manages all assets. It will handle:
    - Asset registration and deregistration
    - Asset lookup by GUID
    - Asset lookup by path
    - Automatic handle file creation, remove, rename, move, etc.
    - Asset dependency management
    - Automatic asset re-importing when source files change.
    - Asset tagging.
    - Add type specific default importer settings for new asset.
    - SQLite (`Microsoft.Data.Sqlite`) for persistent storage and efficient querying.

An simplified data model example in SQLite:
```sql
CREATE TABLE Assets (
    Guid TEXT PRIMARY KEY,
    Path TEXT NOT NULL,
    Type INTEGER,
    Version INTEGER,
    Tags TEXT,
    DependencyGuids TEXT
);
```

## Simplified Workflow

### New Asset Addition

1. A file is added to the project directory.
2. Generates the metadata for the asset with name filename.etx + ".gmeta" (You can get the extension from Ghost.Editor.Core.Utilities.FileExtensions.META_FILE_EXTENSION)
3. Add the asset to the database.

### File Removal

1. A file is removed from the project directory.
2. Deletes the corresponding asset metadata.
3. Remove the asset from the database.
4. Mark dependent assets as dirty for re-importing.

### File Renaming/Moving

1. A file is renamed or moved within the project directory.
2. Check if the new path has an existing metadata file (just in case user move the file with the metadata file together).
    - If exists, validate the data and update the database accordingly.
    - if not, regenerate the metadata file for the new path and update the database.
3. Delete the old metadata file if exists.

### File Modification

1. A file is modified in the project directory.
2. Check the file hash to see if it has changed.
    - If changed, mark the asset as dirty for re-importing.
    - If not, do nothing.

### Asset Importing (You don't need to write any assets importer right now, just write the framework and a simple test importer if it's needed for unit test)

1. An asset is marked as dirty.
2. The asset importer for that type processes the asset based on its importer settings.
3. Validate the references and dependencies, report errors if any (for example, missing dependencies).

## Features Checklist

### In Code (API)

- [ ] Find GUID by path
- [ ] Find path by GUID
- [ ] Load asset by GUID (May need special asset loader, not included in this plan, leave an API and TODO comment)
- [ ] API for adding/removing/moving/copying assets
- [ ] API for opening asset in editor or external program
- [ ] API to set asset dirty and save all assets if dirty
- [ ] Get and set asset tags.
- [ ] Refresh asset database (re-scan project directory for changes)

### In Editor (API Only, I will handle the UI part)

- [ ] Asset Browser window to view and manage assets (automatically refresh when assets change)
- [ ] Skip meta file in asset browser view
- [ ] Search assets by name, tag, type, etc.

### In Background

- [ ] File system watcher to monitor changes in the project directory and update the AssetDB accordingly.
- [ ] Automatic asset re-importing when source files change (detect changes via file system watcher and quick hash comparison).
- [ ] Asset dependency management.
- [ ] Validate and fix AssetDB on project load (check for missing/corrupted assets if user add/delete/rename/move files when the editor is not running, etc.)
- [ ] Asset importer system to handle different asset types and their import settings.

## Testing

Make sure everything builds correctly at first.
Write unit tests and integration tests to ensure the AssetDB functions correctly inside the `Ghost.UnitTests` project.

## Critical Considerations

- Performance: Ensure that the AssetDB operations are efficient, especially for large projects with many assets.
- Stability: The meta data files should be the only source of truth for asset information.
  The AssetDB should be able to recover from inconsistencies by re-generating data from the meta files.
  We still need to trust the meta data files even if they are corrupted or missing by regenerating them when necessary.
  Database is used for caching and quick lookup only.
- Asynchronous patterns: Consider using asynchronous operations for file I/O and database operations to avoid blocking the main thread.
  Packages like `Microsoft.Data.Sqlite` support async operations.