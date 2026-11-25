namespace Ghost.Win32;

public readonly partial struct HRESULT : IComparable, IComparable<HRESULT>, IEquatable<HRESULT>, IFormattable
{
    public readonly int Value;

    public HRESULT(int value)
    {
        Value = value;
    }

    public bool SUCCEEDED => Value >= 0;

    public bool FAILED => Value < 0;

    public static bool operator ==(HRESULT left, HRESULT right) => left.Value == right.Value;

    public static bool operator !=(HRESULT left, HRESULT right) => left.Value != right.Value;

    public static implicit operator int(HRESULT value) => value.Value;

    public static implicit operator HRESULT(int value) => new HRESULT(value);

    public int CompareTo(object? obj)
    {
        if (obj is HRESULT other)
        {
            return CompareTo(other);
        }

        return obj is null ? 1 : throw new ArgumentException("obj is not an instance of HRESULT.");
    }

    public int CompareTo(HRESULT other) => Value.CompareTo(other.Value);

    public bool Equals(HRESULT other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is HRESULT other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => $"0x{Value:X8}";

    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);
}
