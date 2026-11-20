using Ghost.AssetForge.Core.Models;
using Ghost.AssetForge.Core.Services;
using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Engine.Streaming;
using Ghost.Studio.Assets;

namespace Ghost.Studio.Services;

/// <summary>
/// Unified searchable catalog of project source assets (<c>.meta</c>) and built-in engine assets
/// for Object/Asset picker widgets and GUID-to-name resolution.
/// </summary>
internal sealed class EditorAssetCatalog
{
    public readonly record struct Entry(Guid Guid, string DisplayName, string VirtualPath, string Extension, AssetType AssetType, string FullSourcePath, bool IsBuiltIn);

    private readonly ProjectService _projectService;
    private readonly EditorContentProvider? _contentProvider;
    private readonly Dictionary<Guid, Entry> _byGuid = new();
    private readonly Dictionary<string, Entry> _bySourcePath = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Entry> _allEntries = new();

    private bool _dirty = true;

    public EditorAssetCatalog(ProjectService projectService, EditorContentProvider? contentProvider = null)
    {
        _projectService = projectService;
        _contentProvider = contentProvider;
        _projectService.OnProjectLoaded += MarkDirty;
    }

    public void MarkDirty()
    {
        _dirty = true;
    }

    public bool TryGetByGuid(Guid guid, out Entry entry)
    {
        EnsureUpToDate();
        return _byGuid.TryGetValue(guid, out entry);
    }

    public bool TryGetBySourcePath(string fullPath, out Entry entry)
    {
        EnsureUpToDate();
        return _bySourcePath.TryGetValue(Path.GetFullPath(fullPath), out entry);
    }

    public void GetAssets(AssetType filterType, List<Entry> destination)
    {
        EnsureUpToDate();
        destination.Clear();

        for (var i = 0; i < _allEntries.Count; i++)
        {
            var entry = _allEntries[i];
            if (filterType == AssetType.Unknown || entry.AssetType == filterType)
            {
                destination.Add(entry);
            }
        }
    }

    public static string GetTypeTag(AssetType type, string extension)
    {
        if (!string.IsNullOrEmpty(extension))
        {
            return extension.ToUpperInvariant() switch
            {
                ".GMAT" => "[MAT]",
                ".GSHDR" or ".GCOMP" or ".GGRAPH" or ".HLSL" => "[SHD]",
                ".OBJ" or ".FBX" or ".GLTF" or ".GLB" => "[MSH]",
                ".PNG" or ".JPG" or ".TGA" or ".DDS" => "[TEX]",
                _ => GetTagFromAssetType(type),
            };
        }

        return GetTagFromAssetType(type);
    }

    private static string GetTagFromAssetType(AssetType type)
    {
        return type switch
        {
            AssetType.Material => "[MAT]",
            AssetType.Mesh => "[MSH]",
            AssetType.Texture => "[TEX]",
            AssetType.Shader or AssetType.ComputeShader or AssetType.WorkGraph => "[SHD]",
            AssetType.EntityPrefab => "[PFB]",
            AssetType.Scene => "[SCN]",
            AssetType.Audio => "[AUD]",
            _ => "[AST]",
        };
    }

    private void EnsureUpToDate()
    {
        if (!_dirty)
        {
            return;
        }

        Rebuild();
        _dirty = false;
    }

    private void Rebuild()
    {
        _byGuid.Clear();
        _bySourcePath.Clear();
        _allEntries.Clear();

        var assetDirs = _projectService.AssetDirectories;
        for (var d = 0; d < assetDirs.Count; d++)
        {
            var dir = assetDirs[d];
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                continue;
            }

            var fullDir = Path.GetFullPath(dir);
            var metaFiles = Directory.GetFiles(fullDir, "*.meta", SearchOption.AllDirectories);
            for (var i = 0; i < metaFiles.Length; i++)
            {
                var metaPath = metaFiles[i];
                var sourcePath = metaPath[..^5];
                if (!File.Exists(sourcePath))
                {
                    continue;
                }

                AssetMetadata? meta;
                try
                {
                    meta = _projectService.LoadMetadata(metaPath);
                }
                catch
                {
                    continue;
                }

                if (meta == null || meta.Id == Guid.Empty)
                {
                    continue;
                }

                var fullSource = Path.GetFullPath(sourcePath);
                var ext = Path.GetExtension(fullSource);
                var relPath = Path.GetRelativePath(fullDir, fullSource).Replace('\\', '/');
                var entry = new Entry(meta.Id, Path.GetFileName(fullSource), relPath, ext, meta.Type, fullSource, IsBuiltIn: false);

                _byGuid[entry.Guid] = entry;
                _bySourcePath[fullSource] = entry;
                _allEntries.Add(entry);

                var cacheDir = _projectService.CacheDirectory;
                if (!string.IsNullOrWhiteSpace(cacheDir))
                {
                    var keyDir = Path.GetDirectoryName(relPath)?.Replace('\\', '/') ?? string.Empty;
                    var keyName = Path.GetFileNameWithoutExtension(relPath);
                    var extlessKey = string.IsNullOrEmpty(keyDir) ? keyName : $"{keyDir}/{keyName}";
                    var cacheFile = Path.Combine(cacheDir, extlessKey.Replace('/', Path.DirectorySeparatorChar));
                    var subManifest = SubAssetManifest.Load(cacheFile + ".sub.json");
                    if (subManifest != null)
                    {
                        for (var s = 0; s < subManifest.SubAssets.Count; s++)
                        {
                            var sub = subManifest.SubAssets[s];
                            var subGuid = GuidUtility.DeriveSubAssetGuid(meta.Id, sub.SubPath);
                            var subVirtual = $"{relPath}#{sub.SubPath}";
                            var subName = $"{Path.GetFileName(fullSource)}#{sub.SubPath}";
                            var subEntry = new Entry(subGuid, subName, subVirtual, ext, sub.Type, fullSource, IsBuiltIn: false);
                            _byGuid[subGuid] = subEntry;
                            _allEntries.Add(subEntry);
                        }
                    }
                }
            }
        }

        if (_contentProvider != null)
        {
            foreach (var (virtualPath, info) in _contentProvider.BuiltInAssets)
            {
                if (info.AssetId == Guid.Empty || info.AssetId == ShaderBytecodePool.POOL_ASSET_ID || _byGuid.ContainsKey(info.AssetId))
                {
                    continue;
                }

                var displayName = Path.GetFileName(virtualPath);
                var entry = new Entry(info.AssetId, string.IsNullOrEmpty(displayName) ? virtualPath : displayName, virtualPath, string.Empty, info.AssetType, string.Empty, IsBuiltIn: true);
                _byGuid[entry.Guid] = entry;
                _allEntries.Add(entry);
            }
        }

        _allEntries.Sort(static (a, b) =>
        {
            var builtInCmp = a.IsBuiltIn.CompareTo(b.IsBuiltIn);
            return builtInCmp != 0 ? builtInCmp : string.Compare(a.VirtualPath, b.VirtualPath, StringComparison.OrdinalIgnoreCase);
        });
    }
}
