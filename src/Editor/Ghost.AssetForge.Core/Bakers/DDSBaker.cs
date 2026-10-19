using Ghost.Core;
using Ghost.Core.Utilities;
using System.Runtime.InteropServices;

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
        using var fs = File.OpenRead(src);
        if (DDSUtility.TryParseDdsHeader(fs, fs.Length, out var ddsHeader, out var dxt10, out _))
        {
            // We parse dds header in runtime to get image data instead of this, so it's fine to just write a default header here.
            var header = new TextureContentHeader();
            dst.Write(MemoryMarshal.AsBytes(new Span<TextureContentHeader>(ref header)));
        }
        else
        {
            throw new InvalidOperationException($"Failed to parse DDS header for {src}");
        }

        fs.Position = 0;
        await fs.CopyToAsync(dst, cancellationToken).ConfigureAwait(false);
    }
}
