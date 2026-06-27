using Ghost.Core;

namespace Ghost.AssetForge.Core.Bakers;

public sealed class AssetBakerAttribute : Attribute
{
    public required string[] Extensions { get; set; }
    public required AssetType Type { get; set; }
    public required Type SettingsType { get; set; }
}

public interface IBakeSettings;

public interface IAssetBaker
{
    public Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, CancellationToken cancellationToken);
}
