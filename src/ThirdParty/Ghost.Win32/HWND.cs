namespace Ghost.Win32;

public readonly unsafe partial struct HWND : IEquatable<HWND>
{
    public readonly void* Value;

    public HWND(void* value)
    {
        Value = value;
    }

    public static bool operator ==(HWND left, HWND right) => left.Value == right.Value;

    public static bool operator !=(HWND left, HWND right) => left.Value != right.Value;

    public static implicit operator nint(HWND value) => (nint)value.Value;

    public static explicit operator HWND(nint value) => new HWND((void*)value);

    public bool Equals(HWND other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is HWND other && Equals(other);

    public override int GetHashCode() => ((nuint)Value).GetHashCode();
}
