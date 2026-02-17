namespace Ghost.Core.Contracts;

public interface ICloneable
{
    object Clone();
}

public interface ICloneable<T>
{
    T Clone();
}