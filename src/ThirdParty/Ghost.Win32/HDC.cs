namespace Ghost.Win32;

public readonly unsafe partial struct HDC : IEquatable<HDC>
{
    public readonly void* Value;

    public HDC(void* value)
    {
        Value = value;
    }

    public static bool operator ==(HDC left, HDC right) => left.Value == right.Value;

    public static bool operator !=(HDC left, HDC right) => left.Value != right.Value;

    public bool Equals(HDC other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is HDC other && Equals(other);

    public override int GetHashCode() => ((nuint)Value).GetHashCode();
}
