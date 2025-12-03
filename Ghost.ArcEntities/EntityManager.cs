using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.ArcEntities;

public class EntityManager : IDisposable
{
    private struct EntityLocation
    {
        public Identifier<Archetype> archetypeID;
        public int chunkIndex;
        public int rowIndex;
    }

    private World _world;
    private UnsafeSlotMap<EntityLocation> _entityLocations;

    internal EntityManager(World world)
    {
        _world = world;
        _entityLocations = new UnsafeSlotMap<EntityLocation>(16, Allocator.Persistent);
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

    public ResultStatus RemoveEntity(Entity entity)
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

    public ResultStatus AddComponent<T>(Entity entity)
        where T : unmanaged
    {
        // Find current location
        ref var location = ref _entityLocations.GetElementReferenceAt(entity.ID, entity.Generation, out var exist);
        if (!exist)
        {
            return ResultStatus.NotFound;
        }

        // Build new archetype signature
        ref var archetype = ref _world.GetArchetypeReference(location.archetypeID);
        var currentSignature = archetype.Signature;

        var compID = ComponentTypeID<T>.value;

        var largestComponentID = Math.Max(currentSignature.Count, compID);
        var length = UnsafeBitSet.RequiredLength(largestComponentID + 1);

        Span<uint> bits = stackalloc uint[length];
        bits.Clear();

        var newSignature = new SpanBitSet(bits);

        var i = 0;
        var compCount = 0;
        do
        {
            i = currentSignature.NextSetBit(i);
            if (i == -1)
            {
                break;
            }

            newSignature.SetBit(i);
            compCount++;
        } while (true);

        compCount++;
        newSignature.SetBit(compID);

        // Find or create new archetype
        var newSignatureHash = newSignature.GetHashCode();
        var newArcID = _world.GetArchetypeIDBySignatureHash(newSignatureHash);
        if (newArcID.IsNotValid)
        {
            // Create new archetype
            Span<int> componentTypeIDs = stackalloc int[compCount];
            componentTypeIDs[0] = compID;

            do
            {
                i = currentSignature.NextSetBit(i);
                if (i == -1)
                {
                    break;
                }

                componentTypeIDs[--compCount] = i;
            } while (true);

            _world.CreateArchetype(componentTypeIDs, newSignatureHash);
        }

        ref var newArchetype = ref _world.GetArchetypeReference(newArcID);
        newArchetype.AllocateEntity(out var newChunkIndex, out var newRowIndex);
        newArchetype.SetEntity(newChunkIndex, newRowIndex, entity);

        // Update location
        location.archetypeID = newArcID;
        location.chunkIndex = newChunkIndex;
        location.rowIndex = newRowIndex;

        return ResultStatus.Success;
    }

    public void Dispose()
    {
        _entityLocations.Dispose();
    }
}
