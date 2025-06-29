using Ghost.Data.Resources;
using Ghost.Editor.Core.Notifications;
using Ghost.Editor.Core.Progress;
using Ghost.Editor.ViewModels.Windows;
using Ghost.Engine.Resources;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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

        AppWindow.SetIcon(AssetsPath.s_appIconPath);
        Title = EngineData.ENGINE_NAME;
        ExtendsContentIntoTitleBar = true;

        InitializeComponent();

        this.CenterOnScreen();
    }

    private void WindowEx_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        Bindings.Update();

        _notificationService.SetReference(InfoBar, NotificationQueue);
        _progressService.SetReference(ProgressBarContainer);
    }

    private void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        _notificationService.ClearReference();
        _progressService.ClearReference();
    }
}