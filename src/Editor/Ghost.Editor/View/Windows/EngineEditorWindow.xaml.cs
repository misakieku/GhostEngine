using Ghost.Core;
using Ghost.Editor.Core;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.Services;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Ghost.Editor.View.Controls;
using Ghost.Editor.ViewModels.Windows;
using Microsoft.UI.Xaml;
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

        InitializeDockLayout();
    }

    private void InitializeDockLayout()
    {
        var root = new DockGroupNode { Orientation = Orientation.Horizontal };
        
        var leftGroup = new DockGroupNode { Orientation = Orientation.Vertical };
        var hierarchyPanel = new DockPanelNode();
        hierarchyPanel.Items.Add(new TabViewItem { Header = "Hierarchy", Content = new Hierarchy() });
        leftGroup.AddChild(hierarchyPanel);
        
        var centerGroup = new DockGroupNode { Orientation = Orientation.Vertical };
        var scenePanel = new DockPanelNode();
        scenePanel.Items.Add(new ScenePage { Header = "Scene" });
        centerGroup.AddChild(scenePanel);
        
        var rightGroup = new DockGroupNode { Orientation = Orientation.Vertical };
        var inspectorPanel = new DockPanelNode();
        inspectorPanel.Items.Add(new InspectorPage { Header = "Inspector" });
        rightGroup.AddChild(inspectorPanel);

        root.AddChild(leftGroup);
        root.AddChild(centerGroup);
        root.AddChild(rightGroup);

        PART_DockLayout.Root = root;
        PART_DockLayout.TabTornOff += (s, e) => App.CreateAndShowDockWindow(e.TabContent);
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