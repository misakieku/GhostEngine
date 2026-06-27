using Ghost.Core;
using System.Text.Json.Serialization;

namespace Ghost.AssetForge.Core.Models;

public record AssetLocation
{
    public string PackFileName { get; init; } = string.Empty;
    public ulong Offset { get; init; }
    public ulong Size { get; init; }
}

public record Manifest
{
    public CompressionMethod GlobalCompression { get; init; } = CompressionMethod.LZ4;
    public Dictionary<string, AssetLocation> Assets { get; init; } = new();
}
