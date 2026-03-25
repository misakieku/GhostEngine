
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public ref partial struct QueryBuilder
{
    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAll<T0>()
        where T0 : unmanaged, IComponent
    {
        _all.Add(ComponentTypeID<T0>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAllRW<T0>()
        where T0 : unmanaged, IComponent
    {
        _all.Add(ComponentTypeID<T0>.Value);
        _rw.Add(ComponentTypeID<T0>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Any' filter of the query.
    /// Targets entities that have at least one of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAny<T0>()
        where T0 : unmanaged, IComponent
    {
        _any.Add(ComponentTypeID<T0>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Absent' filter of the query.
    /// Targets entities that do not have any of the specified component types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAbsent<T0>()
        where T0 : unmanaged, IComponent
    {
        _absent.Add(ComponentTypeID<T0>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'None' filter of the query.
    /// Targets entities that do not have any of the specified component types, or those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithNone<T0>()
        where T0 : unmanaged, IComponent
    {
        _none.Add(ComponentTypeID<T0>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Disabled' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithDisabled<T0>()
        where T0 : unmanaged, IEnableableComponent
    {
        _disabled.Add(ComponentTypeID<T0>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresent<T0>()
        where T0 : unmanaged, IComponent
    {
        _present.Add(ComponentTypeID<T0>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresentRW<T0>()
        where T0 : unmanaged, IComponent
    {
        _present.Add(ComponentTypeID<T0>.Value);
        _rw.Add(ComponentTypeID<T0>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAll<T0, T1>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        _all.Add(ComponentTypeID<T0>.Value);
        _all.Add(ComponentTypeID<T1>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAllRW<T0, T1>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        _all.Add(ComponentTypeID<T0>.Value);
        _rw.Add(ComponentTypeID<T0>.Value);
        _all.Add(ComponentTypeID<T1>.Value);
        _rw.Add(ComponentTypeID<T1>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Any' filter of the query.
    /// Targets entities that have at least one of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAny<T0, T1>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        _any.Add(ComponentTypeID<T0>.Value);
        _any.Add(ComponentTypeID<T1>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Absent' filter of the query.
    /// Targets entities that do not have any of the specified component types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAbsent<T0, T1>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        _absent.Add(ComponentTypeID<T0>.Value);
        _absent.Add(ComponentTypeID<T1>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'None' filter of the query.
    /// Targets entities that do not have any of the specified component types, or those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithNone<T0, T1>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        _none.Add(ComponentTypeID<T0>.Value);
        _none.Add(ComponentTypeID<T1>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Disabled' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithDisabled<T0, T1>()
        where T0 : unmanaged, IEnableableComponent
        where T1 : unmanaged, IEnableableComponent
    {
        _disabled.Add(ComponentTypeID<T0>.Value);
        _disabled.Add(ComponentTypeID<T1>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresent<T0, T1>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        _present.Add(ComponentTypeID<T0>.Value);
        _present.Add(ComponentTypeID<T1>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresentRW<T0, T1>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
    {
        _present.Add(ComponentTypeID<T0>.Value);
        _rw.Add(ComponentTypeID<T0>.Value);
        _present.Add(ComponentTypeID<T1>.Value);
        _rw.Add(ComponentTypeID<T1>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAll<T0, T1, T2>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        _all.Add(ComponentTypeID<T0>.Value);
        _all.Add(ComponentTypeID<T1>.Value);
        _all.Add(ComponentTypeID<T2>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAllRW<T0, T1, T2>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        _all.Add(ComponentTypeID<T0>.Value);
        _rw.Add(ComponentTypeID<T0>.Value);
        _all.Add(ComponentTypeID<T1>.Value);
        _rw.Add(ComponentTypeID<T1>.Value);
        _all.Add(ComponentTypeID<T2>.Value);
        _rw.Add(ComponentTypeID<T2>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Any' filter of the query.
    /// Targets entities that have at least one of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAny<T0, T1, T2>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        _any.Add(ComponentTypeID<T0>.Value);
        _any.Add(ComponentTypeID<T1>.Value);
        _any.Add(ComponentTypeID<T2>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Absent' filter of the query.
    /// Targets entities that do not have any of the specified component types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAbsent<T0, T1, T2>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        _absent.Add(ComponentTypeID<T0>.Value);
        _absent.Add(ComponentTypeID<T1>.Value);
        _absent.Add(ComponentTypeID<T2>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'None' filter of the query.
    /// Targets entities that do not have any of the specified component types, or those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithNone<T0, T1, T2>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        _none.Add(ComponentTypeID<T0>.Value);
        _none.Add(ComponentTypeID<T1>.Value);
        _none.Add(ComponentTypeID<T2>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Disabled' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithDisabled<T0, T1, T2>()
        where T0 : unmanaged, IEnableableComponent
        where T1 : unmanaged, IEnableableComponent
        where T2 : unmanaged, IEnableableComponent
    {
        _disabled.Add(ComponentTypeID<T0>.Value);
        _disabled.Add(ComponentTypeID<T1>.Value);
        _disabled.Add(ComponentTypeID<T2>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresent<T0, T1, T2>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        _present.Add(ComponentTypeID<T0>.Value);
        _present.Add(ComponentTypeID<T1>.Value);
        _present.Add(ComponentTypeID<T2>.Value);

        return ref this;
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresentRW<T0, T1, T2>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
    {
        _present.Add(ComponentTypeID<T0>.Value);
        _rw.Add(ComponentTypeID<T0>.Value);
        _present.Add(ComponentTypeID<T1>.Value);
        _rw.Add(ComponentTypeID<T1>.Value);
        _present.Add(ComponentTypeID<T2>.Value);
        _rw.Add(ComponentTypeID<T2>.Value);

        return ref this;
    }

}