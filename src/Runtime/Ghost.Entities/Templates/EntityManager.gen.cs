
using Ghost.Core;
using Misaki.HighPerformance.LowLevel;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public unsafe partial class EntityManager
{
    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0>()
        where T0 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
        );

        return CreateEntity(set);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0>(scoped in T0 component0)
        where T0 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
        );

        var entity = CreateEntity(set);
        var err = SetComponents(entity, in component0);
        if (err != Error.None)
        {
            DestroyEntity(entity);
            return Entity.Invalid;
        }

        return entity;
    }

    /// <summary>
    /// Sets the specified components for an existing entity.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <param name="entity">The entity to set the components for.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetComponents<T0>(Entity entity, scoped in T0 component0)
        where T0 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[1];
        var ids = stackalloc Identifier<IComponent>[1];

        ppv[0] = Unsafe.AsPointer(in component0);
        ids[0] = ComponentTypeID<T0>.Value;

        return SetComponents(entity, new ReadOnlySpan<Identifier<IComponent>>(ids, 1), ppv);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1>()
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
        );

        return CreateEntity(set);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1>(scoped in T0 component0, scoped in T1 component1)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
        );

        var entity = CreateEntity(set);
        var err = SetComponents(entity, in component0, in component1);
        if (err != Error.None)
        {
            DestroyEntity(entity);
            return Entity.Invalid;
        }

        return entity;
    }

    /// <summary>
    /// Sets the specified components for an existing entity.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <param name="entity">The entity to set the components for.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetComponents<T0, T1>(Entity entity, scoped in T0 component0, scoped in T1 component1)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[2];
        var ids = stackalloc Identifier<IComponent>[2];

        ppv[0] = Unsafe.AsPointer(in component0);
        ids[0] = ComponentTypeID<T0>.Value;
        ppv[1] = Unsafe.AsPointer(in component1);
        ids[1] = ComponentTypeID<T1>.Value;

        return SetComponents(entity, new ReadOnlySpan<Identifier<IComponent>>(ids, 2), ppv);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2>()
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
        );

        return CreateEntity(set);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
        );

        var entity = CreateEntity(set);
        var err = SetComponents(entity, in component0, in component1, in component2);
        if (err != Error.None)
        {
            DestroyEntity(entity);
            return Entity.Invalid;
        }

        return entity;
    }

    /// <summary>
    /// Sets the specified components for an existing entity.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <param name="entity">The entity to set the components for.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetComponents<T0, T1, T2>(Entity entity, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[3];
        var ids = stackalloc Identifier<IComponent>[3];

        ppv[0] = Unsafe.AsPointer(in component0);
        ids[0] = ComponentTypeID<T0>.Value;
        ppv[1] = Unsafe.AsPointer(in component1);
        ids[1] = ComponentTypeID<T1>.Value;
        ppv[2] = Unsafe.AsPointer(in component2);
        ids[2] = ComponentTypeID<T2>.Value;

        return SetComponents(entity, new ReadOnlySpan<Identifier<IComponent>>(ids, 3), ppv);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3>()
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
        );

        return CreateEntity(set);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
        );

        var entity = CreateEntity(set);
        var err = SetComponents(entity, in component0, in component1, in component2, in component3);
        if (err != Error.None)
        {
            DestroyEntity(entity);
            return Entity.Invalid;
        }

        return entity;
    }

    /// <summary>
    /// Sets the specified components for an existing entity.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <param name="entity">The entity to set the components for.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetComponents<T0, T1, T2, T3>(Entity entity, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[4];
        var ids = stackalloc Identifier<IComponent>[4];

        ppv[0] = Unsafe.AsPointer(in component0);
        ids[0] = ComponentTypeID<T0>.Value;
        ppv[1] = Unsafe.AsPointer(in component1);
        ids[1] = ComponentTypeID<T1>.Value;
        ppv[2] = Unsafe.AsPointer(in component2);
        ids[2] = ComponentTypeID<T2>.Value;
        ppv[3] = Unsafe.AsPointer(in component3);
        ids[3] = ComponentTypeID<T3>.Value;

        return SetComponents(entity, new ReadOnlySpan<Identifier<IComponent>>(ids, 4), ppv);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3, T4>()
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
            , ComponentTypeID<T4>.Value
        );

        return CreateEntity(set);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3, T4>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
            , ComponentTypeID<T4>.Value
        );

        var entity = CreateEntity(set);
        var err = SetComponents(entity, in component0, in component1, in component2, in component3, in component4);
        if (err != Error.None)
        {
            DestroyEntity(entity);
            return Entity.Invalid;
        }

        return entity;
    }

    /// <summary>
    /// Sets the specified components for an existing entity.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <param name="entity">The entity to set the components for.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetComponents<T0, T1, T2, T3, T4>(Entity entity, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[5];
        var ids = stackalloc Identifier<IComponent>[5];

        ppv[0] = Unsafe.AsPointer(in component0);
        ids[0] = ComponentTypeID<T0>.Value;
        ppv[1] = Unsafe.AsPointer(in component1);
        ids[1] = ComponentTypeID<T1>.Value;
        ppv[2] = Unsafe.AsPointer(in component2);
        ids[2] = ComponentTypeID<T2>.Value;
        ppv[3] = Unsafe.AsPointer(in component3);
        ids[3] = ComponentTypeID<T3>.Value;
        ppv[4] = Unsafe.AsPointer(in component4);
        ids[4] = ComponentTypeID<T4>.Value;

        return SetComponents(entity, new ReadOnlySpan<Identifier<IComponent>>(ids, 5), ppv);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3, T4, T5>()
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
            , ComponentTypeID<T4>.Value
            , ComponentTypeID<T5>.Value
        );

        return CreateEntity(set);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <param name="component5">The component to add to the entity.</param>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3, T4, T5>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
            , ComponentTypeID<T4>.Value
            , ComponentTypeID<T5>.Value
        );

        var entity = CreateEntity(set);
        var err = SetComponents(entity, in component0, in component1, in component2, in component3, in component4, in component5);
        if (err != Error.None)
        {
            DestroyEntity(entity);
            return Entity.Invalid;
        }

        return entity;
    }

    /// <summary>
    /// Sets the specified components for an existing entity.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <param name="entity">The entity to set the components for.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <param name="component5">The component to add to the entity.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetComponents<T0, T1, T2, T3, T4, T5>(Entity entity, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[6];
        var ids = stackalloc Identifier<IComponent>[6];

        ppv[0] = Unsafe.AsPointer(in component0);
        ids[0] = ComponentTypeID<T0>.Value;
        ppv[1] = Unsafe.AsPointer(in component1);
        ids[1] = ComponentTypeID<T1>.Value;
        ppv[2] = Unsafe.AsPointer(in component2);
        ids[2] = ComponentTypeID<T2>.Value;
        ppv[3] = Unsafe.AsPointer(in component3);
        ids[3] = ComponentTypeID<T3>.Value;
        ppv[4] = Unsafe.AsPointer(in component4);
        ids[4] = ComponentTypeID<T4>.Value;
        ppv[5] = Unsafe.AsPointer(in component5);
        ids[5] = ComponentTypeID<T5>.Value;

        return SetComponents(entity, new ReadOnlySpan<Identifier<IComponent>>(ids, 6), ppv);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3, T4, T5, T6>()
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
            , ComponentTypeID<T4>.Value
            , ComponentTypeID<T5>.Value
            , ComponentTypeID<T6>.Value
        );

        return CreateEntity(set);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <param name="component5">The component to add to the entity.</param>
    /// <param name="component6">The component to add to the entity.</param>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3, T4, T5, T6>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
            , ComponentTypeID<T4>.Value
            , ComponentTypeID<T5>.Value
            , ComponentTypeID<T6>.Value
        );

        var entity = CreateEntity(set);
        var err = SetComponents(entity, in component0, in component1, in component2, in component3, in component4, in component5, in component6);
        if (err != Error.None)
        {
            DestroyEntity(entity);
            return Entity.Invalid;
        }

        return entity;
    }

    /// <summary>
    /// Sets the specified components for an existing entity.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <param name="entity">The entity to set the components for.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <param name="component5">The component to add to the entity.</param>
    /// <param name="component6">The component to add to the entity.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetComponents<T0, T1, T2, T3, T4, T5, T6>(Entity entity, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[7];
        var ids = stackalloc Identifier<IComponent>[7];

        ppv[0] = Unsafe.AsPointer(in component0);
        ids[0] = ComponentTypeID<T0>.Value;
        ppv[1] = Unsafe.AsPointer(in component1);
        ids[1] = ComponentTypeID<T1>.Value;
        ppv[2] = Unsafe.AsPointer(in component2);
        ids[2] = ComponentTypeID<T2>.Value;
        ppv[3] = Unsafe.AsPointer(in component3);
        ids[3] = ComponentTypeID<T3>.Value;
        ppv[4] = Unsafe.AsPointer(in component4);
        ids[4] = ComponentTypeID<T4>.Value;
        ppv[5] = Unsafe.AsPointer(in component5);
        ids[5] = ComponentTypeID<T5>.Value;
        ppv[6] = Unsafe.AsPointer(in component6);
        ids[6] = ComponentTypeID<T6>.Value;

        return SetComponents(entity, new ReadOnlySpan<Identifier<IComponent>>(ids, 7), ppv);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <typeparam name="T7">The type of the component.</typeparam>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3, T4, T5, T6, T7>()
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
        where T7 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
            , ComponentTypeID<T4>.Value
            , ComponentTypeID<T5>.Value
            , ComponentTypeID<T6>.Value
            , ComponentTypeID<T7>.Value
        );

        return CreateEntity(set);
    }

    /// <summary>
    /// Creates an entity with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <typeparam name="T7">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <param name="component5">The component to add to the entity.</param>
    /// <param name="component6">The component to add to the entity.</param>
    /// <param name="component7">The component to add to the entity.</param>
    /// <returns>The Entity that was created with the specified components.</returns>
    public Entity CreateEntity<T0, T1, T2, T3, T4, T5, T6, T7>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6, scoped in T7 component7)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
        where T7 : unmanaged, IComponentData
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle
            , ComponentTypeID<T0>.Value
            , ComponentTypeID<T1>.Value
            , ComponentTypeID<T2>.Value
            , ComponentTypeID<T3>.Value
            , ComponentTypeID<T4>.Value
            , ComponentTypeID<T5>.Value
            , ComponentTypeID<T6>.Value
            , ComponentTypeID<T7>.Value
        );

        var entity = CreateEntity(set);
        var err = SetComponents(entity, in component0, in component1, in component2, in component3, in component4, in component5, in component6, in component7);
        if (err != Error.None)
        {
            DestroyEntity(entity);
            return Entity.Invalid;
        }

        return entity;
    }

    /// <summary>
    /// Sets the specified components for an existing entity.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <typeparam name="T7">The type of the component.</typeparam>
    /// <param name="entity">The entity to set the components for.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <param name="component5">The component to add to the entity.</param>
    /// <param name="component6">The component to add to the entity.</param>
    /// <param name="component7">The component to add to the entity.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetComponents<T0, T1, T2, T3, T4, T5, T6, T7>(Entity entity, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6, scoped in T7 component7)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
        where T7 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[8];
        var ids = stackalloc Identifier<IComponent>[8];

        ppv[0] = Unsafe.AsPointer(in component0);
        ids[0] = ComponentTypeID<T0>.Value;
        ppv[1] = Unsafe.AsPointer(in component1);
        ids[1] = ComponentTypeID<T1>.Value;
        ppv[2] = Unsafe.AsPointer(in component2);
        ids[2] = ComponentTypeID<T2>.Value;
        ppv[3] = Unsafe.AsPointer(in component3);
        ids[3] = ComponentTypeID<T3>.Value;
        ppv[4] = Unsafe.AsPointer(in component4);
        ids[4] = ComponentTypeID<T4>.Value;
        ppv[5] = Unsafe.AsPointer(in component5);
        ids[5] = ComponentTypeID<T5>.Value;
        ppv[6] = Unsafe.AsPointer(in component6);
        ids[6] = ComponentTypeID<T6>.Value;
        ppv[7] = Unsafe.AsPointer(in component7);
        ids[7] = ComponentTypeID<T7>.Value;

        return SetComponents(entity, new ReadOnlySpan<Identifier<IComponent>>(ids, 8), ppv);
    }

}