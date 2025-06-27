using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Utilities;

public static class EditorApplication
{
    private static IServiceProvider? _serviceProvider;

    public static Application Current => Application.Current;

    internal static void Initialize(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public static T GetService<T>()
        where T : class
    {
        if (_serviceProvider?.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }
}