using Ghost.AssetForge.Core.Bakers;
using Ghost.Core;
using System.Reflection;

namespace Ghost.AssetForge.Core.Services;

public class BakerRegistry
{
    private readonly Dictionary<string, AssetType> _extensionToType = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<AssetType, Type> _typeToSettings = new();
    private readonly Dictionary<AssetType, IAssetBaker> _typeToBaker = new();

    private BakerRegistry()
    {
        var bakerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IAssetBaker).IsAssignableFrom(t));

        foreach (var type in bakerTypes)
        {
            var attr = type.GetCustomAttribute<AssetBakerAttribute>();
            if (attr != null)
            {
                var bakerInstance = (IAssetBaker)Activator.CreateInstance(type, true)!;
                _typeToBaker[attr.Type] = bakerInstance;
                _typeToSettings[attr.Type] = attr.SettingsType;

                foreach (var ext in attr.Extensions)
                {
                    _extensionToType[ext] = attr.Type;
                }
            }
        }
    }

    public AssetType DetectAssetType(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return AssetType.Unknown;
        return _extensionToType.TryGetValue(extension, out var type) ? type : AssetType.Unknown;
    }

    public IBakeSettings? CreateDefaultSettings(AssetType type)
    {
        if (_typeToSettings.TryGetValue(type, out var settingsType))
        {
            return (IBakeSettings?)Activator.CreateInstance(settingsType, true);
        }
        return null;
    }

    public IAssetBaker? GetBaker(AssetType type)
    {
        _typeToBaker.TryGetValue(type, out var baker);
        return baker;
    }

    public Type? GetSettingsType(AssetType type)
    {
        _typeToSettings.TryGetValue(type, out var settingsType);
        return settingsType;
    }
}
