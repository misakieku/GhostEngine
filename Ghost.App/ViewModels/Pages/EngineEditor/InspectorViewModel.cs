using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Contracts;
using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Services.Contracts;

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
        Inspectable = inspectorService.SelectedInspectable;
    }

    public void OnNavigatedFrom()
    {
        inspectorService.OnSelectionChanged -= OnSelectionChanged;
        Inspectable = null;
    }

    private void OnSelectionChanged()
    {
        Inspectable = inspectorService.SelectedInspectable;
    }
}