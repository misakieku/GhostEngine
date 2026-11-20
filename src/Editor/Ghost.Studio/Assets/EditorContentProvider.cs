using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.Streaming;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Studio.Assets;

/// <summary>
/// Layered <see cref="IContentProvider"/> serving fixed built-in engine/editor assets from
/// <see cref="RuntimeContentProvider"/> alongside live project <c>.cache</c> and <c>.sub</c> outputs
/// without requiring edit-time repacking.
/// </summary>
internal sealed class EditorContentProvider : IContentProvider
{
    private readonly record struct ProjectCacheEntry(Guid AssetId, string VirtualPath, AssetType AssetType, string CacheFilePath, int HeaderOffset, Guid[] Dependencies);

    private sealed class ProjectIndex
    {
        public readonly Dictionary<string, ProjectCacheEntry> PathToEntry = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<Guid, ProjectCacheEntry> GuidToEntry = new();
        public readonly List<ShaderCatalogEntry> Shaders = new();
    }

    private readonly ProjectService _projectService;
    private readonly RuntimeContentProvider _builtInProvider;
    private readonly List<ShaderCatalogEntry> _combinedShaders = new();
    private readonly HashSet<Guid> _builtInShaderIds = new();
    private readonly Lock _refreshLock = new();

    private ProjectIndex _projectIndex = new();
    private bool _disposed;

    public IReadOnlyList<ShaderCatalogEntry> ShaderCatalog => _combinedShaders;

    internal IReadOnlyDictionary<string, AssetInfo> BuiltInAssets => _builtInProvider.Assets;

    public EditorContentProvider(ProjectService projectService, string builtInManifestPath)
    {
        _projectService = projectService;
        _builtInProvider = new RuntimeContentProvider(builtInManifestPath);

        for (var i = 0; i < _builtInProvider.ShaderCatalog.Count; i++)
        {
            var shader = _builtInProvider.ShaderCatalog[i];
            _combinedShaders.Add(shader);
            _builtInShaderIds.Add(shader.AssetId);
        }

        _projectService.OnProjectLoaded += RefreshProjectIndex;
        RefreshProjectIndex();
    }

    public void RefreshProjectIndex()
    {
        if (_disposed)
        {
            return;
        }

        var nextIndex = BuildProjectIndex();
        lock (_refreshLock)
        {
            _projectIndex = nextIndex;
            _combinedShaders.Clear();
            _combinedShaders.AddRange(_builtInProvider.ShaderCatalog);
            _combinedShaders.AddRange(nextIndex.Shaders);
        }
    }

    public Guid VirtualPathToGuid(string path)
    {
        var normalized = NormalizeVirtualPath(path);
        var index = Volatile.Read(ref _projectIndex);
        if (index.PathToEntry.TryGetValue(normalized, out var entry))
        {
            return entry.AssetId;
        }

        return _builtInProvider.VirtualPathToGuid(normalized);
    }

    public string VirtualPathFromGuid(Guid guid)
    {
        var index = Volatile.Read(ref _projectIndex);
        if (index.GuidToEntry.TryGetValue(guid, out var entry))
        {
            return entry.VirtualPath;
        }

        return _builtInProvider.HasAsset(guid) ? _builtInProvider.VirtualPathFromGuid(guid) : string.Empty;
    }

    public bool HasAsset(Guid guid)
    {
        var index = Volatile.Read(ref _projectIndex);
        return index.GuidToEntry.ContainsKey(guid) || _builtInProvider.HasAsset(guid);
    }

    public Guid[] GetDependencies(Guid guid)
    {
        var index = Volatile.Read(ref _projectIndex);
        if (index.GuidToEntry.TryGetValue(guid, out var entry))
        {
            return entry.Dependencies;
        }

        return _builtInProvider.HasAsset(guid) ? _builtInProvider.GetDependencies(guid) : Array.Empty<Guid>();
    }

    public AssetType GetAssetType(Guid guid)
    {
        var index = Volatile.Read(ref _projectIndex);
        if (index.GuidToEntry.TryGetValue(guid, out var entry))
        {
            return entry.AssetType;
        }

        return _builtInProvider.HasAsset(guid) ? _builtInProvider.GetAssetType(guid) : AssetType.Unknown;
    }

    public unsafe Result<AssetPayload> OpenReadAsync(Guid guid, CancellationToken token = default)
    {
        var index = Volatile.Read(ref _projectIndex);
        if (index.GuidToEntry.TryGetValue(guid, out var entry))
        {
            return ReadLooseCachePayload(entry.CacheFilePath, entry.HeaderOffset);
        }

        return _builtInProvider.OpenReadAsync(guid, token);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _projectService.OnProjectLoaded -= RefreshProjectIndex;
        _builtInProvider.Dispose();
    }

    private ProjectIndex BuildProjectIndex()
    {
        var index = new ProjectIndex();
        var cacheDir = _projectService.CacheDirectory;
        var assetDirs = _projectService.AssetDirectories;
        if (string.IsNullOrWhiteSpace(cacheDir) || !Directory.Exists(cacheDir) || assetDirs.Count == 0)
        {
            return index;
        }

        var seenShaders = new HashSet<Guid>(_builtInShaderIds);

        for (var d = 0; d < assetDirs.Count; d++)
        {
            var assetDir = assetDirs[d];
            if (string.IsNullOrWhiteSpace(assetDir) || !Directory.Exists(assetDir))
            {
                continue;
            }

            var metaFiles = Directory.GetFiles(assetDir, "*.meta", SearchOption.AllDirectories);
            for (var i = 0; i < metaFiles.Length; i++)
            {
                var metaPath = metaFiles[i];
                var sourceFile = metaPath[..^5];
                if (!File.Exists(sourceFile))
                {
                    continue;
                }

                AssetMetadata? metadata;
                try
                {
                    metadata = _projectService.LoadMetadata(metaPath);
                }
                catch
                {
                    continue;
                }

                if (metadata == null || metadata.Id == Guid.Empty)
                {
                    continue;
                }

                var relativePath = Path.GetRelativePath(assetDir, sourceFile).Replace('\\', '/');
                var key = BuildExtensionlessKey(relativePath);
                var cacheFile = Path.Combine(cacheDir, key.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(cacheFile))
                {
                    continue;
                }

                var cacheInfo = new FileInfo(cacheFile);
                if (cacheInfo.Length <= CacheFileHeader.SIZE)
                {
                    continue;
                }

                var subManifest = SubAssetManifest.Load(cacheFile + ".sub.json");
                var primaryDeps = subManifest != null && subManifest.Dependencies.Count > 0 ? subManifest.Dependencies.ToArray() : Array.Empty<Guid>();

                var primaryEntry = new ProjectCacheEntry(metadata.Id, key, metadata.Type, cacheFile, CacheFileHeader.SIZE, primaryDeps);
                index.PathToEntry[key] = primaryEntry;
                index.GuidToEntry[metadata.Id] = primaryEntry;

                if (metadata.Type == AssetType.Shader && seenShaders.Add(metadata.Id))
                {
                    TryAddShaderCatalogEntry(cacheFile, metadata.Id, CacheFileHeader.SIZE, index.Shaders);
                }

                if (subManifest == null || subManifest.SubAssets.Count == 0)
                {
                    continue;
                }

                var subDir = cacheFile + ".sub";
                for (var s = 0; s < subManifest.SubAssets.Count; s++)
                {
                    var sub = subManifest.SubAssets[s];
                    var subCachePath = Path.Combine(subDir, sub.SubPath.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(subCachePath))
                    {
                        continue;
                    }

                    var subKey = $"{key}#{sub.SubPath}";
                    var subGuid = GuidUtility.DeriveSubAssetGuid(metadata.Id, sub.SubPath);
                    var subDeps = sub.Dependencies != null && sub.Dependencies.Count > 0 ? sub.Dependencies.ToArray() : Array.Empty<Guid>();

                    var subEntry = new ProjectCacheEntry(subGuid, subKey, sub.Type, subCachePath, 0, subDeps);
                    index.PathToEntry[subKey] = subEntry;
                    index.GuidToEntry[subGuid] = subEntry;

                    if (sub.Type == AssetType.Shader && seenShaders.Add(subGuid))
                    {
                        TryAddShaderCatalogEntry(subCachePath, subGuid, 0, index.Shaders);
                    }
                }
            }
        }

        return index;
    }

    private static void TryAddShaderCatalogEntry(string cacheFile, Guid assetId, int headerOffset, List<ShaderCatalogEntry> dest)
    {
        try
        {
            dest.Add(PackService.ReadShaderCatalogEntry(cacheFile, assetId, headerOffset));
        }
        catch (Exception ex)
        {
            Logger.Warning($"EditorContentProvider failed to read shader catalog entry '{cacheFile}': {ex.Message}");
        }
    }

    private static unsafe Result<AssetPayload> ReadLooseCachePayload(string cacheFilePath, int headerOffset)
    {
        try
        {
            using var fs = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var payloadSize = fs.Length - headerOffset;
            if (payloadSize <= 0)
            {
                return Result.Failure($"Cache file '{cacheFilePath}' has invalid payload size {payloadSize}.");
            }

            fs.Seek(headerOffset, SeekOrigin.Begin);
            var alignedSize = MemoryUtility.AlignUp((nuint)payloadSize, 16);
            var block = new MemoryBlock(alignedSize, 16, AllocationHandle.Persistent);
            try
            {
                var span = new Span<byte>((byte*)block.GetUnsafePtr(), (int)payloadSize);
                fs.ReadExactly(span);
                return AssetPayload.FromAllocatedMemory(block, payloadSize);
            }
            catch
            {
                block.Dispose();
                throw;
            }
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to read cache payload '{cacheFilePath}': {ex.Message}");
        }
    }

    private static string BuildExtensionlessKey(string relativePath)
    {
        var dir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/') ?? string.Empty;
        var nameWithoutExt = Path.GetFileNameWithoutExtension(relativePath);
        return string.IsNullOrEmpty(dir) ? nameWithoutExt : $"{dir}/{nameWithoutExt}";
    }

    private static string NormalizeVirtualPath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}
