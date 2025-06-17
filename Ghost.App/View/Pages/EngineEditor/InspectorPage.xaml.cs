using Ghost.Editor.Controls.Internal;
using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Pages.EngineEditor;

internal sealed partial class InspectorPage : NavigationTabPage
{
    public InspectorViewModel ViewModel
    {
        get;
    }

    public InspectorPage()
    {
        ViewModel = EditorApplication.GetService<InspectorViewModel>();

        InitializeComponent();

        Header = "Inspector";
        IconSource = new FontIconSource
        {
            Glyph = "\uEC7A"
        };
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
