namespace Ghost.Win32;

public readonly unsafe partial struct HANDLE : IEquatable<HANDLE>
{
    public readonly void* Value;

    public HANDLE(void* value)
    {
        Value = value;
    }

    public static bool operator ==(HANDLE left, HANDLE right) => left.Value == right.Value;

    public static bool operator !=(HANDLE left, HANDLE right) => left.Value != right.Value;

    public static implicit operator nint(HANDLE value) => (nint)value.Value;

    public static explicit operator HANDLE(nint value) => new HANDLE((void*)value);

    public static implicit operator void*(HANDLE value) => value.Value;

    public static implicit operator HANDLE(void* value) => new HANDLE(value);

    public bool Equals(HANDLE other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is HANDLE other && Equals(other);

    public override int GetHashCode() => ((nuint)Value).GetHashCode();
}
