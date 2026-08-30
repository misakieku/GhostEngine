using Ghost.AssetForge.Core.Bakers;
using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.DSL.Models;

namespace Ghost.AssetForge.Core.Services;

public class BakeService
{
    private readonly ProjectContext _context;
    private readonly BakerRegistry _bakerRegistry;
    private readonly ShaderMetadata _shaderMetadata;

    private enum BakeOutcome
    {
        Succeeded,
        Skipped,
        Failed
    }

    public BakeService(ProjectContext context, BakerRegistry bakerRegistry)
    {
        _context = context;
        _bakerRegistry = bakerRegistry;
        _shaderMetadata = new ShaderMetadata();

        foreach (var path in _context.ShaderMetadataPaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var deserialized = System.Text.Json.JsonSerializer.Deserialize<ShaderMetadata>(json);
                    if (deserialized != null)
                    {
                        _shaderMetadata.Merge(deserialized);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to load shader metadata from {path}: {ex.Message}");
                }
            }
        }
    }

    public event Action<int, int>? OnProgress;

    /// <summary>
    /// Bakes every asset in the project's asset directories into the cache directory.
    /// Assets are baked in parallel (up to one task per CPU core); shader bakes are
    /// serialized internally by <see cref="ShaderBaker"/> because DXC is not
    /// thread-safe. Returns a <see cref="BakeResult"/> summarizing the run.
    /// </summary>
    public async Task<BakeResult> BakeProjectAsync(CancellationToken cancellationToken = default)
    {
        // Map VirtualPath -> AbsolutePath. Later directories overwrite earlier ones.
        var virtualPathToFile = _context.EnumerateAssetFiles();

        if (virtualPathToFile.Count == 0)
        {
            Logger.Error("No files found in any Asset directory.");
            return new BakeResult(0, 0, 0, 0, Array.Empty<string>());
        }

        // Duplicate check (by basename) as a pre-scan BEFORE any baking starts.
        // Collecting every duplicate up front avoids the old behaviour of throwing
        // mid-loop and leaving partially-written cache files behind.
        var duplicates = FindDuplicateBaseNames(virtualPathToFile);
        if (duplicates.Count > 0)
        {
            var duplicateFailedAssets = new List<string>();
            foreach (var (key, files) in duplicates)
            {
                Logger.Error($"Duplicate asset name '{key}' found in folder. Files: {string.Join(", ", files)}");
                duplicateFailedAssets.AddRange(files);
            }

            var duplicateFailedCount = duplicateFailedAssets.Count;
            return new BakeResult(virtualPathToFile.Count, 0, virtualPathToFile.Count - duplicateFailedCount, duplicateFailedCount, duplicateFailedAssets);
        }

        var total = virtualPathToFile.Count;
        var completed = 0;
        var succeeded = 0;
        var skipped = 0;
        var failed = 0;
        var failedAssets = new List<string>();
        var failedAssetsLock = new object();

        OnProgress?.Invoke(0, total);

        await Parallel.ForEachAsync(
            virtualPathToFile,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            },
            async (kvp, ct) =>
            {
                var outcome = await BakeSingleAssetAsync(kvp.Key, kvp.Value, ct).ConfigureAwait(false);

                switch (outcome)
                {
                    case BakeOutcome.Succeeded:
                        Interlocked.Increment(ref succeeded);
                        break;
                    case BakeOutcome.Skipped:
                        Interlocked.Increment(ref skipped);
                        break;
                    case BakeOutcome.Failed:
                        Interlocked.Increment(ref failed);
                        lock (failedAssetsLock)
                        {
                            failedAssets.Add(kvp.Key);
                        }
                        break;
                }

                var current = Interlocked.Increment(ref completed);
                OnProgress?.Invoke(current, total);
            }).ConfigureAwait(false);

        Logger.Info("Baking complete.");
        return new BakeResult(total, succeeded, skipped, failed, failedAssets);
    }

    private async Task<BakeOutcome> BakeSingleAssetAsync(string relativePath, string sourceFile, CancellationToken cancellationToken)
    {
        var cacheDir = _context.CacheDirectory;
        var destPath = Path.Combine(cacheDir, relativePath);
        var destDir = Path.GetDirectoryName(destPath);
        if (destDir != null && !Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        // Path to cache file without extension (cache name = original name without extension)
        var ext = Path.GetExtension(sourceFile);
        var cacheFile = Path.Combine(destDir ?? cacheDir, Path.GetFileNameWithoutExtension(sourceFile));
        var metaFile = sourceFile + ".meta";

        var baker = _bakerRegistry.GetBaker(ext);
        var settingsType = _bakerRegistry.GetSettingsType(ext);

        var needsBake = true;
        if (baker != null && settingsType != null && File.Exists(cacheFile) && File.Exists(metaFile))
        {
            var fileinfo = new FileInfo(cacheFile);
            if (fileinfo.Length != 0)
            {
                var srcTime = File.GetLastWriteTimeUtc(sourceFile);
                var metaTime = File.GetLastWriteTimeUtc(metaFile);
                var cacheTime = File.GetLastWriteTimeUtc(cacheFile);

                if (cacheTime >= srcTime && cacheTime >= metaTime)
                {
                    // Timestamps say the cache file is up to date, but it may have been
                    // produced by an older baker version or a different settings type.
                    // Validate the embedded CacheFileHeader (magic + baker version) and
                    // force a rebake on any mismatch; otherwise stale content could be
                    // packed silently and crash at runtime.
                    var expectedBakerVersion = CacheFileHeader.ComputeBakerVersion(baker.GetType(), settingsType);
                    using var headerFs = new FileStream(cacheFile, FileMode.Open, FileAccess.Read, FileShare.Read);
                    if (CacheFileHeader.TryReadFrom(headerFs, out var header)
                        && header.magic == CacheFileHeader.MAGIC
                        && header.bakerVersion == expectedBakerVersion)
                    {
                        needsBake = false;
                    }
                }
            }
        }

        if (!needsBake)
        {
            Logger.Info($"Skipping {relativePath} (Up to date)");
            return BakeOutcome.Skipped;
        }

        Logger.Info($"Baking {relativePath}...");
        var metadata = _context.LoadMetadata(metaFile);

        if (baker == null)
        {
            Logger.Warning($"No baker for {ext}. Skip.");
            return BakeOutcome.Skipped;
        }

        if (metadata == null)
        {
            metadata = new AssetMetadata
            {
                Id = Guid.NewGuid(),
                Type = _bakerRegistry.DetectAssetType(ext),
                Settings = _bakerRegistry.CreateDefaultSettings(ext)
            };

            _context.SaveMetadata(metaFile, metadata);
            Logger.Info($"Created default metadata for {relativePath}.");
        }

        if (metadata.Settings == null)
        {
            Logger.Error($"Missing settings in metadata for {sourceFile}. Skip.");
            return BakeOutcome.Failed;
        }

        var ctx = new AssetBakerContext
        {
            ShaderMetadata = _shaderMetadata,
            AssetDirectories = _context.AssetDirectories,
        };

        var subAssetCacheDir = cacheFile + ".sub";
        ctx.SubAssetStreamFactory = subPath =>
        {
            var subCachePath = Path.Combine(subAssetCacheDir, subPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(subCachePath)!);
            return new FileStream(subCachePath, FileMode.Create, FileAccess.Write);
        };

        try
        {
            await using var fs = new FileStream(cacheFile, FileMode.Create, FileAccess.Write);

            // Write the CacheFileHeader (magic + baker version) before the baker's payload
            // so future bakes can detect incompatible content formats and force a rebake.
            // settingsType is guaranteed non-null here: BakerRegistry registers the settings
            // type alongside the baker, and we returned Skipped above when the baker is null.
            var cacheHeader = new CacheFileHeader
            {
                bakerVersion = CacheFileHeader.ComputeBakerVersion(baker.GetType(), settingsType!)
            };
            cacheHeader.WriteTo(fs);

            await baker.BakeAssetAsync(sourceFile, fs, metadata.Settings, ctx, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to bake {relativePath}: {ex.Message}");
            return BakeOutcome.Failed;
        }

        if (ctx.SubAssets.Count > 0)
        {
            var subManifest = new SubAssetManifest();
            foreach (var sub in ctx.SubAssets)
                subManifest.SubAssets.Add(new SubAssetManifest.SubAssetRecord(sub.SubPath, sub.Type));
            subManifest.Save(cacheFile + ".sub.json");

            foreach (var sub in ctx.SubAssets)
                Logger.Info($"  Sub-asset: {relativePath}#{sub.SubPath} ({sub.Type})");
        }

        return BakeOutcome.Succeeded;
    }

    private static List<KeyValuePair<string, List<string>>> FindDuplicateBaseNames(Dictionary<string, string> virtualPathToFile)
    {
        var baseNames = new Dictionary<string, List<string>>();
        foreach (var virtualPath in virtualPathToFile.Keys)
        {
            var dir = Path.GetDirectoryName(virtualPath) ?? string.Empty;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(virtualPath);
            var key = Path.Combine(dir, nameWithoutExt).Replace('\\', '/');

            if (!baseNames.TryGetValue(key, out var files))
            {
                files = new List<string>();
                baseNames[key] = files;
            }
            files.Add(virtualPath);
        }

        return baseNames.Where(kvp => kvp.Value.Count > 1).ToList();
    }
}
