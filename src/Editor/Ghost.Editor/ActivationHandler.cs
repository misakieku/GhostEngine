using Ghost.Editor.Core.Utilities;
using Ghost.Editor.Models;
using Ghost.Engine;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Reflection;

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

    public static ValueTask HandleAsync(LaunchArguments args)
    {
        var opts = new AllocationManagerInitOpts
        {
            ArenaCapacity = 1024 * 1024 * 1024, // 1 GB. Arena using virtual memory, so this is just a reservation and won't actually consume physical memory until used.
            StackCapacity = 1024 * 1024 * 32, // 32 MB. Stack using virtual memory, so this is just a reservation and won't actually consume physical memory until used.
            FreeListConcurrencyLevel = Environment.ProcessorCount
        };

        AllocationManager.Initialize(opts);
        
        //App.GetService<EngineCore>();

        return ValueTask.CompletedTask;
    }

    public static void Shutdown()
    {
        AllocationManager.Dispose();
    }
}