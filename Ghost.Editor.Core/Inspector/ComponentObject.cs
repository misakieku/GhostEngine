using Ghost.SparseEntities;
using Ghost.SparseEntities.Components;
using Ghost.SparseEntities.Query;

namespace Ghost.Editor.Core.Inspector;

public unsafe readonly struct ComponentObject
{
    private readonly World _world;
    private readonly Entity _entity;

    internal ComponentObject(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public CompRef<T> GetData<T>()
        where T : unmanaged, IComponentData
    {
        return _world.EntityManager.GetComponent<T>(_entity);
    }

    public void SetData<T>(in T data)
        where T : unmanaged, IComponentData
    {
        _world.EntityManager.SetComponent(_entity, in data);
    }
}