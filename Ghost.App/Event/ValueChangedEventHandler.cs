namespace Ghost.Editor.Event;

public delegate void ValueChangedEventHandler<T>(object? sender, ValueChangedEventArgs<T> args);

public class ValueChangedEventArgs<T> : EventArgs
{
    public T OldValue
    {
        get;
    }

    public T NewValue
    {
        get;
    }

    public ValueChangedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }
}