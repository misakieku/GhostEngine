using Ghost.Entities.Query;
using Ghost.Entities.Utilities;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ghost.Entities;

[SkipLocalsInit]
public struct Entity : IEquatable<Entity>, IComparable<Entity>
{
    public const int WORLD_INDEX_BITS = 4;
    public const int GENERATION_BITS = 8;
    public const int INDEX_BITS = sizeof(EntityID) * 8 - WORLD_INDEX_BITS - GENERATION_BITS;

    private const int _WORLD_INDEX_MASK = (1 << WORLD_INDEX_BITS) - 1;
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

    public readonly WorldID WorldID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (WorldID)(_id >> (INDEX_BITS + GENERATION_BITS) & _WORLD_INDEX_MASK);
    }

    public static Entity Invalid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(INVALID_ID, 0, 0);
    }

    internal Entity(EntityID id, GenerationID generation, WorldID worldID)
    {
        _id = worldID << (INDEX_BITS + GENERATION_BITS) | generation << INDEX_BITS | id;
    }

    public void IncrementGeneration()
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
        return $"Entity {{ Index: {ID}, Generation: {Generation}, WorldIndex: {WorldID} }}";
    }
}

public class EntityManager : IDisposable
{
    private readonly List<Entity> _entities;
    private readonly Queue<EntityID> _freeEntitySlots;

    private readonly World _world;

    public int EntityCount => _entities.Count;
    public ReadOnlySpan<Entity> Entities => CollectionsMarshal.AsSpan(_entities);

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
    public Entity CreateEntity()
    {
        if (_freeEntitySlots.TryDequeue(out var id))
        {
            return _entities[id];
        }
        else
        {
            id = _entities.Count;
            var entity = new Entity(id, 0, _world.ID);
            _entities.Add(entity);
            return entity;
        }
    }

    /// <summary>
    /// Removes the specified <see cref="Entity"/> from the world.
    /// </summary>
    /// <param name="entity"></param>
    public void RemoveEntity(ref Entity entity)
    {
        if (entity.ID >= _entities.Count || _entities[entity.ID].Generation != entity.Generation)
        {
            return;
        }

        _world._componentStorage.Remove(entity);

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
    public bool HasEntity(Entity entity)
    {
        if (!entity.IsValid
            || entity.WorldID != _world.ID
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
    public void AddComponent<T>(Entity entity, T component)
        where T : struct, IComponentData
    {
        _world._componentStorage.GetOrCreateComponentPool<T>().Add(entity, component);
        _world._componentStorage.GetOrCreateMask(TypeHandle<T>.Value).SetBit(entity.ID);
    }

    /// <summary>
    /// Removes a component of type <typeparamref name="T"/> from the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to remove.</typeparam>
    /// <param name="entity">The entity for which the component is to be remove.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveComponent<T>(Entity entity)
        where T : struct, IComponentData
    {
        if (_world._componentStorage.TryGetPool<T>(out var pool) && pool.Has(entity))
        {
            pool.Remove(entity);
            _world._componentStorage.GetOrCreateMask(TypeHandle<T>.Value).ClearBit(entity.ID);
        }
    }

    /// <summary>
    /// Sets a component of type <typeparamref name="T"/> for the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to set.</typeparam>
    /// <param name="entity">The entity for which the component is to be set.</param>
    /// <param name="component">The component value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetComponent<T>(Entity entity, T component)
        where T : struct, IComponentData
    {
        _world._componentStorage.GetOrCreateComponentPool<T>().Set(entity, component);
    }

    /// <summary>
    /// Checks if the given <see cref="Entity"/> has a component of the specified type.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="typeHandle">The handle of the component type.</param>
    /// <returns>True if the entity has the component; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent(Entity entity, nint typeHandle)
    {
        return _world._componentStorage.TryGetMask(typeHandle, out var bitSet) && bitSet.IsSet(entity.ID);
    }

    /// <summary>
    /// Retrieves a reference to a component of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to retrieve.</typeparam>
    /// <param name="entity">The entity whose component is to be retrieved.</param>
    /// <returns>A <see cref="Ref{T}"/> to the component, or a null reference if the component does not exist.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ref<T> GetComponent<T>(Entity entity)
        where T : struct, IComponentData
    {
        if (_world._componentStorage.TryGetPool<T>(out var pool) && pool.Has(entity))
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
    public void AddScript<T>(Entity entity)
        where T : ScriptComponent, new()
    {
        _world._componentStorage.ScriptComponentPool.Add(entity, new T());
    }

    /// <summary>
    /// Adds a script of the specified type to the given <see cref="Entity"/>.
    /// </summary>
    /// <param name="entity">The entity to which the script is to be added.</param>
    /// <param name="type">The type of the script to add.</param>
    /// <exception cref="ArgumentException">Thrown if the specified type does not inherit from <see cref="ScriptComponent"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the script instance could not be created.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddScript(Entity entity, Type type)
    {
        if (!typeof(ScriptComponent).IsAssignableFrom(type))
        {
            throw new ArgumentException($"Type {type} must inherit from ScriptComponent.", nameof(type));
        }

        var instance = (ScriptComponent?)Activator.CreateInstance(type) ?? throw new InvalidOperationException($"Failed to create instance of {type}.");
        _world._componentStorage.ScriptComponentPool.Add(entity, instance);
    }

    /// <summary>
    /// Retrieves the first script of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the script to retrieve.</typeparam>
    /// <param name="entity">The entity whose script is to be retrieved.</param>
    /// <returns>The script of type <typeparamref name="T"/>, or null if no such script exists.</returns>
    public T? GetScript<T>(Entity entity)
        where T : ScriptComponent
    {
        return (T?)_world._componentStorage.ScriptComponentPool.Get(entity)?
            .FirstOrDefault(script => script is T tScript);
    }

    /// <summary>
    /// Retrieves all scripts of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the scripts to retrieve.</typeparam>
    /// <param name="entity">The entity whose scripts are to be retrieved.</param>
    /// <returns>An enumerable of scripts of type <typeparamref name="T"/>.</returns>
    public IEnumerable<T> GetScripts<T>(Entity entity)
        where T : ScriptComponent
    {
        return (IEnumerable<T>?)_world._componentStorage.ScriptComponentPool.Get(entity)?.Where(script => script is T tScript) ?? Enumerable.Empty<T>();
    }

    public void Dispose()
    {
        _entities.Clear();
        _freeEntitySlots.Clear();
    }
}