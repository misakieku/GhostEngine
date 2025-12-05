namespace Ghost.Core;

public interface IHandleType;
public interface IIdentifierType;
public interface IKeyType;

public readonly struct Handle<T> : IEquatable<Handle<T>>
    where T : IHandleType
{
    public readonly int id;
    public readonly int generation;

    public Handle(int id, int generation)
    {
        this.id = id;
        this.generation = generation;
    }

    public static Handle<T> Invalid => new(-1, -1);

    public readonly bool IsValid => this != Invalid;
    public readonly bool IsNotValid => this == Invalid;

    public readonly override int GetHashCode()
    {
        return id + (generation << 16);
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Handle<T> id && Equals(id);
    }

    public override string ToString()
    {
        return $"Handle<{typeof(T).Name}>({id}, {generation})";
    }

    public readonly bool Equals(Handle<T> other)
    {
        return id == other.id;
    }

    public readonly int CompareTo(Handle<T> other)
    {
        return id.CompareTo(other.id);
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
    where T : IIdentifierType
{
    public readonly int value;

    public Identifier(int value)
    {
        this.value = value;
    }

    public static Identifier<T> Invalid => new(-1);

    public readonly bool IsValid => this != Invalid;
    public readonly bool IsNotValid => this == Invalid;

    public readonly override int GetHashCode()
    {
        return value;
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Identifier<T> id && Equals(id);
    }

    public override string ToString()
    {
        return $"Identifier<{typeof(T).Name}>({value})";
    }

    public readonly bool Equals(Identifier<T> other)
    {
        return value == other.value;
    }

    public readonly int CompareTo(Identifier<T> other)
    {
        return value.CompareTo(other.value);
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
        return a.value < b.value;
    }

    public static bool operator >(Identifier<T> a, Identifier<T> b)
    {
        return a.value > b.value;
    }

    public static bool operator <=(Identifier<T> a, Identifier<T> b)
    {
        return a.value <= b.value;
    }

    public static bool operator >=(Identifier<T> a, Identifier<T> b)
    {
        return a.value >= b.value;
    }

    public static implicit operator int(Identifier<T> id) => id.value;
    public static implicit operator Identifier<T>(int value) => new Identifier<T>(value);
}

public readonly struct Key<T>
    where T : IKeyType
{
    public readonly ulong value;

    public Key(ulong value)
    {
        this.value = value;
    }

    public static Key<T> Invalid => new(0);

    public bool IsValid => this != Invalid;

    public readonly override int GetHashCode()
    {
        return value.GetHashCode();
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Key<T> id && Equals(id);
    }

    public readonly bool Equals(Key<T> other)
    {
        return value == other.value;
    }

    public readonly int CompareTo(Key<T> other)
    {
        return value.CompareTo(other.value);
    }

    public static bool operator ==(Key<T> a, Key<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Key<T> a, Key<T> b)
    {
        return !a.Equals(b);
    }
}
