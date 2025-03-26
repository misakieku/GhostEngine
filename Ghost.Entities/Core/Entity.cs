using System.Runtime.CompilerServices;

namespace Ghost.Entities.Core;

[SkipLocalsInit]
public struct Entity : IEquatable<Entity>, IComparable<Entity>
{
    private const EntityID _WORLD_INDEX_BITS = 4u;
    private const EntityID _GENERATION_BITS = 8u;
    private const EntityID _INDEX_BITS = sizeof(EntityID) * 8 - _WORLD_INDEX_BITS - _GENERATION_BITS;

    private const EntityID _WORLD_INDEX_MASK = (1u << (int)_WORLD_INDEX_BITS) - 1;
    private const EntityID _GENERATION_MASK = (1u << (int)_GENERATION_BITS) - 1;
    private const EntityID _INDEX_MASK = (1u << (int)_INDEX_BITS) - 1;
    private const EntityID _ID_MASK = EntityID.MaxValue;

    private uint _id;

    public readonly bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _id != _ID_MASK;
    }

    public readonly EntityID Index
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _id & _INDEX_MASK;
    }

    public readonly GenerationID Generation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (GenerationID)((_id >> (int)_INDEX_BITS) & _GENERATION_MASK);
    }

    public readonly WorldID WorldIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (WorldID)((_id >> (int)(_INDEX_BITS + _GENERATION_BITS)) & _WORLD_INDEX_MASK);
    }

    public void IncrementGeneration()
    {
        var generation = Generation + 1u;
        if (generation >= _GENERATION_MASK)
        {
            throw new InvalidOperationException("Generation overflow");
        }

        _id = (_id & ~(_GENERATION_MASK << (int)_INDEX_BITS)) | (generation << (int)_INDEX_BITS);
    }

    internal Entity(EntityID index, EntityID generation, EntityID worldIndex)
    {
        _id = (worldIndex << (int)(_INDEX_BITS + _GENERATION_BITS)) | (generation << (int)_INDEX_BITS) | index;
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
}