using Ghost.Entities;
using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class ChunkRecyclingTests
{
    private struct Tag : IComponentData
    {
    }

    private struct SharedGroup : ISharedComponent
    {
        public int groupID;
    }

    private World _world = null!;

    [TestInitialize]
    public void Setup()
    {
        _world = World.Create(null, 64);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _world.Dispose();
    }

    private ref Archetype ArchetypeOf<T>() where T : unmanaged, IComponent
    {
        var hash = ComponentRegistry.GetHashCodeForTypeIDs(ComponentTypeID<T>.Value);
        var arcID = _world.ComponentManager.GetArchetypeIDBySignatureHash(hash);
        return ref _world.ComponentManager.GetArchetypeReference(arcID);
    }

    private List<Entity> CreateWithTag(int count)
    {
        var list = new List<Entity>(count);
        for (var i = 0; i < count; i++)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponent<Tag>(e);
            list.Add(e);
        }

        return list;
    }

    [TestMethod]
    public void Churn_Collect_KeepsChunkCountBounded()
    {
        // Round capacity for the Tag archetype: 16384 / sizeof(Entity) = 2048 entities per chunk.
        for (var round = 0; round < 10; round++)
        {
            var entities = CreateWithTag(1500);
            // Destroy a contiguous slice — concentrated in a small number of chunks — so
            // chunks become empty and enter the free/reuse path.
            _world.EntityManager.DestroyEntities(entities.Skip(200).Take(1200).ToArray());
            _world.ComponentManager.Collect();
        }

        ref var archetype = ref ArchetypeOf<Tag>();
        var chunkCount = archetype.ChunkCount;

        // Without recycling this grows by ~1 chunk per round (10+). With recycling the slots
        // are reused and the count stays near the peak concurrent chunk count (≤ 4).
        Assert.IsLessThanOrEqualTo(4, chunkCount, $"Chunk count should stay bounded after churn, was {chunkCount}.");

        // Query results must still be consistent.
        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(_world.EntityManager.EntityCount, query.CalculateEntityCount());
    }

    [TestMethod]
    public void FragmentedChunks_AreReusedBeforeAllocatingNewOnes()
    {
        // Fill chunk 0 completely and spill into chunk 1.
        var entities = CreateWithTag(3100); // chunk 0: 2048, chunk 1: 1052
        Assert.AreEqual(2, ArchetypeOf<Tag>().ChunkCount);

        // Destroy everything in chunk 0 (first 2048 entities are sequential in chunk 0).
        _world.EntityManager.DestroyEntities(entities.Take(2048).ToArray());
        Assert.AreEqual(0, ArchetypeOf<Tag>()._chunks[0]._count, "Chunk 0 should be empty.");

        // Chunk 1 has 1052 entities; adding 1100 overflows it. The empty chunk 0 must be
        // reused instead of allocating a third chunk.
        var extra = CreateWithTag(1100);
        Assert.AreEqual(2, ArchetypeOf<Tag>().ChunkCount, "An empty chunk must be adopted before allocating a new one.");

        // Everything must remain queryable and intact.
        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(_world.EntityManager.EntityCount, query.CalculateEntityCount());
    }

    [TestMethod]
    public void Collect_TrimsEmptyChunkBuffers_AndSlotsAreReused()
    {
        var entities = CreateWithTag(3100);
        Assert.AreEqual(2, ArchetypeOf<Tag>().ChunkCount);

        _world.EntityManager.DestroyEntities(entities.ToArray());
        _world.ComponentManager.Collect();

        ref var archetype = ref ArchetypeOf<Tag>();
        Assert.AreEqual(0, archetype._chunks[0]._count);
        Assert.IsFalse(archetype._chunks[0].IsCreated, "Empty chunk buffers should be trimmed by Collect.");

        // New allocations reuse the trimmed slots (no chunk list growth) and work normally.
        // 500 entities fit in a single chunk, so exactly one trimmed slot is recreated.
        var newEntities = CreateWithTag(500);
        Assert.AreEqual(2, archetype.ChunkCount, "Trimmed slots must be reused, not appended.");

        var recreated = 0;
        for (var i = 0; i < archetype.ChunkCount; i++)
        {
            if (archetype._chunks[i].IsCreated)
            {
                recreated++;
            }
        }

        Assert.AreEqual(1, recreated, "Exactly one trimmed slot should be reused for 500 entities.");

        foreach (var e in newEntities)
        {
            Assert.IsTrue(_world.EntityManager.Exists(e));
        }
    }

    [TestMethod]
    public void Collect_ReclaimsDeadSharedGroups()
    {
        // Two distinct shared values → two chunk groups.
        for (var i = 0; i < 10; i++)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddSharedComponent(e, new SharedGroup { groupID = 1 });
            _world.EntityManager.DestroyEntity(e);
        }

        for (var i = 0; i < 10; i++)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddSharedComponent(e, new SharedGroup { groupID = 2 });
            _world.EntityManager.DestroyEntity(e);
        }

        _world.ComponentManager.Collect();

        // Both groups died; a new value must reuse a dead group slot instead of growing the list.
        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddSharedComponent(e2, new SharedGroup { groupID = 3 });

        ref var archetype = ref ArchetypeOf<SharedGroup>();
        Assert.IsLessThanOrEqualTo(3, archetype._chunkGroups.Count, $"Chunk group slots should be reused, was {archetype._chunkGroups.Count}.");
        Assert.AreEqual(3, _world.EntityManager.GetSharedComponent<SharedGroup>(e2).groupID);
    }

    [TestMethod]
    public void EmptyActiveChunk_StaysActive_AfterDestroy()
    {
        var e = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<Tag>(e);

        Assert.AreEqual(1, ArchetypeOf<Tag>().ChunkCount);
        _world.EntityManager.DestroyEntity(e);

        // The emptied chunk remains the active allocation target.
        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<Tag>(e2);
        Assert.AreEqual(1, ArchetypeOf<Tag>().ChunkCount, "Emptied active chunk must be reused.");
        Assert.IsTrue(_world.EntityManager.Exists(e2));
    }
}
