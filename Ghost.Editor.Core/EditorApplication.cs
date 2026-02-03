using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core;

public static class EditorApplication
{
    public const string ASSETS_FOLDER_NAME = "Assets";
    public const string CACHES_FOLDER_NAME = "Caches";

    private static IServiceProvider? s_serviceProvider;
    private static string s_currentProjectPath = string.Empty;
    private static string s_currentProjectName = string.Empty;

    internal static Application CurrentApplication => Application.Current;
    internal static string CurrentProjectPath => s_currentProjectPath;
    internal static string CurrentProjectName => s_currentProjectName;

    internal static void Initialize(IServiceProvider serviceProvider, string projectPath, string projectName)
    {
        s_serviceProvider = serviceProvider;
        s_currentProjectPath = projectPath;
        s_currentProjectName = projectName;
    }

    public static T GetService<T>()
        where T : class
    {
        if (s_serviceProvider?.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices.");
        }

        return service;
    }

    internal static void Shutdown()
    {
    }
}