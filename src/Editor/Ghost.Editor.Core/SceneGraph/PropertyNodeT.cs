using Ghost.Editor.Core.Inspector;

namespace Ghost.Editor.Core.SceneGraph;

public class PropertyNode<T> : PropertyNode where T : unmanaged
{
    private T _value;
    public bool IsDirty { get; private set; }

    public T Value => _value;

    /// <summary>
    /// Event fired when the value is updated from ECS. UI controls bind to this.
    /// </summary>
    public event Action<T>? OnValueChanged;

    public PropertyNode(PropertyDescriptor descriptor, ComponentNode parent, PropertyNode[]? children = null)
        : base(descriptor, parent)
    {
        Children = children;
    }

    public override void SyncFromECS()
    {
        var newValue = Parent.GetFieldValue<T>(Descriptor);

        if (!EqualityComparer<T>.Default.Equals(_value, newValue))
        {
            _value = newValue;
            OnValueChanged?.Invoke(newValue);
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.SyncFromECS();
            }
        }
    }

    /// <summary>
    /// Called by the UI when the user edits the value.
    /// </summary>
    public void SetValueFromUI(T newValue)
    {
        IsDirty = true;
        _value = newValue;
    }

    public override void FlushToECS()
    {
        if (IsDirty)
        {
            Parent.SetFieldValue(Descriptor, _value);
            IsDirty = false;
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.FlushToECS();
            }
        }
    }
}
