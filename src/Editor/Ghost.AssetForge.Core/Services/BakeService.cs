using Ghost.Core;

namespace Ghost.AssetForge.Core.Services;

public class BakeService
{
    public event Action<int, int>? OnProgress;

    public async Task BakeProjectAsync(CancellationToken cancellationToken = default)
    {
        var project = ProjectService.Instance.CurrentProject ?? throw new InvalidOperationException("No project loaded.");
        var assetDir = Path.Combine(project.RootPath, "Asset");
        var cacheDir = Path.Combine(project.RootPath, "Cache");

        if (!Directory.Exists(assetDir))
        {
            Logger.Error("No Asset directory found.");
            return;
        }

        var allFiles = Directory.GetFiles(assetDir, "*.*", SearchOption.AllDirectories)
                                .Where(f => !f.EndsWith(".meta")).ToList();

        // Duplicate check
        var baseNames = new HashSet<string>();
        foreach (var file in allFiles)
        {
            var relativePath = Path.GetRelativePath(assetDir, file);
            var dir = Path.GetDirectoryName(relativePath) ?? string.Empty;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(file);
            var key = Path.Combine(dir, nameWithoutExt).Replace('\\', '/');

            if (!baseNames.Add(key))
            {
                var err = $"Fatal Error: Duplicate asset name '{key}' found in folder.";
                Logger.Error(err);
                throw new InvalidOperationException(err);
            }
        }

        var completed = 0;
        var total = allFiles.Count;
        OnProgress?.Invoke(completed, total);

        foreach (var sourceFile in allFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(assetDir, sourceFile);
            var destPath = Path.Combine(cacheDir, relativePath);
            var destDir = Path.GetDirectoryName(destPath);
            if (destDir != null && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Path to cache file without extension (cache name = original name without extension)
            var cacheFile = Path.Combine(destDir ?? cacheDir, Path.GetFileNameWithoutExtension(sourceFile));
            var metaFile = sourceFile + ".meta";

            var needsBake = true;
            if (File.Exists(cacheFile) && File.Exists(metaFile))
            {
                var srcTime = File.GetLastWriteTimeUtc(sourceFile);
                var metaTime = File.GetLastWriteTimeUtc(metaFile);
                var cacheTime = File.GetLastWriteTimeUtc(cacheFile);

                if (cacheTime >= srcTime && cacheTime >= metaTime)
                {
                    needsBake = false;
                }
            }

            if (needsBake)
            {
                Logger.Info($"Baking {relativePath}...");
                var metadata = ProjectService.Instance.LoadMetadata(metaFile);
                if (metadata == null)
                {
                    Logger.Error($"Missing metadata for {sourceFile}. Skip.");
                    continue;
                }

                if (metadata.Settings == null)
                {
                    Logger.Error($"Missing settings in metadata for {sourceFile}. Skip.");
                    continue;
                }

                var baker = BakerRegistry.Instance.GetBaker(metadata.Type);
                if (baker == null)
                {
                    Logger.Warning($"No baker for type {metadata.Type}. Skip.");
                    continue;
                }

                using var fs = new FileStream(cacheFile, FileMode.Create, FileAccess.Write);

                await baker.BakeAssetAsync(sourceFile, fs, metadata.Settings, cancellationToken);
            }
            else
            {
                Logger.Info($"Skipping {relativePath} (Up to date)");
            }

            completed++;
            OnProgress?.Invoke(completed, total);
        }

        Logger.Info("Baking complete.");
    }
}
