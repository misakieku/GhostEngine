using Ghost.Editor.Contracts;
using Ghost.Editor.Services.Contracts;

namespace Ghost.Editor.Services;

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