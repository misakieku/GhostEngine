using Ghost.AssetForge.Core.Services;
using Ghost.Core;

namespace Ghost.AssetForge.Core.Bakers;

public sealed class AssetBakerAttribute : Attribute
{
    public required string[] Extensions { get; set; }
    public required AssetType Type { get; set; }
    public required Type SettingsType { get; set; }
    public required int SettingsVersion { get; set; }
}

public interface IBakeSettings
{
    int Version { get; set; }
}

public readonly struct SubAssetEntry
{
    public required string SubPath { get; init; }
    public required AssetType Type { get; init; }
}

public struct AssetBakerContext()
{
    private readonly List<SubAssetEntry> _subAssets = new();
    private readonly HashSet<string> _dependencies = new(StringComparer.OrdinalIgnoreCase);

    public required IReadOnlyList<string> AssetDirectories
    {
        get; init;
    }

    public required BakeConfig BakeConfig
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
    /// <summary>
    /// Gets a value indicating whether the output of this asset baker should be compressed.
    /// </summary>
    bool CompressOutput => true;

    /// <summary>
    /// Bakes an asset from the specified source file to the destination stream.
    /// </summary>
    /// <param name="src">The source file to bake.</param>
    /// <param name="dst">The destination stream to write the baked asset to.</param>
    /// <param name="settings">The settings to use for baking.</param>
    /// <param name="ctx">The context for the baking process.</param>
    /// <param name="cancellationToken">A token to cancel the baking process.</param>
    /// <returns>A task representing the asynchronous baking operation.</returns>
    Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken);
}
