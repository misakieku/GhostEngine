using Ghost.Engine;
using Ghost.Engine.Streaming;
using System.Collections.Concurrent;

namespace Ghost.Editor.Core.Assets;

public readonly struct AssetHandlerInfo
{
    public Type HandlerType { get; init; }
    public AssetType RuntimeAssetType { get; init; }
    public Guid EditorAssetTypeID { get; init; }
    public int Version { get; init; }
}

public static class AssetHandlerRegistry
{
    private static readonly Dictionary<string, AssetHandlerInfo> s_byExtension;
    private static readonly Dictionary<Guid, AssetHandlerInfo> s_byTypeId;
    private static readonly List<(Type Type, string Name)> s_iAssetSettingsTypes;

    private static readonly ConcurrentDictionary<Type, IAssetHandler?> s_handlerCache;

    static AssetHandlerRegistry()
    {
        s_byExtension = new Dictionary<string, AssetHandlerInfo>(StringComparer.OrdinalIgnoreCase);
        s_byTypeId = new Dictionary<Guid, AssetHandlerInfo>();

        s_iAssetSettingsTypes = new List<(Type Type, string Name)>();
        s_handlerCache = new ConcurrentDictionary<Type, IAssetHandler?>();
    }

    public static void RegisterHandler(Type handlerType, Guid assetTypeId, AssetType runtimeAssetType, int version, bool allowCaching, params ReadOnlySpan<string> extensions)
    {
        var info = new AssetHandlerInfo
        {
            HandlerType = handlerType,
            RuntimeAssetType = runtimeAssetType,
            EditorAssetTypeID = assetTypeId,
            Version = version
        };

        s_byTypeId[assetTypeId] = info;

        foreach (var ext in extensions)
        {
            var normalizedExt = ext.StartsWith('.') ? ext : "." + ext;
            s_byExtension[normalizedExt] = info;
        }

        if (allowCaching)
        {
            s_handlerCache[handlerType] = null;
        }
    }

    public static void RegisterIAssetSettingsType(Type type, string name)
    {
        s_iAssetSettingsTypes.Add((type, name));
    }

    public static IAssetHandler? GetByExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return null;
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        if (!s_byExtension.TryGetValue(normalized, out var info))
        {
            return null;
        }

        return s_handlerCache.GetOrAdd(info.HandlerType, t =>
        {
            try
            {
                return (IAssetHandler?)Activator.CreateInstance(t);
            }
            catch
            {
                return null;
            }
        });
    }

    public static IAssetHandler? GetByAssetTypeId(Guid typeId)
    {
        if (!s_byTypeId.TryGetValue(typeId, out var info))
        {
            return null;
        }

        return s_handlerCache.GetOrAdd(info.HandlerType, t =>
        {
            try
            {
                return (IAssetHandler?)Activator.CreateInstance(t);
            }
            catch
            {
                return null;
            }
        });
    }

    public static bool TryGetHandlerInfoByAssetTypeId(Guid typeId, out AssetHandlerInfo info)
    {
        return s_byTypeId.TryGetValue(typeId, out info);
    }

    public static bool TryGetHandlerInfoByExtension(string extension, out AssetHandlerInfo info)
    {
        if (string.IsNullOrEmpty(extension))
        {
            info = default;
            return false;
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        return s_byExtension.TryGetValue(normalized, out info);
    }

    public static IReadOnlyCollection<(Type Type, string Name)> GetIAssetSettingsTypes()
    {
        return s_iAssetSettingsTypes;
    }
}