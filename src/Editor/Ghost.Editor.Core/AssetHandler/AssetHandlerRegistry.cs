using Ghost.Engine;

namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// One-time scan at editor startup → two dictionaries.
/// All lookups are O(1) after construction.
/// </summary>
public static class AssetHandlerRegistry
{
    private static readonly Dictionary<string, IAssetHandler> s_byExtension;
    private static readonly Dictionary<string, AssetType> s_typeByExtension;
    private static readonly Dictionary<Guid, IAssetHandler> s_byTypeId;
    private static readonly Dictionary<Guid, int> s_versionByTypeId;

    static AssetHandlerRegistry()
    {
        s_byExtension = new Dictionary<string, IAssetHandler>(StringComparer.OrdinalIgnoreCase);
        s_typeByExtension = new Dictionary<string, AssetType>(StringComparer.OrdinalIgnoreCase);
        s_byTypeId = new Dictionary<Guid, IAssetHandler>();
        s_versionByTypeId = new Dictionary<Guid, int>();
    }

    public static void RegisterHandler(IAssetHandler handler, Guid typeId, ReadOnlySpan<string> extensions, int version)
    {
        s_byTypeId[typeId] = handler;
        s_versionByTypeId[typeId] = version;

        foreach (var ext in extensions)
        {
            var normalizedExt = ext.StartsWith('.') ? ext : "." + ext;
            s_byExtension[normalizedExt] = handler;
            s_typeByExtension[normalizedExt] = handler.TargetAssetType;
        }
    }

    public static IAssetHandler? GetByExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return null;
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        s_byExtension.TryGetValue(normalized, out var handler);
        return handler;
    }

    public static IAssetHandler? GetByTypeId(Guid typeId)
    {
        s_byTypeId.TryGetValue(typeId, out var handler);
        return handler;
    }

    public static int GetVersionByTypeId(Guid typeId)
    {
        s_versionByTypeId.TryGetValue(typeId, out var version);
        return version;
    }

    public static IEnumerable<string> GetSupportedExtensions()
    {
        return s_byExtension.Keys;
    }

    public static AssetType GetAssetTypeByExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return AssetType.Unknown;
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        return s_typeByExtension.GetValueOrDefault(normalized, AssetType.Unknown);
    }
}
