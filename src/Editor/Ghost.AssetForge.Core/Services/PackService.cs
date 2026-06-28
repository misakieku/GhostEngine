using Ghost.Core;
using K4os.Compression.LZ4.Streams;
using ZstdSharp;

namespace Ghost.AssetForge.Core.Services;

public class PackService
{
    private readonly ProjectService _projectService;

    public PackService(ProjectService projectService)
    {
        _projectService = projectService;
    }

    public event Action<int, int>? OnProgress;

    private static string GetPackFileName(int index)
    {
        return $"pack_{index:D4}.pack";
    }

    public async Task PackProjectAsync(CancellationToken cancellationToken = default)
    {
        var project = _projectService.CurrentProject ?? throw new InvalidOperationException("No project loaded.");
        var cacheDir = Path.Combine(project.RootPath, "Cache");
        var assetDir = Path.Combine(project.RootPath, "Asset");
        var buildDir = Path.Combine(project.RootPath, "Build");

        if (!Directory.Exists(cacheDir))
        {
            Logger.Warning("No Cache directory found. Bake first.");
            return;
        }

        var allAssetFiles = Directory.GetFiles(assetDir, "*", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".meta"))
            .ToArray();

        var manifest = new Manifest
        {
            CompressionMethod = project.BakeSettings.Compression
        };

        long currentPackSize = 0;
        var packIndex = 0;

        var currentPackName = GetPackFileName(packIndex);
        var currentPackPath = Path.Combine(buildDir, currentPackName);

        FileStream? currentPackStream = null;

        try
        {
            var completed = 0;
            var total = allAssetFiles.Length;
            OnProgress?.Invoke(completed, total);

            foreach (var assetFile in allAssetFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(assetDir, assetFile);
                var destPath = Path.Combine(cacheDir, relativePath);
                var destDir = Path.GetDirectoryName(destPath) ?? cacheDir;
                var cacheFile = Path.Combine(destDir, Path.GetFileNameWithoutExtension(assetFile));

                if (!File.Exists(cacheFile))
                {
                    Logger.Warning($"Cache file for {relativePath} not found. Please bake first.");
                    continue;
                }

                var metaFile = assetFile + ".meta";
                var metadata = _projectService.LoadMetadata(metaFile);
                if (metadata == null)
                {
                    Logger.Error($"Missing metadata for {assetFile}");
                    continue;
                }

                var cacheFileInfo = new FileInfo(cacheFile);

                var dir = Path.GetDirectoryName(relativePath) ?? string.Empty;
                var nameWithoutExt = Path.GetFileNameWithoutExtension(assetFile);
                var virtualPath = Path.Combine(dir, nameWithoutExt).Replace('\\', '/');

                // Should we start a new pack file?
                // Size estimate (uncompressed): header + raw data. 
                // Since we compress during packing, actual size will be smaller, but we can use raw size for threshold
                if (currentPackStream != null && currentPackSize + cacheFileInfo.Length > project.BakeSettings.ChunkSizeThreshold)
                {
                    await currentPackStream.DisposeAsync();
                    currentPackStream = null;
                    packIndex++;
                    currentPackName = GetPackFileName(packIndex);
                    currentPackPath = Path.Combine(buildDir, currentPackName);
                    currentPackSize = 0;
                }

                if (currentPackStream == null)
                {
                    Logger.Info($"Creating new pack file: {currentPackName}");
                    currentPackStream = new FileStream(currentPackPath, FileMode.Create, FileAccess.Write);
                }

                var offset = currentPackStream.Position;

                // Compress and write payload
                using var fsIn = new FileStream(cacheFile, FileMode.Open, FileAccess.Read);
                var compressStream = project.BakeSettings.Compression switch
                {
                    CompressionMethod.None => currentPackStream,
                    CompressionMethod.Zstd => new CompressionStream(currentPackStream, leaveOpen: true),
                    CompressionMethod.LZ4 => (Stream)LZ4Stream.Encode(currentPackStream, leaveOpen: true),
                    _ => throw new ArgumentOutOfRangeException(nameof(project.BakeSettings.Compression), project.BakeSettings.Compression, null)
                };

                await fsIn.CopyToAsync(compressStream, cancellationToken);

                if (project.BakeSettings.Compression != CompressionMethod.None)
                {
                    await compressStream.DisposeAsync();
                }

                var size = currentPackStream.Position - offset;
                currentPackSize = currentPackStream.Position;

                manifest.AddAsset(virtualPath, new AssetInfo
                {
                    AssetId = metadata.Id,
                    AssetType = metadata.Type,
                    PackFileName = currentPackName,
                    Offset = offset,
                    Size = size
                });

                Logger.Info($"Packed {virtualPath} into {currentPackName} (Offset: {offset}, Size: {size})");

                completed++;
                OnProgress?.Invoke(completed, total);
            }

            // Write Manifest
            var manifestPath = Path.Combine(buildDir, "manifest.json");
            await manifest.SaveToDiskAsync(manifestPath, cancellationToken);

            Logger.Info("Wrote manifest.json");
            Logger.Info("Packing complete.");
        }
        finally
        {
            if (currentPackStream != null)
            {
                await currentPackStream.DisposeAsync();
            }
        }
    }
}
