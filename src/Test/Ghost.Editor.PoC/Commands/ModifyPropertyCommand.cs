using Ghost.Editor.PoC.Nodes;

namespace Ghost.Editor.PoC.Commands;

public sealed class ModifyPropertyCommand : IEditorCommand
{
    private DateTime _timestamp;

    public PropertyNode Property { get; }
    public object? OldValue { get; }
    public object? NewValue { get; private set; }

    public string Name => $"Modify {Property.ComponentNode.DisplayName}.{Property.Name}";

    public ModifyPropertyCommand(PropertyNode property, object? oldValue, object? newValue)
    {
        Property = property;
        OldValue = oldValue;
        NewValue = newValue;
        _timestamp = DateTime.UtcNow;
    }

    public void Execute()
    {
        Property.SetValue(NewValue);
    }

    public void Undo()
    {
        Property.SetValue(OldValue);
    }

    public bool CanMergeWith(IEditorCommand other)
    {
        if (other is ModifyPropertyCommand mod && mod.Property == Property)
        {
            return (DateTime.UtcNow - _timestamp).TotalMilliseconds < 500;
        }

        return false;
    }

    public void MergeWith(IEditorCommand other)
    {
        if (other is ModifyPropertyCommand mod)
        {
            NewValue = mod.NewValue;
            _timestamp = DateTime.UtcNow;
            Execute();
        }
    }
}
