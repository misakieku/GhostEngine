
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[1];
        ids[0] = ComponentTypeID<T0>.Value;

        var set = new ComponentSetView(ids);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[1];
        ids[0] = ComponentTypeID<T0>.Value;

        var set = new ComponentSetView(ids);
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
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="count">The number of entities to create.</param>
    public Error CreateEntities<T0>(scoped in T0 component0, int count)
        where T0 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[1];
        ids[0] = ComponentTypeID<T0>.Value;

        var set = new ComponentSetView(ids);

        using var scope = AllocationManager.CreateStackScope();
        using var entities = new UnsafeArray<Entity>(count, scope.AllocationHandle);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified component values.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    /// <param name="component0">The component to add to the entity.</param>
    public Error CreateEntities<T0>(Span<Entity> entities, scoped in T0 component0)
        where T0 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[1];
        ids[0] = ComponentTypeID<T0>.Value;

        var set = new ComponentSetView(ids);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <param name="count">The number of entities to create.</param>
    public void CreateEntities<T0>(int count)
        where T0 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[1];
        ids[0] = ComponentTypeID<T0>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(count, set);
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    public void CreateEntities<T0>(Span<Entity> entities)
        where T0 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[1];
        ids[0] = ComponentTypeID<T0>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(entities, set);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[1];

        fixed (T0* pComponent0 = &component0)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;

            return SetComponents(entity, ids, ppv);
        }
    }

    public Error SetComponentsSequential<T0>(ReadOnlySpan<Entity> entities, scoped in T0 component0)
        where T0 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[1];
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[1];

        fixed (T0* pComponent0 = &component0)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;

            return SetComponentsSequential(entities, ids, ppv);
        }
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[2];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;

        var set = new ComponentSetView(ids);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[2];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;

        var set = new ComponentSetView(ids);
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
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="count">The number of entities to create.</param>
    public Error CreateEntities<T0, T1>(scoped in T0 component0, scoped in T1 component1, int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[2];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;

        var set = new ComponentSetView(ids);

        using var scope = AllocationManager.CreateStackScope();
        using var entities = new UnsafeArray<Entity>(count, scope.AllocationHandle);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified component values.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    public Error CreateEntities<T0, T1>(Span<Entity> entities, scoped in T0 component0, scoped in T1 component1)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[2];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;

        var set = new ComponentSetView(ids);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <param name="count">The number of entities to create.</param>
    public void CreateEntities<T0, T1>(int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[2];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(count, set);
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    public void CreateEntities<T0, T1>(Span<Entity> entities)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[2];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(entities, set);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[2];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;

            return SetComponents(entity, ids, ppv);
        }
    }

    public Error SetComponentsSequential<T0, T1>(ReadOnlySpan<Entity> entities, scoped in T0 component0, scoped in T1 component1)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[2];
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[2];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;

            return SetComponentsSequential(entities, ids, ppv);
        }
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[3];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;

        var set = new ComponentSetView(ids);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[3];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;

        var set = new ComponentSetView(ids);
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
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="count">The number of entities to create.</param>
    public Error CreateEntities<T0, T1, T2>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[3];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;

        var set = new ComponentSetView(ids);

        using var scope = AllocationManager.CreateStackScope();
        using var entities = new UnsafeArray<Entity>(count, scope.AllocationHandle);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified component values.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    public Error CreateEntities<T0, T1, T2>(Span<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[3];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;

        var set = new ComponentSetView(ids);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <param name="count">The number of entities to create.</param>
    public void CreateEntities<T0, T1, T2>(int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[3];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(count, set);
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    public void CreateEntities<T0, T1, T2>(Span<Entity> entities)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[3];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(entities, set);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[3];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;

            return SetComponents(entity, ids, ppv);
        }
    }

    public Error SetComponentsSequential<T0, T1, T2>(ReadOnlySpan<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[3];
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[3];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;

            return SetComponentsSequential(entities, ids, ppv);
        }
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[4];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;

        var set = new ComponentSetView(ids);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[4];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;

        var set = new ComponentSetView(ids);
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
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="count">The number of entities to create.</param>
    public Error CreateEntities<T0, T1, T2, T3>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[4];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;

        var set = new ComponentSetView(ids);

        using var scope = AllocationManager.CreateStackScope();
        using var entities = new UnsafeArray<Entity>(count, scope.AllocationHandle);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified component values.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    public Error CreateEntities<T0, T1, T2, T3>(Span<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[4];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;

        var set = new ComponentSetView(ids);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <param name="count">The number of entities to create.</param>
    public void CreateEntities<T0, T1, T2, T3>(int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[4];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(count, set);
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    public void CreateEntities<T0, T1, T2, T3>(Span<Entity> entities)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[4];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(entities, set);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[4];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;

            return SetComponents(entity, ids, ppv);
        }
    }

    public Error SetComponentsSequential<T0, T1, T2, T3>(ReadOnlySpan<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[4];
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[4];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;

            return SetComponentsSequential(entities, ids, ppv);
        }
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[5];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;

        var set = new ComponentSetView(ids);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[5];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;

        var set = new ComponentSetView(ids);
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
    /// Creates entities with the specified components.
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
    /// <param name="count">The number of entities to create.</param>
    public Error CreateEntities<T0, T1, T2, T3, T4>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[5];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;

        var set = new ComponentSetView(ids);

        using var scope = AllocationManager.CreateStackScope();
        using var entities = new UnsafeArray<Entity>(count, scope.AllocationHandle);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3, in component4);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified component values.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    public Error CreateEntities<T0, T1, T2, T3, T4>(Span<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[5];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;

        var set = new ComponentSetView(ids);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3, in component4);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <param name="count">The number of entities to create.</param>
    public void CreateEntities<T0, T1, T2, T3, T4>(int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[5];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(count, set);
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    public void CreateEntities<T0, T1, T2, T3, T4>(Span<Entity> entities)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[5];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(entities, set);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[5];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        fixed (T4* pComponent4 = &component4)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;
            ppv[4] = pComponent4;
            ids[4] = ComponentTypeID<T4>.Value;

            return SetComponents(entity, ids, ppv);
        }
    }

    public Error SetComponentsSequential<T0, T1, T2, T3, T4>(ReadOnlySpan<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[5];
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[5];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        fixed (T4* pComponent4 = &component4)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;
            ppv[4] = pComponent4;
            ids[4] = ComponentTypeID<T4>.Value;

            return SetComponentsSequential(entities, ids, ppv);
        }
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[6];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;

        var set = new ComponentSetView(ids);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[6];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;

        var set = new ComponentSetView(ids);
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
    /// Creates entities with the specified components.
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
    /// <param name="count">The number of entities to create.</param>
    public Error CreateEntities<T0, T1, T2, T3, T4, T5>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[6];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;

        var set = new ComponentSetView(ids);

        using var scope = AllocationManager.CreateStackScope();
        using var entities = new UnsafeArray<Entity>(count, scope.AllocationHandle);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3, in component4, in component5);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified component values.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <param name="component5">The component to add to the entity.</param>
    public Error CreateEntities<T0, T1, T2, T3, T4, T5>(Span<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[6];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;

        var set = new ComponentSetView(ids);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3, in component4, in component5);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <param name="count">The number of entities to create.</param>
    public void CreateEntities<T0, T1, T2, T3, T4, T5>(int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[6];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(count, set);
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    public void CreateEntities<T0, T1, T2, T3, T4, T5>(Span<Entity> entities)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[6];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(entities, set);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[6];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        fixed (T4* pComponent4 = &component4)
        fixed (T5* pComponent5 = &component5)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;
            ppv[4] = pComponent4;
            ids[4] = ComponentTypeID<T4>.Value;
            ppv[5] = pComponent5;
            ids[5] = ComponentTypeID<T5>.Value;

            return SetComponents(entity, ids, ppv);
        }
    }

    public Error SetComponentsSequential<T0, T1, T2, T3, T4, T5>(ReadOnlySpan<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[6];
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[6];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        fixed (T4* pComponent4 = &component4)
        fixed (T5* pComponent5 = &component5)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;
            ppv[4] = pComponent4;
            ids[4] = ComponentTypeID<T4>.Value;
            ppv[5] = pComponent5;
            ids[5] = ComponentTypeID<T5>.Value;

            return SetComponentsSequential(entities, ids, ppv);
        }
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[7];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;

        var set = new ComponentSetView(ids);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[7];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;

        var set = new ComponentSetView(ids);
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
    /// Creates entities with the specified components.
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
    /// <param name="count">The number of entities to create.</param>
    public Error CreateEntities<T0, T1, T2, T3, T4, T5, T6>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6, int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[7];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;

        var set = new ComponentSetView(ids);

        using var scope = AllocationManager.CreateStackScope();
        using var entities = new UnsafeArray<Entity>(count, scope.AllocationHandle);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3, in component4, in component5, in component6);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified component values.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <param name="component5">The component to add to the entity.</param>
    /// <param name="component6">The component to add to the entity.</param>
    public Error CreateEntities<T0, T1, T2, T3, T4, T5, T6>(Span<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[7];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;

        var set = new ComponentSetView(ids);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3, in component4, in component5, in component6);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <param name="count">The number of entities to create.</param>
    public void CreateEntities<T0, T1, T2, T3, T4, T5, T6>(int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[7];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(count, set);
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    public void CreateEntities<T0, T1, T2, T3, T4, T5, T6>(Span<Entity> entities)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[7];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(entities, set);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[7];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        fixed (T4* pComponent4 = &component4)
        fixed (T5* pComponent5 = &component5)
        fixed (T6* pComponent6 = &component6)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;
            ppv[4] = pComponent4;
            ids[4] = ComponentTypeID<T4>.Value;
            ppv[5] = pComponent5;
            ids[5] = ComponentTypeID<T5>.Value;
            ppv[6] = pComponent6;
            ids[6] = ComponentTypeID<T6>.Value;

            return SetComponents(entity, ids, ppv);
        }
    }

    public Error SetComponentsSequential<T0, T1, T2, T3, T4, T5, T6>(ReadOnlySpan<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
    {
        var ppv = stackalloc void*[7];
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[7];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        fixed (T4* pComponent4 = &component4)
        fixed (T5* pComponent5 = &component5)
        fixed (T6* pComponent6 = &component6)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;
            ppv[4] = pComponent4;
            ids[4] = ComponentTypeID<T4>.Value;
            ppv[5] = pComponent5;
            ids[5] = ComponentTypeID<T5>.Value;
            ppv[6] = pComponent6;
            ids[6] = ComponentTypeID<T6>.Value;

            return SetComponentsSequential(entities, ids, ppv);
        }
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[8];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;
        ids[7] = ComponentTypeID<T7>.Value;

        var set = new ComponentSetView(ids);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[8];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;
        ids[7] = ComponentTypeID<T7>.Value;

        var set = new ComponentSetView(ids);
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
    /// Creates entities with the specified components.
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
    /// <param name="count">The number of entities to create.</param>
    public Error CreateEntities<T0, T1, T2, T3, T4, T5, T6, T7>(scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6, scoped in T7 component7, int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
        where T7 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[8];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;
        ids[7] = ComponentTypeID<T7>.Value;

        var set = new ComponentSetView(ids);

        using var scope = AllocationManager.CreateStackScope();
        using var entities = new UnsafeArray<Entity>(count, scope.AllocationHandle);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3, in component4, in component5, in component6, in component7);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified component values.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <typeparam name="T7">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    /// <param name="component0">The component to add to the entity.</param>
    /// <param name="component1">The component to add to the entity.</param>
    /// <param name="component2">The component to add to the entity.</param>
    /// <param name="component3">The component to add to the entity.</param>
    /// <param name="component4">The component to add to the entity.</param>
    /// <param name="component5">The component to add to the entity.</param>
    /// <param name="component6">The component to add to the entity.</param>
    /// <param name="component7">The component to add to the entity.</param>
    public Error CreateEntities<T0, T1, T2, T3, T4, T5, T6, T7>(Span<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6, scoped in T7 component7)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
        where T7 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[8];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;
        ids[7] = ComponentTypeID<T7>.Value;

        var set = new ComponentSetView(ids);

        CreateEntities(entities, set);
        var err = SetComponentsSequential(entities, in component0, in component1, in component2, in component3, in component4, in component5, in component6, in component7);
        
        if (err != Error.None)
        {
            DestroyEntities(entities);
        }

        return err;
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <typeparam name="T7">The type of the component.</typeparam>
    /// <param name="count">The number of entities to create.</param>
    public void CreateEntities<T0, T1, T2, T3, T4, T5, T6, T7>(int count)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
        where T7 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[8];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;
        ids[7] = ComponentTypeID<T7>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(count, set);
    }

    /// <summary>
    /// Creates entities with the specified components.
    /// </summary>
    /// <typeparam name="T0">The type of the component.</typeparam>
    /// <typeparam name="T1">The type of the component.</typeparam>
    /// <typeparam name="T2">The type of the component.</typeparam>
    /// <typeparam name="T3">The type of the component.</typeparam>
    /// <typeparam name="T4">The type of the component.</typeparam>
    /// <typeparam name="T5">The type of the component.</typeparam>
    /// <typeparam name="T6">The type of the component.</typeparam>
    /// <typeparam name="T7">The type of the component.</typeparam>
    /// <param name="entities">The span to hold the created entities.</param>
    public void CreateEntities<T0, T1, T2, T3, T4, T5, T6, T7>(Span<Entity> entities)
        where T0 : unmanaged, IComponentData
        where T1 : unmanaged, IComponentData
        where T2 : unmanaged, IComponentData
        where T3 : unmanaged, IComponentData
        where T4 : unmanaged, IComponentData
        where T5 : unmanaged, IComponentData
        where T6 : unmanaged, IComponentData
        where T7 : unmanaged, IComponentData
    {
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[8];
        ids[0] = ComponentTypeID<T0>.Value;
        ids[1] = ComponentTypeID<T1>.Value;
        ids[2] = ComponentTypeID<T2>.Value;
        ids[3] = ComponentTypeID<T3>.Value;
        ids[4] = ComponentTypeID<T4>.Value;
        ids[5] = ComponentTypeID<T5>.Value;
        ids[6] = ComponentTypeID<T6>.Value;
        ids[7] = ComponentTypeID<T7>.Value;

        var set = new ComponentSetView(ids);
        CreateEntities(entities, set);
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[8];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        fixed (T4* pComponent4 = &component4)
        fixed (T5* pComponent5 = &component5)
        fixed (T6* pComponent6 = &component6)
        fixed (T7* pComponent7 = &component7)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;
            ppv[4] = pComponent4;
            ids[4] = ComponentTypeID<T4>.Value;
            ppv[5] = pComponent5;
            ids[5] = ComponentTypeID<T5>.Value;
            ppv[6] = pComponent6;
            ids[6] = ComponentTypeID<T6>.Value;
            ppv[7] = pComponent7;
            ids[7] = ComponentTypeID<T7>.Value;

            return SetComponents(entity, ids, ppv);
        }
    }

    public Error SetComponentsSequential<T0, T1, T2, T3, T4, T5, T6, T7>(ReadOnlySpan<Entity> entities, scoped in T0 component0, scoped in T1 component1, scoped in T2 component2, scoped in T3 component3, scoped in T4 component4, scoped in T5 component5, scoped in T6 component6, scoped in T7 component7)
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
        var ids = (Span<Identifier<IComponent>>)stackalloc Identifier<IComponent>[8];

        fixed (T0* pComponent0 = &component0)
        fixed (T1* pComponent1 = &component1)
        fixed (T2* pComponent2 = &component2)
        fixed (T3* pComponent3 = &component3)
        fixed (T4* pComponent4 = &component4)
        fixed (T5* pComponent5 = &component5)
        fixed (T6* pComponent6 = &component6)
        fixed (T7* pComponent7 = &component7)
        {
            ppv[0] = pComponent0;
            ids[0] = ComponentTypeID<T0>.Value;
            ppv[1] = pComponent1;
            ids[1] = ComponentTypeID<T1>.Value;
            ppv[2] = pComponent2;
            ids[2] = ComponentTypeID<T2>.Value;
            ppv[3] = pComponent3;
            ids[3] = ComponentTypeID<T3>.Value;
            ppv[4] = pComponent4;
            ids[4] = ComponentTypeID<T4>.Value;
            ppv[5] = pComponent5;
            ids[5] = ComponentTypeID<T5>.Value;
            ppv[6] = pComponent6;
            ids[6] = ComponentTypeID<T6>.Value;
            ppv[7] = pComponent7;
            ids[7] = ComponentTypeID<T7>.Value;

            return SetComponentsSequential(entities, ids, ppv);
        }
    }

}