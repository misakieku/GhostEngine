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

    /// <summary>
    /// Creates a registry that auto-discovers <see cref="IAssetBaker"/> implementations
    /// decorated with <see cref="AssetBakerAttribute"/> via reflection over all loaded assemblies.
    /// </summary>
    public BakerRegistry()
        : this(autoDiscover: true)
    {
    }

    /// <summary>
    /// Creates a registry, optionally skipping the AppDomain reflection scan.
    /// </summary>
    /// <param name="autoDiscover">
    /// When <c>true</c>, scans all loaded assemblies for <see cref="IAssetBaker"/> types.
    /// When <c>false</c>, no bakers are discovered; call <see cref="Register{TBaker}"/> explicitly.
    /// </param>
    public BakerRegistry(bool autoDiscover)
    {
        if (!autoDiscover)
        {
            return;
        }

        var bakerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IAssetBaker).IsAssignableFrom(t));

        foreach (var type in bakerTypes)
        {
            var attr = type.GetCustomAttribute<AssetBakerAttribute>();
            if (attr != null)
            {
                var bakerInstance = (IAssetBaker)Activator.CreateInstance(type, true)!;
                Register(bakerInstance, attr.Type, attr.SettingsType, attr.Extensions);
            }
        }
    }

    /// <summary>
    /// Instantiates <typeparamref name="TBaker"/> and registers it for the given asset type,
    /// settings type, and file extensions.
    /// </summary>
    /// <param name="type">The asset type produced by the baker.</param>
    /// <param name="settingsType">The <see cref="IBakeSettings"/> type consumed by the baker.</param>
    /// <param name="extensions">The source file extensions claimed by the baker (e.g. <c>".png"</c>).</param>
    /// <exception cref="InvalidOperationException">An extension is already claimed by another baker.</exception>
    public void Register<TBaker>(AssetType type, Type settingsType, params string[] extensions)
        where TBaker : IAssetBaker
    {
        var bakerInstance = (TBaker)Activator.CreateInstance(typeof(TBaker), true)!;
        Register(bakerInstance, type, settingsType, extensions);
    }

    private void Register(IAssetBaker baker, AssetType type, Type settingsType, IReadOnlyList<string> extensions)
    {
        foreach (var ext in extensions)
        {
            if (_extToBaker.TryGetValue(ext, out var existingIndex))
            {
                var existingBakerType = _bakers[existingIndex].GetType();
                throw new InvalidOperationException(
                    $"Duplicate asset extension '{ext}' is registered by both '{existingBakerType.FullName}' and '{baker.GetType().FullName}'. Each extension may only be claimed by a single baker.");
            }
        }

        var index = _bakers.Count;
        _bakers.Add(baker);

        foreach (var ext in extensions)
        {
            _extToBaker[ext] = index;
            _extToSettings[ext] = settingsType;
            _extToType[ext] = type;
        }
    }

    public AssetType DetectAssetType(string extension)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return AssetType.Unknown;
        }

        if (string.Equals(extension, ".gcomp", StringComparison.OrdinalIgnoreCase))
        {
            return AssetType.ComputeShader;
        }

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
