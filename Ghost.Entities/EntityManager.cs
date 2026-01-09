using Ghost.Core;
using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;

namespace Ghost.Entities;

internal struct EntityLocation : IComparable<EntityLocation>
{
    public int archetypeID;
    public int chunkIndex;
    public int rowIndex;

    public readonly int CompareTo(EntityLocation other)
    {
        var archComp = chunkIndex.CompareTo(other.chunkIndex);
        if (archComp != 0)
        {
            return archComp;
        }

        var chunkComp = chunkIndex.CompareTo(other.chunkIndex);
        if (chunkComp != 0)
        {
            return chunkComp;
        }

        return rowIndex.CompareTo(other.rowIndex);
    }
}

/// <summary>
/// A manager for creating, destroying, and managing entities and their components.
/// </summary>
/// <remarks>
/// All methods in this class are not thread-safe and all of them will cause structural changes if not mentioned otherwise.
/// Use <see cref="EntityCommandBuffer"/> to defer structural changes to a safe point.
/// Use <see cref="World.GetThreadLocalEntityCommandBuffer(int)"/> to get a thread-local command buffer for multithreaded scenarios.
/// </remarks>
public unsafe partial class EntityManager : IDisposable
{
    private readonly World _world;
    private UnsafeSlotMap<EntityLocation> _entityLocations;
    private bool _disposed;

    public World World => _world;

    internal EntityManager(World world, int initialCapacity)
    {
        _world = world;
        _entityLocations = new UnsafeSlotMap<EntityLocation>(initialCapacity, Allocator.Persistent, AllocationOption.Clear);
        _scriptComponents = new SlotMap<List<ScriptComponent>>(initialCapacity / 2);
        // _storages = new IManagedComponentStorage[16];
    }

    ~EntityManager()
    {
        Dispose();
    }

    internal ErrorStatus UpdateEntityLocation(Entity entity, Identifier<Archetype> newArchetypeID, int newChunkIndex, int newRowIndex)
    {
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return ErrorStatus.NotFound;
        }

        location.archetypeID = newArchetypeID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return ErrorStatus.None;
    }

    internal Result<EntityLocation, ErrorStatus> GetEntityLocation(Entity entity)
    {
        if (_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return location;
        }

        return ErrorStatus.NotFound;
    }

    /// <summary>
    /// Create an entity with no components.
    /// </summary>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity()
    {
        var entities = (Span<Entity>)stackalloc Entity[1];
        CreateEntities(entities);

        return entities[0];
    }

    /// <summary>
    /// Create an entity with specified components.
    /// </summary>
    /// <param name="set">A set of component space IDs to add to the entities.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity(ComponentSet set)
    {
        var entities = (Span<Entity>)stackalloc Entity[1];
        CreateEntities(entities, set);

        return entities[0];
    }

    /// <summary>
    /// Create multiple entities with no components.
    /// </summary>
    /// <param name="entities">The span to store the created entities.</param>
    public void CreateEntities(Span<Entity> entities)
    {
        ref var emptyArchetype = ref _world.ComponentManager.GetArchetypeReference(World.EmptyArchetypeID);
        emptyArchetype.AllocateEntity(out var chunkIndex, out var rowIndex);

        for (var i = 0; i < entities.Length; i++)
        {
            var id = _entityLocations.Add(new EntityLocation
            {
                archetypeID = World.EmptyArchetypeID,
                chunkIndex = chunkIndex,
                rowIndex = rowIndex
            }, out var generation);

            var entity = new Entity(id, generation);
            emptyArchetype.SetEntity(chunkIndex, rowIndex, entity);

            entities[i] = entity;
        }
    }

    /// <summary>
    /// Create multiple entities with no components.
    /// </summary>
    /// <param name="count">The number of entities to create.</param>
    public void CreateEntities(int count)
    {
        ref var emptyArchetype = ref _world.ComponentManager.GetArchetypeReference(World.EmptyArchetypeID);
        emptyArchetype.AllocateEntity(out var chunkIndex, out var rowIndex);

        for (var i = 0; i < count; i++)
        {
            var id = _entityLocations.Add(new EntityLocation
            {
                archetypeID = World.EmptyArchetypeID,
                chunkIndex = chunkIndex,
                rowIndex = rowIndex
            }, out var generation);

            var entity = new Entity(id, generation);
            emptyArchetype.SetEntity(chunkIndex, rowIndex, entity);
        }
    }

    /// <summary>
    /// Create multiple entities with specified components.
    /// </summary>
    /// <param name="entities">The span to store the created entities.</param>
    /// <param name="set">A set of component space IDs to add to the entities.</param>
    /// <returns>An array of the created entities.</returns>
    public void CreateEntities(Span<Entity> entities, ComponentSet set)
    {
        var hash = set.GetHashCode();
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(hash);

        if (arcID.IsInvalid)
        {
            arcID = _world.ComponentManager.CreateArchetype(set.Components, hash);
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(arcID);

        for (var i = 0; i < entities.Length; i++)
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
    }

    /// <summary>
    /// Create multiple entities with specified components.
    /// </summary>
    /// <param name="count">The number of entities to create.</param>
    /// <param name="set">A set of component space IDs to add to the entities.</param>
    public void CreateEntities(int count, ComponentSet set)
    {
        var hash = set.GetHashCode();
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(hash);

        if (arcID.IsInvalid)
        {
            arcID = _world.ComponentManager.CreateArchetype(set.Components, hash);
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(arcID);

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

    private void DestoryManagedEntityIfExists(ref readonly Archetype archetype, EntityLocation location)
    {
        var pManagedRef = archetype.GetComponentData(location.chunkIndex, location.rowIndex, ComponentTypeID<ManagedEntityRef>.Value);
        if (pManagedRef != null)
        {
            DestroyManagedEntity(((ManagedEntityRef*)pManagedRef)->entity);
        }
    }

    /// <summary>
    /// Destroy the specified entity.
    /// </summary>
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus DestroyEntity(Entity entity)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return ErrorStatus.NotFound;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);

        DestoryManagedEntityIfExists(in archetype, location);
        var r = archetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        if (r != ErrorStatus.None)
        {
            return r;
        }

        if (!_entityLocations.Remove(entity.ID, entity.Generation))
        {
            return ErrorStatus.NotFound;
        }

        return ErrorStatus.None;
    }

    /// <summary>
    /// Destroy the specified entities.
    /// </summary>
    /// <param name="entities">The entities to destroy.</param>
    public void DestroyEntities(ReadOnlySpan<Entity> entities)
    {
        void RemoveManagedEntity(ReadOnlySpan<int> rowIndicesCache, ref readonly Archetype archetype, int chunkIndex)
        {
            for (var j = 0; j < rowIndicesCache.Length; j++)
            {
                var rowIndex = rowIndicesCache[j];
                var location = new EntityLocation
                {
                    archetypeID = archetype.ID,
                    chunkIndex = chunkIndex,
                    rowIndex = rowIndex
                };

                DestoryManagedEntityIfExists(in archetype, location);
            }
        }

        if (entities.Length == 0)
        {
            return;
        }

        using var scope = AllocationManager.CreateStackScope();
        var batchDestroy = new UnsafeList<EntityLocation>(entities.Length, scope.AllocationHandle);
        var rowIndicesCache = new UnsafeList<int>(32, scope.AllocationHandle);

        // 1. GATHER
        // Resolve all entities to their locations
        for (var i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
            {
                batchDestroy.Add(location);
            }
        }

        if (batchDestroy.Count == 0)
        {
            return;
        }

        // 2. SORT
        // Sorting groups them by chunk automatically
        batchDestroy.AsSpan().Sort();

        // 3. SWEEP
        // Iterate through the sorted list and batch process each chunk
        var firstLoc = batchDestroy[0];
        var prevArchetypeID = firstLoc.archetypeID;
        var prevChunkIndex = firstLoc.chunkIndex;

        for (var i = 0; i < batchDestroy.Count; i++)
        {
            var loc = batchDestroy[i];

            // Check if we have crossed a boundary (Different Chunk OR Different Archetype)
            var isNewBatch = (loc.chunkIndex != prevChunkIndex) || (loc.archetypeID != prevArchetypeID);

            if (isNewBatch)
            {
                // FLUSH PREVIOUS BATCH
                // We must retrieve the Archetype of the *Previous* batch, not the current 'loc'
                ref var prevArchetype = ref _world.ComponentManager.GetArchetypeReference(prevArchetypeID);

                // Remove Managed Entities first
                RemoveManagedEntity(rowIndicesCache.AsSpan(), in prevArchetype, prevChunkIndex);

                // Execute the hole-filling/swap logic
                prevArchetype.RemoveEntities(prevChunkIndex, rowIndicesCache.AsSpan());

                // RESET
                rowIndicesCache.Clear();
                prevArchetypeID = loc.archetypeID;
                prevChunkIndex = loc.chunkIndex;
            }

            rowIndicesCache.Add(loc.rowIndex);
        }

        // 4. FINAL FLUSH
        // Process the stragglers remaining in the cache
        if (rowIndicesCache.Count > 0)
        {
            ref var lastArchetype = ref _world.ComponentManager.GetArchetypeReference(prevArchetypeID);

            RemoveManagedEntity(rowIndicesCache.AsSpan(), in lastArchetype, prevChunkIndex);
            lastArchetype.RemoveEntities(prevChunkIndex, rowIndicesCache.AsSpan());
        }

        // 5. Remove from Entity Locations
        for (var i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            _entityLocations.Remove(entity.ID, entity.Generation);
        }
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
    /// <param name="componentID">The component space ID of the singleton.</param>
    /// <param name="pComponent">Pointer to the component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus CreateSingleton(Identifier<IComponent> componentID, void* pComponent)
    {
        if (pComponent == null)
        {
            return ErrorStatus.InvalidArgument;
        }

        // Check if singleton already exists
        var signatureHash = ComponentRegistry.GetHashCode(componentID);
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(signatureHash);

        if (arcID.IsValid)
        {
            return ErrorStatus.InvalidArgument;
        }

        arcID = _world.ComponentManager.CreateArchetype([componentID], signatureHash);

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(arcID);
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

        return ErrorStatus.None;
    }

    /// <summary>
    /// Create a singleton entity with the specified component.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="component">The component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus CreateSingleton<T>(T component = default)
        where T : unmanaged, IComponent
    {
        return CreateSingleton(ComponentTypeID<T>.Value, &component);
    }

    /// <summary>
    /// Get a pointer to the singleton component data.
    /// </summary>
    /// <param name="componentID">The component space ID of the singleton.</param>
    /// <returns>Pointer to the component data, or null if not found.</returns>
    public void* GetSingleton(Identifier<IComponent> componentID)
    {
        var signatureHash = ComponentRegistry.GetHashCode(componentID);
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(signatureHash);

        if (arcID.IsInvalid)
        {
            return null;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(arcID);
        var layoutResult = archetype.GetLayout(componentID);
        if (layoutResult.Error != ErrorStatus.None)
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
    /// <typeparam name="T">The component space.</typeparam>
    /// <returns>Reference to the component data. null ref if not found.</returns>
    public ref T GetSingleton<T>()
        where T : unmanaged, IComponent
    {
        var ptr = GetSingleton(ComponentTypeID<T>.Value);
        return ref *(T*)ptr; // This will return null ref if ptr is null.
    }

    private static void CopyData(ref Archetype oldArch, int oldChunk, int oldRow,
                                 ref Archetype newArch, int newChunk, int newRow)
    {
        // Iterate every component space in the OLD archetype
        for (var i = 0; i < oldArch._layouts.Count; i++)
        {
            var layout = oldArch._layouts[i];

            var src = oldArch._chunks[oldChunk].GetUnsafePtr() + layout.offset + (layout.size * oldRow);
            var r = newArch.GetLayout(layout.componentID);
            if (r.Error != ErrorStatus.None)
            {
                // New archetype does not have this component, skip it.
                // This can happen when removing components.
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
    /// <param name="componentID">The component space ID to add.</param>
    /// <param name="pComponent">Pointer to the component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus AddComponent(Entity entity, Identifier<IComponent> componentID, void* pComponent)
    {
        // Find current location
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return ErrorStatus.NotFound;
        }

        // Build new archetype signature
        ref var oldArchetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        var oldSignature = oldArchetype._signature;

        if (oldSignature.IsSet(componentID))
        {
            // Component already exists
            return ErrorStatus.InvalidArgument;
        }

        var newArcID = oldArchetype.GetEdgeAdd(componentID);
        if (newArcID.IsInvalid)
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
            newArcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(newSignatureHash);
            if (newArcID.IsInvalid)
            {
                // Create new archetype
                Span<Identifier<IComponent>> componentTypeIDs = stackalloc Identifier<IComponent>[compCount];

                var newIt = newSignature.GetIterator();
                var i = 0;
                while (newIt.Next(out var index))
                {
                    componentTypeIDs[i++] = index;
                }

                newArcID = _world.ComponentManager.CreateArchetype(componentTypeIDs, newSignatureHash);
            }

            oldArchetype.AddEdgeAdd(componentID, newArcID);
        }

        // Move entity data
        ref var newArchetype = ref _world.ComponentManager.GetArchetypeReference(newArcID);
        newArchetype.AllocateEntity(out var newChunkIndex, out var newRowIndex);
        CopyData(ref oldArchetype, location.chunkIndex, location.rowIndex,
                 ref newArchetype, newChunkIndex, newRowIndex);

        newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);
        newArchetype.SetComponentData(newChunkIndex, newRowIndex, componentID, pComponent);

        var r = oldArchetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Debug.Assert(r == ErrorStatus.None); // We assert it because the entity should exist if the whole system is consistent.
        if (r != ErrorStatus.None)
        {
            return r;
        }

        // Update location
        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return ErrorStatus.None;
    }

    /// <summary>
    /// Add a component to the specified entity.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus AddComponent<T>(Entity entity, T component = default)
        where T : unmanaged, IComponent
    {
        return AddComponent(entity, ComponentTypeID<T>.Value, &component);
    }

    /// <summary>
    /// Remove a component from the specified entity.
    /// </summary>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <param name="componentID">The component space ID to remove.</param>
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus RemoveComponent(Entity entity, Identifier<IComponent> componentID)
    {
        // Find current location
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return ErrorStatus.NotFound;
        }

        // Build new archetype signature
        ref var oldArchetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        var oldSignature = oldArchetype._signature;

        var newArcID = oldArchetype.GetEdgeRemove(componentID);
        if (newArcID.IsInvalid)
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
            newArcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(newSignatureHash);
            if (newArcID.IsInvalid)
            {
                // Create new archetype
                Span<Identifier<IComponent>> componentTypeIDs = stackalloc Identifier<IComponent>[compCount];

                var newIt = newSignature.GetIterator();
                var i = 0;
                while (newIt.Next(out var index))
                {
                    componentTypeIDs[i++] = index;
                }

                newArcID = _world.ComponentManager.CreateArchetype(componentTypeIDs, newSignatureHash);
            }

            oldArchetype.AddEdgeRemove(componentID, newArcID);
        }

        // Move entity data
        ref var newArchetype = ref _world.ComponentManager.GetArchetypeReference(newArcID);
        newArchetype.AllocateEntity(out var newChunkIndex, out var newRowIndex);
        CopyData(ref oldArchetype, location.chunkIndex, location.rowIndex,
                 ref newArchetype, newChunkIndex, newRowIndex);

        newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);

        var r = oldArchetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Debug.Assert(r == ErrorStatus.None); // We assert it because the entity should exist if the whole system is consistent.
        if (r != ErrorStatus.None)
        {
            return r;
        }

        var pManagedRef = oldArchetype.GetComponentData(location.chunkIndex, location.rowIndex, ComponentTypeID<ManagedEntityRef>.Value);
        if (pManagedRef != null)
        {
            DestroyManagedEntity(((ManagedEntityRef*)pManagedRef)->entity);
        }

        // Update location
        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return ErrorStatus.None;
    }

    /// <summary>
    /// Remove a component from the specified entity.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus RemoveComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        return RemoveComponent(entity, ComponentTypeID<T>.Value);
    }

    /// <summary>
    /// Set the component data for the specified entity.
    /// </summary>
    /// <param name="entity">The entity to set the component data for.</param>
    /// <param name="componentID">The component space ID to set.</param>
    /// <param name="pComponent">Pointer to the component data.</param>
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus SetComponent(Entity entity, Identifier<IComponent> componentID, void* pComponent)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return ErrorStatus.NotFound;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        archetype.SetComponentData(location.chunkIndex, location.rowIndex, componentID, pComponent);

        return ErrorStatus.None;
    }

    /// <summary>
    /// Set the component data for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="entity">The entity to set the component data for.</param>
    /// <param name="component">The component data.</param>
    public ErrorStatus SetComponent<T>(Entity entity, T component)
        where T : unmanaged, IComponent
    {
        return SetComponent(entity, ComponentTypeID<T>.Value, &component);
    }

    /// <summary>
    /// Get a pointer to the component data for the specified entity.
    /// </summary>
    /// <param name="entity">The entity to get the component data for.</param>
    /// <param name="componentID">The component space ID to get.</param>
    /// <returns>Pointer to the component data, or null if not found.</returns>
    public void* GetComponent(Entity entity, Identifier<IComponent> componentID)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return null;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        return archetype.GetComponentData(location.chunkIndex, location.rowIndex, componentID);
    }

    /// <summary>
    /// Get a reference to the component data for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="entity">The entity to get the component data for.</param>
    /// <returns>Reference to the component data. null ref if not found.</returns>
    public ref T GetComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        var ptr = GetComponent(entity, ComponentTypeID<T>.Value);
        return ref *(T*)ptr; // This will return null ref if ptr is null.
    }

    /// <summary>
    /// Check if the specified entity has the specified component.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <param name="componentID">The component space ID to check.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    public bool HasComponent(Entity entity, Identifier<IComponent> componentID)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return false;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        return archetype.HasComponent(componentID);
    }

    /// <summary>
    /// Check if the specified entity has the specified component.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    public bool HasComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        return HasComponent(entity, ComponentTypeID<T>.Value);
    }

    /// <summary>
    /// Set the enabled state of an enableable component for the specified entity.
    /// </summary>
    /// <param name="entity">The entity to set the enabled state for.</param>
    /// <param name="componentID">The component space ID of the enableable component.</
    /// <param name="enabled">True to enable the component, false to disable it.</param>
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus SetEnabled(Entity entity, Identifier<IComponent> componentID, bool enabled)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return ErrorStatus.NotFound;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        var chunkIndex = location.chunkIndex;
        var rowIndex = location.rowIndex;

        var layoutResult = archetype.GetLayout(componentID);
        if (layoutResult.Error != ErrorStatus.None)
        {
            return layoutResult.Error;
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

        return ErrorStatus.None;
    }

    /// <summary>
    /// Set the enabled state of an enableable component for the specified entity.
    /// </summary>
    /// <typeparam name="T">The enableable component space.</typeparam>
    /// <param name="entity">The entity to set the enabled state for.</param>
    /// <param name="enabled">True to enable the component, false to disable it.</
    /// <returns>The result status of the operation.</returns>
    public ErrorStatus SetEnabled<T>(Entity entity, bool enabled)
        where T : unmanaged, IEnableableComponent
    {
        return SetEnabled(entity, ComponentTypeID<T>.Value, enabled);
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
