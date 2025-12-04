using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;

namespace Ghost.Entities;

public unsafe class EntityCommandBuffer : IDisposable
{
    private enum CommandType
    {
        CreateEntity,
        DestroyEntity,
        AddComponent,
        RemoveComponent,
        SetComponent,
    }

    private struct Command
    {
        public UnsafeArray<byte> data;
        public CommandType type;
        public Entity entity;
        public int componentTypeID;
    }

    private readonly EntityManager _entityManager;
    private UnsafeList<Command> _commands; // TODO: Maybe use UnsafeArray<byte> directly?

    public EntityCommandBuffer(EntityManager entityManager)
    {
        _entityManager = entityManager;
        _commands = new UnsafeList<Command>(32, Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
    }

    public void CreateEntity()
    {
        var command = new Command
        {
            type = CommandType.CreateEntity,
            data = default,
            entity = default,
            componentTypeID = -1
        };

        _commands.Add(command);
    }

    public void DestroyEntity(Entity entity)
    {
        var command = new Command
        {
            type = CommandType.DestroyEntity,
            data = default,
            entity = entity,
            componentTypeID = -1
        };

        _commands.Add(command);
    }

    public void AddComponent<T>(Entity entity, T component)
        where T : unmanaged, IComponent
    {
        var data = new UnsafeArray<byte>(sizeof(T), Misaki.HighPerformance.LowLevel.Buffer.Allocator.Persistent);
        MemoryUtility.MemCpy(&component, data.GetUnsafePtr(), (nuint)sizeof(T));

        var command = new Command
        {
            type = CommandType.AddComponent,
            data = data,
            entity = entity,
            componentTypeID = ComponentTypeID<T>.value
        };

        _commands.Add(command);
    }

    public void RemoveComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        var command = new Command
        {
            type = CommandType.RemoveComponent,
            data = default,
            entity = entity,
            componentTypeID = ComponentTypeID<T>.value
        };

        _commands.Add(command);
    }

    public void Reset()
    {
        foreach (ref var command in _commands)
        {
            command.data.Dispose();
        }

        _commands.Clear();
    }

    public void Dispose()
    {
        foreach (ref var command in _commands)
        {
            command.data.Dispose();
        }

        _commands.Dispose();
    }
}
