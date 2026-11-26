namespace Ghost.Win32;

public readonly partial struct HRESULT : IComparable, IComparable<HRESULT>, IEquatable<HRESULT>, IFormattable
{
    public readonly int value;

    public HRESULT(int value)
    {
        this.value = value;
    }

    public bool SUCCEEDED => value >= 0;

    public bool FAILED => value < 0;

    public static bool operator ==(HRESULT left, HRESULT right) => left.value == right.value;

    public static bool operator !=(HRESULT left, HRESULT right) => left.value != right.value;

    public static implicit operator int(HRESULT value) => value.value;

    public static implicit operator HRESULT(int value) => new HRESULT(value);

    public int CompareTo(object? obj)
    {
        if (obj is HRESULT other)
        {
            return CompareTo(other);
        }

        return obj is null ? 1 : throw new ArgumentException("obj is not an instance of HRESULT.");
    }

    public int CompareTo(HRESULT other) => value.CompareTo(other.value);

    public bool Equals(HRESULT other) => value == other.value;

    public override bool Equals(object? obj) => obj is HRESULT other && Equals(other);

    public override int GetHashCode() => value.GetHashCode();

    public override string ToString() => $"0x{value:X8}";

    public string ToString(string? format, IFormatProvider? formatProvider) => value.ToString(format, formatProvider);
}
