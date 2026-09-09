using Ghost.AssetForge.Core.Services;
using Ghost.AssetForge.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;

namespace Ghost.AssetForge;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static IHost AppHost { get; private set; } = null!;
    public static Window MainWindowInstance { get; private set; } = null!;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();

        AppHost = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Register Core services
                services.AddSingleton<BakerRegistry>();
                services.AddSingleton<ProjectService>();
                services.AddTransient<BakeService>();
                services.AddTransient<PackService>();
                services.AddSingleton<Views.Inspector.InspectorDrawerRegistry>();

                // Register ViewModels
                services.AddSingleton<MainViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<ProjectExplorerViewModel>();
                services.AddTransient<PackingViewModel>();

                // Register Views
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await AppHost.StartAsync();

        MainWindowInstance = AppHost.Services.GetRequiredService<MainWindow>();
        MainWindowInstance.Activate();
    }
}
