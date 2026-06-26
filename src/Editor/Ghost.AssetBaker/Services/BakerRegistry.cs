using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ghost.AssetBaker.Bakers;
using Ghost.AssetBaker.Models;
using Ghost.Core;

namespace Ghost.AssetBaker.Services;

public class BakerRegistry
{
    private static readonly Lazy<BakerRegistry> s_instance = new(() => new BakerRegistry());
    public static BakerRegistry Instance => s_instance.Value;

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
}
