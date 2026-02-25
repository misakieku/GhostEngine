using Ghost.Editor.Core.Utilities;
using Ghost.Editor.Models;
using Ghost.Engine;
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

    public static async Task HandleAsync(LaunchArguments args)
    {
        await Task.Run(() =>
        {
            TypeCache.Init();
            ((EngineCore)App.GetService<IEngineContext>()).Init();
        });

        await ((Core.AssetHandle.AssetService)App.GetService<IAssetService>()).Init();

        // TODO: Init other subsystems here.
        // await Task.Delay(10000); // Wait 10 seconds to simulate work.
    }
}