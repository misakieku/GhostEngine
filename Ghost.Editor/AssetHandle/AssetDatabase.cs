using System.Diagnostics;
using System.Reflection;

namespace Ghost.Editor.AssetHandle;

public static class AssetDatabase
{
    private static readonly Dictionary<string, Action<string>> _assetOpenHandlers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Func<string, Task>> _asyncAssetOpenHandler = new(StringComparer.OrdinalIgnoreCase);

    static AssetDatabase()
    {
        RegisterAssetHandles();
    }

    private static void RegisterAssetHandles()
    {
        var methods = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
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
                    throw new InvalidOperationException($"Duplicate handler for extension '{ext}'");
                }

                _assetOpenHandlers[ext] = del;
            }
        }

        var asyncMethods = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(m => m.GetCustomAttribute<AsyncAssetOpenHandlerAttribute>() != null &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(string) &&
                m.ReturnType == typeof(Task));

        foreach (var method in asyncMethods)
        {
            var attr = method.GetCustomAttribute<AsyncAssetOpenHandlerAttribute>()!;
            var del = (Func<string, Task>)Delegate.CreateDelegate(typeof(Func<string, Task>), method);
            foreach (var ext in attr.Extensions)
            {
                if (_asyncAssetOpenHandler.ContainsKey(ext))
                {
                    throw new InvalidOperationException($"Duplicate async handler for extension '{ext}'");
                }
                _asyncAssetOpenHandler[ext] = del;
            }
        }
    }

    public static async ValueTask OpenAsset(string path)
    {
        var extension = Path.GetExtension(path);
        if (_assetOpenHandlers.TryGetValue(extension, out var handler))
        {
            handler(path);
        }
        else if (_asyncAssetOpenHandler.TryGetValue(extension, out var asyncHandler))
        {
            await asyncHandler(path);
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