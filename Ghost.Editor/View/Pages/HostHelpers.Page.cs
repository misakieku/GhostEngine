using Ghost.Editor.View.Pages.Landing;
using Ghost.Editor.View.Windows;
using Ghost.Editor.ViewModel.Pages.Landing;
using Ghost.Editor.ViewModel.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ghost.Editor.View.Pages;

internal static partial class HostHelper
{
    public static void SetupPageService(HostBuilderContext context, IServiceCollection services)
    {
        services.AddTransient<LandingWindow>();
        services.AddTransient<LandingViewModel>();

        services.AddTransient<CreateProjectPage>();
        services.AddTransient<CreateProjectViewModel>();

        services.AddTransient<OpenProjectPage>();
    }
}