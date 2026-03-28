using Ghost.Core;
using Ghost.Editor.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
using Ghost.Editor.View.Pages.EngineEditor;
using Ghost.Editor.View.Windows;
using Ghost.Editor.ViewModels.Controls;
using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Ghost.Editor.ViewModels.Windows;
using Ghost.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using WinUIEx;

namespace Ghost.Editor;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    private readonly List<Window> _secondaryWindows = new();

    internal static Window? Window
    {
        get => (Current as App)!._window;
        set
        {
            if (Current is App app)
            {
                // HACK: As far as I can tell, there is no proper application shutdown event in WinUI 3.
                app._window?.Closed -= app.OnClosed;
                app._window = value;
                app._window?.Closed += app.OnClosed;
            }
        }
    }

    internal static void AddSecondaryWindow(Window window)
    {
        if (Current is App app)
        {
            app._secondaryWindows.Add(window);
            window.Closed += (s, e) => app._secondaryWindows.Remove(window);
        }
        else
        {
            throw new InvalidOperationException("App instance is not available.");
        }
    }

    /// <summary>
    /// Creates, registers, and shows a new DockWindow for a torn-off tab.
    /// </summary>
    internal static void CreateAndShowDockWindow(object tabContent)
    {
        var newWindow = new Ghost.Editor.View.Windows.DockWindow(tabContent);
        try
        {
            AddSecondaryWindow(newWindow);
            newWindow.Activate();
        }
        catch (Exception ex)
        {
            // Cleanup partially created window
            if (Current is App app)
            {
                app._secondaryWindows.Remove(newWindow);
            }
            newWindow.Close();
            Logger.LogError(ex);
            throw;
        }
    }

    internal IHost Host
    {
        get;
    }

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    internal App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                services.AddSingleton<EngineCore>();

                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IProgressService, ProgressService>();
                services.AddSingleton<IInspectorService, InspectorService>();
                services.AddSingleton<IPreviewService, PreviewService>();
                // services.AddSingleton<IAssetService, AssetService>();

                services.AddSingleton<EngineEditorViewModel>();

                services.AddTransient<ProjectBrowserViewModel>();

                #region Should be deleted
                services.AddTransient<ScenePage>();

                services.AddTransient<HierarchyPage>();
                services.AddTransient<HierarchyViewModel>();

                services.AddTransient<ProjectPage>();
                services.AddTransient<ProjectViewModel>();

                services.AddTransient<ConsolePage>();
                services.AddTransient<ConsoleViewModel>();

                services.AddTransient<InspectorPage>();
                services.AddTransient<InspectorViewModel>();
                #endregion
            })
            .Build();

        UnhandledException += App_UnhandledException;
    }

    internal static IServiceScope CreateScope()
    {
        return (Current as App)!.Host.Services.CreateScope();
    }

    public static T GetService<T>() where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        var arguments = ActivationHandler.ParseArguments("--project-path F:/GhostProject/Test2 --project-name Test2"); // args.Arguments
        if (!arguments.IsValid())
        {
            Exit();
            return;
        }

        EditorApplication.Initialize(Host.Services, arguments.ProjectPath, arguments.ProjectName);

        // NOTE: We must call DispatcherQueue.GetForCurrentThread() on the UI thread before any await.
        EditorApplication.SetDispatcherQueue(DispatcherQueue.GetForCurrentThread());

        var splashWindow = new SplashWindow();
        splashWindow.Activate();
        Window = splashWindow;

        await Host.StartAsync();
        await ActivationHandler.HandleAsync(arguments);

        splashWindow.Hide();

        var editorWindow = new EngineEditorWindow();
        editorWindow.Activate();
        Window = editorWindow;

        splashWindow.Close();
    }

    private void OnClosed(object? sender, WindowEventArgs args)
    {
        try
        {
            // Close all secondary windows when the primary window closes
            var secondaryWindows = _secondaryWindows.ToArray();
            foreach (var window in secondaryWindows)
            {
                window.Close();
            }
            _secondaryWindows.Clear();

            Host.StopAsync().GetAwaiter().GetResult();
            Host.Dispose();

            EditorApplication.Shutdown();
        }
        catch (Exception ex)
        {
            Debugger.BreakForUserUnhandledException(ex);
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Logger.LogError(e.Exception);
#if DEBUG
        Debugger.BreakForUserUnhandledException(e.Exception);
#endif
    }
}
