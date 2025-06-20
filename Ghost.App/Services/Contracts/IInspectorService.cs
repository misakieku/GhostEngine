using Ghost.Editor.Core.Inspector;

namespace Ghost.Editor.Services.Contracts;

internal interface IInspectorService
{
    public IInspectable? SelectedInspectable
    {
        get;
        set;
    }

    public event Action? OnSelectionChanged;
}