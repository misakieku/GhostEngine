using System.Diagnostics.CodeAnalysis;

namespace Ghost.Entities;

public partial struct World : IDisposable
{
    private static int _nextWorldIndex = 0;

    private readonly int _worldIndex;

    private List<Entity> _entities;
    private readonly Stack<EntityID> _freeSlots;
    private readonly Dictionary<Type, object> _pools;

    public World()
    {
        _worldIndex = _nextWorldIndex++;

        _entities = new List<Entity>();
        _freeSlots = new Stack<int>();
        _pools = new();
    }

    public readonly Entity CreateEntity()
    {
        if (_freeSlots.Count > 0)
        {
            var index = _freeSlots.Pop();
            return _entities[index];
        }
        else
        {
            var index = _entities.Count;
            var entity = new Entity(index, 0, _worldIndex);
            _entities.Add(entity);
            return entity;
        }
    }

    public readonly void RemoveEntity(Entity e)
    {
        if (e.ID >= _entities.Count || _entities[e.ID].Generation != e.Generation)
        {
            return;
        }

        var entity = _entities[e.ID];
        entity.IncrementGeneration();
        _entities[e.ID] = entity;
        _freeSlots.Push(e.ID);
    }

    public void AddComponent<T>(Entity entity, T component) where T : struct, IComponent
        => GetOrCreatePool<T>().Add(entity, component);

    public void SetComponent<T>(Entity entity, T component) where T : struct, IComponent
        => GetOrCreatePool<T>().Set(entity, component);

    public bool HasComponent<T>(Entity entity) where T : struct, IComponent
        => GetOrCreatePool<T>().Has(entity);

    private readonly bool TryGetPool<T>([MaybeNullWhen(false)] out ComponentPool<T> pool)
        where T : struct, IComponent
    {
        var type = typeof(T);
        if (_pools.TryGetValue(type, out var obj))
        {
            pool = (ComponentPool<T>)obj;
            return true;
        }

        pool = null;
        return false;
    }

    private readonly ComponentPool<T> GetOrCreatePool<T>()
        where T : struct, IComponent
    {
        var type = typeof(T);
        if (!_pools.TryGetValue(type, out var obj))
        {
            var pool = new ComponentPool<T>();
            _pools[type] = pool;
            return pool;
        }

        return (ComponentPool<T>)obj;
    }

    public readonly void Dispose()
    {
        foreach (var pool in _pools.Values)
        {
            if (pool is IDisposable disposablePool)
            {
                disposablePool.Dispose();
            }
        }

        _pools.Clear();
        _freeSlots.Clear();
    }
}