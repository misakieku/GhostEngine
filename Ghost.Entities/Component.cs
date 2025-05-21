namespace Ghost.Entities;

public interface IComponent
{

}

internal class ComponentPool<T> : IDisposable
    where T : struct, IComponent
{
    private struct ComponentData
    {
        public T data;
        public Entity owner;
    }

    private EntityID _nextId;
    private EntityID _capacity;

    private ComponentData[] _components;
    private EntityID[] _lookup;

    public ComponentPool(int initialSize = 16)
    {
        _nextId = 0;
        _capacity = initialSize;

        _components = new ComponentData[initialSize];
        _lookup = new EntityID[initialSize];

        _lookup.AsSpan().Fill(Entity.INVALID_ID);
    }

    public EntityID Count => _nextId;

    private EntityID GetComponentIndex(Entity entity)
    {
        return _lookup[entity.ID];
    }

    public void Add(Entity entity, T component)
    {
        if (entity.ID >= _lookup.Length)
        {
            _lookup.AsSpan(_nextId, entity.ID - _lookup.Length + 1).Fill(Entity.INVALID_ID);
        }

        if (_lookup[entity.ID] != Entity.INVALID_ID)
        {
            // Overwrite the old data if generation is larger
            if (entity.Generation > _components[_lookup[entity.ID]].owner.Generation)
            {
                var index = _lookup[entity.ID];
                _components[index].data = component;
                _components[index].owner = entity;
            }

            return;
        }

        if (_nextId >= _capacity)
        {
            var newCapacity = _capacity * 2;
            Array.Resize(ref _components, newCapacity);
            Array.Resize(ref _lookup, newCapacity);
            _lookup.AsSpan(_capacity, newCapacity - _capacity).Fill(Entity.INVALID_ID);

            _capacity = newCapacity;
        }

        _components[_nextId] = new ComponentData
        {
            data = component,
            owner = entity
        };
        _lookup[entity.ID] = _nextId;

        _nextId++;
    }

    public ref T GetRef(Entity entity)
    {
        return ref _components[_lookup[entity.ID]].data;
    }

    public bool Has(Entity entity)
    {
        if (entity.ID >= _lookup.Length)
        {
            return false;
        }

        var index = GetComponentIndex(entity);
        return index != Entity.INVALID_ID && _components[index].owner.Generation == entity.Generation;
    }

    public void Set(Entity entity, T component)
    {
        if (entity.ID >= _lookup.Length || _lookup[entity.ID] == Entity.INVALID_ID)
        {
            return;
        }

        var index = _lookup[entity.ID];
        _components[index].data = component;
        _components[index].owner = entity;
    }

    public void Dispose()
    {
    }
}