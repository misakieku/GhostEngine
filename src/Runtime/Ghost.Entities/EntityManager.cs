using Ghost.Core;
using Misaki.HighPerformance.Collections;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Runtime.CompilerServices;

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
    public int EntityCount => _entityLocations.Count;

    internal EntityManager(World world, int initialCapacity)
    {
        _world = world;
        _entityLocations = new UnsafeSlotMap<EntityLocation>(initialCapacity, AllocationHandle.Persistent, AllocationOption.Clear);
    }

    ~EntityManager()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Error UpdateEntityLocation(Entity entity, Identifier<Archetype> newArchetypeID, int newChunkIndex, int newRowIndex)
    {
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        location.archetypeID = newArchetypeID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return Error.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Result<EntityLocation, Error> GetEntityLocation(Entity entity)
    {
        if (_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return location;
        }

        return Error.NotFound;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Clear()
    {
        _entityLocations.Clear();
    }

    /// <summary>
    /// Get or compute the cleanup archetype for <paramref name="archetype"/>.
    /// The cleanup archetype contains only <see cref="ICleanupComponent"/> components,
    /// so they can get a final tick before the entity is fully destroyed.
    /// </summary>
    private Identifier<Archetype> GetOrCreateCleanupArchetype(ref Archetype archetype)
    {
        if (archetype._cleanupEdge >= 0)
        {
            return archetype._cleanupEdge;
        }

        ref var signature = ref archetype._signature;

        using var scope = AllocationManager.CreateStackScope();
        using var newSignature = new UnsafeBitSet(signature.Count, scope.AllocationHandle);

        var compCount = 0;
        var it = signature.GetIterator();
        while (it.Next(out var componentID))
        {
            if (ComponentRegistry.GetComponentInfo(componentID).isCleanup)
            {
                newSignature.SetBit(componentID);
                compCount++;
            }
        }

        var newSignatureHash = newSignature.GetHashCode();
        var newArcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(newSignatureHash);
        if (newArcID.IsInvalid)
        {
            Span<Identifier<IComponent>> componentTypeIDs = stackalloc Identifier<IComponent>[compCount];

            var newIt = newSignature.GetIterator();
            var i = 0;
            while (newIt.Next(out var cid))
            {
                componentTypeIDs[i++] = cid;
            }

            newArcID = _world.ComponentManager.CreateArchetype(componentTypeIDs, newSignatureHash);
        }

        archetype._cleanupEdge = newArcID;
        return newArcID;
    }

    /// <summary>
    /// Look up or create an archetype from a <see cref="SpanBitSet"/> signature.
    /// </summary>
    private Identifier<Archetype> FindOrCreateArchetype(ref readonly SpanBitSet signature, int componentCount)
    {
        var hash = signature.GetHashCode();
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(hash);
        if (arcID.IsInvalid)
        {
            Span<Identifier<IComponent>> componentTypeIDs = stackalloc Identifier<IComponent>[componentCount];

            var it = signature.GetIterator();
            var i = 0;
            while (it.Next(out var cid))
            {
                componentTypeIDs[i++] = cid;
            }

            arcID = _world.ComponentManager.CreateArchetype(componentTypeIDs, hash);
        }

        return arcID;
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
            if (r.Error != Error.None)
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
    /// Create an entity with no components.
    /// </summary>
    /// <returns>The created entity.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity CreateEntity(ComponentSetView set)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CreateEntities(int count)
    {
        ref var emptyArchetype = ref _world.ComponentManager.GetArchetypeReference(World.EmptyArchetypeID);

        var chunkIndices = (Span<int>)stackalloc int[count];
        var rowIndices = (Span<int>)stackalloc int[count];
        emptyArchetype.AllocateEntities(chunkIndices, rowIndices);

        for (var i = 0; i < count; i++)
        {
            var id = _entityLocations.Add(new EntityLocation
            {
                archetypeID = World.EmptyArchetypeID,
                chunkIndex = chunkIndices[i],
                rowIndex = rowIndices[i]
            }, out var generation);

            var entity = new Entity(id, generation);
            emptyArchetype.SetEntity(chunkIndices[i], rowIndices[i], entity);
        }
    }

    /// <summary>
    /// Create multiple entities with specified components.
    /// </summary>
    /// <param name="entities">The span to store the created entities.</param>
    /// <param name="set">A set of component space IDs to add to the entities.</param>
    /// <returns>An array of the created entities.</returns>
    public void CreateEntities(Span<Entity> entities, ComponentSetView set)
    {
        var hash = set.ComponentHashCode;
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(hash);

        if (arcID.IsInvalid)
        {
            arcID = _world.ComponentManager.CreateArchetype(set.Components, hash);
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(arcID);

        for (var i = 0; i < entities.Length; i++)
        {
            archetype.AllocateEntity(set.SharedComponentData, set.SharedDataHashCode, out var chunkIndex, out var rowIndex);

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
    public void CreateEntities(int count, ComponentSetView set)
    {
        var hash = set.ComponentHashCode;
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(hash);

        if (arcID.IsInvalid)
        {
            arcID = _world.ComponentManager.CreateArchetype(set.Components, hash);
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(arcID);

        for (var i = 0; i < count; i++)
        {
            archetype.AllocateEntity(set.SharedComponentData, set.SharedDataHashCode, out var chunkIndex, out var rowIndex);

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

    private Error DestroyEntity_Internal(Entity entity, EntityLocation location)
    {
        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);

        // DestoryManagedEntityIfExists(in archetype, location);
        var r = archetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        if (r != Error.None)
        {
            return r;
        }

        if (!_entityLocations.Remove(entity.ID, entity.Generation))
        {
            return Error.NotFound;
        }

        return Error.None;
    }

    /// <summary>
    /// Destroy the specified entity.
    /// </summary>
    /// <returns>The result status of the operation.</returns>
    public Error DestroyEntity(Entity entity)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return Error.NotFound;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);

        // 0 means no cleanup component (since 0 is the empty archetype), -1 means haven't computed yet, positive value means the archetype id of the cleanup edge.
        if (archetype._cleanupEdge == 0)
        {
            return DestroyEntity_Internal(entity, location);
        }
        else
        {
            var newArcID = GetOrCreateCleanupArchetype(ref archetype);

            ref var newArchetype = ref _world.ComponentManager.GetArchetypeReference(newArcID);
            newArchetype.AllocateEntity(out var newChunkIndex, out var newRowIndex);
            CopyData(ref archetype, location.chunkIndex, location.rowIndex,
                    ref newArchetype, newChunkIndex, newRowIndex);

            newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);
        }

        return Error.None;
    }

    /// <summary>
    /// Destroy the specified entities.
    /// </summary>
    /// <param name="entities">The entities to destroy.</param>
    public void DestroyEntities(params ReadOnlySpan<Entity> entities)
    {
        if (entities.Length == 0)
        {
            return;
        }

        using var scope = AllocationManager.CreateStackScope();
        using var batchDestroy = new UnsafeList<EntityLocation>(entities.Length, scope.AllocationHandle);
        using var rowIndicesCache = new UnsafeList<int>(32, scope.AllocationHandle);

        Span<bool> cleanupMigrated = stackalloc bool[entities.Length];
        cleanupMigrated.Clear();

        // 1. GATHER
        // Resolve all entities to their locations.
        // Entities with ICleanupComponent are handled immediately — moved to a cleanup-only archetype
        // where they survive until cleanup systems finish processing them.
        for (var i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
            {
                ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);

                // 0 means no cleanup component (empty archetype or no ICleanupComponent types).
                if (archetype._cleanupEdge == 0)
                {
                    batchDestroy.Add(location);
                }
                else
                {
                    // Archetype has ICleanupComponent — move entity to cleanup archetype.
                    var newArcID = GetOrCreateCleanupArchetype(ref archetype);

                    ref var newArchetype = ref _world.ComponentManager.GetArchetypeReference(newArcID);
                    newArchetype.AllocateEntity(out var newChunkIndex, out var newRowIndex);
                    CopyData(ref archetype, location.chunkIndex, location.rowIndex,
                            ref newArchetype, newChunkIndex, newRowIndex);

                    newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);

                    // Remove from old archetype.
                    archetype.RemoveEntity(location.chunkIndex, location.rowIndex);

                    // Update entity location to point to the cleanup archetype.
                    UpdateEntityLocation(entity, newArcID, newChunkIndex, newRowIndex);

                    // Mark as cleanup-migrated — entity survives in cleanup archetype, do NOT remove from _entityLocations.
                    cleanupMigrated[i] = true;
                }
            }
        }

        if (batchDestroy.Count == 0)
        {
            return;
        }

        // 2. BATCH DESTROY
        // Sorting groups them by chunk automatically.
        batchDestroy.AsSpan().Sort();

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
                ref var prevArchetype = ref _world.ComponentManager.GetArchetypeReference(prevArchetypeID);

                // Remove Managed Entities first
                // RemoveManagedEntity(rowIndicesCache.AsSpan(), in prevArchetype, prevChunkIndex);

                // Execute the hole-filling/swap logic
                prevArchetype.RemoveEntities(prevChunkIndex, rowIndicesCache.AsSpan());

                rowIndicesCache.Clear();
                prevArchetypeID = loc.archetypeID;
                prevChunkIndex = loc.chunkIndex;
            }

            rowIndicesCache.Add(loc.rowIndex);
        }

        // Process the stragglers remaining in the cache
        if (rowIndicesCache.Count > 0)
        {
            ref var lastArchetype = ref _world.ComponentManager.GetArchetypeReference(prevArchetypeID);

            // RemoveManagedEntity(rowIndicesCache.AsSpan(), in lastArchetype, prevChunkIndex);
            lastArchetype.RemoveEntities(prevChunkIndex, rowIndicesCache.AsSpan());
        }

        // 3. Remove from Entity Locations — skip cleanup-migrated entities.
        for (var i = 0; i < entities.Length; i++)
        {
            if (cleanupMigrated[i])
            {
                continue;
            }

            var entity = entities[i];
            _entityLocations.Remove(entity.ID, entity.Generation);
        }
    }

    /// <summary>
    /// Check if the specified entity exists.
    /// </summary>
    /// <param name="entity">The entity to check.</param>
    /// <returns>True if the entity exists, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    public Error CreateSingleton(Identifier<IComponent> componentID, void* pComponent)
    {
        if (pComponent == null)
        {
            return Error.InvalidArgument;
        }

        // Check if singleton already exists
        var signatureHash = ComponentRegistry.GetHashCodeForTypeIDs(componentID);
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(signatureHash);

        if (arcID.IsValid)
        {
            return Error.InvalidArgument;
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

        return Error.None;
    }

    /// <summary>
    /// Create a singleton entity with the specified component.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="component">The component data.</param>
    /// <returns>The result status of the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error CreateSingleton<T>(T component = default)
        where T : unmanaged, IComponentData
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
        var signatureHash = ComponentRegistry.GetHashCodeForTypeIDs(componentID);
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(signatureHash);

        if (arcID.IsInvalid)
        {
            return null;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(arcID);
        var layoutResult = archetype.GetLayout(componentID);
        if (layoutResult.Error != Error.None)
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetSingleton<T>()
        where T : unmanaged, IComponentData
    {
        var ptr = GetSingleton(ComponentTypeID<T>.Value);
        return ref *(T*)ptr; // This will return null ref if ptr is null.
    }

    private static void BuildSharedData(
        ref Archetype oldArch, int oldGroupIndex,
        ref Archetype newArch,
        Identifier<IComponent> changedID, void* pNewData,   // non-null = adding shared, null = removing or non-shared change
        Span<byte> outSharedData)
    {
        if (newArch._sharedLayouts.Count == 0)
        {
            return;
        }

        var oldShared = oldArch._chunkGroups.Count > 0 && oldArch._chunkGroups[oldGroupIndex].sharedData.IsCreated
            ? oldArch._chunkGroups[oldGroupIndex].sharedData.AsSpan()
            : ReadOnlySpan<byte>.Empty;

        for (var i = 0; i < newArch._sharedLayouts.Count; i++)
        {
            ref var newLayout = ref newArch._sharedLayouts[i];

            if (newLayout.componentID == changedID.Value && pNewData != null)
            {
                // Adding this shared component — write the provided value.
                new ReadOnlySpan<byte>(pNewData, newLayout.size)
                    .CopyTo(outSharedData.Slice(newLayout.offset, newLayout.size));
            }
            else
            {
                // Carry over from old archetype's shared data (skip if old doesn't have it).
                var oldLayoutResult = oldArch.GetSharedLayout(newLayout.componentID);
                if (oldLayoutResult.IsSuccess)
                {
                    oldShared.Slice(oldLayoutResult.Value.offset, oldLayoutResult.Value.size)
                        .CopyTo(outSharedData.Slice(newLayout.offset, newLayout.size));
                }
            }
        }
    }

    /// <summary>
    /// Add a component to the specified entity.
    /// </summary>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="componentID">The component space ID to add.</param>
    /// <param name="pComponent">Pointer to the component data.</param>
    /// <returns>The result status of the operation.</returns>
    public Error AddComponent(Entity entity, Identifier<IComponent> componentID, void* pComponent)
    {
#if GHOST_SAFETY_CHECKS
        if (ComponentRegistry.GetComponentInfo(componentID).isShared)
        {
            return Error.InvalidArgument;
        }
#endif

        // Find current location
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        // Build new archetype signature
        ref var oldArchetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        var oldSignature = oldArchetype._signature;

        if (oldSignature.IsSet(componentID))
        {
            // Component already exists
            return Error.InvalidArgument;
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
            newArcID = FindOrCreateArchetype(ref newSignature, compCount);

            oldArchetype.AddEdgeAdd(componentID, newArcID);
        }

        // Move entity data
        ref var newArchetype = ref _world.ComponentManager.GetArchetypeReference(newArcID);

        // Carry existing shared values into the new archetype unchanged.
        int newChunkIndex, newRowIndex;
        if (oldArchetype._sharedLayouts.Count > 0)
        {
            Span<byte> newSharedData = stackalloc byte[newArchetype._sharedDataSize];
            ref var oldChunk = ref oldArchetype.GetChunkReference(location.chunkIndex);
            BuildSharedData(ref oldArchetype, oldChunk._groupIndex,
                            ref newArchetype, default, null, newSharedData);

            var sharedHash = ComponentRegistry.GetHashCodeForSharedData(newSharedData);
            newArchetype.AllocateEntity(newSharedData, sharedHash, out newChunkIndex, out newRowIndex);
        }
        else
        {
            newArchetype.AllocateEntity(out newChunkIndex, out newRowIndex);
        }

        CopyData(ref oldArchetype, location.chunkIndex, location.rowIndex,
                 ref newArchetype, newChunkIndex, newRowIndex);

        newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);
        newArchetype.SetComponentData(newChunkIndex, newRowIndex, componentID, pComponent);

        var r = oldArchetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Logger.DebugAssert(r == Error.None); // We assert it because the entity should exist if the whole system is consistent.
        if (r != Error.None)
        {
            return r;
        }

        // Update location
        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return Error.None;
    }

    /// <summary>
    /// Add a component to the specified entity.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component data.</param>
    /// <returns>The result status of the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error AddComponent<T>(Entity entity, T component = default)
        where T : unmanaged, IComponentData
    {
        return AddComponent(entity, ComponentTypeID<T>.Value, &component);
    }

    /// <summary>
    /// Remove a component from the specified entity.
    /// </summary>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <param name="componentID">The component space ID to remove.</param>
    /// <returns>The result status of the operation.</returns>
    public Error RemoveComponent(Entity entity, Identifier<IComponent> componentID)
    {
#if GHOST_SAFETY_CHECKS
        if (ComponentRegistry.GetComponentInfo(componentID).isShared)
        {
            return Error.InvalidArgument;
        }
#endif

        // Find current location
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
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

            if (compCount == 0)
            {
                // If there is no component left, we destroy the entity directly.
                return DestroyEntity_Internal(entity, location);
            }

            // Find or create new archetype
            newArcID = FindOrCreateArchetype(ref newSignature, compCount);

            oldArchetype.AddEdgeRemove(componentID, newArcID);
        }

        // Move entity data
        ref var newArchetype = ref _world.ComponentManager.GetArchetypeReference(newArcID);

        // Carry existing shared values into the new archetype unchanged.
        int newChunkIndex, newRowIndex;
        if (oldArchetype._sharedLayouts.Count > 0)
        {
            Span<byte> newSharedData = stackalloc byte[newArchetype._sharedDataSize];
            ref var oldChunk = ref oldArchetype.GetChunkReference(location.chunkIndex);
            BuildSharedData(ref oldArchetype, oldChunk._groupIndex,
                            ref newArchetype, default, null, newSharedData);

            var sharedHash = ComponentRegistry.GetHashCodeForSharedData(newSharedData);
            newArchetype.AllocateEntity(newSharedData, sharedHash, out newChunkIndex, out newRowIndex);
        }
        else
        {
            newArchetype.AllocateEntity(out newChunkIndex, out newRowIndex);
        }

        CopyData(ref oldArchetype, location.chunkIndex, location.rowIndex,
                 ref newArchetype, newChunkIndex, newRowIndex);

        newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);

        var r = oldArchetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Logger.DebugAssert(r == Error.None); // We assert it because the entity should exist if the whole system is consistent.
        if (r != Error.None)
        {
            return r;
        }

        // Update location
        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return Error.None;
    }

    /// <summary>
    /// Remove a component from the specified entity.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="entity">The entity to remove the component from.</param>
    /// <returns>The result status of the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error RemoveComponent<T>(Entity entity)
        where T : unmanaged, IComponentData
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error SetComponent(Entity entity, Identifier<IComponent> componentID, void* pComponent)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return Error.NotFound;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        archetype.SetComponentData(location.chunkIndex, location.rowIndex, componentID, pComponent);

        return Error.None;
    }

    /// <summary>
    /// Set the component data for the specified entity.
    /// </summary>
    /// <typeparam name="T">The component space.</typeparam>
    /// <param name="entity">The entity to set the component data for.</param>
    /// <param name="component">The component data.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error SetComponent<T>(Entity entity, T component)
        where T : unmanaged, IComponentData
    {
        return SetComponent(entity, ComponentTypeID<T>.Value, &component);
    }

    /// <summary>
    /// Get a pointer to the component data for the specified entity.
    /// </summary>
    /// <param name="entity">The entity to get the component data for.</param>
    /// <param name="componentID">The component space ID to get.</param>
    /// <returns>Pointer to the component data, or null if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetComponent<T>(Entity entity)
        where T : unmanaged, IComponentData
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        return HasComponent(entity, ComponentTypeID<T>.Value);
    }

    internal ref readonly Archetype GetEntityArchetype(Entity entity)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
            throw new ArgumentException("Entity does not exist.", nameof(entity));
            
        return ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
    }

    /// <summary>
    /// Set the enabled state of an enableable component for the specified entity.
    /// </summary>
    /// <param name="entity">The entity to set the enabled state for.</param>
    /// <param name="componentID">The component space ID of the enableable component.</
    /// <param name="enabled">True to enable the component, false to disable it.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetEnabled(Entity entity, Identifier<IComponent> componentID, bool enabled)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return Error.NotFound;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        var chunkIndex = location.chunkIndex;
        var rowIndex = location.rowIndex;

        var layoutResult = archetype.GetLayout(componentID);
        if (layoutResult.Error != Error.None)
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

        chunk.GetVersionUnsafePtr()[layoutResult.Value.versionIndex] = _world.Version;

        return Error.None;
    }

    /// <summary>
    /// Set the enabled state of an enableable component for the specified entity.
    /// </summary>
    /// <typeparam name="T">The enableable component space.</typeparam>
    /// <param name="entity">The entity to set the enabled state for.</param>
    /// <param name="enabled">True to enable the component, false to disable it.</
    /// <returns>The result status of the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error SetEnabled<T>(Entity entity, bool enabled)
        where T : unmanaged, IEnableableComponent
    {
        return SetEnabled(entity, ComponentTypeID<T>.Value, enabled);
    }

    /// <summary>
    /// Add a shared component to the specified entity, moving it into the appropriate chunk group.
    /// </summary>
    /// <param name="entity">The entity to add the shared component to.</param>
    /// <param name="componentID">The shared component ID to add.</param>
    /// <param name="pComponent">Pointer to the shared component value.</param>
    /// <returns>The result status of the operation.</returns>
    public Error AddSharedComponent(Entity entity, Identifier<IComponent> componentID, void* pComponent)
    {
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        ref var oldArchetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);
        var oldSignature = oldArchetype._signature;

        if (oldSignature.IsSet(componentID))
        {
            return Error.InvalidArgument;
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

            newArcID = FindOrCreateArchetype(ref newSignature, compCount);

            oldArchetype.AddEdgeAdd(componentID, newArcID);
        }

        ref var newArchetype = ref _world.ComponentManager.GetArchetypeReference(newArcID);

        // Build shared data: carry existing values + insert the new shared component.
        Span<byte> newSharedData = stackalloc byte[newArchetype._sharedDataSize];
        ref var oldChunk = ref oldArchetype.GetChunkReference(location.chunkIndex);
        BuildSharedData(ref oldArchetype, oldChunk._groupIndex,
                        ref newArchetype, componentID, pComponent, newSharedData);

        var sharedHash = ComponentRegistry.GetHashCodeForSharedData(newSharedData);
        newArchetype.AllocateEntity(newSharedData, sharedHash, out var newChunkIndex, out var newRowIndex);

        CopyData(ref oldArchetype, location.chunkIndex, location.rowIndex,
                 ref newArchetype, newChunkIndex, newRowIndex);

        newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);

        var r = oldArchetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Logger.DebugAssert(r == Error.None);
        if (r != Error.None)
        {
            return r;
        }

        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return Error.None;
    }

    /// <summary>
    /// Add a shared component to the specified entity, moving it into the appropriate chunk group.
    /// </summary>
    /// <typeparam name="T">The shared component type.</typeparam>
    /// <param name="entity">The entity to add the shared component to.</param>
    /// <param name="value">The shared component value.</param>
    /// <returns>The result status of the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error AddSharedComponent<T>(Entity entity, T value = default)
        where T : unmanaged, ISharedComponent
    {
        return AddSharedComponent(entity, ComponentTypeID<T>.Value, &value);
    }

    /// <summary>
    /// Remove a shared component from the specified entity, moving it to the appropriate chunk group.
    /// </summary>
    /// <param name="entity">The entity to remove the shared component from.</param>
    /// <param name="componentID">The shared component ID to remove.</param>
    /// <returns>The result status of the operation.</returns>
    public Error RemoveSharedComponent(Entity entity, Identifier<IComponent> componentID)
    {
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

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

            if (compCount == 0)
            {
                return DestroyEntity_Internal(entity, location);
            }

            newArcID = FindOrCreateArchetype(ref newSignature, compCount);

            oldArchetype.AddEdgeRemove(componentID, newArcID);
        }

        ref var newArchetype = ref _world.ComponentManager.GetArchetypeReference(newArcID);

        // Build shared data: carry existing values, omitting the removed shared component.
        Span<byte> newSharedData = stackalloc byte[newArchetype._sharedDataSize];
        ref var oldChunk = ref oldArchetype.GetChunkReference(location.chunkIndex);
        BuildSharedData(ref oldArchetype, oldChunk._groupIndex,
                        ref newArchetype, componentID, null, newSharedData);

        var sharedHash = ComponentRegistry.GetHashCodeForSharedData(newSharedData);
        newArchetype.AllocateEntity(newSharedData, sharedHash, out var newChunkIndex, out var newRowIndex);

        CopyData(ref oldArchetype, location.chunkIndex, location.rowIndex,
                 ref newArchetype, newChunkIndex, newRowIndex);

        newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);

        var r = oldArchetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Logger.DebugAssert(r == Error.None);
        if (r != Error.None)
        {
            return r;
        }

        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return Error.None;
    }

    /// <summary>
    /// Remove a shared component from the specified entity, moving it to the appropriate chunk group.
    /// </summary>
    /// <typeparam name="T">The shared component type.</typeparam>
    /// <param name="entity">The entity to remove the shared component from.</param>
    /// <returns>The result status of the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error RemoveSharedComponent<T>(Entity entity)
        where T : unmanaged, ISharedComponent
    {
        return RemoveSharedComponent(entity, ComponentTypeID<T>.Value);
    }

    /// <summary>
    /// Move an entity to the chunk group matching the new shared component value.
    /// The archetype is unchanged — only the chunk group (and thus the chunk) changes.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="componentID">The shared component ID to change.</param>
    /// <param name="pComponent">Pointer to the new shared component value.</param>
    /// <returns>The result status of the operation.</returns>
    public Error SetSharedComponent(Entity entity, Identifier<IComponent> componentID, void* pComponent)
    {
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return Error.NotFound;
        }

        ref var archetype = ref _world.ComponentManager.GetArchetypeReference(location.archetypeID);

        var sharedLayoutResult = archetype.GetSharedLayout(componentID);
        if (sharedLayoutResult.IsFailure)
        {
            return sharedLayoutResult.Error;
        }

        // Build new shared data blob: copy the current group's data, then overwrite the changed component.
        ref var oldChunk = ref archetype.GetChunkReference(location.chunkIndex);
        var oldGroup = archetype._chunkGroups[oldChunk._groupIndex];

        Span<byte> newSharedData = stackalloc byte[archetype._sharedDataSize];
        oldGroup.sharedData.AsSpan().CopyTo(newSharedData);

        var layout = sharedLayoutResult.Value;
        new ReadOnlySpan<byte>(pComponent, layout.size).CopyTo(newSharedData.Slice(layout.offset, layout.size));

        var sharedHash = ComponentRegistry.GetHashCodeForSharedData(newSharedData);

        // Same hash and same bytes → entity is already in the right group, nothing to do.
        if (sharedHash == oldGroup.sharedDataHash && oldGroup.sharedData.AsSpan().SequenceEqual(newSharedData))
        {
            return Error.None;
        }

        // Allocate a slot in the target chunk group (may create a new group + chunk).
        archetype.AllocateEntity(newSharedData, sharedHash, out var newChunkIndex, out var newRowIndex);

        // memcpy all per-entity component data (layouts are identical — same archetype).
        CopyData(ref archetype, location.chunkIndex, location.rowIndex,
                 ref archetype, newChunkIndex, newRowIndex);

        archetype.SetEntity(newChunkIndex, newRowIndex, entity);

        var r = archetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Logger.DebugAssert(r == Error.None);
        if (r != Error.None)
        {
            return r;
        }

        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return Error.None;
    }

    /// <summary>
    /// Move an entity to the chunk group matching the new shared component value.
    /// </summary>
    /// <typeparam name="T">The shared component type.</typeparam>
    /// <param name="entity">The entity to update.</param>
    /// <param name="value">The new shared component value.</param>
    /// <returns>The result status of the operation.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Error SetSharedComponent<T>(Entity entity, T value)
        where T : unmanaged, ISharedComponent
    {
        return SetSharedComponent(entity, ComponentTypeID<T>.Value, &value);
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
