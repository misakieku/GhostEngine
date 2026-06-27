using Ghost.Core;

namespace Ghost.AssetForge.Core.Models;

public record BakeSettings
{
    public CompressionMethod Compression { get; init; } = CompressionMethod.LZ4;
    public long ChunkSizeThreshold { get; init; } = 1024L * 1024L * 1024L; // Default 1GB
}
