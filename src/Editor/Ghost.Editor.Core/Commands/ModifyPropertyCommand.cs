using Ghost.Editor.Core.Selection;
using Ghost.Editor.Core.Sync;

namespace Ghost.Editor.Core.Commands;

public sealed class ModifyPropertyCommand : IEditorCommand
{
    private readonly object? _oldValue;
    private object? _newValue;
    private DateTime _timestamp;

    public PropertyNode Property { get; }

    public ushort? TargetSceneId => Property.ComponentNode?.EntityNode?.SceneId;

    public string Name => $"Set {(Property.ComponentNode != null ? Property.ComponentNode.TypeName + "." : "")}{Property.Name}";

    public ModifyPropertyCommand(PropertyNode property, object? oldValue, object? newValue)
    {
        Property = property;
        _oldValue = oldValue;
        _newValue = newValue;
        _timestamp = DateTime.UtcNow;
    }

    public void Execute()
    {
        Property.ApplyValue(_newValue);
    }

    public void Undo()
    {
        Property.ApplyValue(_oldValue);
        if (Property.ComponentNode?.EntityNode != null)
        {
            EditorSelection.Instance.Select(Property.ComponentNode.EntityNode);
        }
    }

    public void Redo()
    {
        Property.ApplyValue(_newValue);
        if (Property.ComponentNode?.EntityNode != null)
        {
            EditorSelection.Instance.Select(Property.ComponentNode.EntityNode);
        }
    }

    public bool CanMergeWith(IEditorCommand other)
    {
        if (other is ModifyPropertyCommand mod && ReferenceEquals(mod.Property, Property))
        {
            return (DateTime.UtcNow - _timestamp).TotalMilliseconds < 500.0;
        }

        return false;
    }

    public void MergeWith(IEditorCommand other)
    {
        if (other is ModifyPropertyCommand mod)
        {
            _newValue = mod._newValue;
            _timestamp = DateTime.UtcNow;
        }
    }
}
