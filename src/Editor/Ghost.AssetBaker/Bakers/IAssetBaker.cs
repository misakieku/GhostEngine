using Ghost.AssetBaker.Models;

namespace Ghost.AssetBaker.Bakers;

public sealed class AssetBakerAttribute : Attribute
{
    public required string[] Extensions { get; set; }
    public required AssetType Type { get; set; }
    public required Type SettingsType { get; set; }
}

public interface IBakeSettings;

public interface IAssetBaker
{
    public Task<Stream> BakeAssetAsync(string assetPath, IBakeSettings settings);
}
