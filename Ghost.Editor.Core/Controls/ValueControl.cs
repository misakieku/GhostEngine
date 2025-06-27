using Ghost.Editor.Core.Event;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Controls;

public partial class ValueControl<T> : Control
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

            if (!valueControl._suppressChangedEvent)
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
    /// Sets the value without notifying the change event.
    /// </summary>
    /// <param name="value">The new value to set.</param>
    /// <remarks>This method only suppresses the change event notification, not the <see cref="ValueChanged(T, T)"/> method.
    /// Useful when you need to change the value programmatically without triggering the change event.</remarks>
    public void SetValueWithoutNotifying(T value)
    {
        _suppressChangedEvent = true;
        SetValue(ValueProperty, value);
        _suppressChangedEvent = false;
    }
}