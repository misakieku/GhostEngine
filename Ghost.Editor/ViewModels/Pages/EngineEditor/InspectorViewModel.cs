using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.ViewModels.Pages.EngineEditor;

internal partial class InspectorViewModel(IInspectorService inspectorService) : ObservableObject, INavigationAware
{
    [ObservableProperty]
    public partial IInspectable? Inspectable
    {
        get;
        set;
    }

    public void OnNavigatedTo(object? parameter)
    {
        inspectorService.OnSelectionChanged += OnSelectionChanged;
        Inspectable = inspectorService.Selected;
    }

    public void OnNavigatedFrom()
    {
        inspectorService.OnSelectionChanged -= OnSelectionChanged;
        Inspectable = null;
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        Inspectable = inspectorService.Selected;
    }
}