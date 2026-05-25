namespace Ghost.Editor.Core.Inspector;

public interface IPropertyModel
{
    PropertyDescriptor Descriptor { get; }
    IPropertyModel[]? Children { get; }
    bool IsDirty { get; }

    unsafe void SyncFromECS(void* pComponent);
    unsafe void FlushToECS(void* pComponent);
}


public interface IPropertyModel<T> : IPropertyModel
    where T : unmanaged
{
    T Value { get; }
    event Action<T>? OnValueChanged;

    void SetValueFromUI(T newValue);
}