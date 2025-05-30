using Ghost.Data.Services;
using Ghost.Editor.View.Pages.Landing;
using Ghost.Editor.View.Windows;
using Ghost.Editor.ViewModel.Pages.Landing;
using Ghost.Editor.ViewModel.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ghost.Editor.Helpers;

internal static partial class HostHelper
{
    public static void AddLandingScope(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<LandingWindow>();

        services.AddTransient<CreateProjectPage>();
        services.AddTransient<CreateProjectViewModel>();

        services.AddTransient<OpenProjectPage>();

        services.AddTransient<ProjectService>();
    }

    public static void AddEngineScope(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<EngineEditorWindow>();
        services.AddSingleton<EngineEditorViewModel>();
    }
}