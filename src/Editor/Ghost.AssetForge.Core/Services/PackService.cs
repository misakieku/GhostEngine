using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using K4os.Compression.LZ4.Streams;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZstdSharp;

namespace Ghost.AssetForge.Core.Services;

public class PackService
{
    private readonly ProjectService _projectService;

    public PackService(ProjectService projectService)
    {
        _projectService = projectService;
    }

    private readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

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

        var allCacheFiles = Directory.GetFiles(cacheDir, "*.*", SearchOption.AllDirectories).ToList();

        // Sort files alphabetically so packing is deterministic
        allCacheFiles.Sort();

        var manifest = new Manifest
        {
            GlobalCompression = project.BakeSettings.Compression
        };

        long currentPackSize = 0;
        var packIndex = 0;

        var currentPackName = GetPackFileName(packIndex);
        var currentPackPath = Path.Combine(buildDir, currentPackName);

        FileStream? currentPackStream = null;

        try
        {
            var completed = 0;
            var total = allCacheFiles.Count;
            OnProgress?.Invoke(completed, total);

            foreach (var cacheFile in allCacheFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativePath = Path.GetRelativePath(cacheDir, cacheFile);
                var originalNameWithoutExt = Path.GetFileNameWithoutExtension(cacheFile);
                var dir = Path.GetDirectoryName(relativePath) ?? string.Empty;
                var virtualPath = Path.Combine(dir, originalNameWithoutExt).Replace('\\', '/');

                // Find original meta file to get AssetType
                // We need to search the Asset folder to find the original extension
                var searchPattern = originalNameWithoutExt + ".*.meta"; // Wait, meta file is originalFile.ext.meta
                var assetSubDir = Path.Combine(assetDir, dir);

                // Let's just find the first file that matches nameWithoutExt in assetSubDir
                var originalFiles = Directory.GetFiles(assetSubDir, originalNameWithoutExt + ".*")
                                             .Where(f => !f.EndsWith(".meta")).ToList();

                if (originalFiles.Count == 0)
                {
                    Logger.Error($"Could not find original asset for cached file {cacheFile}");
                    continue;
                }

                var originalFile = originalFiles[0];
                var metaFile = originalFile + ".meta";

                var metadata = _projectService.LoadMetadata(metaFile);
                if (metadata == null)
                {
                    Logger.Error($"Missing metadata for {originalFile}");
                    continue;
                }

                var cacheFileInfo = new FileInfo(cacheFile);

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

                var offset = (ulong)currentPackStream.Position;

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

                // Important: Close/Dispose the compression stream to flush it!
                // But don't close the underlying stream (currentPackStream)
                if (project.BakeSettings.Compression != CompressionMethod.None)
                {
                    await compressStream.DisposeAsync();
                    // Warning: Some compression streams close the underlying stream by default.
                    // We must ensure CompressorUtility.GetCompressionStream uses leaveOpen = true.
                }

                var size = (ulong)currentPackStream.Position - offset;
                currentPackSize = currentPackStream.Position;

                manifest.Assets[virtualPath] = new AssetLocation
                {
                    PackFileName = currentPackName,
                    Offset = offset,
                    Size = size
                };

                Logger.Info($"Packed {virtualPath} into {currentPackName} (Offset: {offset}, Size: {size})");

                completed++;
                OnProgress?.Invoke(completed, total);
            }

            // Write Manifest
            var manifestPath = Path.Combine(buildDir, "manifest.json");

            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, _jsonOpts), cancellationToken);
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
