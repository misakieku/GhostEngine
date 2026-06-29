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
    public required IReadOnlyDictionary<string, ShaderReflectionData> ShaderNameToReflectionData
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
    virtual string ResolveVirtualPath(string rootDirectory, string assetPath)
    {
        var relativePath = Path.GetRelativePath(rootDirectory, assetPath);
        var dir = Path.GetDirectoryName(relativePath) ?? string.Empty;
        var nameWithoutExt = Path.GetFileNameWithoutExtension(relativePath);
        return Path.Combine(dir, nameWithoutExt).Replace('\\', '/');
    }

    Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken);
}
