using Ghost.Core;
using Ghost.Editor.Core.Utilities;
using System.Diagnostics;
using System.Reflection;

namespace Ghost.Editor.Core.AssetHandle;

public partial class AssetService
{
    private readonly Dictionary<string, Action<string>> _assetOpenHandlers = new(StringComparer.OrdinalIgnoreCase);

    private void InitializeAssetHandle()
    {
        var methods = TypeCache.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttribute<AssetOpenHandlerAttribute>() != null &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(string));

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<AssetOpenHandlerAttribute>()!;
            var del = (Action<string>)Delegate.CreateDelegate(typeof(Action<string>), method);
            foreach (var ext in attr.Extensions)
            {
                if (_assetOpenHandlers.ContainsKey(ext))
                {
                    Logger.LogError($"Duplicate asset open handler for extension '{ext}' found in method '{method.Name}'. Existing handler will be overwritten.");
                }

                _assetOpenHandlers[ext] = del;
            }
        }
    }

    public void OpenAsset(string path)
    {
        var extension = Path.GetExtension(path);
        if (_assetOpenHandlers.TryGetValue(extension, out var handler))
        {
            handler(path);
        }
        else
        {
            Process.Start(new ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
        }
    }
}
