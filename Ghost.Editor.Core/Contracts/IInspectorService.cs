namespace Ghost.Editor.Core.Contracts;

public class InspectorSelectionChangedEventArgs : EventArgs
{
    public object? Source
    {
        get;
    }

    public IInspectable? Selected
    {
        get;
    }

    public InspectorSelectionChangedEventArgs(object? source, IInspectable? selected)
    {
        Source = source;
        Selected = selected;
    }
}

public interface IInspectorService
{
    IInspectable? Selected
    {
        get;
    }

    event EventHandler<InspectorSelectionChangedEventArgs> OnSelectionChanged;

    void SetSelected(IInspectable? inspectable, object? source);
}