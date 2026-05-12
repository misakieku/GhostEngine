namespace Ghost.Core;

public interface ICloneable<T>
    where T : ICloneable<T>
{
    /// <summary>
    /// Deep copy the object to create a new instance that contains the same value.
    /// </summary>
    /// <remarks>
    /// This often does not clone any gpu resources if the object holds any.
    /// </remarks>
    T Clone();
}

public class Wrapper<T>
{
    public T? Value
    {
        get; set;
    }
}
