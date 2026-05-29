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
        DestroyEntities,
        AddComponent,
        RemoveComponent,
        SetComponent,
        AddSharedComponent,
        RemoveSharedComponent,
        SetSharedComponent,
    }

    private BufferWriter _writer;
    private int _nextTempId;

    public EntityCommandBuffer(int capacity, AllocationHandle allocationHandle)
    {
        _writer = new BufferWriter(capacity, allocationHandle);
        _nextTempId = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity CreateEntity()
    {
        _writer.Write(ECBOpCode.CreateEntity);
        _writer.Write(1);
        var tempId = _nextTempId--;
        return new Entity(tempId, -1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateEntities(int count)
    {
        _writer.Write(ECBOpCode.CreateEntity);
        _writer.Write(count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity CreateEntity(ComponentSetView set)
    {
        _writer.Write(ECBOpCode.CreateEntityWithComponents);
        _writer.Write(1);
        _writer.Write(set.Components.Length);
        _writer.WriteSpan(set.Components);
        _writer.Write(set.SharedComponentData.Length);
        _writer.WriteSpan(set.SharedComponentData);
        var tempId = _nextTempId--;
        return new Entity(tempId, -1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntity(Entity entity)
    {
        _writer.Write(ECBOpCode.DestroyEntity);
        _writer.Write(entity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DestroyEntities(params ReadOnlySpan<Entity> entities)
    {
        _writer.Write(ECBOpCode.DestroyEntities);
        _writer.Write(entities.Length);
        _writer.WriteSpan(entities);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddComponent<T>(Entity entity, T component = default)
        where T : unmanaged, IComponentData
    {
        _writer.Write(ECBOpCode.AddComponent);
        _writer.Write(entity);
        _writer.Write(ComponentTypeID<T>.Value);
        _writer.Write(component);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveComponent<T>(Entity entity)
        where T : unmanaged, IComponentData
    {
        _writer.Write(ECBOpCode.RemoveComponent);
        _writer.Write(entity);
        _writer.Write(ComponentTypeID<T>.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetComponent<T>(Entity entity, T component)
        where T : unmanaged, IComponentData
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Entity MapEntity(Entity entity, ref Misaki.HighPerformance.LowLevel.Collections.UnsafeList<Entity> map)
    {
        if (entity.ID < 0)
        {
            int index = (-entity.ID) - 1;
            if (index >= 0 && index < map.Count)
            {
                return map[index];
            }
        }
        return entity;
    }

    public readonly void Playback(EntityManager entityManager)
    {
        var reader = _writer.AsReader();

        using var scope = AllocationManager.CreateStackScope();
        var tempMap = new Misaki.HighPerformance.LowLevel.Collections.UnsafeList<Entity>(16, scope.AllocationHandle);

        while (reader.RemainingBytes > 0)
        {
            var op = reader.Read<ECBOpCode>();

            switch (op)
            {
                case ECBOpCode.CreateEntity:
                    var count = reader.Read<int>();
                    if (count == 1)
                    {
                        var e = entityManager.CreateEntity();
                        tempMap.Add(e);
                    }
                    else
                    {
                        entityManager.CreateEntities(count);
                    }
                    break;

                case ECBOpCode.CreateEntityWithComponents:
                    var entityCount = reader.Read<int>();

                    var compCount = reader.Read<int>();
                    var components = reader.ReadSpan<Identifier<IComponent>>(compCount);
                    var sharedDataLength = reader.Read<int>();
                    var sharedData = reader.ReadSpan<byte>(sharedDataLength);

                    var set = new ComponentSetView(components, sharedData);
                    
                    if (entityCount == 1)
                    {
                        var e = entityManager.CreateEntity(set);
                        tempMap.Add(e);
                    }
                    else
                    {
                        entityManager.CreateEntities(entityCount, set);
                    }
                    break;

                case ECBOpCode.DestroyEntity:
                    var entityToDestroy = reader.Read<Entity>();
                    entityManager.DestroyEntity(MapEntity(entityToDestroy, ref tempMap));
                    break;

                case ECBOpCode.DestroyEntities:
                    var removeCount = reader.Read<int>();
                    var entitiesToRemove = reader.ReadSpan<Entity>(removeCount);
                    // Mapped destruction for multiple entities not typically used with temp IDs, 
                    // but we map them just in case.
                    for (int i = 0; i < removeCount; i++)
                    {
                        entityManager.DestroyEntity(MapEntity(entitiesToRemove[i], ref tempMap));
                    }
                    break;

                case ECBOpCode.AddComponent:
                    var entityToAdd = reader.Read<Entity>();
                    var addCompTypeID = reader.Read<Identifier<IComponent>>();
                    var pAddCompData = reader.ReadBuffer((nuint)ComponentRegistry.GetComponentInfo(addCompTypeID).size);
                    entityManager.AddComponent(MapEntity(entityToAdd, ref tempMap), addCompTypeID, pAddCompData);
                    break;

                case ECBOpCode.RemoveComponent:
                    var entityToRemove = reader.Read<Entity>();
                    var removeCompTypeID = reader.Read<Identifier<IComponent>>();
                    entityManager.RemoveComponent(MapEntity(entityToRemove, ref tempMap), removeCompTypeID);
                    break;

                case ECBOpCode.SetComponent:
                    var entityToSet = reader.Read<Entity>();
                    var setCompTypeID = reader.Read<Identifier<IComponent>>();
                    var pSetCompData = reader.ReadBuffer((nuint)ComponentRegistry.GetComponentInfo(setCompTypeID).size);
                    entityManager.SetComponent(MapEntity(entityToSet, ref tempMap), setCompTypeID, pSetCompData);
                    break;

                case ECBOpCode.AddSharedComponent:
                    var entityToAddShared = reader.Read<Entity>();
                    var addSharedTypeID = reader.Read<Identifier<IComponent>>();
                    var pAddSharedData = reader.ReadBuffer((nuint)ComponentRegistry.GetComponentInfo(addSharedTypeID).size);
                    entityManager.AddSharedComponent(MapEntity(entityToAddShared, ref tempMap), addSharedTypeID, pAddSharedData);
                    break;

                case ECBOpCode.RemoveSharedComponent:
                    var entityToRemoveShared = reader.Read<Entity>();
                    var removeSharedTypeID = reader.Read<Identifier<IComponent>>();
                    entityManager.RemoveSharedComponent(MapEntity(entityToRemoveShared, ref tempMap), removeSharedTypeID);
                    break;

                case ECBOpCode.SetSharedComponent:
                    var entityToSetShared = reader.Read<Entity>();
                    var setSharedTypeID = reader.Read<Identifier<IComponent>>();
                    var pSetSharedData = reader.ReadBuffer((nuint)ComponentRegistry.GetComponentInfo(setSharedTypeID).size);
                    entityManager.SetSharedComponent(MapEntity(entityToSetShared, ref tempMap), setSharedTypeID, pSetSharedData);
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        _writer.Reset();
        _nextTempId = -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _writer.Dispose();
    }
}
