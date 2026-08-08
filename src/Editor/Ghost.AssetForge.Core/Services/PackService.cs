using Ghost.AssetForge.Core.Models;
using Ghost.Core.Utilities;
using Ghost.Core;
using K4os.Compression.LZ4.Streams;
using ZstdSharp;

namespace Ghost.AssetForge.Core.Services;

public class PackService
{
    private readonly ProjectContext _context;
    private readonly BakerRegistry _bakerRegistry;

    public PackService(ProjectContext context, BakerRegistry bakerRegistry)
    {
        _context = context;
        _bakerRegistry = bakerRegistry;
    }

    public event Action<int, int>? OnProgress;

    private static string GetPackFileName(int index)
    {
        return $"pack_{index:D4}.pack";
    }

    public async Task PackProjectAsync(CancellationToken cancellationToken = default)
    {
        var project = _context.Project;
        var cacheDir = _context.CacheDirectory;
        var buildDir = _context.BuildDirectory;

        if (!Directory.Exists(cacheDir))
        {
            Logger.Warning("No Cache directory found. Bake first.");
            return;
        }

        var virtualPathToFile = _context.EnumerateAssetFiles();

        var allAssetFiles = virtualPathToFile.Values.ToArray();
        var allAssetVirtualPaths = virtualPathToFile.Keys.ToArray();

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

            foreach (var kvp in virtualPathToFile)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativePath = kvp.Key;
                var assetFile = kvp.Value;
                var destPath = Path.Combine(cacheDir, relativePath);
                var destDir = Path.GetDirectoryName(destPath) ?? cacheDir;
                var cacheFile = Path.Combine(destDir, Path.GetFileNameWithoutExtension(assetFile));

                if (!File.Exists(cacheFile))
                {
                    Logger.Warning($"Cache file for {relativePath} not found. Please bake first.");
                    continue;
                }

                var metaFile = assetFile + ".meta";
                var metadata = _context.LoadMetadata(metaFile);
                if (metadata == null)
                {
                    Logger.Error($"Missing metadata for {assetFile}");
                    continue;
                }

                // relative path without extension
                var dir = Path.GetDirectoryName(relativePath) ?? string.Empty;
                var nameWithoutExt = Path.GetFileNameWithoutExtension(relativePath);
                var key = Path.Combine(dir, nameWithoutExt).Replace('\\', '/');
                var cacheFileInfo = new FileInfo(cacheFile);
                // Cache files start with a 16-byte CacheFileHeader; it is not part of the
                // compressed payload, so exclude it from the size estimate.
                var uncompressedSize = Math.Max(0, cacheFileInfo.Length - CacheFileHeader.SIZE);

                // Should we start a new pack file?
                // uncompressedSize is the size of the *uncompressed* cache payload (the
                // CacheFileHeader was stripped from the file length above), so it is a
                // conservative upper-bound estimate of the bytes actually written to the
                // pack: LZ4/Zstd typically shrink data, and per-asset frame overhead is
                // negligible next to the GB-scale ChunkSizeThreshold. Using this estimate
                // guarantees pack files never meaningfully overshoot the configured threshold.
                if (currentPackStream != null && currentPackSize + uncompressedSize > project.BakeSettings.ChunkSizeThreshold)
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
                    new PackFileHeader().WriteTo(currentPackStream);
                }

                var offset = currentPackStream.Position;

                // Compress and write payload (seek past the 16-byte CacheFileHeader)
                using var fsIn = new FileStream(cacheFile, FileMode.Open, FileAccess.Read);
                fsIn.Seek(CacheFileHeader.SIZE, SeekOrigin.Begin);
                var size = await CompressAndWriteAsync(fsIn, currentPackStream, project.BakeSettings.Compression, cancellationToken);
                currentPackSize = currentPackStream.Position;

                manifest.AddAsset(key, new AssetInfo
                {
                    AssetId = metadata.Id,
                    AssetType = metadata.Type,
                    PackFileName = currentPackName,
                    Offset = offset,
                    Size = size,
                    UncompressedSize = uncompressedSize,
                });

                Logger.Info($"Packed {key} into {currentPackName} (Offset: {offset}, Size: {size})");

                // Pack sub-assets
                var subManifestPath = cacheFile + ".sub.json";
                var subManifest = SubAssetManifest.Load(subManifestPath);
                if (subManifest != null)
                {
                    var subAssetCacheDir = cacheFile + ".sub";
                    foreach (var sub in subManifest.SubAssets)
                    {
                        var subCachePath = Path.Combine(subAssetCacheDir, sub.SubPath.Replace('/', Path.DirectorySeparatorChar));
                        if (!File.Exists(subCachePath)) continue;

                        var subFileInfo = new FileInfo(subCachePath);
                        var subUncompressedSize = subFileInfo.Length;

                        // Should we start a new pack file? Same conservative upper-bound
                        // reasoning as the main asset loop: subUncompressedSize over-estimates
                        // the compressed bytes actually written (LZ4/Zstd typically shrink data;
                        // frame overhead is negligible vs the GB-scale threshold).
                        if (currentPackStream != null && currentPackSize + subUncompressedSize > project.BakeSettings.ChunkSizeThreshold)
                        {
                            await currentPackStream.DisposeAsync();
                            packIndex++;
                            currentPackName = GetPackFileName(packIndex);
                            currentPackPath = Path.Combine(buildDir, currentPackName);
                            Logger.Info($"Creating new pack file: {currentPackName}");
                            currentPackStream = new FileStream(currentPackPath, FileMode.Create, FileAccess.Write);
                            new PackFileHeader().WriteTo(currentPackStream);
                            currentPackSize = 0;
                        }

                        var subOffset = currentPackStream!.Position;
                        using var subFsIn = new FileStream(subCachePath, FileMode.Open, FileAccess.Read);
                        var subSize = await CompressAndWriteAsync(subFsIn, currentPackStream, project.BakeSettings.Compression, cancellationToken);
                        currentPackSize = currentPackStream.Position;

                        var subKey = $"{key}#{sub.SubPath}";
                        manifest.AddAsset(subKey, new AssetInfo
                        {
                            AssetId = GuidUtility.DeriveSubAssetGuid(metadata.Id, sub.SubPath),
                            AssetType = sub.Type,
                            PackFileName = currentPackName,
                            Offset = subOffset,
                            Size = subSize,
                            UncompressedSize = subUncompressedSize,
                        });

                        Logger.Info($"Packed {subKey} into {currentPackName} (Offset: {subOffset}, Size: {subSize})");
                    }
                }

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

    private static async Task<long> CompressAndWriteAsync(Stream src, Stream dst, CompressionMethod compression, CancellationToken cancellationToken)
    {
        var startPos = dst.Position;
        var compressStream = compression switch
        {
            CompressionMethod.None => dst,
            CompressionMethod.Zstd => new CompressionStream(dst, leaveOpen: true),
            CompressionMethod.LZ4 => (Stream)LZ4Stream.Encode(dst, leaveOpen: true),
            _ => throw new ArgumentOutOfRangeException(nameof(compression), compression, null)
        };

        await src.CopyToAsync(compressStream, cancellationToken);

        if (compression != CompressionMethod.None)
        {
            await compressStream.DisposeAsync();
        }

        return dst.Position - startPos;
    }
}
