using Ghost.Core;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Runtime.CompilerServices;

namespace Ghost.Entities;

public unsafe struct EntityCommandBuffer : IDisposable
{
    private enum ECBOpCode : byte
    {
        CreateEntity,
        CreateEntityWithComponents,
        DestroyEntity,
        AddComponent,
        RemoveComponent,
        SetComponent,
        AddSharedComponent,
        RemoveSharedComponent,
        SetSharedComponent,
    }

    private BufferWriter _writer;

    public EntityCommandBuffer(int capacity, AllocationHandle allocationHandle)
    {
        _writer = new BufferWriter(capacity, allocationHandle);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateEntity(int count = 1)
    {
        _writer.Write(ECBOpCode.CreateEntity);
        _writer.Write(count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateEntity(int count, ComponentSetView set)
    {
        _writer.Write(ECBOpCode.CreateEntityWithComponents);
        _writer.Write(count);
        _writer.Write(set.Components.Length);
        _writer.WriteSpan(set.Components);
        _writer.Write(set.SharedComponentData.Length);
        _writer.WriteSpan(set.SharedComponentData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(Entity entity)
    {
        _writer.Write(ECBOpCode.DestroyEntity);
        _writer.Write(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddComponent<T>(Entity entity, T component = default)
        where T : unmanaged, IComponent
    {
        _writer.Write(ECBOpCode.AddComponent);
        _writer.Write(entity);
        _writer.Write(ComponentTypeID<T>.Value);
        _writer.Write(component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        _writer.Write(ECBOpCode.RemoveComponent);
        _writer.Write(entity);
        _writer.Write(ComponentTypeID<T>.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetComponent<T>(Entity entity, T component)
        where T : unmanaged, IComponent
    {
        _writer.Write(ECBOpCode.SetComponent);
        _writer.Write(entity);
        _writer.Write(ComponentTypeID<T>.Value);
        _writer.Write(component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddSharedComponent<T>(Entity entity, T component)
        where T : unmanaged, ISharedComponent
    {
        _writer.Write(ECBOpCode.AddSharedComponent);
        _writer.Write(entity);
        _writer.Write(ComponentTypeID<T>.Value);
        _writer.Write(component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveSharedComponent<T>(Entity entity)
        where T : unmanaged, ISharedComponent
    {
        _writer.Write(ECBOpCode.RemoveSharedComponent);
        _writer.Write(entity);
        _writer.Write(ComponentTypeID<T>.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSharedComponent<T>(Entity entity, T component)
        where T : unmanaged, ISharedComponent
    {
        _writer.Write(ECBOpCode.SetSharedComponent);
        _writer.Write(entity);
        _writer.Write(ComponentTypeID<T>.Value);
        _writer.Write(component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetComponent(Entity entity, Identifier<IComponent> componentID, void* data)
    {
        _writer.Write(ECBOpCode.SetComponent);
        _writer.Write(entity);
        _writer.Write(componentID);
        _writer.WriteMemory(data, ComponentRegistry.GetComponentInfo(componentID).size);
    }

    public readonly void Playback(EntityManager entityManager)
    {
        var reader = _writer.AsReader();

        while (reader.RemainingBytes > 0)
        {
            var op = reader.Read<ECBOpCode>();

            switch (op)
            {
                case ECBOpCode.CreateEntity:
                    var count = reader.Read<int>();
                    entityManager.CreateEntities(count);
                    break;

                case ECBOpCode.CreateEntityWithComponents:
                    var entityCount = reader.Read<int>();

                    var compCount = reader.Read<int>();
                    var components = reader.ReadSpan<Identifier<IComponent>>(compCount);
                    var sharedDataLength = reader.Read<int>();
                    var sharedData = reader.ReadSpan<byte>(sharedDataLength);

                    var set = new ComponentSetView(components, sharedData);
                    entityManager.CreateEntities(entityCount, set);
                    break;

                case ECBOpCode.DestroyEntity:
                    var entityToDestroy = reader.Read<Entity>();
                    entityManager.DestroyEntity(entityToDestroy);
                    break;

                case ECBOpCode.AddComponent:
                    var entityToAdd = reader.Read<Entity>();
                    var addCompTypeID = reader.Read<Identifier<IComponent>>();
                    var pAddCompData = reader.ReadMemory((nuint)ComponentRegistry.GetComponentInfo(addCompTypeID).size);
                    entityManager.AddComponent(entityToAdd, addCompTypeID, pAddCompData);
                    break;

                case ECBOpCode.RemoveComponent:
                    var entityToRemove = reader.Read<Entity>();
                    var removeCompTypeID = reader.Read<Identifier<IComponent>>();
                    entityManager.RemoveComponent(entityToRemove, removeCompTypeID);
                    break;

                case ECBOpCode.SetComponent:
                    var entityToSet = reader.Read<Entity>();
                    var setCompTypeID = reader.Read<Identifier<IComponent>>();
                    var pSetCompData = reader.ReadMemory((nuint)ComponentRegistry.GetComponentInfo(setCompTypeID).size);
                    entityManager.SetComponent(entityToSet, setCompTypeID, pSetCompData);
                    break;

                case ECBOpCode.AddSharedComponent:
                    var entityToAddShared = reader.Read<Entity>();
                    var addSharedTypeID = reader.Read<Identifier<IComponent>>();
                    var pAddSharedData = reader.ReadMemory((nuint)ComponentRegistry.GetComponentInfo(addSharedTypeID).size);
                    entityManager.AddSharedComponent(entityToAddShared, addSharedTypeID, pAddSharedData);
                    break;

                case ECBOpCode.RemoveSharedComponent:
                    var entityToRemoveShared = reader.Read<Entity>();
                    var removeSharedTypeID = reader.Read<Identifier<IComponent>>();
                    entityManager.RemoveSharedComponent(entityToRemoveShared, removeSharedTypeID);
                    break;

                case ECBOpCode.SetSharedComponent:
                    var entityToSetShared = reader.Read<Entity>();
                    var setSharedTypeID = reader.Read<Identifier<IComponent>>();
                    var pSetSharedData = reader.ReadMemory((nuint)ComponentRegistry.GetComponentInfo(setSharedTypeID).size);
                    entityManager.SetSharedComponent(entityToSetShared, setSharedTypeID, pSetSharedData);
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _writer.Reset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _writer.Dispose();
    }
}
