using Ghost.Core;

namespace Ghost.AssetForge.Core.Bakers;

internal class DDSBakeSettings : IBakeSettings
{
    public int Version { get; set; } = VERSION;
    public const int VERSION = 1;
}

[AssetBaker(Extensions = [".dds"], Type = AssetType.Texture, SettingsType = typeof(DDSBakeSettings), SettingsVersion = DDSBakeSettings.VERSION)]
internal class DDSBaker : IAssetBaker
{
    public async Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken)
    {
        // DDS files are already in a format that can be used directly by the engine, so we just copy the file to the destination stream.
        using var fs = File.OpenRead(src);
        await fs.CopyToAsync(dst, cancellationToken);
    }
}
