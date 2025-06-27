using Ghost.Data.Services;
using Ghost.Editor.View.Pages.EngineEditor;
using Ghost.Editor.View.Pages.Landing;
using Ghost.Editor.View.Windows;
using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Ghost.Editor.ViewModels.Pages.Landing;
using Ghost.Editor.ViewModels.Windows;
using Ghost.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ghost.Editor.Utilities;

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

        services.AddTransient<ScenePage>();

        services.AddTransient<HierarchyPage>();
        services.AddTransient<HierarchyViewModel>();

        services.AddTransient<ProjectPage>();
        services.AddTransient<ProjectViewModel>();

        services.AddTransient<ConsolePage>();
        services.AddTransient<ConsoleViewModel>();

        services.AddTransient<InspectorPage>();
        services.AddTransient<InspectorViewModel>();
    }
}