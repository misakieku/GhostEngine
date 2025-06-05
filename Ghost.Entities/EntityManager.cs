using Ghost.Entities.Components;
using Ghost.Entities.Query;
using Ghost.Entities.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities;
public struct EntityManager : IDisposable
{
    private readonly List<Entity> _entities;
    private readonly Queue<EntityID> _freeEntitySlots;

    private readonly World _world;

    public readonly int EntityCount => _entities.Count;
    public readonly ReadOnlySpan<Entity> Entities => CollectionsMarshal.AsSpan(_entities);

    public event Action<Entity>? OnEntityCreated;
    public event Action<EntityID>? OnEntityRemoved;
    public event Action<Entity, Type>? OnComponentAdded;
    public event Action<Entity, Type>? OnComponentRemoved;

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

        OnEntityCreated?.Invoke(entity);
        return entity;
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

        OnEntityRemoved?.Invoke(entity.ID);
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

    /// <summary>
    /// Adds a component of type <typeparamref name="T"/> to the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to set.</typeparam>
    /// <param name="entity">The entity for which the component is to be add.</param>
    /// <param name="component">The component value to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void AddComponent<T>(Entity entity, T component)
        where T : struct, IComponentData
    {
        _world.ComponentStorage.GetOrCreateComponentPool<T>().Add(entity, component);
        _world.ComponentStorage.GetOrCreateMask(TypeHandle.Get<T>()).SetBit(entity.ID);
        OnComponentAdded?.Invoke(entity, typeof(T));
    }

    /// <summary>
    /// Removes a component of type <typeparamref name="T"/> from the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to remove.</typeparam>
    /// <param name="entity">The entity for which the component is to be remove.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool RemoveComponent<T>(Entity entity)
        where T : struct, IComponentData
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
        OnComponentRemoved?.Invoke(entity, typeof(T));

        return true;
    }

    /// <summary>
    /// Sets a component of type <typeparamref name="T"/> for the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to set.</typeparam>
    /// <param name="entity">The entity for which the component is to be set.</param>
    /// <param name="component">The component value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void SetComponent<T>(Entity entity, in T component)
        where T : struct, IComponentData
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
    public readonly bool HasComponent(Entity entity, nint typeHandle)
    {
        return _world.ComponentStorage.TryGetMask(typeHandle, out var bitSet) && bitSet.IsSet(entity.ID);
    }

    /// <summary>
    /// Retrieves a reference to a component of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to retrieve.</typeparam>
    /// <param name="entity">The entity whose component is to be retrieved.</param>
    /// <returns>A <see cref="Ref{T}"/> to the component, or a null reference if the component does not exist.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Ref<T> GetComponent<T>(Entity entity)
        where T : struct, IComponentData
    {
        if (_world.ComponentStorage.TryGetPool<T>(out var pool) && pool.Has(entity))
        {
            return new Ref<T>(ref pool.GetRef(entity));
        }
        else
        {
            return new Ref<T>(ref Unsafe.NullRef<T>(), false);
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
        OnComponentAdded?.Invoke(entity, typeof(ScriptComponent));
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
        OnComponentAdded?.Invoke(entity, typeof(ScriptComponent));
    }

    public readonly bool RemoveScript<T>(Entity entity)
        where T : ScriptComponent
    {
        if (!_world.ComponentStorage.ScriptComponentPool.Remove<T>(entity))
        {
            return false;
        }

        OnComponentRemoved?.Invoke(entity, typeof(ScriptComponent));
        return true;
    }

    public readonly bool RemoveScriptAt(Entity entity, int index)
    {
        if (!_world.ComponentStorage.ScriptComponentPool.RemoveAt(entity, index))
        {
            return false;
        }

        OnComponentRemoved?.Invoke(entity, typeof(ScriptComponent));
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
        return (T?)_world.ComponentStorage.ScriptComponentPool.Get(entity)?
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
        return (IEnumerable<T>?)_world.ComponentStorage.ScriptComponentPool.Get(entity)?.Where(script => script is T tScript) ?? Enumerable.Empty<T>();
    }

    public readonly void Dispose()
    {
        _entities.Clear();
        _freeEntitySlots.Clear();
    }
}