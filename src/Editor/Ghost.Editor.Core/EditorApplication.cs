using Ghost.Editor.Core.Utilities;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core;

public static class EditorApplication
{
    public const string ASSETS_FOLDER_NAME = "Assets";
    public const string PACKAGES_FOLDER_NAME = "Packages";
    public const string LIBRARY_FOLDER_NAME = "Library";
    public const string CACHE_FOLDER_NAME = "Cache";
    public const string CONFIG_FOLDER_NAME = "Config";

    public const string IMPORTS_FOLDER_NAME = "Imports";

    private static IServiceProvider? s_serviceProvider;

    private static string s_currentProjectPath = string.Empty;
    private static string s_currentProjectName = string.Empty;

    private static string s_assetsFolderPath = string.Empty;
    private static string s_packagesFolderPath = string.Empty;
    private static string s_libraryFolderPath = string.Empty;
    private static string s_cacheFolderPath = string.Empty;
    private static string s_configFolderPath = string.Empty;

    private static string s_libraryImportsFolderPath = string.Empty;

    private static DispatcherQueue? s_dispatcherQueue;

    internal static Application CurrentApplication => Application.Current;

    public static string ProjectPath => s_currentProjectPath;
    public static string ProjectName => s_currentProjectName;

    public static string AssetsFolderPath => s_assetsFolderPath;
    public static string PackagesFolderPath => s_packagesFolderPath;
    public static string LibraryFolderPath => s_libraryFolderPath;
    public static string ConfigFolderPath => s_configFolderPath;
    public static string CacheFolderPath => s_cacheFolderPath;
    public static string LibraryImportsFolderPath => s_libraryImportsFolderPath;

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
        projectPath = PathUtility.Normalize(projectPath);

        Environment.CurrentDirectory = projectPath;

        s_serviceProvider = serviceProvider;
        s_currentProjectPath = projectPath;
        s_currentProjectName = projectName;

        s_assetsFolderPath = Path.Combine(projectPath, ASSETS_FOLDER_NAME);
        s_packagesFolderPath = Path.Combine(projectPath, PACKAGES_FOLDER_NAME);
        s_libraryFolderPath = Path.Combine(projectPath, LIBRARY_FOLDER_NAME);
        s_configFolderPath = Path.Combine(projectPath, CONFIG_FOLDER_NAME);
        s_cacheFolderPath = Path.Combine(projectPath, CACHE_FOLDER_NAME);

        s_libraryImportsFolderPath = Path.Combine(s_libraryFolderPath, IMPORTS_FOLDER_NAME);

        Directory.CreateDirectory(s_assetsFolderPath);
        Directory.CreateDirectory(s_packagesFolderPath);
        Directory.CreateDirectory(s_libraryFolderPath);
        Directory.CreateDirectory(s_configFolderPath);
        Directory.CreateDirectory(s_cacheFolderPath);

        Directory.CreateDirectory(s_libraryImportsFolderPath);
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
