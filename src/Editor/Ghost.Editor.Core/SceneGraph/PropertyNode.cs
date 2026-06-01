using Ghost.Editor.Core.Inspector;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ghost.Editor.Core.SceneGraph;

/// <summary>
/// Represents a single property/field within a ComponentNode.
/// Handles ECS reading/writing as well as serialization overrides (like Guid metadata).
/// </summary>
public abstract class PropertyNode
{
    public PropertyDescriptor Descriptor { get; }
    public ComponentNode Parent { get; }
    public PropertyNode[]? Children { get; protected set; }

    protected PropertyNode(PropertyDescriptor descriptor, ComponentNode parent)
    {
        Descriptor = descriptor;
        Parent = parent;
    }

    /// <summary>
    /// Synchronize the cached value from the ECS backend.
    /// </summary>
    public abstract void Sync();

    public virtual void SerializeOverride(JsonObject jsonRoot, object boxedComponent)
    {
    }

    public virtual void DeserializeOverride(JsonElement jsonRoot, object boxedComponent)
    {
    }

    public virtual void Validate(object boxedComponent)
    {
    }
}

public class PropertyNode<T> : PropertyNode
    where T : unmanaged
{
    private T _value;
    public T Value => _value;

    /// <summary>
    /// Event fired when the value is updated from ECS. UI controls bind to this.
    /// </summary>
    public event Action<T>? OnValueChanged;

    public PropertyNode(PropertyDescriptor descriptor, ComponentNode parent, PropertyNode[]? children = null)
        : base(descriptor, parent)
    {
        _value = parent.GetPropertyValue<T>(descriptor);
        Children = children;
    }

    public override void Sync()
    {
        var newValue = Parent.GetPropertyValue<T>(Descriptor);

        if (!EqualityComparer<T>.Default.Equals(_value, newValue))
        {
            _value = newValue;
            OnValueChanged?.Invoke(newValue);
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.Sync();
            }
        }
    }

    /// <summary>
    /// Called by the UI when the user edits the value.
    /// </summary>
    public void SetValueFromUI(T newValue)
    {
        _value = newValue;
        Parent.SetPropertyValue(Descriptor, newValue);
    }
}