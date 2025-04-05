using Ghost.Data.Models;
using Ghost.Data.Resources;
using Ghost.Editor.ViewModel.Windows;
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
    public EngineEditorViewModel ViewModel
    {
        get;
    }

    public EngineEditorWindow()
    {
        ViewModel = App.GetService<EngineEditorViewModel>();

        AppWindow.SetIcon(AssetsPath.AppIconPath);
        Title = EngineData.ENGINE_NAME;
        ExtendsContentIntoTitleBar = true;

        InitializeComponent();

        this.CenterOnScreen();
    }

    public static bool TryLoadProject(ProjectInfo project)
    {
        try
        {
            var window = App.GetService<EngineEditorWindow>();
            window.ViewModel.CurrentProject = project;

            window.Activate();
            window.Bindings.Update();

            App.SetWindow(window);

            return true;
        }
        catch (System.Exception)
        {
            return false;
        }
    }
}