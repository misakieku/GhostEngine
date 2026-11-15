using Ghost.Core;

namespace Ghost.AssetForge.Core.Models;

public class BakeSettings
{
    public CompressionMethod Compression { get; set; } = CompressionMethod.Zstd;

    public long ChunkSizeThreshold { get; set; } = 1024L * 1024L * 1024L;
}
