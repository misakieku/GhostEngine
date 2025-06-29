using Ghost.Data.Resources;
using Ghost.Editor.Core.Notifications;
using Ghost.Editor.View.Pages.Landing;
using Ghost.Engine.Resources;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using WinUIEx;

namespace Ghost.Editor.View.Windows;

internal sealed partial class LandingWindow : WindowEx
{
    private readonly NotificationService _notificationService;

    private int _previousSelectedIndex;

    public LandingWindow()
    {
        _notificationService = (NotificationService)App.GetService<INotificationService>();

        AppWindow.SetIcon(AssetsPath.s_appIconPath);
        Title = EngineData.ENGINE_NAME;

        InitializeComponent();

        this.SetWindowSize(1000, 750);
        this.CenterOnScreen();

        ExtendsContentIntoTitleBar = true;
    }

    private void WindowEx_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        _notificationService.SetReference(InfoBar, NotificationQueue);
    }

    private void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
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

        ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        _previousSelectedIndex = currentSelectedIndex;
    }
}