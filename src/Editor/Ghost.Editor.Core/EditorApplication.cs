using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core;

public static class EditorApplication
{
    public const string ASSETS_FOLDER_NAME = "Assets";
    public const string SOURCES_FOLDER_NAME = "Sources";
    public const string PACKAGES_FOLDER_NAME = "Packages";
    public const string LIBRARY_FOLDER_NAME = "Library";
    public const string CONFIG_FOLDER_NAME = "Config";

    private static IServiceProvider? s_serviceProvider;
    private static string s_currentProjectPath = string.Empty;
    private static string s_currentProjectName = string.Empty;

    private static DispatcherQueue? s_dispatcherQueue;

    internal static Application CurrentApplication => Application.Current;

    public static string ProjectPath => s_currentProjectPath;
    public static string ProjectName => s_currentProjectName;

    public static string AssetsFolderPath => Path.Combine(ProjectPath, ASSETS_FOLDER_NAME);
    public static string SourcesFolderPath => Path.Combine(ProjectPath, SOURCES_FOLDER_NAME);
    public static string PackagesFolderPath => Path.Combine(ProjectPath, PACKAGES_FOLDER_NAME);
    public static string LibraryFolderPath => Path.Combine(ProjectPath, LIBRARY_FOLDER_NAME);
    public static string ConfigFolderPath => Path.Combine(ProjectPath, CONFIG_FOLDER_NAME);

    public static DispatcherQueue DispatcherQueue
    {
        get
        {
            if (s_dispatcherQueue is null)
            {
                throw new InvalidOperationException("DispatcherQueue is not initialized.");
            }

            return s_dispatcherQueue;
        }
    }

    internal static void Initialize(IServiceProvider serviceProvider, string projectPath, string projectName)
    {
        s_serviceProvider = serviceProvider;
        s_currentProjectPath = projectPath;
        s_currentProjectName = projectName;
    }

    internal static void SetDispatcherQueue(DispatcherQueue dispatcherQueue)
    {
        s_dispatcherQueue = dispatcherQueue;
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