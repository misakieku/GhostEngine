using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;

namespace Ghost.Entities;

public unsafe partial class EntityManager : IDisposable
{
    private struct EntityLocation
    {
        public Identifier<Archetype> archetypeID;
        public int chunkIndex;
        public int rowIndex;
    }

    private readonly World _world;
    private UnsafeSlotMap<EntityLocation> _entityLocations;
    private bool _disposed;

    internal EntityManager(World world, int initialCapacity)
    {
        _world = world;
        _entityLocations = new UnsafeSlotMap<EntityLocation>(initialCapacity, Allocator.Persistent, AllocationOption.Clear);
    }

    ~EntityManager()
    {
        Dispose();
    }

    internal ResultStatus UpdateEntityLocation(Entity entity, Identifier<Archetype> newArchetypeID, int newChunkIndex, int newRowIndex)
    {
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return ResultStatus.NotFound;
        }

        location.archetypeID = newArchetypeID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return ResultStatus.Success;
    }

    /// <summary>
    /// Create an entity with no components.
    /// </summary>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity()
    {
        // Put into empty archetype
        ref var emptyArchetype = ref _world.GetArchetypeReference(World.EmptyArchetypeID);
        emptyArchetype.AllocateEntity(out var chunkIndex, out var rowIndex);

        var id = _entityLocations.Add(new EntityLocation
        {
            archetypeID = World.EmptyArchetypeID,
            chunkIndex = chunkIndex,
            rowIndex = rowIndex
        }, out var generation);

        var entity = new Entity(id, generation);
        emptyArchetype.SetEntity(chunkIndex, rowIndex, entity);

        return entity;
    }

    /// <summary>
    /// Create an entity with specified components.
    /// </summary>
    /// <param name="componentTypeIDs">The component type IDs to add to the entity.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity(params ReadOnlySpan<Identifier<IComponent>> componentTypeIDs)
    {
        var signatureHash = ComponentRegister.GetHashCode(componentTypeIDs);
        var arcID = _world.GetArchetypeIDBySignatureHash(signatureHash);

        if (arcID.IsNotValid)
        {
            arcID = _world.CreateArchetype(componentTypeIDs, signatureHash);
        }

        ref var archetype = ref _world.GetArchetypeReference(arcID);
        archetype.AllocateEntity(out var chunkIndex, out var rowIndex);

        var id = _entityLocations.Add(new EntityLocation
        {
            archetypeID = arcID,
            chunkIndex = chunkIndex,
            rowIndex = rowIndex
        }, out var generation);

        var entity = new Entity(id, generation);
        archetype.SetEntity(chunkIndex, rowIndex, entity);

        return entity;
    }

    /// <summary>
    /// Create multiple entities with specified components.
    /// </summary>
    /// <param name="count">The number of entities to create.</param>
    /// <param name="allocator">The allocator to use for the returned array.</param>
    /// <param name="componentTypeIDs">The component type IDs to add to the entities. </param>
    /// <returns>An array of the created entities.</returns>
    public UnsafeArray<Entity> CreateEntities(int count, Allocator allocator, params ReadOnlySpan<Identifier<IComponent>> componentTypeIDs)
    {
        var signatureHash = ComponentRegister.GetHashCode(componentTypeIDs);
        var arcID = _world.GetArchetypeIDBySignatureHash(signatureHash);

        if (arcID.IsNotValid)
        {
            arcID = _world.CreateArchetype(componentTypeIDs, signatureHash);
        }

        ref var archetype = ref _world.GetArchetypeReference(arcID);

        var entities = new UnsafeArray<Entity>(count, allocator);
        for (var i = 0; i < count; i++)
        {
            archetype.AllocateEntity(out var chunkIndex, out var rowIndex);

            var id = _entityLocations.Add(new EntityLocation
            {
                archetypeID = arcID,
                chunkIndex = chunkIndex,
                rowIndex = rowIndex
            }, out var generation);

            var entity = new Entity(id, generation);
            archetype.SetEntity(chunkIndex, rowIndex, entity);

            entities[i] = entity;
        }

        return entities;
    }

    /// <summary>
    /// Create multiple entities with specified components.
    /// </summary>
    /// <param name="count">The number of entities to create.</param>
    /// <param name="componentTypeIDs">The component type IDs to add to the entities. </param>
    public void CreateEntities(int count, params ReadOnlySpan<Identifier<IComponent>> componentTypeIDs)
    {
        var signatureHash = ComponentRegister.GetHashCode(componentTypeIDs);
        var arcID = _world.GetArchetypeIDBySignatureHash(signatureHash);

        if (arcID.IsNotValid)
        {
            arcID = _world.CreateArchetype(componentTypeIDs, signatureHash);
        }

        ref var archetype = ref _world.GetArchetypeReference(arcID);

        for (var i = 0; i < count; i++)
        {
            archetype.AllocateEntity(out var chunkIndex, out var rowIndex);

            var id = _entityLocations.Add(new EntityLocation
            {
                archetypeID = arcID,
                chunkIndex = chunkIndex,
                rowIndex = rowIndex
            }, out var generation);

            var entity = new Entity(id, generation);
            archetype.SetEntity(chunkIndex, rowIndex, entity);
        }
    }

    /// <summary>
    /// Destroy the specified entity.
    /// </summary>
    /// <returns>The result status of the operation.</returns>
    public ResultStatus DestroyEntity(Entity entity)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return ResultStatus.NotFound;
        }

        ref var archetype = ref _world.GetArchetypeReference(location.archetypeID);
        var r = archetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        if (r != ResultStatus.Success)
        {
            return r;
        }

        if (!_entityLocations.Remove(entity.ID, entity.Generation))
        {
            return ResultStatus.NotFound;
        }

        return ResultStatus.Success;
    }

    /// <summary>
    /// Check if the specified entity exists.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity exists, false otherwise.</returns>
    public bool Exists(Entity entity)
    {
        return _entityLocations.Contains(entity.ID, entity.Generation);
    }

    /// <summary>
    /// Create a singleton entity with the specified component.
    /// </summary>
    /// <param name="componentID">The component type ID of the singleton.</param>
    /// <param name="pComponent">Pointer to the component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ResultStatus CreateSingleton(Identifier<IComponent> componentID, void* pComponent)
    {
        if (pComponent == null)
        {
            return ResultStatus.InvalidArgument;
        }

        // Check if singleton already exists
        var signatureHash = ComponentRegister.GetHashCode(componentID);
        var arcID = _world.GetArchetypeIDBySignatureHash(signatureHash);

        if (arcID.IsValid)
        {
            return ResultStatus.InvalidArgument;
        }

        arcID = _world.CreateArchetype([componentID], signatureHash);

        ref var archetype = ref _world.GetArchetypeReference(arcID);
        archetype.AllocateEntity(out var chunkIndex, out var rowIndex);

        var id = _entityLocations.Add(new EntityLocation
        {
            archetypeID = arcID,
            chunkIndex = chunkIndex,
            rowIndex = rowIndex
        }, out var generation);

        var entity = new Entity(id, generation);
        archetype.SetEntity(chunkIndex, rowIndex, entity);
        archetype.SetComponentData(chunkIndex, rowIndex, componentID, pComponent);

        return ResultStatus.Success;
    }

    /// <summary>
    /// Create a singleton entity with the specified component.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="component">The component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ResultStatus CreateSingleton<T>(T component = default)
        where T : unmanaged, IComponent
    {
        return CreateSingleton(ComponentTypeID<T>.value, &component);
    }

    /// <summary>
    /// Get a pointer to the singleton component data.
    /// </summary>
    /// <param name="componentID">The component type ID of the singleton.</param>
    /// <returns>Pointer to the component data, or null if not found.</returns>
    public void* GetSingleton(Identifier<IComponent> componentID)
    {
        var signatureHash = ComponentRegister.GetHashCode(componentID);
        var arcID = _world.GetArchetypeIDBySignatureHash(signatureHash);

        if (arcID.IsNotValid)
        {
            return null;
        }

        ref var archetype = ref _world.GetArchetypeReference(arcID);
        var layoutResult = archetype.GetLayout(componentID);
        if (layoutResult.Status != ResultStatus.Success)
        {
            return null;
        }

        var chunk = archetype._chunks[0];
        var ptr = chunk.GetUnsafePtr() + layoutResult.Value.offset;

        return ptr;
    }

    /// <summary>
    /// Get a reference to the singleton component data.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>Reference to the component data. null ref if not found.</returns>
    public ref T GetSingleton<T>()
        where T : unmanaged, IComponent
    {
        var ptr = GetSingleton(ComponentTypeID<T>.value);
        return ref *(T*)ptr; // This will return null ref if ptr is null.
    }

    private static void CopyData(ref Archetype oldArch, int oldChunk, int oldRow,
                                 ref Archetype newArch, int newChunk, int newRow)
    {
        // Iterate every component type in the OLD archetype
        for (var i = 0; i < oldArch._layouts.Count; i++)
        {
            var layout = oldArch._layouts[i];

            var src = oldArch._chunks[oldChunk].GetUnsafePtr() + layout.offset + (layout.size * oldRow);
            var r = newArch.GetLayout(layout.componentID);
            Debug.Assert(r.Status == ResultStatus.Success); // This should always be true if the system is consistent.
            if (r.Status != ResultStatus.Success)
            {
                continue;
            }

            var dst = newArch._chunks[newChunk].GetUnsafePtr() + r.Value.offset + (layout.size * newRow);

            MemoryUtility.MemCpy(dst, src, (nuint)layout.size);
        }
    }

    /// <summary>
    /// Add a component to the specified entity.
    /// </summary>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="componentID">The component type ID to add.</param>
    /// <param name="pComponent">Pointer to the component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ResultStatus AddComponent(Entity entity, Identifier<IComponent> componentID, void* pComponent)
    {
        // Find current location
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return ResultStatus.NotFound;
        }

        // Build new archetype signature
        ref var oldArchetype = ref _world.GetArchetypeReference(location.archetypeID);
        var oldSignature = oldArchetype._signature;

        // TODO: Check edge cache first.
        var newArcID = oldArchetype.GetEdgeAdd(componentID);
        if (newArcID.IsNotValid)
        {
            var largestComponentID = Math.Max(oldSignature.Count, componentID);
            var length = UnsafeBitSet.RequiredLength(largestComponentID + 1);

            Span<uint> bits = stackalloc uint[length];
            bits.Clear();

            var newSignature = new SpanBitSet(bits);

            var oldIt = oldSignature.GetIterator();
            var compCount = 0;
            while (oldIt.Next(out var index))
            {
                newSignature.SetBit(index);
                compCount++;
            }

            compCount++;
            newSignature.SetBit(componentID);

            // Find or create new archetype
            var newSignatureHash = newSignature.GetHashCode();
            newArcID = _world.GetArchetypeIDBySignatureHash(newSignatureHash);
            if (newArcID.IsNotValid)
            {
                // Create new archetype
                Span<Identifier<IComponent>> componentTypeIDs = stackalloc Identifier<IComponent>[compCount];

                var newIt = newSignature.GetIterator();
                var i = 0;
                while (newIt.Next(out var index))
                {
                    componentTypeIDs[i++] = index;
                }

                newArcID = _world.CreateArchetype(componentTypeIDs, newSignatureHash);
            }

            oldArchetype.AddEdgeAdd(componentID, newArcID);
        }

        // Move entity data
        ref var newArchetype = ref _world.GetArchetypeReference(newArcID);
        newArchetype.AllocateEntity(out var newChunkIndex, out var newRowIndex);
        CopyData(ref oldArchetype, location.chunkIndex, location.rowIndex,
                 ref newArchetype, newChunkIndex, newRowIndex);

        newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);
        newArchetype.SetComponentData(newChunkIndex, newRowIndex, componentID, pComponent);

        var r = oldArchetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Debug.Assert(r == ResultStatus.Success); // We assert it because the entity should exist if the whole system is consistent.
        if (r != ResultStatus.Success)
        {
            return r;
        }

        // Update location
        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return ResultStatus.Success;
    }

    /// <summary>
    /// Add a component to the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ResultStatus AddComponent<T>(Entity entity, T component = default)
        where T : unmanaged, IComponent
    {
        return AddComponent(entity, ComponentTypeID<T>.value, &component);
    }

    /// <summary>
    /// Remove a component from the specified entity.
    /// </summary>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <param name="componentID">The component type ID to remove.</param>
    /// <returns>The result status of the operation.</returns>
    public ResultStatus RemoveComponent(Entity entity, Identifier<IComponent> componentID)
    {
        // Find current location
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return ResultStatus.NotFound;
        }

        // Build new archetype signature
        ref var oldArchetype = ref _world.GetArchetypeReference(location.archetypeID);
        var oldSignature = oldArchetype._signature;

        var newArcID = oldArchetype.GetEdgeRemove(componentID);
        if (newArcID.IsNotValid)
        {
            var largestComponentID = Math.Max(oldSignature.Count, componentID);
            var length = UnsafeBitSet.RequiredLength(largestComponentID + 1);

            Span<uint> bits = stackalloc uint[length];
            bits.Clear();

            var newSignature = new SpanBitSet(bits);

            var oldIt = oldSignature.GetIterator();
            var compCount = 0;
            while (oldIt.Next(out var index))
            {
                if (index != componentID)
                {
                    newSignature.SetBit(index);
                    compCount++;
                }
            }

            // Find or create new archetype
            var newSignatureHash = newSignature.GetHashCode();
            newArcID = _world.GetArchetypeIDBySignatureHash(newSignatureHash);
            if (newArcID.IsNotValid)
            {
                // Create new archetype
                Span<Identifier<IComponent>> componentTypeIDs = stackalloc Identifier<IComponent>[compCount];

                var newIt = newSignature.GetIterator();
                var i = 0;
                while (newIt.Next(out var index))
                {
                    componentTypeIDs[i++] = index;
                }

                newArcID = _world.CreateArchetype(componentTypeIDs, newSignatureHash);
            }

            oldArchetype.AddEdgeRemove(componentID, newArcID);
        }

        // Move entity data
        ref var newArchetype = ref _world.GetArchetypeReference(newArcID);
        newArchetype.AllocateEntity(out var newChunkIndex, out var newRowIndex);
        newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);

        var r = oldArchetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Debug.Assert(r == ResultStatus.Success); // We assert it because the entity should exist if the whole system is consistent.
        if (r != ResultStatus.Success)
        {
            return r;
        }

        // Update location
        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return ResultStatus.Success;
    }

    /// <summary>
    /// Remove a component from the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <returns>The result status of the operation.</returns>
    public ResultStatus RemoveComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        return RemoveComponent(entity, ComponentTypeID<T>.value);
    }

    /// <summary>
    /// Set the component data for the specified entity.
    /// </summary>
    /// <param name="entity">The entity to set the component data for.</param>
    /// <param name="componentID">The component type ID to set.</param>
    /// <param name="pComponent">Pointer to the component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ResultStatus SetComponent(Entity entity, Identifier<IComponent> componentID, void* pComponent)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return ResultStatus.NotFound;
        }

        ref var archetype = ref _world.GetArchetypeReference(location.archetypeID);
        archetype.SetComponentData(location.chunkIndex, location.rowIndex, componentID, pComponent);

        return ResultStatus.Success;
    }

    /// <summary>
    /// Set the component data for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to set the component data for.</param>
    /// <param name="component">The component data.</param>
    public ResultStatus SetComponent<T>(Entity entity, T component)
        where T : unmanaged, IComponent
    {
        return SetComponent(entity, ComponentTypeID<T>.value, &component);
    }

    /// <summary>
    /// Get a pointer to the component data for the specified entity.
    /// </summary>
    /// <param name="entity">The entity to get the component data for.</param>
    /// <param name="componentID">The component type ID to get.</param>
    /// <returns>Pointer to the component data, or null if not found.</returns>
    public void* GetComponent(Entity entity, Identifier<IComponent> componentID)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return null;
        }

        ref var archetype = ref _world.GetArchetypeReference(location.archetypeID);
        return archetype.GetComponentData(location.chunkIndex, location.rowIndex, componentID);
    }

    /// <summary>
    /// Get a reference to the component data for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to get the component data for.</param>
    /// <returns>Reference to the component data. null ref if not found.</returns>
    public ref T GetComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        var ptr = GetComponent(entity, ComponentTypeID<T>.value);
        return ref *(T*)ptr; // This will return null ref if ptr is null.
    }

    /// <summary>
    /// Check if the specified entity has the specified component.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="componentID">The component type ID to check.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    public bool HasComponent(Entity entity, Identifier<IComponent> componentID)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return false;
        }

        ref var archetype = ref _world.GetArchetypeReference(location.archetypeID);
        return archetype.HasComponent(componentID);
    }

    /// <summary>
    /// Check if the specified entity has the specified component.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    public bool HasComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        return HasComponent(entity, ComponentTypeID<T>.value);
    }

    /// <summary>
    /// Set the enabled state of an enableable component for the specified entity.
    /// </summary>
    /// <param name="entity">The entity to set the enabled state for.</param>
    /// <param name="componentID">The component type ID of the enableable component.</
    /// <param name="enabled">True to enable the component, false to disable it.</param>
    /// <returns>The result status of the operation.</returns>
    public ResultStatus SetEnabled(Entity entity, Identifier<IComponent> componentID, bool enabled)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return ResultStatus.NotFound;
        }

        ref var archetype = ref _world.GetArchetypeReference(location.archetypeID);
        var chunkIndex = location.chunkIndex;
        var rowIndex = location.rowIndex;

        var layoutResult = archetype.GetLayout(componentID);
        if (layoutResult.Status != ResultStatus.Success)
        {
            return layoutResult.Status;
        }

        ref var chunk = ref archetype.GetChunkReference(chunkIndex);
        var chunkBase = chunk.GetUnsafePtr();
        var maskBase = chunkBase + layoutResult.Value.enableBitsOffset;

        var byteIndex = rowIndex >> Chunk.BIT_SHIFT;
        var bitIndex = rowIndex & Chunk.BIT_ALIGNMENT_MINUS_ONE;

        if (enabled)
        {
            maskBase[byteIndex] |= (byte)(1 << bitIndex);
        }
        else
        {
            maskBase[byteIndex] &= (byte)~(1 << bitIndex);
        }

        return ResultStatus.Success;
    }

    /// <summary>
    /// Set the enabled state of an enableable component for the specified entity.
    /// </summary>
    /// <typeparam name="T">The enableable component type.</typeparam>
    /// <param name="entity">The entity to set the enabled state for.</param>
    /// <param name="enabled">True to enable the component, false to disable it.</
    /// <returns>The result status of the operation.</returns>
    public ResultStatus SetEnabled<T>(Entity entity, bool enabled)
        where T : unmanaged, IEnableableComponent
    {
        return SetEnabled(entity, ComponentTypeID<T>.value, enabled);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _entityLocations.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
