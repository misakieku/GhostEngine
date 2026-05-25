using System.Runtime.CompilerServices;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Strongly-typed, zero-boxing model for a single property.
/// </summary>
public sealed class PropertyModel<T> : IPropertyModel<T>
    where T : unmanaged
{
    private T _value;

    public PropertyDescriptor Descriptor { get; }
    public IPropertyModel[]? Children { get; }
    public bool IsDirty { get; private set; }

    public T Value => _value;

    /// <summary>
    /// Event fired when the value is updated from ECS. UI controls bind to this.
    /// </summary>
    public event Action<T>? OnValueChanged;

    internal PropertyModel(PropertyDescriptor descriptor, IPropertyModel[]? children = null)
    {
        Descriptor = descriptor;
        Children = children;
    }

    /// <summary>
    /// Syncs value from ECS memory without reflection or boxing.
    /// </summary>
    public unsafe void SyncFromECS(void* pComponentData)
    {
        var newValue = Unsafe.ReadUnaligned<T>((byte*)pComponentData + Descriptor.OffsetInComponent);

        if (!EqualityComparer<T>.Default.Equals(_value, newValue))
        {
            _value = newValue;
            OnValueChanged?.Invoke(newValue);
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.SyncFromECS(pComponentData);
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

    /// <summary>
    /// Writes the strongly-typed value back to ECS memory.
    /// </summary>
    public unsafe void FlushToECS(void* pComponentData)
    {
        if (IsDirty)
        {
            Unsafe.WriteUnaligned((byte*)pComponentData + Descriptor.OffsetInComponent, _value);
            IsDirty = false;
        }

        if (Children != null)
        {
            foreach (var child in Children)
            {
                child.FlushToECS(pComponentData);
            }
        }
    }
}
