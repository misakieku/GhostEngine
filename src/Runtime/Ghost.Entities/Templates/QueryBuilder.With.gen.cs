
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
        return ref WithAll(ComponentTypeID<T0>.Value);
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
        return ref WithAllRW(ComponentTypeID<T0>.Value);
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
        return ref WithAny(ComponentTypeID<T0>.Value);
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
        return ref WithAbsent(ComponentTypeID<T0>.Value);
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
        return ref WithNone(ComponentTypeID<T0>.Value);
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
        return ref WithDisabled(ComponentTypeID<T0>.Value);
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
        return ref WithPresent(ComponentTypeID<T0>.Value);
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
        return ref WithPresentRW(ComponentTypeID<T0>.Value);
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
        return ref WithAll(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value);
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
        return ref WithAllRW(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value);
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
        return ref WithAny(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value);
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
        return ref WithAbsent(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value);
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
        return ref WithNone(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value);
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
        return ref WithDisabled(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value);
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
        return ref WithPresent(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value);
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
        return ref WithPresentRW(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value);
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
        return ref WithAll(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value);
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
        return ref WithAllRW(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value);
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
        return ref WithAny(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value);
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
        return ref WithAbsent(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value);
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
        return ref WithNone(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value);
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
        return ref WithDisabled(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value);
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
        return ref WithPresent(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value);
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
        return ref WithPresentRW(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAll<T0, T1, T2, T3>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        return ref WithAll(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAllRW<T0, T1, T2, T3>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        return ref WithAllRW(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Any' filter of the query.
    /// Targets entities that have at least one of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAny<T0, T1, T2, T3>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        return ref WithAny(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Absent' filter of the query.
    /// Targets entities that do not have any of the specified component types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAbsent<T0, T1, T2, T3>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        return ref WithAbsent(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'None' filter of the query.
    /// Targets entities that do not have any of the specified component types, or those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithNone<T0, T1, T2, T3>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        return ref WithNone(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Disabled' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithDisabled<T0, T1, T2, T3>()
        where T0 : unmanaged, IEnableableComponent
        where T1 : unmanaged, IEnableableComponent
        where T2 : unmanaged, IEnableableComponent
        where T3 : unmanaged, IEnableableComponent
    {
        return ref WithDisabled(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresent<T0, T1, T2, T3>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        return ref WithPresent(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresentRW<T0, T1, T2, T3>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
    {
        return ref WithPresentRW(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAll<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        return ref WithAll(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value, ComponentTypeID<T4>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'All' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAllRW<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        return ref WithAllRW(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value, ComponentTypeID<T4>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Any' filter of the query.
    /// Targets entities that have at least one of the specified component types and those component(s) must be enabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAny<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        return ref WithAny(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value, ComponentTypeID<T4>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Absent' filter of the query.
    /// Targets entities that do not have any of the specified component types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithAbsent<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        return ref WithAbsent(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value, ComponentTypeID<T4>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'None' filter of the query.
    /// Targets entities that do not have any of the specified component types, or those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithNone<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        return ref WithNone(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value, ComponentTypeID<T4>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Disabled' filter of the query.
    /// Targets entities that have all of the specified component types and those component(s) are disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithDisabled<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IEnableableComponent
        where T1 : unmanaged, IEnableableComponent
        where T2 : unmanaged, IEnableableComponent
        where T3 : unmanaged, IEnableableComponent
        where T4 : unmanaged, IEnableableComponent
    {
        return ref WithDisabled(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value, ComponentTypeID<T4>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresent<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        return ref WithPresent(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value, ComponentTypeID<T4>.Value);
    }

    /// <summary>
    /// Adds the specified component type(s) to the 'Present' filter of the query and requires read-write access.
    /// Targets entities that have all of the specified component types, regardless of whether those component(s) are enabled or disabled.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnscopedRef]
    public ref QueryBuilder WithPresentRW<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IComponent
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
    {
        return ref WithPresentRW(ComponentTypeID<T0>.Value, ComponentTypeID<T1>.Value, ComponentTypeID<T2>.Value, ComponentTypeID<T3>.Value, ComponentTypeID<T4>.Value);
    }

}