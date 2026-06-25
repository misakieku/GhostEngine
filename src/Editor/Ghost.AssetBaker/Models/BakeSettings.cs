namespace Ghost.AssetBaker.Models;

public enum CompressionLevel
{
    None,
    Fast,
    High
}

public record BakeSettings
{
    public CompressionLevel Compression { get; init; } = CompressionLevel.Fast;
    public bool GenerateMipmaps { get; init; } = true;
    public bool OptimizeMesh { get; init; } = true;
    public bool GenerateLods { get; init; } = false;
    public bool BundleOutput { get; init; } = false;
    public string OutputPath { get; init; } = string.Empty;
}
