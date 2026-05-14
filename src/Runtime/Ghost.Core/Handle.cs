using System.Runtime.InteropServices;

namespace Ghost.Core;

public readonly struct Handle<T> : IEquatable<Handle<T>>
{
    public int ID
    {
        get;
    }

    public int Generation
    {
        get;
    }

    public Handle(int id, int generation)
    {
        ID = id;
        Generation = generation;
    }

    public static Handle<T> Invalid => default;

    public readonly bool IsValid => this != Invalid;
    public readonly bool IsInvalid => this == Invalid;

    public override readonly int GetHashCode()
    {
        return ID + (Generation << 16);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Handle<T> id && Equals(id);
    }

    public override string ToString()
    {
        return $"Handle<{typeof(T).Name}>({ID}, {Generation})";
    }

    public readonly bool Equals(Handle<T> other)
    {
        return ID == other.ID && Generation == other.Generation;
    }

    public readonly int CompareTo(Handle<T> other)
    {
        return ID.CompareTo(other.ID);
    }

    public static bool operator ==(Handle<T> a, Handle<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Handle<T> a, Handle<T> b)
    {
        return !a.Equals(b);
    }
}

public readonly struct Identifier<T> : IEquatable<Identifier<T>>
{
    public int Value
    {
        get => field - 1;
    }

    public Identifier(int value)
    {
        Value = value + 1;
    }

    public static Identifier<T> Invalid => default;

    public readonly bool IsValid => this != Invalid;
    public readonly bool IsInvalid => this == Invalid;

    public override readonly int GetHashCode()
    {
        return Value;
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Identifier<T> id && Equals(id);
    }

    public override string ToString()
    {
        return $"Identifier<{typeof(T).Name}>({Value})";
    }

    public readonly bool Equals(Identifier<T> other)
    {
        return Value == other.Value;
    }

    public readonly int CompareTo(Identifier<T> other)
    {
        return Value.CompareTo(other.Value);
    }

    public static bool operator ==(Identifier<T> a, Identifier<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Identifier<T> a, Identifier<T> b)
    {
        return !a.Equals(b);
    }

    public static bool operator <(Identifier<T> a, Identifier<T> b)
    {
        return a.Value < b.Value;
    }

    public static bool operator >(Identifier<T> a, Identifier<T> b)
    {
        return a.Value > b.Value;
    }

    public static bool operator <=(Identifier<T> a, Identifier<T> b)
    {
        return a.Value <= b.Value;
    }

    public static bool operator >=(Identifier<T> a, Identifier<T> b)
    {
        return a.Value >= b.Value;
    }

    public static implicit operator int(Identifier<T> id) => id.Value;
    public static implicit operator Identifier<T>(int value) => new Identifier<T>(value);
}

public readonly struct Key64<T> : IEquatable<Key64<T>>
{
    public ulong Value
    {
        get;
    }

    public Key64(ulong value)
    {
        Value = value;
    }

    public static Key64<T> Invalid => new(0);

    public bool IsValid => this != Invalid;
    public bool IsInvalid => this == Invalid;

    public override readonly int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public readonly bool Equals(Key64<T> other)
    {
        return Value == other.Value;
    }

    public readonly int CompareTo(Key64<T> other)
    {
        return Value.CompareTo(other.Value);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Key64<T> id && Equals(id);
    }

    public override string ToString()
    {
        return Value.ToString("X16");
    }

    public static bool operator ==(Key64<T> a, Key64<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Key64<T> a, Key64<T> b)
    {
        return !a.Equals(b);
    }

    public static implicit operator ulong(Key64<T> key) => key.Value;
    public static implicit operator Key64<T>(ulong value) => new Key64<T>(value);
}

public readonly struct Key128<T> : IEquatable<Key128<T>>
{
    public UInt128 Value
    {
        get;
    }

    public Key128(UInt128 value)
    {
        Value = value;
    }

    public static Key128<T> Invalid => new(0);

    public bool IsValid => this != Invalid;
    public bool IsInvalid => this == Invalid;

    public override readonly int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public readonly bool Equals(Key128<T> other)
    {
        return Value == other.Value;
    }

    public readonly int CompareTo(Key128<T> other)
    {
        return Value.CompareTo(other.Value);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Key128<T> id && Equals(id);
    }

    public override string ToString()
    {
        return Value.ToString("X16");
    }

    public static bool operator ==(Key128<T> a, Key128<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Key128<T> a, Key128<T> b)
    {
        return !a.Equals(b);
    }

    public static implicit operator UInt128(Key128<T> key) => key.Value;
    public static implicit operator Key128<T>(UInt128 value) => new Key128<T>(value);
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct AssetRef<T> : IEquatable<AssetRef<T>>
{
    public Guid ID
    {
        get;
    }

    public static AssetRef<T> Null => default;

    public bool IsValid => ID != Guid.Empty;

    public AssetRef(Guid id)
    {
        ID = id;
    }

    public bool Equals(AssetRef<T> other)
    {
        return ID == other.ID;
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is AssetRef<T> r && Equals(r);
    }

    public override string ToString()
    {
        return $"AssetRef<{typeof(T).Name}>({ID:N})";
    }

    public static bool operator ==(AssetRef<T> a, AssetRef<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(AssetRef<T> a, AssetRef<T> b)
    {
        return !a.Equals(b);
    }
}
