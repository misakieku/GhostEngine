namespace Ghost.Core;

public readonly struct Handle<T>
{
    public readonly int id;

    public readonly int generation;

    public Handle(int id, int generation)
    {
        this.id = id;
        this.generation = generation;
    }

    public static Handle<T> Invalid => new(-1, -1);

    public readonly override int GetHashCode()
    {
        return id.GetHashCode();
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Handle<T> id && Equals(id);
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

public readonly struct Identifier<T>
{
    public readonly int value;

    public Identifier(int value)
    {
        this.value = value;
    }

    public static Identifier<T> Invalid => new(-1);

    public readonly override int GetHashCode()
    {
        return value.GetHashCode();
    }

    public readonly override bool Equals(object? obj)
    {
        return obj is Identifier<T> id && Equals(id);
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
}