namespace Ghost.Editor.Core.Inspector;

public class InspectorService : IInspectorService
{
    public IInspectable? SelectedInspectable
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                OnSelectionChanged?.Invoke();
            }
        }
    }

    public event Action? OnSelectionChanged;
}