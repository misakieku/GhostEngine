using Ghost.Editor.Controls;
using Ghost.Editor.ViewModels.Pages.EngineEditor;

namespace Ghost.Editor.Views.Pages.EngineEditor;

internal sealed partial class InspectorPage : NavigationTabPage
{
    public InspectorViewModel ViewModel
    {
        get;
    }

    public InspectorPage()
    {
        ViewModel = App.GetService<InspectorViewModel>();

        InitializeComponent();
    }

    public override void OnNavigatedTo(object? parameter)
    {
        ViewModel.OnNavigatedTo(parameter);
    }

    public override void OnNavigatedFrom()
    {
        ViewModel.OnNavigatedFrom();
    }
}
