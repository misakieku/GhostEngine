using Ghost.Entities.Components;
using System.Runtime.CompilerServices;

namespace Ghost.Entities.Query;

public interface IQueryTypeParameter<T>
    where T : IComponentData
{
}

public ref struct CompRef<T> : IQueryTypeParameter<T>
    where T : IComponentData
{
    internal ref T _value;

    public ref T ValueRW
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _value;
    }

    public readonly ref T ValueRO
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _value;
    }

    public readonly bool IsValid
    {
        get;
        init;
    }

    public CompRef(ref T value, bool isValid)
    {
        _value = ref value;
        IsValid = isValid;
    }

    public CompRef(ref T value)
        : this(ref value, true)
    {
    }
}

public readonly ref struct CompRO<T> : IQueryTypeParameter<T>
    where T : IComponentData
{
    internal readonly ref T _value;

    public readonly ref T ValueRO
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _value;
    }

    public readonly bool IsValid
    {
        get;
        init;
    }

    public CompRO(ref T value, bool isValid)
    {
        _value = ref value;
        IsValid = isValid;
    }

    public CompRO(ref T value)
        : this(ref value, true)
    {
    }
}