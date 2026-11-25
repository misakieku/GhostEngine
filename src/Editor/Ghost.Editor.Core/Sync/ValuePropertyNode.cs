namespace Ghost.Editor.Core.Sync;

/// <summary>
/// Property node for inline value-type, enum, and string component fields backed directly by ECS memory.
/// </summary>
public sealed class ValuePropertyNode : PropertyNode
{
    public ValuePropertyNode(string name, Type propertyType, ComponentNode componentNode, object? value, Action<object?> onChange, bool isReadOnly = false, string? displayName = null, string? groupName = null)
        : base(name, propertyType, componentNode, value, onChange, isReadOnly, displayName, groupName)
    {
    }

    public override void ApplyValue(object? newValue)
    {
        Value = newValue;
        OnChange(newValue);
    }

    public override object? GetSerializableValue()
    {
        return Value;
    }

    public override PropertyNode Clone(ComponentNode targetComponent)
    {
        return new ValuePropertyNode(Name, PropertyType, targetComponent, Value, OnChange, IsReadOnly, DisplayName, GroupName);
    }
}
