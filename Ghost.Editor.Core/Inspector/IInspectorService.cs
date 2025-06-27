namespace Ghost.Editor.Core.Inspector;

internal interface IInspectorService
{
    public IInspectable? SelectedInspectable
    {
        get;
        set;
    }

    public event Action? OnSelectionChanged;
}