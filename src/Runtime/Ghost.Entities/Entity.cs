using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities;

[StructLayout(LayoutKind.Sequential, Size = 8)]
public readonly record struct Entity
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
        get => new(INVALID_ID, INVALID_ID);
    }

    internal Entity(EntityID id, GenerationID generation)
    {
        _id = id;
        _generation = generation;
    }

    public override string ToString()
    {
        return $"Entity {{ Index: {ID}, Generation: {Generation} }}";
    }
}