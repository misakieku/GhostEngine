using Ghost.Entities.Query;
using System.Runtime.CompilerServices;

namespace Ghost.Entities.Helpers;

/// <summary>
/// Provides extension methods for working with entities in the Ghost framework.
/// </summary>
public static class EntityHelpers
{
    public static World GetWorld(this Entity entity)
    {
        return World.GetWorld(entity.WorldID);
    }

    /// <summary>
    /// Adds a component of type <typeparamref name="T"/> to the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to set.</typeparam>
    /// <param name="entity">The entity for which the component is to be add.</param>
    /// <param name="component">The component value to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddComponent<T>(this Entity entity, T component)
        where T : struct, IComponentData
    {
        var world = entity.GetWorld();
        world.EntityManager.AddComponent<T>(entity, component);
    }

    /// <summary>
    /// Removes a component of type <typeparamref name="T"/> from the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to remove.</typeparam>
    /// <param name="entity">The entity for which the component is to be remove.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RemoveComponent<T>(this Entity entity)
        where T : struct, IComponentData
    {
        var world = entity.GetWorld();
        world.EntityManager.RemoveComponent<T>(entity);
    }

    /// <summary>
    /// Sets a component of type <typeparamref name="T"/> for the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to set.</typeparam>
    /// <param name="entity">The entity for which the component is to be set.</param>
    /// <param name="component">The component value to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetComponent<T>(this Entity entity, T component)
        where T : struct, IComponentData
    {
        var world = entity.GetWorld();
        world.EntityManager.SetComponent<T>(entity, component);
    }

    /// <summary>
    /// Checks if the given <see cref="Entity"/> has a component of the specified type.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="typeHandle">The handle of the component type.</param>
    /// <returns>True if the entity has the component; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasComponent(this Entity entity, nint typeHandle)
    {
        var world = entity.GetWorld();
        return world.EntityManager.HasComponent(entity, typeHandle);
    }

    /// <summary>
    /// Retrieves a reference to a component of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the component to retrieve.</typeparam>
    /// <param name="entity">The entity whose component is to be retrieved.</param>
    /// <returns>A <see cref="Ref{T}"/> to the component, or a null reference if the component does not exist.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Ref<T> GetComponent<T>(this Entity entity)
        where T : struct, IComponentData
    {
        var world = entity.GetWorld();
        return world.EntityManager.GetComponent<T>(entity);
    }

    /// <summary>
    /// Adds a script of type <typeparamref name="T"/> to the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the script to add.</typeparam>
    /// <param name="entity">The entity to which the script is to be added.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddScript<T>(this Entity entity)
        where T : ScriptComponent, new()
    {
        var world = entity.GetWorld();
        world.EntityManager.AddScript<T>(entity);
    }

    /// <summary>
    /// Adds a script of the specified type to the given <see cref="Entity"/>.
    /// </summary>
    /// <param name="entity">The entity to which the script is to be added.</param>
    /// <param name="type">The type of the script to add.</param>
    /// <exception cref="ArgumentException">Thrown if the specified type does not inherit from <see cref="ScriptComponent"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the script instance could not be created.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddScript(this Entity entity, Type type)
    {
        var world = entity.GetWorld();
        world.EntityManager.AddScript(entity, type);
    }

    /// <summary>
    /// Retrieves the first script of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the script to retrieve.</typeparam>
    /// <param name="entity">The entity whose script is to be retrieved.</param>
    /// <returns>The script of type <typeparamref name="T"/>, or null if no such script exists.</returns>
    public static T? GetScript<T>(this Entity entity)
        where T : ScriptComponent
    {
        var world = entity.GetWorld();
        return world.EntityManager.GetScript<T>(entity);
    }

    /// <summary>
    /// Retrieves all scripts of type <typeparamref name="T"/> associated with the given <see cref="Entity"/>.
    /// </summary>
    /// <typeparam name="T">The type of the scripts to retrieve.</typeparam>
    /// <param name="entity">The entity whose scripts are to be retrieved.</param>
    /// <returns>An enumerable of scripts of type <typeparamref name="T"/>.</returns>
    public static IEnumerable<T> GetScripts<T>(this Entity entity)
        where T : ScriptComponent
    {
        var world = entity.GetWorld();
        return world.EntityManager.GetScripts<T>(entity);
    }

    /// <summary>
    /// Destroys the given <see cref="Entity"/> by removing it from its associated <see cref="World"/>.
    /// </summary>
    /// <param name="entity">The entity to destroy.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Destroy(this Entity entity)
    {
        var world = entity.GetWorld();
        world.EntityManager.RemoveEntity(ref entity);
    }
}