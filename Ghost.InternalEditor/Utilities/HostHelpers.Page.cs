using Ghost.App.View.Pages.EngineEditor;
using Ghost.App.View.Pages.Landing;
using Ghost.App.View.Windows;
using Ghost.Data.Services;
using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Ghost.Editor.ViewModels.Pages.Landing;
using Ghost.Editor.ViewModels.Windows;
using Ghost.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ghost.App.Utilities;

internal static partial class HostHelper
{
    public static void AddLandingScope(HostBuilderContext context, IServiceCollection services)
    {
        services.AddTransient<LandingWindow>();

        services.AddTransient<CreateProjectPage>();
        services.AddTransient<CreateProjectViewModel>();

        services.AddTransient<OpenProjectPage>();
        services.AddTransient<OpenProjectViewModel>();

        services.AddTransient<ProjectService>();
    }

    public static void AddEngineScope(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<EngineCore>();

        services.AddTransient<EngineEditorWindow>();
        services.AddTransient<EngineEditorViewModel>();

        services.AddTransient<HierarchyPage>();
        services.AddTransient<HierarchyViewModel>();

        services.AddTransient<ProjectPage>();
        services.AddTransient<ProjectViewModel>();

        services.AddTransient<ConsolePage>();
        services.AddTransient<ConsoleViewModel>();
    }
}