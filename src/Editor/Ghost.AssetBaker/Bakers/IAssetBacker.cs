using System.IO;

namespace Ghost.AssetBaker.Bakers;

public sealed class AssetBakerAttribute : System.Attribute
{
    public required string[] Extensions { get; set; }
}

public interface IBakeSettings;

public interface IAssetBacker
{
    public Task<Stream> BakeAssetAsync(string assetPath, IBakeSettings settings);
}
