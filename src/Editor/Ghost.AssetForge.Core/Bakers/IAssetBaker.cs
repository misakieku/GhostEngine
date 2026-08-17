using System.Collections.Concurrent;
using Ghost.Core;
using Ghost.DSL.Models;
using Ghost.DSL.ShaderCompiler;

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

    public required ShaderMetadata ShaderMetadata
    {
        get; init;
    }

    public required IReadOnlyList<string> AssetDirectories
    {
        get; init;
    }

    public ShaderWorkspace? ShaderWorkspace
    {
        get; init;
    }

    public ConcurrentDictionary<ulong, (ShaderStage stage, byte[] bytecode)[]>? SharedPassBytecodeCache
    {
        get; init;
    }
    public readonly IReadOnlyList<SubAssetEntry> SubAssets => _subAssets;

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

public interface IAssetBaker
{
    Task BakeAssetAsync(string src, Stream dst, IBakeSettings settings, AssetBakerContext ctx, CancellationToken cancellationToken);
}
