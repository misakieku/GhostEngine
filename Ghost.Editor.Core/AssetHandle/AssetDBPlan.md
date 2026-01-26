# Asset Database Plan

AssetDB is a core component of the Ghost Editor that manages the storage, retrieval, and organization of various assets used within the editor.
This document outlines the plan for implementing the AssetDB, including its structure, functionality, and integration with other components of the Ghost Editor.

## Data Structure

- Asset Metadata: Each asset will have associated metadata, including:
    - Unique Identifier (GUID)
    - Version
    - Importer Settings

An example of metadata file (filename.png.gmeta):
```json
{
    "Guid": "123e4567-e89b-12d3-a456-426614174000",
    "Version": 1,
    "ImporterSettings": {
        "TextureImporter": {
            "MaxSize": 2048,
            "MipLevels": 1
        },
        "OtherImporter": {

        }
    }
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
    - SQLite for persistent storage and efficient querying. (Don't use JSON or XML for the database itself, only for metadata files. Also don't use heavy)

## Simplified Workflow

### New Asset Addition

1. A file is added to the project directory.
2. Generates the metadata for the asset with name filename.etx + ".gmeta" (You can get the extension from Ghost.Editor.Core.Utilities.FileExtensions.META_FILE_EXTENSION)
3. Add the asset to the database.

### File Removal

1. A file is removed from the project directory.
2. Deletes the corresponding asset metadata.

### File Renaming/Moving

1. A file is renamed or moved within the project directory.
2. Check if the new path has an existing metadata file (just in case user move the file with the metadata file together).
    - If exists, validate the data and update the database accordingly.
    - if not, regenerate the metadata file for the new path and update the database.
3. Delete the old metadata file if exists.

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
