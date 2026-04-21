using System.Runtime.InteropServices;

namespace Ghost.Engine.AssetLoader;

public readonly struct AssetReference : IEquatable<AssetReference>
{
    private readonly int _value;

    /// <summary>
    /// The index of the asset in the dependency list.
    /// </summary>
    public int Index
    {
        get => Math.Abs(_value) - 1;
    }

    public static AssetReference Null => default;

    public readonly bool IsInternal => _value >= 0;
    public readonly bool IsExternal => _value < 0;

    public bool Equals(AssetReference other)
    {
        return _value == other._value;
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is AssetReference reference && Equals(reference);
    }

    public static bool operator ==(AssetReference left, AssetReference right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AssetReference left, AssetReference right)
    {
        return !(left == right);
    }
}
