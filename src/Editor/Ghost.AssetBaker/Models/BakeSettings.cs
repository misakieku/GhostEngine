using Ghost.Core;

namespace Ghost.AssetBaker.Models;

public record BakeSettings
{
    public CompressionMethod Compression { get; init; } = CompressionMethod.None;
    public string OutputPath { get; init; } = string.Empty;
    public Bakers.IBakeSettings? AssetSettings { get; init; }
}
