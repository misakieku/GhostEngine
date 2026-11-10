using Ghost.Core;
using K4os.Compression.LZ4;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Collections.Concurrent;
using ZstdSharp;

namespace Ghost.Engine.Streaming;

public class RuntimeContentProvider : IContentProvider
{
    private readonly Manifest _manifest;
    private readonly Dictionary<Guid, AssetInfo> _guidToInfo;
    private readonly Dictionary<Guid, string> _guidToVirtualPath;
    private readonly ConcurrentDictionary<string, PackFileMapping> _packFiles;
    private readonly string _packDirectory;

    public IReadOnlyList<ShaderCatalogEntry> ShaderCatalog => _manifest.Shaders;

    public RuntimeContentProvider(string manifestPath)
    {
        _manifest = Manifest.LoadFromDiskAsync(manifestPath).GetAwaiter().GetResult();
        _guidToInfo = new Dictionary<Guid, AssetInfo>(_manifest.Assets.Count);
        _guidToVirtualPath = new Dictionary<Guid, string>(_manifest.Assets.Count);
        _packFiles = new ConcurrentDictionary<string, PackFileMapping>(StringComparer.Ordinal);
        _packDirectory = Path.GetDirectoryName(manifestPath) ?? string.Empty;

        foreach (var (path, location) in _manifest.Assets)
        {
            _guidToInfo[location.AssetId] = location;
            _guidToVirtualPath[location.AssetId] = path;
        }
    }

    public Guid VirtualPathToGuid(string path)
    {
        return _manifest.Assets.GetValueOrDefault(path).AssetId;
    }

    public string VirtualPathFromGuid(Guid guid)
    {
        return _guidToVirtualPath[guid];
    }

    public AssetType GetAssetType(Guid guid)
    {
        return _guidToInfo.GetValueOrDefault(guid).AssetType;
    }

    public Guid[] GetDependencies(Guid guid)
    {
        return _guidToInfo.TryGetValue(guid, out var info) && info.Dependencies != null ? info.Dependencies : Array.Empty<Guid>();
    }

    public bool HasAsset(Guid guid)
    {
        return _guidToInfo.ContainsKey(guid);
    }

    public unsafe Result<AssetPayload> OpenReadAsync(Guid guid, CancellationToken token = default)
    {
        if (!_guidToInfo.TryGetValue(guid, out var info))
        {
            return Result.Failure($"Asset with GUID {guid} not found in the manifest.");
        }

        var packPath = ResolvePackPath(info.PackFileName);
        var pack = GetOrOpenPackFile(packPath);
        if (pack is null)
        {
            return Result.Failure($"Pack file '{info.PackFileName}' could not be opened or mapped.");
        }

        if (info.Size <= 0)
        {
            return Result.Failure($"Asset with GUID {guid} has an invalid size ({info.Size}) in the manifest.");
        }

        var compression = info.Compression;
        switch (compression)
        {
            case CompressionMethod.None:
                return AssetPayload.FromMappedMemory(pack.GetPointer(info.Offset), info.Size);

            case CompressionMethod.LZ4:
            {
                var uncompressedSize = info.UncompressedSize > 0 ? info.UncompressedSize : info.Size;
                var alignedSize = MemoryUtility.AlignUp((nuint)uncompressedSize, 16);
                var memoryBlock = new MemoryBlock(alignedSize, 16, AllocationHandle.Persistent);
                var srcPtr = pack.GetPointer(info.Offset);
                var dstPtr = (byte*)memoryBlock.GetUnsafePtr();
                var decodedBytes = LZ4Codec.Decode(srcPtr, (int)info.Size, dstPtr, (int)uncompressedSize);
                if (decodedBytes < 0)
                {
                    memoryBlock.Dispose();
                    return Result.Failure($"Failed to decompress LZ4 payload for asset {guid}.");
                }

                return AssetPayload.FromAllocatedMemory(memoryBlock, decodedBytes);
            }

            case CompressionMethod.Zstd:
            {
                var uncompressedSize = info.UncompressedSize > 0 ? info.UncompressedSize : info.Size;
                var alignedSize = MemoryUtility.AlignUp((nuint)uncompressedSize, 16);
                var memoryBlock = new MemoryBlock(alignedSize, 16, AllocationHandle.Persistent);
                var srcSpan = pack.GetSpan(info.Offset, (int)info.Size);
                var dstSpan = new Span<byte>(memoryBlock.GetUnsafePtr(), (int)uncompressedSize);
                using var decompressor = new Decompressor();
                var decompressedSize = decompressor.Unwrap(srcSpan, dstSpan);
                return AssetPayload.FromAllocatedMemory(memoryBlock, decompressedSize);
            }

            default:
                return Result.Failure($"Unsupported compression method: {compression}");
        }
    }

    private string ResolvePackPath(string packFileName)
    {
        return Path.IsPathRooted(packFileName)
            ? packFileName
            : Path.Combine(_packDirectory, packFileName);
    }

    private PackFileMapping? GetOrOpenPackFile(string packPath)
    {
        try
        {
            return _packFiles.GetOrAdd(packPath, static path => new PackFileMapping(path));
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to open or map pack file '{packPath}': {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        foreach (var packFile in _packFiles.Values)
        {
            packFile?.Dispose();
        }

        _packFiles.Clear();
        GC.SuppressFinalize(this);
    }
}
