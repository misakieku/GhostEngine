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

public readonly struct SubAssetEntry
{
    public required string SubPath { get; init; }
    public required AssetType Type { get; init; }
}

public struct AssetBakerContext()
{
    private readonly List<SubAssetEntry> _subAssets = new();
    private readonly HashSet<string> _dependencies = new(StringComparer.OrdinalIgnoreCase);

    public required ShaderMetadata ShaderMetadata
    {
        get; init;
    }

    public required IReadOnlyList<string> AssetDirectories
    {
        get; init;
    }

    public readonly IReadOnlyList<SubAssetEntry> SubAssets => _subAssets;

    public readonly IReadOnlyCollection<string> Dependencies => _dependencies;

    public void AddDependency(string filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _dependencies.Add(Path.GetFullPath(filePath).Replace('\\', '/'));
        }
    }

    internal void ResetDependencies()
    {
        _dependencies.Clear();
    }

    internal Func<string, Stream>? SubAssetStreamFactory { get; set; }

    public Stream AddSubAsset(string subPath, AssetType type)
    {
        if (SubAssetStreamFactory is null)
        {
            throw new InvalidOperationException("Sub-asset output is not configured.");
        }

        var stream = SubAssetStreamFactory(subPath);
        _subAssets.Add(new SubAssetEntry { SubPath = subPath, Type = type });
        return stream;
    }

    internal void ResetSubAssets()
    {
        _subAssets.Clear();
    }
}

/// <summary>
/// Optional interface implemented by asset bakers that can discover dependencies
/// (e.g. shader includes, material textures/shaders, prefab references).
/// </summary>
public interface IAssetDependencyScanner
{
    IEnumerable<string> ScanDependencies(string sourceFile, IBakeSettings settings, AssetBakerContext ctx);
}

public interface IAssetBaker
{
    Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken);
}
