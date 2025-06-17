using Ghost.Editor.Core.AppState;
using Ghost.Editor.Services;
using Ghost.Editor.Services.Contracts;
using Ghost.Editor.Utilities;
using Ghost.Engine.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ghost.Editor;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class EditorApplication : Application
{
    private Window? _window;

    internal static Window? Window
    {
        get => (Current as EditorApplication)!._window;
        set
        {
            if (Current is EditorApplication app)
            {
                app._window = value;
            }
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
    internal EditorApplication()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((context, services) =>
            {
                HostHelper.AddLandingScope(context, services);
                HostHelper.AddEngineScope(context, services);

                services.AddSingleton<AppStateMachine>();
                services.AddSingleton<INotificationService, NotificationService>();
                services.AddSingleton<IProgressService, ProgressService>();
                services.AddSingleton<IInspectorService, InspectorService>();
            })
            .Build();

        UnhandledException += App_UnhandledException;
    }

    internal static IServiceScope CreateScope()
    {
        return (Current as EditorApplication)!.Host.Services.CreateScope();
    }

    public static T GetService<T>() where T : class
    {
        if ((Current as EditorApplication)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        Host.Start();
        ActivationHandler.Handle(args);

        var stateMachine = GetService<AppStateMachine>();
        stateMachine.RegisterState(StateKey.Landing, () => new LandingState());
        stateMachine.RegisterState(StateKey.EngineEditor, () => new EditorState());

        await stateMachine.TransitionToAsync(StateKey.Landing);
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Logger.LogError(e.Exception);
    }
}