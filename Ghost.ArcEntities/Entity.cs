using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.ArcEntities;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly struct Entity : IEquatable<Entity>, IComparable<Entity>
{
    public const EntityID INVALID_ID = -1;

    private readonly EntityID _id;
    private readonly GenerationID _generation;

    public EntityID ID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _id;
    }

    public GenerationID Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _generation;
    }

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ID != INVALID_ID;
    }

    public static Entity Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(INVALID_ID, GenerationID.MaxValue);
    }

    internal Entity(EntityID id, GenerationID generation)
    {
        _id = id;
        _generation = generation;
    }

    public bool Equals(Entity other)
    {
        return _id == other._id && _generation == other._generation;
    }

    public int CompareTo(Entity other)
    {
        return _id.CompareTo(other._id);
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id ^ _generation << 16;
    }

    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"Entity {{ Index: {ID}, Generation: {Generation} }}";
    }
}
