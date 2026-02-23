using Ghost.Editor.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
using Ghost.Editor.ViewModels.Windows;
using System.Diagnostics;
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