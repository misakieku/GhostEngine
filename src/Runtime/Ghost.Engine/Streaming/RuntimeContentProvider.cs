using Ghost.Core;
using Ghost.Core.Utilities;
using K4os.Compression.LZ4.Streams;
using ZstdSharp;

namespace Ghost.Engine.Streaming;

public class RuntimeContentProvider : IContentProvider
{
    private readonly Manifest _manifest;
    private readonly Dictionary<Guid, AssetInfo> _guidToInfo;

    public RuntimeContentProvider(string manifestPath)
    {
        _manifest = Manifest.LoadFromDiskAsync(manifestPath).GetAwaiter().GetResult();
        _guidToInfo = new Dictionary<Guid, AssetInfo>(_manifest.Assets.Count);

        foreach (var (_, location) in _manifest.Assets)
        {
            _guidToInfo[location.AssetId] = location;
        }
    }

    public Guid VirtualPathToGuid(string path)
    {
        return _manifest.Assets.GetValueOrDefault(path).AssetId;
    }

    public AssetType GetAssetType(Guid guid)
    {
        return _guidToInfo.GetValueOrDefault(guid).AssetType;
    }

    public Guid[] GetDependencies(Guid guid)
    {
        return Array.Empty<Guid>();
    }

    public bool HasAsset(Guid guid)
    {
        return _guidToInfo.ContainsKey(guid);
    }

    public Result<AssetReadData> OpenReadAsync(Guid guid, CancellationToken token = default)
    {
        if (_guidToInfo.TryGetValue(guid, out var info))
        {
            var fs = new FileStream(info.PackFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var subStream = new SubReadOnlyStream(fs, info.Offset, info.Size, leaveOpen: false);

            var decompressedStream = _manifest.CompressionMethod switch
            {
                CompressionMethod.None => fs,
                CompressionMethod.Zstd => new DecompressionStream(subStream, leaveOpen: false),
                CompressionMethod.LZ4 => (Stream)LZ4Stream.Decode(subStream, leaveOpen: false),
                _ => throw new NotSupportedException($"Unsupported compression method: {_manifest.CompressionMethod}")
            };

            return new AssetReadData
            {
                assetId = info.AssetId,
                assetType = info.AssetType,
                stream = decompressedStream,
            };
        }

        return Result.Failure($"Asset with GUID {guid} not found in the manifest.");
    }
}
