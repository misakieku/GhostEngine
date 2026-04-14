using System.Reflection;
using Ghost.Editor.Core.Utilities;

namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// One-time scan at editor startup → two dictionaries.
/// All lookups are O(1) after construction.
/// </summary>
internal sealed class AssetHandlerRegistry
{
    private readonly Dictionary<string, IAssetHandler> _byExtension;
    private readonly Dictionary<Guid, IAssetHandler> _byTypeId;
    private readonly Dictionary<Guid, int> _versionByTypeId;

    public AssetHandlerRegistry()
    {
        _byExtension = new Dictionary<string, IAssetHandler>(StringComparer.OrdinalIgnoreCase);
        _byTypeId = new Dictionary<Guid, IAssetHandler>();
        _versionByTypeId = new Dictionary<Guid, int>();

        foreach (var typeInfo in TypeCache.GetTypes())
        {
            if (typeInfo.IsAbstract || typeInfo.IsInterface)
            {
                continue;
            }

            if (!typeof(IAssetHandler).IsAssignableFrom(typeInfo))
            {
                continue;
            }

            var attr = typeInfo.GetCustomAttribute<CustomAssetHandlerAttribute>();
            if (attr == null)
            {
                continue;
            }

            if (!Guid.TryParse(attr.ID, out var typeId))
            {
                continue;
            }

            try
            {
                if (Activator.CreateInstance(typeInfo) is IAssetHandler handler)
                {
                    _byTypeId[typeId] = handler;
                    // Note: Versioning could be expanded, but for now we assume version 1 or look for a constant
                    _versionByTypeId[typeId] = 1;

                    foreach (var ext in attr.SupportedExtensions)
                    {
                        var normalizedExt = ext.StartsWith('.') ? ext : "." + ext;
                        _byExtension[normalizedExt] = handler;
                    }
                }
            }
            catch
            {
                // Log failure to instantiate handler in real app
            }
        }
    }

    public IAssetHandler? GetByExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return null;
        }

        var normalized = extension.StartsWith('.') ? extension : "." + extension;
        _byExtension.TryGetValue(normalized, out var handler);
        return handler;
    }

    public IAssetHandler? GetByTypeId(Guid typeId)
    {
        _byTypeId.TryGetValue(typeId, out var handler);
        return handler;
    }

    public int GetVersionByTypeId(Guid typeId)
    {
        _versionByTypeId.TryGetValue(typeId, out var version);
        return version;
    }

    public IEnumerable<string> GetSupportedExtensions() => _byExtension.Keys;
}
