using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;
using Misaki.HighPerformance.LowLevel.Utilities;
using System.Diagnostics;

namespace Ghost.Entities;

public unsafe class EntityManager : IDisposable
{
    private struct EntityLocation
    {
        public Identifier<Archetype> archetypeID;
        public int chunkIndex;
        public int rowIndex;
    }

    private World _world;
    private UnsafeSlotMap<EntityLocation> _entityLocations;

    internal EntityManager(World world, int initialCapacity)
    {
        _world = world;
        _entityLocations = new UnsafeSlotMap<EntityLocation>(initialCapacity, Allocator.Persistent);
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

    public ResultStatus DestoryEntity(Entity entity)
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

    private static void CopyData(ref Archetype oldArch, int oldChunk, int oldRow,
                                 ref Archetype newArch, int newChunk, int newRow)
    {
        // Iterate every component type in the OLD archetype
        for (var i = 0; i < oldArch.Layouts.Count; i++)
        {
            var layout = oldArch.Layouts[i];

            var src = oldArch.Chunks[oldChunk].GetUnsafePtr() + layout.offset + (layout.size * oldRow);
            var newOffset = newArch.GetOffset(layout.componentID); // O(1) Lookup
            var dst = oldArch.Chunks[oldChunk].GetUnsafePtr() + newOffset + (layout.size * newRow);

            MemoryUtility.MemCpy(src, dst, (nuint)layout.size);
        }
    }

    public ResultStatus AddComponent(Entity entity, Identifier<IComponent> componentID, void* component)
    {
        // Find current location
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return ResultStatus.NotFound;
        }

        // Build new archetype signature
        ref var oldArchetype = ref _world.GetArchetypeReference(location.archetypeID);
        var oldSignature = oldArchetype.Signature;

        // TODO: Check edge cache first.
        var newArcID = oldArchetype.GetEdgeAdd(componentID);
        if (newArcID.IsNotValid)
        {
            var largestComponentID = Math.Max(oldSignature.Count, componentID);
            var length = UnsafeBitSet.RequiredLength(largestComponentID + 1);

            Span<uint> bits = stackalloc uint[length];
            bits.Clear();

            var newSignature = new SpanBitSet(bits);

            var iterator = 0;
            var compCount = 0;
            while (true)
            {
                var bit = oldSignature.NextSetBit(iterator);
                if (bit == -1)
                {
                    break;
                }

                newSignature.SetBit(bit);
                iterator = bit + 1;
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
                componentTypeIDs[0] = componentID;

                iterator = 0;
                while (true)
                {
                    var bit = oldSignature.NextSetBit(iterator);
                    if (bit == -1)
                    {
                        break;
                    }

                    componentTypeIDs[--compCount] = bit;
                    iterator = bit + 1;
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
        newArchetype.SetComponentData(newChunkIndex, newRowIndex, componentID, component);

        var r = oldArchetype.RemoveEntity(location.chunkIndex, location.rowIndex);
        Debug.Assert(r == ResultStatus.Success); // We assert it because the entity should exist if the whole system is consistent.
        // if (r != ResultStatus.Success)
        // {
        //     return r;
        // }

        // Update location
        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return ResultStatus.Success;
    }

    public ResultStatus AddComponent<T>(Entity entity, ref T component)
        where T : unmanaged, IComponent
    {
        return AddComponent(entity, ComponentTypeID<T>.value, UnsafeUtility.AddressOf(ref component));
    }

    public ResultStatus SetComponentData(Entity entity, Identifier<IComponent> componentID, void* pComponent)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return ResultStatus.NotFound;
        }

        ref var archetype = ref _world.GetArchetypeReference(location.archetypeID);
        archetype.SetComponentData(location.chunkIndex, location.rowIndex, componentID, pComponent);

        return ResultStatus.Success;
    }

    public ResultStatus SetComponentData<T>(Entity entity, ref T component)
        where T : unmanaged, IComponent
    {
        return SetComponentData(entity, ComponentTypeID<T>.value, UnsafeUtility.AddressOf(ref component));
    }

    public bool HasComponent(Entity entity, Identifier<IComponent> componentID)
    {
        if (!_entityLocations.TryGetElementAt(entity.ID, entity.Generation, out var location))
        {
            return false;
        }

        ref var archetype = ref _world.GetArchetypeReference(location.archetypeID);
        return archetype.HasComponent(componentID);
    }

    public bool HasComponent<T>(Entity entity)
        where T : unmanaged, IComponent
    {
        return HasComponent(entity, ComponentTypeID<T>.value);
    }

    public void Dispose()
    {
        _entityLocations.Dispose();
    }
}
