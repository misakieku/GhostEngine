using Ghost.Editor.PoC.Nodes;

namespace Ghost.Editor.PoC.Services;

public static class EditorSelection
{
    public static object? ActiveObject { get; private set; }

    public static EntityNode? ActiveEntity => ActiveObject as EntityNode;

    public static event Action<object?>? SelectionChanged;

    public static void Select(object? target)
    {
        if (ReferenceEquals(ActiveObject, target))
        {
            return;
        }

        ActiveObject = target;
        SelectionChanged?.Invoke(target);
    }

    public static void Clear()
    {
        Select(null);
    }
}
