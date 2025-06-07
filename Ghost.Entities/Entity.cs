using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Ghost.Entities;

[SkipLocalsInit]
public struct Entity : IEquatable<Entity>, IComparable<Entity>
{
    public const EntityID INVALID_ID = -1;

    [JsonInclude]
    private EntityID _id;
    private GenerationID _generation;

    public readonly EntityID ID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _id;
    }

    public readonly GenerationID Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _generation;
    }

    public readonly bool IsValid
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void IncrementGeneration() => _generation++;

    public readonly bool Equals(Entity other)
    {
        return _id == other._id;
    }

    public readonly int CompareTo(Entity other)
    {
        return _id.CompareTo(other._id);
    }

    public override readonly bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    public override readonly int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(Entity left, Entity right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }

    public override readonly string ToString()
    {
        return $"Entity {{ Index: {ID}, Generation: {Generation} }}";
    }
}