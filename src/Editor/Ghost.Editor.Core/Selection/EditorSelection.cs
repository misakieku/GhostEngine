using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Selection;

public sealed class EditorSelection
{
    private readonly HashSet<object> _selectedObjects = new();

    public static EditorSelection Instance { get; } = new();

    public object? ActiveObject { get; private set; }

    public EntityNode? ActiveEntity => ActiveObject as EntityNode;

    public IReadOnlySet<object> SelectedObjects => _selectedObjects;

    public event Action? SelectionChanged;

    public void Select(object? target)
    {
        if (target == null)
        {
            Clear();
            return;
        }

        if (_selectedObjects.Count == 1 && ReferenceEquals(ActiveObject, target))
        {
            return;
        }

        _selectedObjects.Clear();
        _selectedObjects.Add(target);
        ActiveObject = target;
        SelectionChanged?.Invoke();
    }

    public void SelectMany(IEnumerable<object> targets)
    {
        _selectedObjects.Clear();
        object? first = null;
        foreach (var t in targets)
        {
            first ??= t;
            _selectedObjects.Add(t);
        }

        ActiveObject = first;
        SelectionChanged?.Invoke();
    }

    public void ToggleSelection(object target)
    {
        if (_selectedObjects.Contains(target))
        {
            _selectedObjects.Remove(target);
            if (ReferenceEquals(ActiveObject, target))
            {
                ActiveObject = _selectedObjects.FirstOrDefault();
            }
        }
        else
        {
            _selectedObjects.Add(target);
            ActiveObject = target;
        }

        SelectionChanged?.Invoke();
    }

    public void Deselect(object target)
    {
        if (_selectedObjects.Remove(target))
        {
            if (ReferenceEquals(ActiveObject, target))
            {
                ActiveObject = _selectedObjects.FirstOrDefault();
            }

            SelectionChanged?.Invoke();
        }
    }

    public void Clear()
    {
        if (_selectedObjects.Count == 0 && ActiveObject == null)
        {
            return;
        }

        _selectedObjects.Clear();
        ActiveObject = null;
        SelectionChanged?.Invoke();
    }
}
