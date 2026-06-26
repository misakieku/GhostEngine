using Ghost.Core;

namespace Ghost.AssetBaker.Models;

public enum AssetState
{
    Pending,
    Baking,
    Success,
    Failed
}

public record QueuedAsset
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FilePath { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public long SizeInBytes { get; init; }
    public AssetType Type { get; init; } = AssetType.Unknown;
    public AssetState Status { get; set; } = AssetState.Pending;
    public double Progress { get; set; } = 0.0;
    public string ErrorMessage { get; set; } = string.Empty;
    public BakeSettings Settings { get; init; } = new();

    public string SizeFormatted
    {
        get
        {
            if (SizeInBytes >= 1024 * 1024 * 1024)
                return $"{SizeInBytes / (1024.0 * 1024 * 1024):F2} GB";
            if (SizeInBytes >= 1024 * 1024)
                return $"{SizeInBytes / (1024.0 * 1024):F2} MB";
            if (SizeInBytes >= 1024)
                return $"{SizeInBytes / 1024.0:F2} KB";
            return $"{SizeInBytes} Bytes";
        }
    }
}
