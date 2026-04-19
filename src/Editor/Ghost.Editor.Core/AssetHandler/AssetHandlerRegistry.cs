namespace Ghost.Editor.Core.AssetHandler;

/// <summary>
/// One-time scan at editor startup → two dictionaries.
/// All lookups are O(1) after construction.
/// </summary>
public sealed class AssetHandlerRegistry
{
    private readonly Dictionary<string, IAssetHandler> _byExtension;
    private readonly Dictionary<Guid, IAssetHandler> _byTypeId;
    private readonly Dictionary<Guid, int> _versionByTypeId;

    public AssetHandlerRegistry()
    {
        _byExtension = new Dictionary<string, IAssetHandler>(StringComparer.OrdinalIgnoreCase);
        _byTypeId = new Dictionary<Guid, IAssetHandler>();
        _versionByTypeId = new Dictionary<Guid, int>();
    }

    public void RegisterHandler(IAssetHandler handler, Guid typeId, ReadOnlySpan<string> extensions, int version)
    {
        _byTypeId[typeId] = handler;
        _versionByTypeId[typeId] = version;

        foreach (var ext in extensions)
        {
            var normalizedExt = ext.StartsWith('.') ? ext : "." + ext;
            _byExtension[normalizedExt] = handler;
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
