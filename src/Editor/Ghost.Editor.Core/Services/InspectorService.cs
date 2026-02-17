using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.Core.Services;

public class InspectorService : IInspectorService
{
    private IInspectable? _selected;

    public IInspectable? Selected => _selected;

    public event EventHandler<InspectorSelectionChangedEventArgs>? OnSelectionChanged;

    public void SetSelected(IInspectable? inspectable, object? source)
    {
        if (_selected != inspectable)
        {
            _selected = inspectable;
            OnSelectionChanged?.Invoke(this, new InspectorSelectionChangedEventArgs(source, inspectable));
        }
    }
}