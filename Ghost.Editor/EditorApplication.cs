namespace Ghost.Editor;

public static class EditorApplication
{
    private static IServiceProvider? _serviceProvider;

    public static bool IsInitialized => _serviceProvider != null;

    internal static void Activate(object? parameters, IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }


    public static T GetService<T>() where T : class
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("EditorApplication is not initialized.");
        }

        if (_serviceProvider!.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in the service provider.");
        }

        return service;
    }
}