using Ghost.Editor.Core.Event;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;

namespace Ghost.Editor.Core.Controls;

public interface INotifyValueChanged<T>
{
    T Value { get; set; }

    event ValueChangedEventHandler<T>? OnValueChanged;

    void SetValueWithoutNotify(T value);
}

public abstract class ValueControl<T> : Control, INotifyValueChanged<T>
{
    private bool _suppressChangedEvent;

    protected bool SuppressChangedEvent
    {
        get => _suppressChangedEvent;
        set => _suppressChangedEvent = value;
    }

    public T Value
    {
        get => (T)GetValue(ValueProperty);
        set
        {
            if (EqualityComparer<T>.Default.Equals(Value, value))
            {
                return;
            }

            SetValue(ValueProperty, value);
        }
    }

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(T), typeof(ValueControl<T>), new PropertyMetadata(default(T), ChangedCallback));

    public event ValueChangedEventHandler<T>? OnValueChanged;

    private static void ChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ValueControl<T> valueControl)
        {
            valueControl.ValueChanged((T)e.OldValue, (T)e.NewValue);

            if (!valueControl.SuppressChangedEvent)
            {
                valueControl.OnValueChanged?.Invoke(valueControl, new((T)e.OldValue, (T)e.NewValue));
            }
        }
    }

    protected virtual void ValueChanged(T oldValue, T newValue)
    {
    }

    protected void RiseChangedEvent(T oldValue, T newValue)
    {
        OnValueChanged?.Invoke(this, new(oldValue, newValue));
    }

    /// <summary>
    /// Sets the value of the control.
    /// </summary>
    /// <param name="value">The new value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue(T value)
    {
        Value = value;
    }

    /// <summary>
    /// Sets the _value without notifying the change event.
    /// </summary>
    /// <param name="value">The new _value to set.</param>
    /// <remarks>This method only suppresses the change event notification, not the <see cref="ValueChanged(T, T)"/> method.
    /// Useful when you need to change the _value programmatically without triggering the change event.</remarks>
    public void SetValueWithoutNotify(T value)
    {
        SuppressChangedEvent = true;
        SetValue(value);
        SuppressChangedEvent = false;
    }
}