using Ghost.Engine;

namespace Ghost.Editor.Core.AssetHandler;

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

    public static void RegisterHandler(IAssetHandler handler, Guid assetTypeId, ReadOnlySpan<string> extensions, int version)
    {
        s_byTypeId[assetTypeId] = handler;
        s_versionByTypeId[assetTypeId] = version;

        foreach (var ext in extensions)
        {
            var normalizedExt = ext.StartsWith('.') ? ext : "." + ext;
            s_byExtension[normalizedExt] = handler;
            s_typeByExtension[normalizedExt] = handler.RuntimeAssetType;
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

    public static IAssetHandler? GetByAssetTypeId(Guid typeId)
    {
        s_byTypeId.TryGetValue(typeId, out var handler);
        return handler;
    }

    public static int GetVersionByAssetTypeId(Guid typeId)
    {
        s_versionByTypeId.TryGetValue(typeId, out var version);
        return version;
    }

    public static IEnumerable<string> GetSupportedExtensions()
    {
        return s_byExtension.Keys;
    }

    public static AssetType GetRuntimeAssetTypeByExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return AssetType.Unknown;
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        return s_typeByExtension.GetValueOrDefault(normalized, AssetType.Unknown);
    }
}
