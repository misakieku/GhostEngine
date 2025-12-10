using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public record struct ManagedEntity
{
    public int id;
    public int generation;

    public override readonly string ToString()
    {
        return $"ManagedEntity({id}, {generation})";
    }
}

public struct ManagedEntityRef : IComponent
{
    public ManagedEntity entity;
}

public abstract class ScriptComponent : IComponent
{
    internal World _world = null!;
    internal Entity _entity;
    internal ManagedEntity _managedEntity;

    public World World => _world;
    public Entity Entity => _entity;
    public ManagedEntity ManagedEntity => _managedEntity;

    protected ref T GetComponent<T>()
        where T : unmanaged, IComponent
    {
        ref var value = ref _world.EntityManager.GetComponent<T>(_entity);
        if (Unsafe.IsNullRef(ref value))
        {
            throw new InvalidOperationException($"Entity {_entity} does not have component of type {typeof(T)}");
        }

        return ref value;
    }

    public virtual void OnCreate()
    {
    }

    public virtual void OnDestroy()
    {
    }

    public virtual void OnEnable()
    {
    }

    public virtual void OnDisable()
    {
    }

    public virtual void Start()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public virtual void LateUpdate()
    {
    }
}
