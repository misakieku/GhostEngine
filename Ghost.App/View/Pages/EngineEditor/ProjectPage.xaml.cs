using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Ghost.Editor.View.Pages.EngineEditor;

internal sealed partial class ProjectPage : Page
{
    public ProjectViewModel ViewModel
    {
        get;
    }

    public ProjectPage()
    {
        ViewModel = EditorApplication.GetService<ProjectViewModel>();

        InitializeComponent();
    }

    private void GridViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ViewModel.OpenSelected();
    }
}