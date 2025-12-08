using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.InteropServices;

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
        public Entity entity;
        public CommandType type;
        public Identifier<IComponent> componentTypeID;
    }

    private readonly EntityManager _entityManager;
    private UnsafeList<Command> _commands; // TODO: Maybe use UnsafeArray<byte> directly to save additional memory allocation in Unsafe<byte> data inside Command struct.
    private bool _disposed;

    public EntityCommandBuffer(EntityManager entityManager)
    {
        _entityManager = entityManager;
        _commands = new UnsafeList<Command>(32, Allocator.Persistent);
    }

    ~EntityCommandBuffer()
    {
        Dispose();
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

    public void CreateEntity(params ReadOnlySpan<Identifier<IComponent>> componentTypeIDs)
    {
        var data = new UnsafeArray<byte>(componentTypeIDs.Length * sizeof(int), Allocator.Temp);
        MemoryMarshal.Cast<Identifier<IComponent>, byte>(componentTypeIDs).CopyTo(data.AsSpan());

        var command = new Command
        {
            type = CommandType.CreateEntity,
            data = data,
            entity = Entity.Invalid,
            componentTypeID = Identifier<IComponent>.Invalid
        };

        _commands.Add(command);
    }

    public void DestroyEntity(Entity entity)
    {
        _commands.Add(new Command
        {
            type = CommandType.DestroyEntity,
            data = default,
            entity = entity,
            componentTypeID = Identifier<IComponent>.Invalid
        });
    }

    public void AddComponent<T>(Entity entity, T component = default)
        where T : unmanaged, IComponent
    {
        var data = new UnsafeArray<byte>(sizeof(T), Allocator.Temp);
        MemoryUtility.MemCpy(data.GetUnsafePtr(), &component, (nuint)sizeof(T));

        _commands.Add(new Command
        {
            type = CommandType.AddComponent,
            data = data,
            entity = entity,
            componentTypeID = ComponentTypeID<T>.value
        });
    }

    public void RemoveComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        _commands.Add(new Command
        {
            type = CommandType.RemoveComponent,
            data = default,
            entity = entity,
            componentTypeID = ComponentTypeID<T>.value
        });
    }

    public void SetComponent<T>(Entity entity, T component)
        where T : unmanaged, IComponent
    {
        var data = new UnsafeArray<byte>(sizeof(T), Allocator.Temp);
        MemoryUtility.MemCpy(data.GetUnsafePtr(), &component, (nuint)sizeof(T));

        _commands.Add(new Command
        {
            type = CommandType.SetComponent,
            data = data,
            entity = entity,
            componentTypeID = ComponentTypeID<T>.value
        });
    }

    internal void Playback()
    {
        foreach (ref var command in _commands)
        {
            switch (command.type)
            {
                case CommandType.CreateEntity:
                    if (command.data.Count > 0)
                    {
                        _entityManager.CreateEntity(MemoryMarshal.Cast<byte, Identifier<IComponent>>(command.data.AsSpan()));
                    }
                    else
                    {
                        _entityManager.CreateEntity();
                    }
                    break;
                case CommandType.DestroyEntity:
                    _entityManager.DestroyEntity(command.entity);
                    break;
                case CommandType.AddComponent:
                    _entityManager.AddComponent(command.entity, command.componentTypeID, command.data.GetUnsafePtr());
                    break;
                case CommandType.RemoveComponent:
                    _entityManager.RemoveComponent(command.entity, command.componentTypeID);
                    break;
                case CommandType.SetComponent:
                    _entityManager.SetComponent(command.entity, command.componentTypeID, command.data.GetUnsafePtr());
                    break;
            }
        }

        Reset();
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
        if (_disposed)
        {
            return;
        }

        foreach (ref var command in _commands)
        {
            command.data.Dispose();
        }

        _commands.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
