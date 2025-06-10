using Ghost.App.Services;
using Ghost.App.View.Pages.Landing;
using Ghost.Data.Resources;
using Ghost.Editor.Services.Contracts;
using Ghost.Engine.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using WinUIEx;

namespace Ghost.App.View.Windows;

internal sealed partial class LandingWindow : WindowEx
{
    private IServiceScope? _landingScope;

    private readonly NotificationService _notificationService;

    private int _previousSelectedIndex;

    public LandingWindow()
    {
        _notificationService = (NotificationService)GhostApplication.GetService<INotificationService>();

        AppWindow.SetIcon(AssetsPath.s_appIconPath);
        Title = EngineData.ENGINE_NAME;

        InitializeComponent();

        this.SetWindowSize(1000, 750);
        this.CenterOnScreen();

        ExtendsContentIntoTitleBar = true;
    }

    private void WindowEx_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        _landingScope?.Dispose();
        _landingScope = GhostApplication.CreateScope();
        _notificationService.SetReference(InfoBar, NotificationQueue);
    }

    private void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        _landingScope?.Dispose();
        _notificationService.ClearReference();
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs e)
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        var pageType = currentSelectedIndex switch
        {
            1 => typeof(CreateProjectPage),
            _ => typeof(OpenProjectPage),
        };

        var slideNavigationTransitionEffect = currentSelectedIndex - _previousSelectedIndex > 0 ?
            SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(pageType, _landingScope, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        _previousSelectedIndex = currentSelectedIndex;
    }
}