using Ghost.Core;
using Ghost.Core.Utilities;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
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
        MigrateEntity,
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
    public void CreateEntities(Span<Entity> entities)
    {
        _writer.Write(ECBOpCode.CreateEntity);
        _writer.Write(entities.Length);

        for (int i = 0; i < entities.Length; i++)
        {
            var tempId = _nextTempId--;
            entities[i] = new Entity(tempId, -1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateEntities(int count)
    {
        _writer.Write(ECBOpCode.CreateEntity);
        _writer.Write(-count); // Negative count indicates multiple entities without returning temp IDs.
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
    public void CreateEntities(Span<Entity> entities, ComponentSetView set)
    {
        _writer.Write(ECBOpCode.CreateEntityWithComponents);
        _writer.Write(entities.Length);
        _writer.Write(set.Components.Length);
        _writer.WriteSpan(set.Components);
        _writer.Write(set.SharedComponentData.Length);
        _writer.WriteSpan(set.SharedComponentData);

        for (int i = 0; i < entities.Length; i++)
        {
            var tempId = _nextTempId--;
            entities[i] = new Entity(tempId, -1);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateEntities(int count, ComponentSetView set)
    {
        _writer.Write(ECBOpCode.CreateEntityWithComponents);
        _writer.Write(-count); // Negative count indicates multiple entities without returning temp IDs.
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
    public void MigrateEntity(Entity entity, ComponentSetView set)
    {
        _writer.Write(ECBOpCode.MigrateEntity);
        _writer.Write(entity);
        _writer.Write(set.Components.Length);
        _writer.WriteSpan(set.Components);
        _writer.Write(set.SharedComponentData.Length);
        _writer.WriteSpan(set.SharedComponentData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Entity MapEntity(Entity entity, UnsafeList<Entity>* map)
    {
        if (entity.ID < 0)
        {
            int index = (-entity.ID) - 1;
            if (index >= 0 && index < map->Count)
            {
                return ((Entity*)map->GetUnsafePtr())[index];
            }
        }

        return entity;
    }

    public readonly void Playback(EntityManager entityManager)
    {
        var reader = _writer.AsReader();

        using var scope = AllocationManager.CreateStackScope();
        using var tempMap = new UnsafeList<Entity>(16, scope.AllocationHandle);
        using var tempEntities = new UnsafeList<Entity>(16, scope.AllocationHandle);

        while (reader.RemainingBytes > 0)
        {
            var op = reader.Read<ECBOpCode>();

            switch (op)
            {
                case ECBOpCode.CreateEntity:
                    var count = reader.Read<int>();
                    var needRemap = count > 0;
                    count = count < 0 ? -count : count;

                    if (needRemap)
                    {
                        if (tempEntities.Capacity < count)
                        {
                            tempEntities.Resize(count);
                        }

                        tempEntities.UnsafeSetCount(count);
                        entityManager.CreateEntities(tempEntities);
                        tempMap.AddRange(tempEntities);
                    }
                    else
                    {
                        entityManager.CreateEntities(count);
                    }

                    break;

                case ECBOpCode.CreateEntityWithComponents:
                    var entityCount = reader.Read<int>();
                    var entityNeedRemap = entityCount > 0;
                    entityCount = entityCount < 0 ? -entityCount : entityCount;

                    var compCount = reader.Read<int>();
                    var components = reader.ReadSpan<Identifier<IComponent>>(compCount);
                    var sharedDataLength = reader.Read<int>();
                    var sharedData = reader.ReadSpan<byte>(sharedDataLength);

                    var set = new ComponentSetView(components, sharedData);
                    
                    if (entityNeedRemap)
                    {
                        if (tempEntities.Capacity < entityCount)
                        {
                            tempEntities.Resize(entityCount);
                        }

                        tempEntities.UnsafeSetCount(entityCount);
                        entityManager.CreateEntities(tempEntities, set);
                        tempMap.AddRange(tempEntities);
                    }
                    else
                    {
                        entityManager.CreateEntities(entityCount, set);
                    }

                    break;

                case ECBOpCode.DestroyEntity:
                    var entityToDestroy = reader.Read<Entity>();
                    entityManager.DestroyEntity(MapEntity(entityToDestroy, &tempMap));
                    break;

                case ECBOpCode.DestroyEntities:
                    var removeCount = reader.Read<int>();
                    var entitiesToRemove = reader.ReadSpan<Entity>(removeCount);
                    // Mapped destruction for multiple entities not typically used with temp IDs, 
                    // but we map them just in case.
                    for (int i = 0; i < removeCount; i++)
                    {
                        entityManager.DestroyEntity(MapEntity(entitiesToRemove[i], &tempMap));
                    }
                    break;

                case ECBOpCode.AddComponent:
                    var entityToAdd = reader.Read<Entity>();
                    var addCompTypeID = reader.Read<Identifier<IComponent>>();
                    var pAddCompData = reader.ReadBuffer((nuint)ComponentRegistry.GetComponentInfo(addCompTypeID).size);
                    entityManager.AddComponent(MapEntity(entityToAdd, &tempMap), addCompTypeID, pAddCompData);
                    break;

                case ECBOpCode.RemoveComponent:
                    var entityToRemove = reader.Read<Entity>();
                    var removeCompTypeID = reader.Read<Identifier<IComponent>>();
                    entityManager.RemoveComponent(MapEntity(entityToRemove, &tempMap), removeCompTypeID);
                    break;

                case ECBOpCode.SetComponent:
                    var entityToSet = reader.Read<Entity>();
                    var setCompTypeID = reader.Read<Identifier<IComponent>>();
                    var pSetCompData = reader.ReadBuffer((nuint)ComponentRegistry.GetComponentInfo(setCompTypeID).size);
                    entityManager.SetComponent(MapEntity(entityToSet, &tempMap), setCompTypeID, pSetCompData);
                    break;

                case ECBOpCode.AddSharedComponent:
                    var entityToAddShared = reader.Read<Entity>();
                    var addSharedTypeID = reader.Read<Identifier<IComponent>>();
                    var pAddSharedData = reader.ReadBuffer((nuint)ComponentRegistry.GetComponentInfo(addSharedTypeID).size);
                    entityManager.AddSharedComponent(MapEntity(entityToAddShared, &tempMap), addSharedTypeID, pAddSharedData);
                    break;

                case ECBOpCode.RemoveSharedComponent:
                    var entityToRemoveShared = reader.Read<Entity>();
                    var removeSharedTypeID = reader.Read<Identifier<IComponent>>();
                    entityManager.RemoveSharedComponent(MapEntity(entityToRemoveShared, &tempMap), removeSharedTypeID);
                    break;

                case ECBOpCode.SetSharedComponent:
                    var entityToSetShared = reader.Read<Entity>();
                    var setSharedTypeID = reader.Read<Identifier<IComponent>>();
                    var pSetSharedData = reader.ReadBuffer((nuint)ComponentRegistry.GetComponentInfo(setSharedTypeID).size);
                    entityManager.SetSharedComponent(MapEntity(entityToSetShared, &tempMap), setSharedTypeID, pSetSharedData);
                    break;

                case ECBOpCode.MigrateEntity:
                    var entityToMigrate = reader.Read<Entity>();
                    var migrateCompCount = reader.Read<int>();
                    var migrateComponents = reader.ReadSpan<Identifier<IComponent>>(migrateCompCount);
                    var migrateSharedDataLength = reader.Read<int>();
                    var migrateSharedData = reader.ReadSpan<byte>(migrateSharedDataLength);
                    var migrateSet = new ComponentSetView(migrateComponents, migrateSharedData);
                    entityManager.MigrateEntity(MapEntity(entityToMigrate, &tempMap), migrateSet);
                    break;
            }

            tempEntities.Clear();
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
