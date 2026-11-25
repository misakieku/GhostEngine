namespace Ghost.Win32;

public readonly unsafe partial struct HMONITOR : IEquatable<HMONITOR>
{
    public readonly void* Value;

    public HMONITOR(void* value)
    {
        Value = value;
    }

    public static bool operator ==(HMONITOR left, HMONITOR right) => left.Value == right.Value;

    public static bool operator !=(HMONITOR left, HMONITOR right) => left.Value != right.Value;

    public bool Equals(HMONITOR other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is HMONITOR other && Equals(other);

    public override int GetHashCode() => ((nuint)Value).GetHashCode();
}
