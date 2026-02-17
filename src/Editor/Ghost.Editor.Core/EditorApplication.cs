using Ghost.Editor.Core.Contracts;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core;

public static class EditorApplication
{
    public const string ASSETS_FOLDER_NAME = "Assets";
    public const string CACHES_FOLDER_NAME = "Caches";

    private static IServiceProvider? s_serviceProvider;
    private static string s_currentProjectPath = string.Empty;
    private static string s_currentProjectName = string.Empty;

    private static DispatcherQueue? s_dispatcherQueue;

    internal static Application CurrentApplication => Application.Current;
    internal static string CurrentProjectPath => s_currentProjectPath;
    internal static string CurrentProjectName => s_currentProjectName;

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
        if (s_serviceProvider?.GetService(typeof(IAssetService)) is AssetHandle.AssetService assetService)
        {
            assetService.Shutdown();
        }
    }
}