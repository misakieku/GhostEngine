using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.Utilities;

namespace Ghost.Entities;

public partial class EntityManager
{
    private readonly SlotMap<List<ScriptComponent>> _scriptComponents;

    internal SlotMap<List<ScriptComponent>> ScriptComponents => _scriptComponents;

    /// <summary>
    /// Creates a new ManagedEntity and associates it with the given Entity.
    /// </summary>
    /// <param name="entity">The Entity to associate with the ManagedEntity.</param>
    /// <returns>The created ManagedEntity.</returns>
    public ManagedEntity CreateManagedEntity(Entity entity)
    {
        var managedEntity = CreateManagedEntity();
        AddComponent(entity, new ManagedEntityRef
        {
            entity = managedEntity
        });

        return managedEntity;
    }

    /// <summary>
    /// Creates a new ManagedEntity.
    /// </summary>
    /// <remarks>
    /// You must call this if you add <see cref="ManagedEntityRef"/> manually to an entity.
    /// Otherwise, use <see cref="CreateManagedEntity(Entity)"/>.
    /// </remarks>
    /// <returns>The created ManagedEntity.</returns>
    public ManagedEntity CreateManagedEntity()
    {
        var id = _scriptComponents.Add(new(8), out var generation);
        var managedEntity = new ManagedEntity
        {
            id = id,
            generation = generation
        };

        return managedEntity;
    }

    /// <summary>
    /// Destroys the given ManagedEntity and calls OnDestroy on all associated ScriptComponents.
    /// </summary>
    /// <param name="managedEntity">The ManagedEntity to destroy.</param>
    public void DestroyManagedEntity(ManagedEntity managedEntity)
    {
        if (_scriptComponents.TryGetElement(managedEntity.id, managedEntity.generation, out var scripts))
        {
            foreach (var script in scripts)
            {
                script.OnDestroy();
            }

            _scriptComponents.Remove(managedEntity.id, managedEntity.generation);
        }
    }

    /// <summary>
    /// Checks if the given ManagedEntity exists.
    /// </summary>
    /// <param name="managedEntity">The ManagedEntity to check.</param>
    /// <returns>True if the ManagedEntity exists, false otherwise.</returns>
    public bool Exists(ManagedEntity managedEntity)
    {
        return _scriptComponents.Contains(managedEntity.id, managedEntity.generation);
    }

    /// <summary>
    /// Adds a ScriptComponent of type T to the given ManagedEntity and Entity.
    /// </summary>
    /// <typeparam name="T">The type of ScriptComponent to add.</typeparam>
    /// <param name="managedEntity">The ManagedEntity to add the ScriptComponent to.</
    /// <param name="entity">The Entity associated with the ManagedEntity.</param>
    public void AddScriptComponent<T>(ManagedEntity managedEntity, Entity entity)
        where T : ScriptComponent, new()
    {
        if (_scriptComponents.TryGetElement(managedEntity.id, managedEntity.generation, out var scripts))
        {
            var script = new T
            {
                _world = _world,
                _entity = entity,
                _managedEntity = managedEntity
            };

            scripts.Add(script);
            script.OnCreate();

            return;
        }

        throw new InvalidOperationException($"ManagedEntity {managedEntity} does not exist.");
    }

    /// <summary>
    /// Adds a ScriptComponent of type T to the given Entity.
    /// </summary>
    /// <typeparam name="T">The type of ScriptComponent to add.</typeparam>
    /// <param name="entity">The Entity to add the ScriptComponent to.</param>
    public unsafe void AddScriptComponent<T>(Entity entity)
        where T : ScriptComponent, new()
    {
        var location = _entityLocations.GetElementAt(entity.ID, entity.Generation);
        ref var archetype = ref _world.GetArchetypeReference(location.archetypeID);

        var pManagedEntityRef = (ManagedEntityRef*)archetype.GetComponentData(location.chunkIndex, location.rowIndex, ComponentTypeID<ManagedEntityRef>.Value);
        if (pManagedEntityRef == null)
        {
            throw new InvalidOperationException($"Entity {entity} does not have ManagedEntityRef component.");
        }

        AddScriptComponent<T>(pManagedEntityRef->entity, entity);
    }

    /// <summary>
    /// Destroys the ScriptComponent of type T associated with the given ManagedEntity.
    /// </summary>
    /// <typeparam name="T">The type of ScriptComponent to destroy.</typeparam>
    /// <param name="managedEntity">The ManagedEntity whose ScriptComponent is to be destroyed </param>
    /// <returns>True if the ScriptComponent was found and destroyed, false otherwise.</returns
    public bool DestroyScriptComponent<T>(ManagedEntity managedEntity)
        where T : ScriptComponent
    {
        if (_scriptComponents.TryGetElement(managedEntity.id, managedEntity.generation, out var scripts))
        {
            for (var i = 0; i < scripts.Count; i++)
            {
                if (scripts[i] is T script)
                {
                    script.OnDestroy();
                    scripts.RemoveAndSwapBack(i);
                    return true;
                }
            }

            return false;
        }

        throw new InvalidOperationException($"ManagedEntity {managedEntity} does not exist.");
    }

    /// <summary>
    /// Checks if the given ManagedEntity has a ScriptComponent of type T.
    /// </summary>
    /// <typeparam name="T">The type of ScriptComponent to check for.</typeparam>
    /// <param name="managedEntity">The ManagedEntity to check.</param>
    /// <returns>True if the ManagedEntity has a ScriptComponent of type T, false </returns>
    public bool HasScriptComponent<T>(ManagedEntity managedEntity)
        where T : ScriptComponent
    {
        if (_scriptComponents.TryGetElement(managedEntity.id, managedEntity.generation, out var scripts))
        {
            foreach (var script in scripts)
            {
                if (script is T)
                {
                    return true;
                }
            }

            return false;
        }

        throw new InvalidOperationException($"ManagedEntity {managedEntity} does not exist.");
    }

    /// <summary>
    /// Gets the ScriptComponent of type T associated with the given ManagedEntity.
    /// </summary>
    /// <typeparam name="T">The type of ScriptComponent to get.</typeparam>
    /// <param name="managedEntity">The ManagedEntity whose ScriptComponent is to be retrieved
    /// <returns>The ScriptComponent of type T.</returns>
    public T GetScriptComponent<T>(ManagedEntity managedEntity)
        where T : ScriptComponent
    {
        if (_scriptComponents.TryGetElement(managedEntity.id, managedEntity.generation, out var scripts))
        {
            foreach (var script in scripts)
            {
                if (script is T typedScript)
                {
                    return typedScript;
                }
            }

            throw new InvalidOperationException($"ManagedEntity {managedEntity} does not have script component of type {typeof(T)}.");
        }

        throw new InvalidOperationException($"ManagedEntity {managedEntity} does not exist.");
    }

    /// <summary>
    /// Gets all ScriptComponents of type T associated with the given ManagedEntity.
    /// </summary>
    /// <typeparam name="T">The type of ScriptComponent to get.</typeparam>
    /// <param name="managedEntity">The ManagedEntity whose ScriptComponents are to be retrieved
    /// <returns>The list of ScriptComponents of type T.</returns>
    public List<T> GetScriptComponents<T>(ManagedEntity managedEntity)
        where T : ScriptComponent
    {
        if (_scriptComponents.TryGetElement(managedEntity.id, managedEntity.generation, out var scripts))
        {
            var result = new List<T>();
            foreach (var script in scripts)
            {
                if (script is T typedScript)
                {
                    result.Add(typedScript);
                }
            }

            return result;
        }

        throw new InvalidOperationException($"ManagedEntity {managedEntity} does not exist.");
    }
}
