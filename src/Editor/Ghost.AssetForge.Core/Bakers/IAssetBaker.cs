using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using System.Collections.Concurrent;
using System.Text.Json;

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

public readonly struct SubAssetEntry()
{
    public required string SubPath { get; init; }
    public required AssetType Type { get; init; }
    public Guid[] Dependencies { get; init; } = Array.Empty<Guid>();
}

public readonly record struct EmittedAssetEntry(string RelativePath, string SourceFile);

public class AssetBakerContext
{
    private static readonly ConcurrentDictionary<string, Lock> s_importLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lazy<BakerRegistry> s_fallbackRegistry = new(static () => new BakerRegistry());

    private readonly List<SubAssetEntry> _subAssets = new();
    private readonly List<Guid> _assetDependencies = new();
    private readonly HashSet<string> _dependencies = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EmittedAssetEntry> _emittedAssets = new();
    private readonly Lock _emittedAssetsLock = new();

    public Guid AssetId
    {
        get; init;
    }

    public string SourceFile
    {
        get; init;
    } = string.Empty;

    public string RelativePath
    {
        get; init;
    } = string.Empty;

    public required IReadOnlyList<string> AssetDirectories
    {
        get; init;
    }

    public required BakeConfig BakeConfig
    {
        get; init;
    }

    internal BakerRegistry? BakerRegistry
    {
        get; init;
    }

    public IReadOnlyList<SubAssetEntry> SubAssets => _subAssets;

    public IReadOnlyList<Guid> AssetDependencies => _assetDependencies;

    public IReadOnlyCollection<string> Dependencies => _dependencies;

    public IReadOnlyList<EmittedAssetEntry> EmittedAssets
    {
        get
        {
            lock (_emittedAssetsLock)
            {
                return _emittedAssets.ToArray();
            }
        }
    }

    /// <summary>
    /// Writes or resolves an editable project asset under the writable primary asset directory,
    /// ensures its <c>.meta</c> file exists, queues it for follow-up baking in <see cref="BakeService"/>,
    /// and returns its asset <see cref="Guid"/>.
    /// </summary>
    public Guid ImportAsset(string relativeVirtualPath, ReadOnlySpan<byte> data, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(relativeVirtualPath))
        {
            throw new ArgumentException("Relative virtual path cannot be empty.", nameof(relativeVirtualPath));
        }

        if (AssetDirectories.Count == 0)
        {
            throw new InvalidOperationException("AssetDirectories must contain at least one directory.");
        }

        var primaryDir = Path.GetFullPath(AssetDirectories[^1]);
        var normalizedRel = relativeVirtualPath.Replace('\\', '/').TrimStart('/');
        var destFullPath = Path.GetFullPath(Path.Combine(primaryDir, normalizedRel));

        // Guard against '..' segments escaping outside the project's primary asset directory.
        var primaryDirWithSep = primaryDir.EndsWith(Path.DirectorySeparatorChar) || primaryDir.EndsWith(Path.AltDirectorySeparatorChar)
            ? primaryDir
            : primaryDir + Path.DirectorySeparatorChar;
        if (!destFullPath.StartsWith(primaryDirWithSep, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Relative virtual path '{relativeVirtualPath}' escapes outside the primary asset directory.", nameof(relativeVirtualPath));
        }

        var canonicalVirtualPath = Path.GetRelativePath(primaryDir, destFullPath).Replace('\\', '/');
        var pathLock = s_importLocks.GetOrAdd(destFullPath, static _ => new Lock());

        Guid assetGuid;
        lock (pathLock)
        {
            var destDir = Path.GetDirectoryName(destFullPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            if (!File.Exists(destFullPath) || overwrite)
            {
                using var fs = new FileStream(destFullPath, FileMode.Create, FileAccess.Write, FileShare.None);
                fs.Write(data);
            }

            assetGuid = EnsureAssetMetadata(destFullPath);
        }

        RecordEmittedAsset(new EmittedAssetEntry(canonicalVirtualPath, destFullPath));
        return assetGuid;
    }

    /// <summary>
    /// Resolves an existing project asset's <see cref="Guid"/> and registers its source file for
    /// follow-up baking if it resides in the writable asset directory.
    /// </summary>
    internal bool TryResolveProjectAssetGuid(string relativeOrVirtualPath, out Guid guid, out string resolvedFullPath)
    {
        guid = Guid.Empty;
        resolvedFullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(relativeOrVirtualPath) || AssetDirectories.Count == 0)
        {
            return false;
        }

        var normalizedRel = relativeOrVirtualPath.Replace('\\', '/').TrimStart('/');

        for (var i = AssetDirectories.Count - 1; i >= 0; i--)
        {
            var rootDir = Path.GetFullPath(AssetDirectories[i]);
            var rootWithSep = rootDir.EndsWith(Path.DirectorySeparatorChar) || rootDir.EndsWith(Path.AltDirectorySeparatorChar)
                ? rootDir
                : rootDir + Path.DirectorySeparatorChar;

            var candidatePath = Path.GetFullPath(Path.Combine(rootDir, normalizedRel));
            if (!candidatePath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) || !File.Exists(candidatePath))
            {
                continue;
            }

            resolvedFullPath = candidatePath;
            var metaPath = candidatePath + ".meta";

            if (i == AssetDirectories.Count - 1)
            {
                var pathLock = s_importLocks.GetOrAdd(candidatePath, static _ => new Lock());
                lock (pathLock)
                {
                    guid = EnsureAssetMetadata(candidatePath);
                }

                var canonicalVirtualPath = Path.GetRelativePath(rootDir, candidatePath).Replace('\\', '/');
                RecordEmittedAsset(new EmittedAssetEntry(canonicalVirtualPath, candidatePath));
                return guid != Guid.Empty;
            }

            if (File.Exists(metaPath))
            {
                var meta = JsonSerializer.Deserialize<AssetMetadata>(File.ReadAllText(metaPath), ProjectContext.JsonOptions);
                if (meta != null && meta.Id != Guid.Empty)
                {
                    guid = meta.Id;
                    return true;
                }
            }
        }

        return false;
    }

    private Guid EnsureAssetMetadata(string assetFullPath)
    {
        var metaPath = assetFullPath + ".meta";
        if (File.Exists(metaPath))
        {
            try
            {
                var existing = JsonSerializer.Deserialize<AssetMetadata>(File.ReadAllText(metaPath), ProjectContext.JsonOptions);
                if (existing != null && existing.Id != Guid.Empty)
                {
                    return existing.Id;
                }
            }
            catch
            {
                // Fall through and regenerate corrupt or incomplete metadata.
            }
        }

        var registry = BakerRegistry ?? s_fallbackRegistry.Value;
        var ext = Path.GetExtension(assetFullPath);
        var metadata = new AssetMetadata
        {
            Id = Guid.NewGuid(),
            Type = registry.DetectAssetType(ext),
            Settings = registry.CreateDefaultSettings(ext)
        };

        File.WriteAllText(metaPath, JsonSerializer.Serialize(metadata, ProjectContext.JsonOptions));
        return metadata.Id;
    }

    private void RecordEmittedAsset(EmittedAssetEntry entry)
    {
        lock (_emittedAssetsLock)
        {
            for (var i = 0; i < _emittedAssets.Count; i++)
            {
                if (string.Equals(_emittedAssets[i].RelativePath, entry.RelativePath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            _emittedAssets.Add(entry);
        }
    }

    public void AddAssetDependency(Guid assetGuid)
    {
        if (assetGuid != Guid.Empty && !_assetDependencies.Contains(assetGuid))
        {
            _assetDependencies.Add(assetGuid);
        }
    }

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

    public Stream AddSubAsset(string subPath, AssetType type, ReadOnlySpan<Guid> dependencies = default)
    {
        if (SubAssetStreamFactory is null)
        {
            throw new InvalidOperationException("Sub-asset output is not configured.");
        }

        var stream = SubAssetStreamFactory(subPath);
        _subAssets.Add(new SubAssetEntry
        {
            SubPath = subPath,
            Type = type,
            Dependencies = dependencies.Length > 0 ? dependencies.ToArray() : Array.Empty<Guid>()
        });
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
