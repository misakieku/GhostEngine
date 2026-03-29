using Ghost.Editor.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
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

    private void MainGrid_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        PART_TitleBar.Title = EditorApplication.ProjectName;
        PART_TitleBar.Subtitle = $"Ghost Engine {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}";

        _notificationService.SetReference(InfoBar, NotificationQueue);
        _progressService.SetReference(ProgressBarContainer);

        InitializeDockingLayout();
    }

    private void InitializeDockingLayout()
    {
        var sceneDoc = new Controls.Docking.DockDocument { Title = "Scene", Content = new Pages.EngineEditor.ScenePage() };
        var hierarchyDoc = new Controls.Docking.DockDocument { Title = "Hierarchy", Content = new Controls.Hierarchy() };
        var inspectorDoc = new Controls.Docking.DockDocument { Title = "Inspector", Content = new Pages.EngineEditor.InspectorPage() };
        var projectDoc = new Controls.Docking.DockDocument { Title = "Project", Content = new Controls.ProjectBrowser() };
        var consoleDoc = new Controls.Docking.DockDocument { Title = "Console", Content = new Pages.EngineEditor.ConsolePage() };

        var leftGroup = new Controls.Docking.DockGroup();
        leftGroup.AddChild(hierarchyDoc);

        var centerGroup = new Controls.Docking.DockGroup();
        centerGroup.AddChild(sceneDoc);

        var rightGroup = new Controls.Docking.DockGroup();
        rightGroup.AddChild(inspectorDoc);

        var bottomGroup = new Controls.Docking.DockGroup();
        bottomGroup.AddChild(projectDoc);
        bottomGroup.AddChild(consoleDoc);

        var topPanel = new Controls.Docking.DockPanel { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal };
        topPanel.AddChild(leftGroup);
        topPanel.AddChild(centerGroup);
        topPanel.AddChild(rightGroup);

        var rootPanel = new Controls.Docking.DockPanel { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Vertical };
        rootPanel.AddChild(topPanel);
        rootPanel.AddChild(bottomGroup);

        MainDockingLayout.RootModule = rootPanel;
    }

    private void MainGrid_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _notificationService.ClearReference();
        _progressService.ClearReference();
    }
}