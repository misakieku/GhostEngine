namespace Ghost.Win32;

public readonly unsafe partial struct HMODULE : IEquatable<HMODULE>
{
    public readonly void* Value;

    public HMODULE(void* value)
    {
        Value = value;
    }

    public static bool operator ==(HMODULE left, HMODULE right) => left.Value == right.Value;

    public static bool operator !=(HMODULE left, HMODULE right) => left.Value != right.Value;

    public bool Equals(HMODULE other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is HMODULE other && Equals(other);

    public override int GetHashCode() => ((nuint)Value).GetHashCode();
}
