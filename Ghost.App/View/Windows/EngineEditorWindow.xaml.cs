using Ghost.App.Services;
using Ghost.Data.Resources;
using Ghost.Editor.Services.Contracts;
using Ghost.Editor.ViewModels.Windows;
using Ghost.Engine.Resources;
using Microsoft.Extensions.DependencyInjection;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ghost.App.View.Windows;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class EngineEditorWindow : WindowEx
{
    private IServiceScope? _editorScope;

    private readonly NotificationService _notificationService;
    private readonly ProgressService _progressService;

    public EngineEditorViewModel ViewModel
    {
        get;
    }

    public EngineEditorWindow()
    {
        ViewModel = GhostApplication.GetService<EngineEditorViewModel>();

        _notificationService = (NotificationService)GhostApplication.GetService<INotificationService>();
        _progressService = (ProgressService)GhostApplication.GetService<IProgressService>();

        AppWindow.SetIcon(AssetsPath.s_appIconPath);
        Title = EngineData.ENGINE_NAME;
        ExtendsContentIntoTitleBar = true;

        InitializeComponent();

        this.CenterOnScreen();
    }

    private void WindowEx_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        Bindings.Update();

        _editorScope?.Dispose();
        _editorScope = GhostApplication.CreateScope();

        _notificationService.SetReference(InfoBar, NotificationQueue);
        _progressService.SetReference(ProgressBarContainer);
    }

    private void WindowEx_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        _editorScope?.Dispose();

        _notificationService.ClearReference();
        _progressService.ClearReference();
    }
}