using Ghost.Core;
using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

public record Project
{
    public string Name { get; init; } = "New Project";
    public string ThumbnailPath { get; init; } = string.Empty;
    public CompressionMethod Compression { get; init; } = CompressionMethod.LZ4;
    public long ChunkSizeThreshold { get; init; } = 1024L * 1024L * 1024L; // Default 1GB

    public BakeSettings BakeSettings { get; init; } = new BakeSettings();

    // Non-serialized property
    [JsonIgnore]
    public string RootPath { get; set; } = string.Empty;
}
