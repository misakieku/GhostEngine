using Ghost.AssetForge.Core.Models;
using Ghost.Core;
using Ghost.Core.Utilities;
using K4os.Compression.LZ4.Streams;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using ZstdSharp;

namespace Ghost.AssetForge.Core.Services;

public class PackService
{
    private readonly ProjectContext _context;
    private readonly BakerRegistry _bakerRegistry;

    public PackService(ProjectContext context, BakerRegistry? bakerRegistry = null)
    {
        _context = context;
        _bakerRegistry = bakerRegistry ?? new BakerRegistry();
    }

    public event Action<int, int>? OnProgress;

    private static string GetPackFileName(int index)
    {
        return $"pack_{index:D4}.pack";
    }

    private static string ReadUtf8String(Stream stream, long assetStart, long offset, uint size)
    {
        if (offset < 0 || size > int.MaxValue || offset > stream.Length - assetStart || size > stream.Length - assetStart - offset)
        {
            throw new InvalidDataException("Shader catalog contains an invalid string range.");
        }

        var position = stream.Position;
        stream.Position = assetStart + offset;
        var bytes = new byte[(int)size];
        stream.ReadExactly(bytes);
        stream.Position = position;
        return Encoding.UTF8.GetString(bytes);
    }

    private static ShaderCatalogEntry ReadShaderCatalogEntry(string cacheFile, Guid assetId)
    {
        using var stream = new FileStream(cacheFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        var assetStart = CacheFileHeader.SIZE;
        stream.Position = assetStart;

        var header = stream.Read<ShaderContentHeader>();
        if (header.magic != ShaderContentHeader.MAGIC || header.version != ShaderContentHeader.VERSION)
        {
            throw new InvalidDataException($"Shader cache '{cacheFile}' uses an unsupported content format.");
        }

        if (header.passCount > 16)
        {
            throw new InvalidDataException($"Shader cache '{cacheFile}' contains {header.passCount} passes; at most 16 are supported.");
        }

        var passes = new ShaderCatalogPass[header.passCount];
        var nextPassOffset = header.nameOffset + header.nameSize;
        for (var i = 0; i < passes.Length; i++)
        {
            if (nextPassOffset < 0 || nextPassOffset > stream.Length - assetStart - Unsafe.SizeOf<ShaderContentHeader.PassHeader>())
            {
                throw new InvalidDataException($"Shader cache '{cacheFile}' contains an invalid pass header range.");
            }

            stream.Position = assetStart + nextPassOffset;
            var pass = stream.Read<ShaderContentHeader.PassHeader>();

            ShaderCatalogPassVariant[] variants;
            ulong[] defaultBytecodeHashes;

            if (pass.variantCount > 1)
            {
                var variantHeaders = new ShaderContentHeader.VariantHeader[pass.variantCount];
                stream.Position = assetStart + pass.dataOffset;
                for (var v = 0; v < pass.variantCount; v++)
                {
                    variantHeaders[v] = stream.Read<ShaderContentHeader.VariantHeader>();
                }

                variants = new ShaderCatalogPassVariant[pass.variantCount];
                for (var v = 0; v < pass.variantCount; v++)
                {
                    var vHeader = variantHeaders[v];
                    var kwStr = ReadUtf8String(stream, assetStart, vHeader.keywordOffset, vHeader.keywordSize);
                    var keywords = string.IsNullOrEmpty(kwStr) ? Array.Empty<string>() : kwStr.Split(';');

                    var vBytecodeHashes = new ulong[vHeader.entryPointCount];
                    stream.Position = assetStart + vHeader.dataOffset;
                    for (var ep = 0; ep < vHeader.entryPointCount; ep++)
                    {
                        var epHeader = stream.Read<ShaderContentHeader.EntryPointHeader>();
                        vBytecodeHashes[ep] = epHeader.bytecodeHash;
                    }

                    variants[v] = new ShaderCatalogPassVariant
                    {
                        Keywords = keywords,
                        BytecodeHashes = vBytecodeHashes,
                    };
                }

                defaultBytecodeHashes = variants.Length > 0 ? variants[0].BytecodeHashes : Array.Empty<ulong>();
            }
            else
            {
                var bytecodeHashes = new ulong[pass.entryPointCount];
                var entryHeadersSize = pass.entryPointCount * Unsafe.SizeOf<ShaderContentHeader.EntryPointHeader>();
                if (pass.dataSize >= entryHeadersSize && assetStart + pass.dataOffset + entryHeadersSize <= stream.Length)
                {
                    var entryHeadersOffset = assetStart + pass.dataOffset;
                    stream.Position = entryHeadersOffset;
                    for (var ep = 0; ep < pass.entryPointCount; ep++)
                    {
                        var entryHeader = stream.Read<ShaderContentHeader.EntryPointHeader>();
                        bytecodeHashes[ep] = entryHeader.bytecodeHash;
                    }
                }

                defaultBytecodeHashes = bytecodeHashes;
                variants = new[]
                {
                    new ShaderCatalogPassVariant
                    {
                        Keywords = Array.Empty<string>(),
                        BytecodeHashes = bytecodeHashes,
                    }
                };
            }

            passes[i] = new ShaderCatalogPass
            {
                Name = ReadUtf8String(stream, assetStart, pass.nameOffset, pass.nameSize),
                Semantic = pass.semantic,
                StageMask = pass.stageMask,
                EntryPointCount = pass.entryPointCount,
                PassId = pass.passId,
                LocalPipeline = pass.localPipeline,
                ShadingModelId = pass.shadingModelId,
                BytecodeHashes = defaultBytecodeHashes,
                Variants = variants,
            };
            nextPassOffset = pass.dataOffset + pass.dataSize;
        }

        return new ShaderCatalogEntry
        {
            AssetId = assetId,
            ShaderType = header.shaderType,
            Name = ReadUtf8String(stream, assetStart, header.nameOffset, header.nameSize),
            ShaderId = header.shaderId,
            FamilyId = header.familyId,
            LayoutHash = header.layoutHash,
            PropertyBufferSize = header.propertyBufferSize,
            Passes = passes,
        };
    }

    private struct StrippedVariantInfo
    {
        public byte[] KeywordBytes;
        public ShaderContentHeader.EntryPointHeader[] EntryPoints;
    }

    private struct StrippedPassInfo
    {
        public ShaderContentHeader.PassHeader Header;
        public byte[] NameBytes;
        public ShaderContentHeader.EntryPointHeader[]? SingleEntryPoints;
        public StrippedVariantInfo[]? Variants;
    }

    private static bool TryCreateStrippedShaderStream(string cacheFile, Dictionary<ulong, byte[]> uniqueBytecodes, [NotNullWhen(true)] out MemoryStream? strippedStream)
    {
        strippedStream = null;
        try
        {
            using var stream = new FileStream(cacheFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            var assetStart = CacheFileHeader.SIZE;
            if (stream.Length < assetStart + Unsafe.SizeOf<ShaderContentHeader>())
            {
                return false;
            }

            stream.Position = assetStart;
            var header = stream.Read<ShaderContentHeader>();
            if (header.magic != ShaderContentHeader.MAGIC || header.version != ShaderContentHeader.VERSION)
            {
                return false;
            }

            if (header.passCount == 0 || header.passCount > 16 ||
                header.nameOffset < 0 || header.nameSize == 0 ||
                assetStart + header.nameOffset + header.nameSize > stream.Length)
            {
                return false;
            }

            var nameBytes = new byte[header.nameSize];
            stream.Position = assetStart + header.nameOffset;
            stream.ReadExactly(nameBytes);

            var strippedPasses = new StrippedPassInfo[header.passCount];
            var nextPassOffset = header.nameOffset + header.nameSize;
            var hasAnyPooled = false;

            for (var i = 0; i < header.passCount; i++)
            {
                if (nextPassOffset < 0 || assetStart + nextPassOffset + Unsafe.SizeOf<ShaderContentHeader.PassHeader>() > stream.Length)
                {
                    return false;
                }

                stream.Position = assetStart + nextPassOffset;
                var passHeader = stream.Read<ShaderContentHeader.PassHeader>();

                if (passHeader.nameOffset < 0 || assetStart + passHeader.nameOffset + passHeader.nameSize > stream.Length)
                {
                    return false;
                }

                var pName = new byte[passHeader.nameSize];
                stream.Position = assetStart + passHeader.nameOffset;
                stream.ReadExactly(pName);

                if (passHeader.variantCount > 1)
                {
                    var variantHeaders = new ShaderContentHeader.VariantHeader[passHeader.variantCount];
                    stream.Position = assetStart + passHeader.dataOffset;
                    for (var v = 0; v < passHeader.variantCount; v++)
                    {
                        variantHeaders[v] = stream.Read<ShaderContentHeader.VariantHeader>();
                    }

                    var variants = new StrippedVariantInfo[passHeader.variantCount];
                    for (var v = 0; v < passHeader.variantCount; v++)
                    {
                        var vHeader = variantHeaders[v];
                        var kwBytes = new byte[vHeader.keywordSize];
                        if (vHeader.keywordSize > 0)
                        {
                            stream.Position = assetStart + vHeader.keywordOffset;
                            stream.ReadExactly(kwBytes);
                        }

                        var entryPoints = new ShaderContentHeader.EntryPointHeader[vHeader.entryPointCount];
                        stream.Position = assetStart + vHeader.dataOffset;
                        for (var ep = 0; ep < entryPoints.Length; ep++)
                        {
                            entryPoints[ep] = stream.Read<ShaderContentHeader.EntryPointHeader>();
                            if (entryPoints[ep].bytecodeHash != 0)
                            {
                                hasAnyPooled = true;
                                if (entryPoints[ep].byteCodeSize > 0 && !uniqueBytecodes.ContainsKey(entryPoints[ep].bytecodeHash))
                                {
                                    var codeOffset = assetStart + vHeader.dataOffset + entryPoints[ep].byteCodeOffset;
                                    if (codeOffset + entryPoints[ep].byteCodeSize <= stream.Length)
                                    {
                                        var codeBytes = new byte[entryPoints[ep].byteCodeSize];
                                        var prevPos = stream.Position;
                                        stream.Position = codeOffset;
                                        stream.ReadExactly(codeBytes);
                                        stream.Position = prevPos;
                                        uniqueBytecodes[entryPoints[ep].bytecodeHash] = codeBytes;
                                    }
                                }
                            }
                        }

                        variants[v] = new StrippedVariantInfo
                        {
                            KeywordBytes = kwBytes,
                            EntryPoints = entryPoints,
                        };
                    }

                    strippedPasses[i] = new StrippedPassInfo
                    {
                        Header = passHeader,
                        NameBytes = pName,
                        Variants = variants,
                    };
                }
                else
                {
                    var entryHeadersSize = passHeader.entryPointCount * Unsafe.SizeOf<ShaderContentHeader.EntryPointHeader>();
                    if (passHeader.dataSize < entryHeadersSize ||
                        passHeader.dataOffset < 0 ||
                        assetStart + passHeader.dataOffset + entryHeadersSize > stream.Length)
                    {
                        return false;
                    }

                    var entryPoints = new ShaderContentHeader.EntryPointHeader[passHeader.entryPointCount];
                    stream.Position = assetStart + passHeader.dataOffset;
                    for (var ep = 0; ep < entryPoints.Length; ep++)
                    {
                        entryPoints[ep] = stream.Read<ShaderContentHeader.EntryPointHeader>();
                        if (entryPoints[ep].bytecodeHash != 0)
                        {
                            hasAnyPooled = true;
                            if (entryPoints[ep].byteCodeSize > 0 && !uniqueBytecodes.ContainsKey(entryPoints[ep].bytecodeHash))
                            {
                                var codeOffset = assetStart + passHeader.dataOffset + entryPoints[ep].byteCodeOffset;
                                if (codeOffset + entryPoints[ep].byteCodeSize <= stream.Length)
                                {
                                    var codeBytes = new byte[entryPoints[ep].byteCodeSize];
                                    var prevPos = stream.Position;
                                    stream.Position = codeOffset;
                                    stream.ReadExactly(codeBytes);
                                    stream.Position = prevPos;
                                    uniqueBytecodes[entryPoints[ep].bytecodeHash] = codeBytes;
                                }
                            }
                        }
                    }

                    strippedPasses[i] = new StrippedPassInfo
                    {
                        Header = passHeader,
                        NameBytes = pName,
                        SingleEntryPoints = entryPoints,
                    };
                }

                nextPassOffset = passHeader.dataOffset + passHeader.dataSize;
            }

            if (!hasAnyPooled)
            {
                return false;
            }

            var ms = new MemoryStream();
            ms.Write(header); // placeholder

            var newNameOffset = ms.Position;
            ms.Write(nameBytes);
            header.nameOffset = newNameOffset;
            header.nameSize = (uint)nameBytes.Length;

            var passHeaderPositions = new long[header.passCount];
            for (var i = 0; i < header.passCount; i++)
            {
                var passInfo = strippedPasses[i];
                var curPassHeader = passInfo.Header;
                passHeaderPositions[i] = ms.Position;
                ms.Write(curPassHeader); // placeholder

                var newPassNameOffset = ms.Position;
                ms.Write(passInfo.NameBytes);
                curPassHeader.nameOffset = newPassNameOffset;
                curPassHeader.nameSize = (uint)passInfo.NameBytes.Length;

                var newPassDataOffset = ms.Position;
                if (curPassHeader.variantCount > 1 && passInfo.Variants != null)
                {
                    var variantHeadersPos = ms.Position;
                    for (var v = 0; v < passInfo.Variants.Length; v++)
                    {
                        ms.Write(default(ShaderContentHeader.VariantHeader));
                    }

                    var newVariantHeaders = new ShaderContentHeader.VariantHeader[passInfo.Variants.Length];
                    for (var v = 0; v < passInfo.Variants.Length; v++)
                    {
                        var vInfo = passInfo.Variants[v];
                        var newKwOffset = ms.Position;
                        if (vInfo.KeywordBytes.Length > 0)
                        {
                            ms.Write(vInfo.KeywordBytes);
                        }

                        var newVarDataOffset = ms.Position;
                        for (var ep = 0; ep < vInfo.EntryPoints.Length; ep++)
                        {
                            var epHeader = vInfo.EntryPoints[ep];
                            epHeader.byteCodeOffset = 0;
                            ms.Write(epHeader);
                        }

                        newVariantHeaders[v] = new ShaderContentHeader.VariantHeader
                        {
                            entryPointCount = (uint)vInfo.EntryPoints.Length,
                            keywordCount = 0,
                            keywordOffset = newKwOffset,
                            keywordSize = (uint)vInfo.KeywordBytes.Length,
                            dataOffset = newVarDataOffset,
                            dataSize = ms.Position - newVarDataOffset,
                        };
                    }

                    var endOfPassData = ms.Position;
                    ms.Position = variantHeadersPos;
                    for (var v = 0; v < newVariantHeaders.Length; v++)
                    {
                        ms.Write(newVariantHeaders[v]);
                    }
                    ms.Position = endOfPassData;
                }
                else if (passInfo.SingleEntryPoints != null)
                {
                    for (var ep = 0; ep < passInfo.SingleEntryPoints.Length; ep++)
                    {
                        var epHeader = passInfo.SingleEntryPoints[ep];
                        epHeader.byteCodeOffset = 0;
                        ms.Write(epHeader);
                    }
                }

                curPassHeader.dataOffset = newPassDataOffset;
                curPassHeader.dataSize = ms.Position - newPassDataOffset;

                var curPos = ms.Position;
                ms.Position = passHeaderPositions[i];
                ms.Write(curPassHeader);
                ms.Position = curPos;
            }

            ms.Position = 0;
            ms.Write(header);
            ms.Position = 0;

            strippedStream = ms;
            return true;
        }
        catch
        {
            strippedStream?.Dispose();
            strippedStream = null;
            return false;
        }
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

            var uniqueBytecodes = new Dictionary<ulong, byte[]>();

            foreach (var kvp in virtualPathToFile)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var relativePath = kvp.Key;
                var assetFile = kvp.Value;
                var ext = Path.GetExtension(assetFile);
                var baker = _bakerRegistry.GetBaker(ext);

                // Skip header/include files that are dependencies and not standalone runtime assets
                if (string.Equals(ext, ".hlsl", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ext, ".h", StringComparison.OrdinalIgnoreCase))
                {
                    completed++;
                    OnProgress?.Invoke(completed, total);
                    continue;
                }

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
                if (cacheFileInfo.Length <= CacheFileHeader.SIZE)
                {
                    Logger.Error($"Cache file for {relativePath} is empty or corrupted ({cacheFileInfo.Length} bytes). Please rebake.");
                    continue;
                }

                var isShader = metadata.Type == AssetType.Shader || metadata.Type == AssetType.ComputeShader || metadata.Type == AssetType.WorkGraph;
                if (metadata.Type == AssetType.Shader)
                {
                    manifest.Shaders.Add(ReadShaderCatalogEntry(cacheFile, metadata.Id));
                }

                Stream assetPayloadStream;
                long uncompressedSize;
                if (isShader && TryCreateStrippedShaderStream(cacheFile, uniqueBytecodes, out var strippedStream))
                {
                    assetPayloadStream = strippedStream;
                    uncompressedSize = strippedStream.Length;
                }
                else
                {
                    var fs = new FileStream(cacheFile, FileMode.Open, FileAccess.Read);
                    fs.Seek(CacheFileHeader.SIZE, SeekOrigin.Begin);
                    assetPayloadStream = fs;
                    uncompressedSize = cacheFileInfo.Length - CacheFileHeader.SIZE;
                }

                try
                {
                    // Should we start a new pack file?
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

                    var shouldCompress = baker == null || baker.CompressOutput;
                    var compression = shouldCompress ? project.BakeSettings.Compression : CompressionMethod.None;

                    // Compress and write payload
                    var size = await CompressAndWriteAsync(assetPayloadStream, currentPackStream, compression, cancellationToken);
                    currentPackSize = currentPackStream.Position;

                    manifest.AddAsset(key, new AssetInfo
                    {
                        AssetId = metadata.Id,
                        AssetType = metadata.Type,
                        Compression = compression,
                        PackFileName = currentPackName,
                        Offset = offset,
                        Size = size,
                        UncompressedSize = uncompressedSize,
                    });

                    Logger.Info($"Packed {key} into {currentPackName} (Offset: {offset}, Size: {size})");
                }
                finally
                {
                    await assetPayloadStream.DisposeAsync();
                }

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

                        // Should we start a new pack file?
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
                        var subCompression = (baker != null && !baker.CompressOutput) || (sub.Type == AssetType.Mesh)
                            ? CompressionMethod.None
                            : project.BakeSettings.Compression;

                        using var subFsIn = new FileStream(subCachePath, FileMode.Open, FileAccess.Read);
                        var subSize = await CompressAndWriteAsync(subFsIn, currentPackStream, subCompression, cancellationToken);
                        currentPackSize = currentPackStream.Position;

                        var subKey = $"{key}#{sub.SubPath}";
                        var subGuid = GuidUtility.DeriveSubAssetGuid(metadata.Id, sub.SubPath);
                        if (sub.Type == AssetType.Shader)
                        {
                            manifest.Shaders.Add(ReadShaderCatalogEntry(subCachePath, subGuid));
                        }

                        manifest.AddAsset(subKey, new AssetInfo
                        {
                            AssetId = subGuid,
                            AssetType = sub.Type,
                            Compression = subCompression,
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

            if (uniqueBytecodes.Count > 0)
            {
                using var poolStream = new MemoryStream();
                var magic = ShaderBytecodePoolConstants.MAGIC;
                poolStream.Write(magic);
                var count = (uint)uniqueBytecodes.Count;
                poolStream.Write(count);
                foreach (var (hash, bytes) in uniqueBytecodes)
                {
                    var h = hash;
                    poolStream.Write(h);
                    var s = (uint)bytes.Length;
                    poolStream.Write(s);
                    poolStream.Write(bytes);
                }
                poolStream.Position = 0;

                var uncompressedPoolSize = poolStream.Length;
                if (currentPackStream != null && currentPackSize + uncompressedPoolSize > project.BakeSettings.ChunkSizeThreshold)
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

                var poolOffset = currentPackStream.Position;
                var poolSize = await CompressAndWriteAsync(poolStream, currentPackStream, project.BakeSettings.Compression, cancellationToken);
                currentPackSize = currentPackStream.Position;

                manifest.AddAsset(ShaderBytecodePoolConstants.POOL_ASSET_KEY, new AssetInfo
                {
                    AssetId = ShaderBytecodePoolConstants.POOL_ASSET_ID,
                    AssetType = AssetType.Shader,
                    Compression = project.BakeSettings.Compression,
                    PackFileName = currentPackName,
                    Offset = poolOffset,
                    Size = poolSize,
                    UncompressedSize = uncompressedPoolSize,
                });

                Logger.Info($"Packed {ShaderBytecodePoolConstants.POOL_ASSET_KEY} into {currentPackName} ({uniqueBytecodes.Count} unique bytecodes, Offset: {poolOffset}, Size: {poolSize})");
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
            CompressionMethod.LZ4 => LZ4Stream.Encode(dst, leaveOpen: true),
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
