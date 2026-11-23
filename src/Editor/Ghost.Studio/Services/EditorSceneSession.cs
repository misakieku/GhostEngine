using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Serialization;
using Ghost.Editor.Core.Sync;
using Ghost.Engine;
using Ghost.Entities;
using Ghost.Studio.UI;

namespace Ghost.Studio.Services;

/// <summary>
/// Coordinates active <c>.gscene</c> open, additive load, save, creation, and <see cref="Project.DefaultScene"/> startup resolution.
/// </summary>
internal sealed class EditorSceneSession
{
    private readonly ProjectService _projectService;
    private readonly EditorAssetCatalog _assetCatalog;
    private readonly EditorSyncBridge _bridge;

    public string? CurrentScenePath { get; private set; }

    public bool IsDirty => _bridge.HasUnsavedSceneChanges;

    public string CurrentSceneDisplayName => string.IsNullOrWhiteSpace(CurrentScenePath) ? "Untitled" : Path.GetFileNameWithoutExtension(CurrentScenePath);

    public EditorSceneSession(ProjectService projectService, EditorAssetCatalog assetCatalog, EditorSyncBridge? bridge = null)
    {
        _projectService = projectService;
        _assetCatalog = assetCatalog;
        _bridge = bridge ?? EditorSyncBridge.Instance;
    }

    public void NewScene()
    {
        var blank = new SceneAssetDocument();
        CurrentScenePath = null;
        _bridge.ActiveScenePath = null;
        _bridge.EnqueueWorldAction(world =>
        {
            SceneSerializer.LoadFromDocument(world, _bridge, blank, SceneLoadingType.Single, null, EditorSelection.Instance, CommandHistory.Instance);
        });
    }

    public void OpenScene(string filePath, SceneLoadingType mode = SceneLoadingType.Single)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            Logger.Warning($"Scene file not found: '{filePath}'.");
            return;
        }

        var fullPath = Path.GetFullPath(filePath);
        if (mode == SceneLoadingType.Single)
        {
            CurrentScenePath = fullPath;
            _bridge.ActiveScenePath = fullPath;
        }

        _bridge.EnqueueWorldAction(world =>
        {
            var res = SceneSerializer.LoadFromFile(world, _bridge, fullPath, mode, null, EditorSelection.Instance, CommandHistory.Instance);
            if (res.IsFailure)
            {
                Logger.Error($"Failed to open scene '{fullPath}': {res.Message}");
            }
        });
    }

    public bool SaveScene(string? pathOverride = null)
    {
        var targetPath = !string.IsNullOrWhiteSpace(pathOverride) ? Path.GetFullPath(pathOverride) : CurrentScenePath;
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return false;
        }

        if (!targetPath.EndsWith(SceneAssetDocument.EXTENSION, StringComparison.OrdinalIgnoreCase))
        {
            targetPath += SceneAssetDocument.EXTENSION;
        }

        var snapshot = _bridge.LatestSnapshot;
        if (snapshot == null)
        {
            return false;
        }

        SceneSerializer.SaveToFile(targetPath, snapshot, _bridge);
        CurrentScenePath = targetPath;
        _bridge.ActiveScenePath = targetPath;
        _bridge.HasUnsavedSceneChanges = false;
        _bridge.RegisterScene(_bridge.ActiveSceneId, targetPath);

        if (_projectService.CurrentProject != null)
        {
            EnsureSceneMetadata(targetPath);
        }

        _assetCatalog.MarkDirty();
        return true;
    }

    public string CreateEmptySceneAsset(string targetDirectory, string baseName = "NewScene")
    {
        Directory.CreateDirectory(targetDirectory);
        var candidate = Path.Combine(targetDirectory, $"{baseName}{SceneAssetDocument.EXTENSION}");
        var index = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(targetDirectory, $"{baseName}_{index++}{SceneAssetDocument.EXTENSION}");
        }

        SceneSerializer.WriteDocumentToFile(candidate, new SceneAssetDocument());
        if (_projectService.CurrentProject != null)
        {
            EnsureSceneMetadata(candidate);
        }

        _assetCatalog.MarkDirty();
        return candidate;
    }

    private void EnsureSceneMetadata(string sceneFilePath)
    {
        var metaPath = sceneFilePath + ".meta";
        if (File.Exists(metaPath))
        {
            return;
        }

        var meta = _projectService.GetOrCreateMetadata(sceneFilePath) ?? new AssetMetadata
        {
            Id = Guid.NewGuid(),
            Type = AssetType.Scene
        };
        _projectService.SaveMetadata(metaPath, meta);
    }

    public bool TryLoadDefaultSceneOnStartup(World world)
    {
        var defaultScene = _projectService.CurrentProject?.DefaultScene;
        if (string.IsNullOrWhiteSpace(defaultScene))
        {
            return false;
        }

        var resolved = ResolveScenePath(defaultScene);
        if (string.IsNullOrEmpty(resolved) || !File.Exists(resolved))
        {
            Logger.Warning($"Project DefaultScene '{defaultScene}' could not be resolved on disk; falling back to starter scene.");
            return false;
        }

        var res = SceneSerializer.LoadFromFile(world, _bridge, resolved, SceneLoadingType.Single, null, EditorSelection.Instance, CommandHistory.Instance);
        if (res.IsFailure)
        {
            Logger.Warning($"Failed to load DefaultScene '{resolved}': {res.Message}; falling back to starter scene.");
            return false;
        }

        CurrentScenePath = resolved;
        _bridge.ActiveScenePath = resolved;
        _bridge.HasUnsavedSceneChanges = false;
        return true;
    }

    private string? ResolveScenePath(string sceneRef)
    {
        if (Guid.TryParse(sceneRef, out var guid) && _assetCatalog.TryGetByGuid(guid, out var entry) && !string.IsNullOrEmpty(entry.FullSourcePath))
        {
            return Path.GetFullPath(entry.FullSourcePath);
        }

        if (Path.IsPathRooted(sceneRef) && File.Exists(sceneRef))
        {
            return Path.GetFullPath(sceneRef);
        }

        var hasExt = sceneRef.EndsWith(SceneAssetDocument.EXTENSION, StringComparison.OrdinalIgnoreCase);
        var fileRef = hasExt ? sceneRef : $"{sceneRef}{SceneAssetDocument.EXTENSION}";

        var assetDirs = _projectService.AssetDirectories;
        for (var i = assetDirs.Count - 1; i >= 0; i--)
        {
            var candidate = Path.GetFullPath(Path.Combine(assetDirs[i], fileRef));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var rootPath = _projectService.CurrentProject?.RootPath;
        if (!string.IsNullOrEmpty(rootPath))
        {
            var candidate = Path.GetFullPath(Path.Combine(rootPath, fileRef));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
