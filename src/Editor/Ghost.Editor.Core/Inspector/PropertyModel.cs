using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Observable model for a single property. Implements INotifyPropertyChanged
/// so WinUI controls can bind to it natively.
/// </summary>
public sealed class PropertyModel : INotifyPropertyChanged
{
    private readonly PropertyDescriptor _descriptor;
    private object? _cachedValue;
    private bool _isDirty;

    public PropertyDescriptor Descriptor => _descriptor;

    public object? Value
    {
        get => _value;
        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                OnPropertyChanged();
            }
        }
    }

    private object? _value;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public bool IsDirty => _isDirty;
    
    public PropertyModel[]? Children { get; }

    internal PropertyModel(PropertyDescriptor descriptor, PropertyModel[]? children = null)
    {
        _descriptor = descriptor;
        Children = children;
    }

    /// <summary>
    /// Called by sync pump: updates Value only if ECS value actually changed.
    /// Does NOT mark dirty (it's an ECS->UI sync, not a user edit).
    /// </summary>
    internal void SetValueFromECS(object? newValue)
    {
        if (Equals(_cachedValue, newValue)) return;
        
        _cachedValue = newValue;
        Value = newValue; // Fires OnPropertyChanged
    }

    /// <summary>
    /// Called when user edits via UI: marks dirty for write-back.
    /// </summary>
    public void SetValueFromUI(object? newValue)
    {
        _isDirty = true;
        Value = newValue;
    }

    /// <summary>
    /// Writes dirty value back to ECS memory. Called by flush.
    /// </summary>
    internal unsafe void FlushToECS(void* pComponent)
    {
        if (!_isDirty || Value == null) return;
        
        _descriptor.WriteBoxed(pComponent, Value);
        _isDirty = false;
        _cachedValue = Value;
    }
}
