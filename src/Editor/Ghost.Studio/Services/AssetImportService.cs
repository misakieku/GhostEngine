using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Utilities;
using System.Threading.Channels;

namespace Ghost.Studio.Services;

/// <summary>
/// Asynchronous Explorer drag-and-drop asset importer backed by <see cref="ProjectService"/> and <see cref="BakeService"/>.
/// Files without a registered baker are still copied into the project folder so <see cref="UI.ProjectPanel"/> displays them,
/// while supported assets are baked in the background without stalling the render loop.
/// </summary>
internal sealed class AssetImportService : IDisposable
{
    public readonly record struct ImportProgressSnapshot(bool IsImporting, int CompletedFiles, int TotalFiles, string CurrentItemName);

    private readonly record struct ImportRequest(string[] DroppedPaths, string TargetDirectory, bool IsReimport, bool ForceRebake);

    private readonly ProjectService _projectService;
    private readonly Channel<ImportRequest> _queue = Channel.CreateUnbounded<ImportRequest>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _cts = new();
    private readonly Lock _statusLock = new();
    private readonly Task _workerTask;

    private bool _isImporting;
    private int _completedFiles;
    private int _totalFiles;
    private string _currentItemName = string.Empty;
    private int _importRevision;
    private bool _disposed;

    public AssetImportService(ProjectService projectService)
    {
        _projectService = projectService;
        _workerTask = Task.Run(ProcessQueueAsync);
    }

    public int ImportRevision => Volatile.Read(ref _importRevision);

    public event Action? ProjectCacheUpdated;

    public event Action<IReadOnlyList<Guid>>? ProjectAssetsBaked;

    public ImportProgressSnapshot GetProgress()
    {
        lock (_statusLock)
        {
            return new ImportProgressSnapshot(_isImporting, _completedFiles, _totalFiles, _currentItemName);
        }
    }

    public void EnqueueImport(string[] droppedPaths, string targetDirectory)
    {
        if (_disposed || droppedPaths.Length == 0 || _projectService.CurrentProject == null)
        {
            return;
        }

        _queue.Writer.TryWrite(new ImportRequest(droppedPaths, targetDirectory, IsReimport: false, ForceRebake: false));
    }

    public void EnqueueReimport(string assetFilePath, bool forceRebake = true)
    {
        if (_disposed || string.IsNullOrWhiteSpace(assetFilePath) || _projectService.CurrentProject == null)
        {
            return;
        }

        _queue.Writer.TryWrite(new ImportRequest(new[] { assetFilePath }, string.Empty, IsReimport: true, ForceRebake: forceRebake));
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            await foreach (var request in _queue.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
            {
                try
                {
                    if (request.IsReimport)
                    {
                        await ReimportSingleAssetAsync(request.DroppedPaths[0], request.ForceRebake, _cts.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        await ImportBatchAsync(request, _cts.Token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Asset import failed: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            SetProgress(false, 0, 0, string.Empty);
        }
    }

    private async Task ReimportSingleAssetAsync(string assetFilePath, bool forceRebake, CancellationToken cancellationToken)
    {
        if (!File.Exists(assetFilePath))
        {
            Logger.Warning($"Cannot reimport missing asset: {assetFilePath}");
            return;
        }

        var ext = Path.GetExtension(assetFilePath);
        if (!_projectService.BakerRegistry.HasBaker(ext))
        {
            Logger.Info($"Skipped reimport for '{Path.GetFileName(assetFilePath)}' (no baker for '{ext}').");
            return;
        }

        var baseContext = _projectService.GetContext();
        var primaryAssetDir = baseContext.AssetDirectories.LastOrDefault();
        if (string.IsNullOrEmpty(primaryAssetDir))
        {
            return;
        }

        var fullAssetPath = Path.GetFullPath(assetFilePath);
        var relativePath = Path.GetRelativePath(Path.GetFullPath(primaryAssetDir), fullAssetPath).Replace('\\', '/');

        var metaFilePath = fullAssetPath + ".meta";
        if (!File.Exists(metaFilePath))
        {
            var defaultMeta = _projectService.GetOrCreateMetadata(fullAssetPath);
            if (defaultMeta != null)
            {
                _projectService.SaveMetadata(metaFilePath, defaultMeta);
                Interlocked.Increment(ref _importRevision);
            }
        }

        var targets = new[] { (relativePath, fullAssetPath) };
        var result = await RunBakeTargetsAsync(baseContext, targets, forceRebake, cancellationToken).ConfigureAwait(false);
        SetProgress(false, 0, 0, string.Empty);
        NotifyBakedAssets(baseContext, targets, result);
        Interlocked.Increment(ref _importRevision);

        Logger.Info($"Reimport finished for '{relativePath}': {result.Succeeded} baked, {result.Skipped} skipped, {result.Failed} failed.");
    }

    private async Task ImportBatchAsync(ImportRequest request, CancellationToken cancellationToken)
    {
        var baseContext = _projectService.GetContext();
        var primaryAssetDir = baseContext.AssetDirectories.LastOrDefault();
        if (string.IsNullOrEmpty(primaryAssetDir))
        {
            return;
        }

        var fullPrimaryDir = Path.GetFullPath(primaryAssetDir);
        var baseTargetDir = ResolveTargetDirectory(fullPrimaryDir, request.TargetDirectory);
        var baseVirtualDir = GetRelativeVirtualDirectory(fullPrimaryDir, baseTargetDir);

        var itemsToImport = ExpandDroppedPaths(request.DroppedPaths, baseVirtualDir);
        if (itemsToImport.Count == 0)
        {
            return;
        }

        SetProgress(true, 0, itemsToImport.Count, "Copying files...");

        var bakeTargets = new List<(string relativePath, string sourceFile)>();
        var copiedCount = 0;
        var skippedNoBakerCount = 0;

        for (var i = 0; i < itemsToImport.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (sourceFile, virtualSubDir) = itemsToImport[i];
            var fileName = Path.GetFileName(sourceFile);

            SetProgress(true, i, itemsToImport.Count, $"Importing {fileName}");

            var imported = _projectService.ImportAssetFile(sourceFile, virtualSubDir);
            if (imported == null)
            {
                continue;
            }

            copiedCount++;
            Interlocked.Increment(ref _importRevision);

            if (imported.Value.HasBaker)
            {
                bakeTargets.Add((imported.Value.RelativePath, imported.Value.DestinationPath));
            }
            else
            {
                skippedNoBakerCount++;
                var ext = Path.GetExtension(fileName);
                Logger.Info($"Imported '{imported.Value.RelativePath}' (no baker for '{ext}'; skipped baking).");
            }
        }

        var bakedSucceeded = 0;
        var bakedSkipped = 0;
        var bakedFailed = 0;

        if (bakeTargets.Count > 0)
        {
            var bakeResult = await RunBakeTargetsAsync(baseContext, bakeTargets, request.ForceRebake, cancellationToken).ConfigureAwait(false);
            bakedSucceeded = bakeResult.Succeeded;
            bakedSkipped = bakeResult.Skipped;
            bakedFailed = bakeResult.Failed;
            NotifyBakedAssets(baseContext, bakeTargets, bakeResult);
            Interlocked.Increment(ref _importRevision);
        }

        SetProgress(false, 0, 0, string.Empty);

        var totalSkipped = skippedNoBakerCount + bakedSkipped;
        Logger.Info($"Import finished: {copiedCount} file(s) added ({bakedSucceeded} baked, {totalSkipped} skipped, {bakedFailed} failed).");
    }

    private async Task<BakeResult> RunBakeTargetsAsync(ProjectContext baseContext, IReadOnlyList<(string relativePath, string sourceFile)> bakeTargets, bool forceRebake, CancellationToken cancellationToken)
    {
        var bakeContext = BuildBakeContext(baseContext);
        var bakeService = new BakeService(bakeContext, _projectService.BakerRegistry);
        bakeService.OnProgress += (done, total) => SetProgress(true, done, total, total == 1 ? $"Baking {Path.GetFileName(bakeTargets[0].relativePath)}" : $"Baking assets ({done}/{total})");

        var bakeConfig = new BakeConfig
        {
            ForceRebake = forceRebake,
            ForceShaderDebugInfo = true,
            TargetGraphicsAPI = "DirectX",
        };

        var bakeResult = await bakeService.BakeAssetsAsync(bakeTargets, bakeConfig, cancellationToken).ConfigureAwait(false);
        if (bakeResult.Failed > 0)
        {
            foreach (var failedAsset in bakeResult.FailedAssets)
            {
                Logger.Error($"Failed to bake asset: {failedAsset}");
            }
        }

        return bakeResult;
    }

    private void NotifyBakedAssets(ProjectContext baseContext, IReadOnlyList<(string relativePath, string sourceFile)> targets, BakeResult bakeResult)
    {
        ProjectCacheUpdated?.Invoke();

        if (bakeResult.Succeeded <= 0 || ProjectAssetsBaked == null)
        {
            return;
        }

        var bakedGuids = new List<Guid>(targets.Count);
        for (var i = 0; i < targets.Count; i++)
        {
            var meta = _projectService.LoadMetadata(targets[i].sourceFile + ".meta");
            if (meta == null || meta.Id == Guid.Empty)
            {
                continue;
            }

            bakedGuids.Add(meta.Id);

            if (!string.IsNullOrWhiteSpace(baseContext.CacheDirectory))
            {
                var relDir = Path.GetDirectoryName(targets[i].relativePath)?.Replace('\\', '/') ?? string.Empty;
                var stem = Path.GetFileNameWithoutExtension(targets[i].relativePath);
                var extKey = string.IsNullOrEmpty(relDir) ? stem : $"{relDir}/{stem}";
                var cacheFile = Path.Combine(baseContext.CacheDirectory, extKey.Replace('/', Path.DirectorySeparatorChar));
                var subManifest = SubAssetManifest.Load(cacheFile + ".sub.json");
                if (subManifest != null)
                {
                    for (var s = 0; s < subManifest.SubAssets.Count; s++)
                    {
                        bakedGuids.Add(GuidUtility.DeriveSubAssetGuid(meta.Id, subManifest.SubAssets[s].SubPath));
                    }
                }
            }
        }

        if (bakedGuids.Count > 0)
        {
            ProjectAssetsBaked.Invoke(bakedGuids);
        }
    }

    private void SetProgress(bool isImporting, int completed, int total, string currentItem)
    {
        lock (_statusLock)
        {
            _isImporting = isImporting;
            _completedFiles = completed;
            _totalFiles = total;
            _currentItemName = currentItem;
        }
    }

    private static string ResolveTargetDirectory(string fullPrimaryAssetDir, string requestedTargetDir)
    {
        if (string.IsNullOrWhiteSpace(requestedTargetDir) || !Directory.Exists(requestedTargetDir))
        {
            return fullPrimaryAssetDir;
        }

        var fullTarget = Path.GetFullPath(requestedTargetDir);
        var rel = Path.GetRelativePath(fullPrimaryAssetDir, fullTarget);
        if (rel == "." || (!rel.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(rel)))
        {
            return fullTarget;
        }

        return fullPrimaryAssetDir;
    }

    private static string GetRelativeVirtualDirectory(string fullPrimaryAssetDir, string targetDirectory)
    {
        var rel = Path.GetRelativePath(fullPrimaryAssetDir, targetDirectory);
        return rel == "." ? string.Empty : rel;
    }

    private static List<(string sourceFile, string virtualSubDir)> ExpandDroppedPaths(string[] droppedPaths, string baseVirtualDir)
    {
        var results = new List<(string sourceFile, string virtualSubDir)>();

        for (var i = 0; i < droppedPaths.Length; i++)
        {
            var rawPath = droppedPaths[i];
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            if (File.Exists(rawPath))
            {
                if (!rawPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add((rawPath, baseVirtualDir));
                }
            }
            else if (Directory.Exists(rawPath))
            {
                var rootFolderName = Path.GetFileName(rawPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                var folderVirtualBase = string.IsNullOrEmpty(baseVirtualDir) ? rootFolderName : Path.Combine(baseVirtualDir, rootFolderName);

                foreach (var file in Directory.EnumerateFiles(rawPath, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var relDir = Path.GetDirectoryName(Path.GetRelativePath(rawPath, file));
                    var subDir = string.IsNullOrEmpty(relDir) || relDir == "." ? folderVirtualBase : Path.Combine(folderVirtualBase, relDir);
                    results.Add((file, subDir));
                }
            }
        }

        return results;
    }

    private static ProjectContext BuildBakeContext(ProjectContext baseContext)
    {
        var engineAssetDir = ProjectService.ResolveEngineAssetDirectory();
        if (string.IsNullOrEmpty(engineAssetDir))
        {
            return baseContext;
        }

        for (var i = 0; i < baseContext.AssetDirectories.Count; i++)
        {
            if (string.Equals(Path.GetFullPath(baseContext.AssetDirectories[i]), engineAssetDir, StringComparison.OrdinalIgnoreCase))
            {
                return baseContext;
            }
        }

        var combinedDirs = new string[baseContext.AssetDirectories.Count + 1];
        combinedDirs[0] = engineAssetDir;
        for (var i = 0; i < baseContext.AssetDirectories.Count; i++)
        {
            combinedDirs[i + 1] = baseContext.AssetDirectories[i];
        }

        return new ProjectContext(baseContext.Project, combinedDirs, baseContext.CacheDirectory, baseContext.BuildDirectory);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _queue.Writer.TryComplete();
        _cts.Cancel();

        try
        {
            _workerTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }

        _cts.Dispose();
    }
}
