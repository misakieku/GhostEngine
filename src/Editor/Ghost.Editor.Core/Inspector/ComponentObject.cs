using Ghost.Entities;

namespace Ghost.Editor.Core.Inspector;

public readonly struct ComponentObject
{
    private readonly World _world;
    private readonly Entity _entity;

    internal ComponentObject(World world, Entity entity)
    {
        _world = world;
        _entity = entity;
    }

    public ref T GetData<T>()
        where T : unmanaged, IComponentData
    {
        return ref _world.EntityManager.GetComponent<T>(_entity);
    }

    public void SetData<T>(in T data)
        where T : unmanaged, IComponentData
    {
        _world.EntityManager.SetComponent(_entity, data);
    }
}