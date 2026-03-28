using Ghost.Core;
using Ghost.Editor.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Ghost.Editor.View.Controls;
using Ghost.Editor.ViewModels.Windows;
using Windows.ApplicationModel;
using WinUIEx;

namespace Ghost.Editor.View.Windows;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class EngineEditorWindow : WindowEx
{
    private readonly NotificationService _notificationService;
    private readonly ProgressService _progressService;

    public EngineEditorViewModel ViewModel
    {
        get;
    }

    public EngineEditorWindow()
    {
        ViewModel = App.GetService<EngineEditorViewModel>();

        _notificationService = (NotificationService)App.GetService<INotificationService>();
        _progressService = (ProgressService)App.GetService<IProgressService>();

        InitializeComponent();

        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets/icon.ico"));
        Title = "Ghost Engine";
        ExtendsContentIntoTitleBar = true;

        SetTitleBar(PART_TitleBar);
        this.CenterOnScreen();
    }

    private void OnTabDroppedOutside(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabDroppedOutsideEventArgs args)
    {
        // For static tabs in EngineEditorWindow, we remove the item from TabItems
        if (sender.TabItems is System.Collections.IList list)
        {
            if (list.Contains(args.Item))
            {
                var result = TabTearOffService.TryTearOffTab(list, args.Item, (tab) =>
                {
                    App.CreateAndShowDockWindow(tab);
                }, sender);
                
                if (!result.IsSuccess)
                {
                    Logger.LogWarning($"Tab tear-off failed: {result.Message}");
                }
            }
            else
            {
                string itemInfo = args.Item is FrameworkElement fe ? fe.GetType().Name : args.Item?.ToString() ?? "unknown";
                Logger.LogWarning($"OnTabDroppedOutside: Item '{itemInfo}' not found in source TabView (Items count: {list.Count}).");
            }
        }
    }

    private void MainGrid_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PART_TitleBar.Title = EditorApplication.ProjectName;
        PART_TitleBar.Subtitle = $"Ghost Engine {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

        _notificationService.SetReference(InfoBar, NotificationQueue);
        _progressService.SetReference(ProgressBarContainer);
    }

    private void MainGrid_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _notificationService.ClearReference();
        _progressService.ClearReference();
    }
}