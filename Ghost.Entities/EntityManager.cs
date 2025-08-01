using Ghost.Core;
using Ghost.Entities.Components;
using Ghost.Entities.Query;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities;

public readonly struct EntityManager : IDisposable
{
    private readonly List<Entity> _entities;
    private readonly Queue<EntityID> _freeEntitySlots;

    private readonly World _world;

    public readonly int EntityCount => _entities.Count;
    public readonly ReadOnlySpan<Entity> Entities => CollectionsMarshal.AsSpan(_entities);

    internal EntityManager(World world, int initialCapacity)
    {
        _entities = new(initialCapacity);
        _freeEntitySlots = new(initialCapacity);
        _world = world;
    }

    /// <summary>
    /// Adds a new <see cref="Entity"/> to the world.
    /// </summary>
    /// <returns>The created <see cref="Entity"/>.</returns>
    public readonly Entity CreateEntity()
    {
        Entity entity;
        if (_freeEntitySlots.TryDequeue(out var id))
        {
            entity = _entities[id];
        }
        else
        {
            id = _entities.Count;
            entity = new Entity(id, 0);
            _entities.Add(entity);
        }

        return entity;
    }

    internal readonly void AddEntityInternal(Entity entity)
    {
        _entities.Add(entity);
    }

    /// <summary>
    /// Removes the specified <see cref="Entity"/> from the world.
    /// </summary>
    /// <param name="entity"></param>
    public readonly void RemoveEntity(ref Entity entity)
    {
        if (entity.ID >= _entities.Count || _entities[entity.ID].Generation != entity.Generation)
        {
            return;
        }

        _world.ComponentStorage.Remove(entity);

        var slot = _entities[entity.ID];
        slot.IncrementGeneration();
        _entities[entity.ID] = slot;
        _freeEntitySlots.Enqueue(entity.ID);

        entity = Entity.Invalid;
    }

    /// <summary>
    /// Checks if the given <see cref="Entity"/> is valid and belongs to this <see cref="World"/>.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity is valid and belongs to this world; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasEntity(Entity entity)
    {
        if (!entity.IsValid
            || entity.ID >= _entities.Count)
        {
            return false;
        }

        return _entities[entity.ID].Generation == entity.Generation;
    }

    public readonly void AddComponent(Entity entity, IComponentData component, Type type)
    {
        var typeHandle = TypeHandle.Get(type);
        ref var pool = ref CollectionsMarshal.GetValueRefOrAddDefault(_world.ComponentStorage.ComponentPools, typeHandle, out var exists);
        if (!exists)
        {
            var poolType = typeof(ComponentPool<>).MakeGenericType(type);
            pool = (IComponentPool)(Activator.CreateInstance(poolType) ?? throw new InvalidOperationException($"Failed to create component pool for type {type}."));
        }

        pool!.Add(entity, component);
        _world.ComponentStorage.GetOrCreateMask(typeHandle).SetBit(entity.ID);
    }

    /// <summary>
    /// Adds a component of type <typeparamref name="T"/> to the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to set.</typeparam>
    /// <param name="entity">The entity for which the component is to be add.</param>
    /// <param name="component">The component value to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void AddComponent<T>(Entity entity, T component)
        where T : unmanaged, IComponentData
    {
        _world.ComponentStorage.GetOrCreateComponentPool<T>().Add(entity, component);
        _world.ComponentStorage.GetOrCreateMask(TypeHandle.Get<T>()).SetBit(entity.ID);
    }

    /// <summary>
    /// Removes a component of type <typeparamref name="T"/> from the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to remove.</typeparam>
    /// <param name="entity">The entity for which the component is to be remove.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool RemoveComponent<T>(Entity entity)
        where T : unmanaged, IComponentData
    {
        if (!_world.ComponentStorage.TryGetPool<T>(out var pool) || !pool.Has(entity))
        {
            return false;
        }

        if (!pool.Remove(entity))
        {
            return false;
        }

        _world.ComponentStorage.GetOrCreateMask(TypeHandle.Get<T>()).ClearBit(entity.ID);

        return true;
    }

    /// <summary>
    /// Sets a component of the specified type for the given <see cref="Entity"/>.
    /// </summary>
    /// <param name="entity">The entity for which the component is to be set.</param>
    /// <param name="component">The component value to set.</param>
    /// <param name="type">The type of the component to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetComponent(Entity entity, IComponentData component, Type type)
    {
        var typeHandle = TypeHandle.Get(type);
        if (!_world.ComponentStorage.TryGetPool(typeHandle, out var pool))
        {
            return;
        }

        if (!pool.Has(entity))
        {
            return;
        }

        pool.Set(entity, component);
    }

    /// <summary>
    /// Sets a component of type <typeparamref name="T"/> for the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to set.</typeparam>
    /// <param name="entity">The entity for which the component is to be set.</param>
    /// <param name="component">The component value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetComponent<T>(Entity entity, in T component)
        where T : unmanaged, IComponentData
    {
        _world.ComponentStorage.GetOrCreateComponentPool<T>().Set(entity, in component);
    }

    /// <summary>
    /// Checks if the given <see cref="Entity"/> has a component of the specified type.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="typeHandle">The handle of the component type.</param>
    /// <returns>True if the entity has the component; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool HasComponent(Entity entity, TypeHandle typeHandle)
    {
        return _world.ComponentStorage.TryGetMask(typeHandle, out var bitSet) && bitSet.IsSet(entity.ID);
    }

    /// <summary>
    /// Retrieves a reference to a component of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to retrieve.</typeparam>
    /// <param name="entity">The entity whose component is to be retrieved.</param>
    /// <returns>A <see cref="CompRef{T}"/> to the component, or a null reference if the component does not exist.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly CompRef<T> GetComponent<T>(Entity entity)
        where T : unmanaged, IComponentData
    {
        if (_world.ComponentStorage.TryGetPool<T>(out var pool) && pool.Has(entity))
        {
            return new CompRef<T>(ref pool.GetRef(entity));
        }
        else
        {
            return new CompRef<T>(ref Unsafe.NullRef<T>(), false);
        }
    }

    /// <summary>
    /// Retrieves all components associated with the specified entity.
    /// </summary>
    /// <remarks>This method iterates through all available component pools to find components  associated
    /// with the given entity. It is designed to lazily yield components, making it efficient for scenarios where only
    /// a subset of components may be needed.</remarks>
    /// <param name="entity">The entity for which components are to be retrieved.</param>
    /// <returns>An enumerable collection of components associated with the specified entity. If the entity has no components,
    /// the collection will be empty.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IEnumerable<IComponentData> GetComponents(Entity entity)
    {
        foreach (var pool in _world.ComponentStorage.ComponentPools.Values)
        {
            if (pool.Has(entity))
            {
                yield return pool.Get(entity);
            }
        }
    }

    /// <summary>
    /// Retrieves an enumerable collection of raw pointers to the components associated with the specified entity.
    /// </summary>
    /// <remarks>This method provides direct access to the memory locations of components, bypassing type
    /// safety. Use with caution, as improper handling of raw pointers can lead to undefined behavior or memory
    /// corruption. Ensure that the entity is valid and exists within the current world context before calling this
    /// method.</remarks>
    /// <param name="entity">The entity whose components are to be retrieved.</param>
    /// <returns>An enumerable collection of <see cref="IntPtr"/> representing the memory addresses of the components associated
    /// with the specified entity. The collection will be empty if the entity has no associated components.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IEnumerable<(TypeHandle, IntPtr)> GetComponentsUnsafe(Entity entity)
    {
        foreach (var (typeHandle, pool) in _world.ComponentStorage.ComponentPools)
        {
            if (pool.Has(entity))
            {
                yield return (typeHandle, pool.GetUnsafe(entity));
            }
        }
    }

    /// <summary>
    /// Adds a script of type <typeparamref name="T"/> to the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the script to add.</typeparam>
    /// <param name="entity">The entity to which the script is to be added.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void AddScript<T>(Entity entity)
        where T : ScriptComponent, new()
    {
        _world.ComponentStorage.ScriptComponentPool.Add(entity, new T());
    }

    /// <summary>
    /// Adds a script of the specified type to the given <see cref="Entity"/>.
    /// </summary>
    /// <param name="entity">The entity to which the script is to be added.</param>
    /// <param name="type">The type of the script to add.</param>
    /// <exception cref="ArgumentException">Thrown if the specified type does not inherit from <see cref="ScriptComponent"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the script instance could not be created.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void AddScript(Entity entity, Type type)
    {
        if (!typeof(ScriptComponent).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type {type} must inherit from ScriptComponent.", nameof(type));
        }

        var instance = (ScriptComponent?)Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Failed to create instance of {type}.");
        _world.ComponentStorage.ScriptComponentPool.Add(entity, instance);
    }

    /// <summary>
    /// Removes a script of type <typeparamref name="T"/> from the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the script to remove.</typeparam>
    /// <param name="entity">The entity from which the script is to be removed.</param>
    /// <returns>True if the script was successfully removed; otherwise, false.</returns>
    public readonly bool RemoveScript<T>(Entity entity)
        where T : ScriptComponent
    {
        if (!_world.ComponentStorage.ScriptComponentPool.Remove<T>(entity))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes a script at the specified index from the given <see cref="Entity"/>.
    /// </summary>
    /// <param name="entity">The entity from which the script is to be removed.</param>
    /// <param name="index">The index of the script to remove.</param>
    /// <returns>True if the script was successfully removed; otherwise, false.</returns>
    public readonly bool RemoveScriptAt(Entity entity, int index)
    {
        if (!_world.ComponentStorage.ScriptComponentPool.RemoveAt(entity, index))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Retrieves the first script of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the script to retrieve.</typeparam>
    /// <param name="entity">The entity whose script is to be retrieved.</param>
    /// <returns>The script of type <typeparamref name="T"/>, or null if no such script exists.</returns>
    public readonly T? GetScript<T>(Entity entity)
        where T : ScriptComponent
    {
        return (T?)_world.ComponentStorage.ScriptComponentPool.GetAll(entity)?
            .FirstOrDefault(script => script is T tScript);
    }

    /// <summary>
    /// Retrieves all scripts of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the scripts to retrieve.</typeparam>
    /// <param name="entity">The entity whose scripts are to be retrieved.</param>
    /// <returns>An enumerable of scripts of type <typeparamref name="T"/>.</returns>
    public readonly IEnumerable<T> GetScripts<T>(Entity entity)
        where T : ScriptComponent
    {
        return (IEnumerable<T>?)_world.ComponentStorage.ScriptComponentPool.GetAll(entity)?.Where(script => script is T tScript) ?? Enumerable.Empty<T>();
    }

    public readonly void Dispose()
    {
        _entities.Clear();
        _freeEntitySlots.Clear();
    }
}