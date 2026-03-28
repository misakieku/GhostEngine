using Ghost.Editor.Core.Utilities;
using Ghost.Editor.Models;
using Ghost.Engine;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ghost.Editor;

internal static class ActivationHandler
{
    public static LaunchArguments ParseArguments(ReadOnlySpan<char> args)
    {
        var arguments = new LaunchArguments();
        var properties = typeof(LaunchArguments).GetProperties();
        var split = args.Split(' ');

        while (split.MoveNext())
        {
            var range = split.Current;
            var arg = args[range.Start..range.End];
            if (arg.Length > 2)
            {
                if (arg[0] == '-' && arg[1] == '-')
                {
                    var argName = arg[2..];
                    foreach (var property in properties)
                    {
                        var propName = property.Name;
                        var attr = property.GetCustomAttributes<ArgumentNameAttribute>(false).FirstOrDefault();
                        if (attr != null)
                        {
                            propName = attr.Name;
                        }

                        if (argName.Equals(propName, StringComparison.OrdinalIgnoreCase))
                        {
                            if (split.MoveNext())
                            {
                                var valueRange = split.Current;
                                var value = args[valueRange.Start..valueRange.End];
                                var convertedValue = Convert.ChangeType(value.ToString(), property.PropertyType);

                                property.SetValue(arguments, convertedValue);
                                break;
                            }
                        }
                    }
                }
            }
        }

        return arguments;
    }

    private static void LoadDll()
    {
        var currentDir = AppContext.BaseDirectory;
        var platform = OperatingSystem.IsWindows() ? "win" :
                       OperatingSystem.IsLinux() ? "linux" :
                       OperatingSystem.IsMacOS() ? "osx" : "unknown";
        var arch = Environment.Is64BitProcess ? "x64" : "x86";
        var nativeDllDir = Path.Combine(currentDir, "runtimes", platform + "-" + arch, "native");
        if (Directory.Exists(nativeDllDir))
        {
            foreach (var dll in Directory.EnumerateFiles(nativeDllDir, "*.dll"))
            {
                NativeLibrary.Load(dll);
            }
        }
    }

    public static async Task HandleAsync(LaunchArguments args)
    {
        var opts = new AllocationManagerInitOpts
        {
            ArenaCapacity = 1024 * 1024 * 1024, // 1 GB. Arena using virtual memory, so this is just a reservation and won't actually consume physical memory until used.
            StackCapacity = 1024 * 1024 * 32, // 32 MB. Stack using virtual memory, so this is just a reservation and won't actually consume physical memory until used.
            FreeListConcurrencyLevel = Environment.ProcessorCount
        };

        AllocationManager.Initialize(opts);

        await Task.Run(() =>
        {
            TypeCache.Init();
            //LoadDll();
            //App.GetService<EngineCore>();
        });

        // await ((Core.AssetHandle.AssetService)App.GetService<IAssetService>()).Init();

        // TODO: Init other subsystems here.
        // await Task.Delay(10000); // Wait 10 seconds to simulate work.
    }
}