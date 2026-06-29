using Ghost.AssetForge.Core.Bakers;
using Ghost.Core;
using System.Reflection;

namespace Ghost.AssetForge.Core.Services;

public class BakerRegistry : IDisposable
{
    private readonly List<IAssetBaker> _bakers = new();
    private readonly Dictionary<string, AssetType> _extToType = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Type> _extToSettings = new();
    private readonly Dictionary<string, int> _extToBaker = new();

    public BakerRegistry()
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
                var index = _bakers.Count;

                _bakers.Add(bakerInstance);
                foreach (var ext in attr.Extensions)
                {
                    _extToBaker[ext] = index;
                    _extToSettings[ext] = attr.SettingsType;
                    _extToType[ext] = attr.Type;
                }
            }
        }
    }

    public AssetType DetectAssetType(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return AssetType.Unknown;
        return _extToType.TryGetValue(extension, out var type) ? type : AssetType.Unknown;
    }

    public IBakeSettings? CreateDefaultSettings(string ext)
    {
        if (_extToSettings.TryGetValue(ext, out var settingsType))
        {
            return (IBakeSettings?)Activator.CreateInstance(settingsType, true);
        }

        return null;
    }

    public IAssetBaker? GetBaker(string ext)
    {
        if (_extToBaker.TryGetValue(ext, out var index))
        {
            return _bakers[index];
        }

        return null;
    }

    public Type? GetSettingsType(string ext)
    {
        return _extToSettings.GetValueOrDefault(ext);
    }

    public void Dispose()
    {
        foreach (var baker in _bakers)
        {
            if (baker is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        GC.SuppressFinalize(this);
    }
}
