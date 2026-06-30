using Ghost.Core;
using Ghost.DSL.Models;

namespace Ghost.AssetForge.Core.Bakers;

public sealed class AssetBakerAttribute : Attribute
{
    public required string[] Extensions { get; set; }
    public required AssetType Type { get; set; }
    public required Type SettingsType { get; set; }
}

public interface IBakeSettings;

public class AssetBakerContext
{
    public required ShaderMetadata ShderMetadata
    {
        get; init;
    }

    public required IReadOnlyList<string> AssetDirectories
    {
        get; init;
    }
}

public interface IAssetBaker
{
    Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken);
}
