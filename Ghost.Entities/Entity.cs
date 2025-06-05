using System.Runtime.CompilerServices;

namespace Ghost.Entities;

[SkipLocalsInit]
public struct Entity : IEquatable<Entity>, IComparable<Entity>
{
    // Is 256 generations enough? If not, increase the size of GenerationID or make generation as a separate int field.
    public const int GENERATION_BITS = sizeof(GenerationID) * 8;
    public const int INDEX_BITS = sizeof(EntityID) * 8 - GENERATION_BITS;

    private const int _GENERATION_MASK = (1 << GENERATION_BITS) - 1;
    private const int _INDEX_MASK = (1 << INDEX_BITS) - 1;

    public const EntityID INVALID_ID = -1;

    private EntityID _id;

    public readonly bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ID != INVALID_ID;
    }

    public readonly EntityID ID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _id & _INDEX_MASK;
    }

    public readonly GenerationID Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (GenerationID)(_id >> INDEX_BITS & _GENERATION_MASK);
    }

    public static Entity Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(INVALID_ID, 0);
    }

    internal Entity(EntityID id, GenerationID generation)
    {
        _id = generation << INDEX_BITS | id;
    }

    internal void IncrementGeneration()
    {
        var generation = Generation + 1;
        if (generation >= _GENERATION_MASK)
        {
            throw new InvalidOperationException("Generation overflow");
        }

        _id = _id & ~(_GENERATION_MASK << INDEX_BITS) | generation << INDEX_BITS;
    }

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