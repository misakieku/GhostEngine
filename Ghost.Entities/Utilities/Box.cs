namespace Ghost.Entities.Utilities;

internal class Box<T>
    where T : struct
{
    public T Value
    {
        get;
        set;
    }

    public Box(T value)
    {
        Value = value;
    }

    public static implicit operator T(Box<T> box) => box.Value;
    public static implicit operator Box<T>(T value) => new(value);
}