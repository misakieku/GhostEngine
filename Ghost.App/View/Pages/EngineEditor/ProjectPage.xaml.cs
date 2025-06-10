using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Ghost.App.View.Pages.EngineEditor;

internal sealed partial class ProjectPage : Page
{
    public ProjectViewModel ViewModel
    {
        get;
    }

    public ProjectPage()
    {
        ViewModel = GhostApplication.GetService<ProjectViewModel>();

        InitializeComponent();
    }

    private async void GridViewItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        await ViewModel.OpenSelected();
    }
}