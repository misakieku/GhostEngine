using Ghost.Core;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class SharedComponentTests
{
    private struct Tag : IComponentData { }

    private struct Tag2 : IComponentData { }

    private struct ScalarData : IComponentData
    {
        public float value;
    }

    private struct SharedGroup : ISharedComponent
    {
        public int groupID;
    }

    private struct SharedGroup2 : ISharedComponent
    {
        public int subID;
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


    /// <summary>Creates an entity that carries Tag + SharedGroup(groupID).</summary>
    private unsafe Entity CreateWithSharedGroup(int groupID)
    {
        var sharedValue = new SharedGroup { groupID = groupID };

        // Build shared data blob: SharedGroup is the only shared component.
        using var sharedSet = new SharedComponentSet(sizeof(SharedGroup), AllocationHandle.Persistent);
        sharedSet.With(sharedValue);

        Identifier<IComponent>[] ids = [ComponentTypeID<Tag>.Value, ComponentTypeID<SharedGroup>.Value];
        var set = new ComponentSetView(ids, sharedSet);

        Span<Entity> result = stackalloc Entity[1];
        _world.EntityManager.CreateEntities(result, set);
        return result[0];
    }

    /// <summary>Counts entities visible to a single-component query.</summary>
    private int CountQuery<T0>() where T0 : unmanaged, IComponent
    {
        var queryID = QueryBuilder.New().WithAll<T0>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        var count = 0;
        foreach (var chunk in query.GetChunkIterator())
        {
            count += chunk.EntityCount;
        }

        return count;
    }

    /// <summary>Returns (groupID → count) map across all chunks matching Tag.</summary>
    private Dictionary<int, int> CollectGroupCounts()
    {
        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        var map = new Dictionary<int, int>();
        foreach (var chunk in query.GetChunkIterator())
        {
            var gid = chunk.GetSharedComponent<SharedGroup>().groupID;
            map.TryGetValue(gid, out var existing);
            map[gid] = existing + chunk.EntityCount;
        }

        return map;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. Chunk grouping — same archetype, different groups
    // ══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void EntitiesWithDifferentSharedValues_AreInDifferentChunkGroups()
    {
        CreateWithSharedGroup(1);
        CreateWithSharedGroup(2);

        // Two distinct shared values → two distinct chunk groups.
        var groups = CollectGroupCounts();

        Assert.AreEqual(2, groups.Count, "Expected 2 distinct chunk groups.");
        Assert.IsTrue(groups.ContainsKey(1));
        Assert.IsTrue(groups.ContainsKey(2));
    }

    [TestMethod]
    public void EntitiesWithSameSharedValue_AreInSameChunkGroup()
    {
        CreateWithSharedGroup(42);
        CreateWithSharedGroup(42);

        // Both entities have the same shared value → single chunk group with 2 entities.
        var groups = CollectGroupCounts();

        Assert.AreEqual(1, groups.Count, "Expected a single chunk group.");
        Assert.AreEqual(2, groups[42]);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. ChunkView.GetSharedComponent
    // ══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ChunkView_GetSharedComponent_ReturnsCorrectValue()
    {
        CreateWithSharedGroup(99);

        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        var found = false;
        foreach (var chunk in query.GetChunkIterator())
        {
            Assert.AreEqual(99, chunk.GetSharedComponent<SharedGroup>().groupID);
            found = true;
        }

        Assert.IsTrue(found, "Expected at least one chunk.");
    }

    [TestMethod]
    public void ChunkView_GetSharedComponent_DistinctValuesPerChunkGroup()
    {
        CreateWithSharedGroup(1);
        CreateWithSharedGroup(2);

        var seen = new HashSet<int>();

        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        foreach (var chunk in query.GetChunkIterator())
        {
            seen.Add(chunk.GetSharedComponent<SharedGroup>().groupID);
        }

        CollectionAssert.AreEquivalent(new[] { 1, 2 }, seen.ToArray());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. SetSharedComponent — intra-archetype move
    // ══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void SetSharedComponent_MovesEntityToNewGroup()
    {
        CreateWithSharedGroup(1);
        var movingEntity = CreateWithSharedGroup(1);

        var error = _world.EntityManager.SetSharedComponent(movingEntity, new SharedGroup { groupID = 2 });
        Assert.AreEqual(Error.None, error);

        var groups = CollectGroupCounts();
        Assert.IsTrue(groups.ContainsKey(2), "Entity should now be in group 2.");
        Assert.AreEqual(1, groups[1], "One entity should remain in group 1.");
        Assert.AreEqual(1, groups[2], "One entity should now be in group 2.");
    }

    [TestMethod]
    public void SetSharedComponent_NoOp_WhenValueUnchanged()
    {
        var entity = CreateWithSharedGroup(7);

        // Query the chunk before the call.
        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        var chunkEntityCountBefore = 0;
        foreach (var chunk in query.GetChunkIterator())
        {
            chunkEntityCountBefore = chunk.EntityCount;
        }

        var error = _world.EntityManager.SetSharedComponent(entity, new SharedGroup { groupID = 7 });
        Assert.AreEqual(Error.None, error);

        var chunkEntityCountAfter = 0;
        foreach (var chunk in query.GetChunkIterator())
        {
            chunkEntityCountAfter = chunk.EntityCount;
        }

        // Same chunk, same count — no move happened.
        Assert.AreEqual(chunkEntityCountBefore, chunkEntityCountAfter);
    }

    [TestMethod]
    public void SetSharedComponent_PreservesPerEntityComponentData()
    {
        var entity = CreateWithSharedGroup(1);
        _world.EntityManager.AddComponent(entity, new ScalarData { value = 3.14f });

        _world.EntityManager.SetSharedComponent(entity, new SharedGroup { groupID = 2 });

        ref var data = ref _world.EntityManager.GetComponent<ScalarData>(entity);
        Assert.AreEqual(3.14f, data.value, 1e-6f, "Per-entity data must be preserved after SetSharedComponent.");
    }

    [TestMethod]
    public void SetSharedComponent_ReturnsNotFound_ForInvalidEntity()
    {
        // Entity.Invalid is the sentinel for non-existent entities.
        var error = _world.EntityManager.SetSharedComponent(Entity.Invalid, new SharedGroup { groupID = 1 });
        Assert.AreEqual(Error.NotFound, error);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. AddSharedComponent
    // ══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void AddSharedComponent_MakesValueAccessibleViaChunkView()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<Tag>(entity);

        var error = _world.EntityManager.AddSharedComponent(entity, new SharedGroup { groupID = 77 });
        Assert.AreEqual(Error.None, error);

        var queryID = QueryBuilder.New().WithAll<Tag, SharedGroup>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        var found = false;
        foreach (var chunk in query.GetChunkIterator())
        {
            Assert.AreEqual(77, chunk.GetSharedComponent<SharedGroup>().groupID);
            found = true;
        }

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void AddSharedComponent_Returns_InvalidArgument_IfAlreadyPresent()
    {
        var entity = CreateWithSharedGroup(1);
        var error = _world.EntityManager.AddSharedComponent(entity, new SharedGroup { groupID = 2 });
        Assert.AreEqual(Error.InvalidArgument, error);
    }

    [TestMethod]
    public void AddSharedComponent_EntityCountCorrectAfterAdd()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<Tag>(entity);
        _world.EntityManager.AddSharedComponent(entity, new SharedGroup { groupID = 5 });

        Assert.AreEqual(1, CountQuery<Tag>());
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. RemoveSharedComponent
    // ══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void RemoveSharedComponent_EntityNoLongerHasSharedComponent()
    {
        var entity = CreateWithSharedGroup(1);
        _world.EntityManager.RemoveSharedComponent<SharedGroup>(entity);
        Assert.IsFalse(_world.EntityManager.HasComponent<SharedGroup>(entity));
    }

    [TestMethod]
    public void RemoveSharedComponent_PreservesOtherComponents()
    {
        var entity = CreateWithSharedGroup(1);
        _world.EntityManager.AddComponent(entity, new ScalarData { value = 2.71f });

        var error = _world.EntityManager.RemoveSharedComponent<SharedGroup>(entity);
        Assert.AreEqual(Error.None, error);

        Assert.IsTrue(_world.EntityManager.HasComponent<ScalarData>(entity));
        Assert.IsTrue(_world.EntityManager.HasComponent<Tag>(entity));
        ref var data = ref _world.EntityManager.GetComponent<ScalarData>(entity);
        Assert.AreEqual(2.71f, data.value, 1e-6f);
    }

    [TestMethod]
    public void RemoveSharedComponent_EntityCountRemainsCorrect()
    {
        var entity = CreateWithSharedGroup(1);
        _world.EntityManager.RemoveSharedComponent<SharedGroup>(entity);
        Assert.AreEqual(1, CountQuery<Tag>(), "Entity should still be visible after removing its shared component.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. Non-shared Add/Remove carries shared data through
    // ══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void AddComponent_PreservesSharedDataInNewArchetype()
    {
        var entity = CreateWithSharedGroup(55);

        // Add a normal component — entity migrates to a new archetype.
        _world.EntityManager.AddComponent(entity, new ScalarData { value = 1.0f });

        // Shared component value must still be readable via ChunkView.
        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        var found = false;
        foreach (var chunk in query.GetChunkIterator())
        {
            Assert.AreEqual(55, chunk.GetSharedComponent<SharedGroup>().groupID);
            found = true;
        }

        Assert.IsTrue(found, "Shared value must survive a non-shared AddComponent move.");
    }

    [TestMethod]
    public void RemoveComponent_PreservesSharedDataInNewArchetype()
    {
        var entity = CreateWithSharedGroup(33);
        _world.EntityManager.AddComponent<Tag2>(entity);

        // Remove Tag2 — entity migrates back but shared data must persist.
        _world.EntityManager.RemoveComponent<Tag2>(entity);

        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        var found = false;
        foreach (var chunk in query.GetChunkIterator())
        {
            Assert.AreEqual(33, chunk.GetSharedComponent<SharedGroup>().groupID);
            found = true;
        }

        Assert.IsTrue(found, "Shared value must survive a non-shared RemoveComponent move.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. Multiple shared components
    // ══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void MultipleSharedComponents_BothAccessibleViaChunkView()
    {
        // Use AddSharedComponent to avoid having to guess archetype blob layout order.
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<Tag>(entity);
        _world.EntityManager.AddSharedComponent(entity, new SharedGroup { groupID = 10 });
        _world.EntityManager.AddSharedComponent(entity, new SharedGroup2 { subID = 20 });

        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        var found = false;
        foreach (var chunk in query.GetChunkIterator())
        {
            if (chunk.EntityCount == 0) continue;
            Assert.AreEqual(10, chunk.GetSharedComponent<SharedGroup>().groupID);
            Assert.AreEqual(20, chunk.GetSharedComponent<SharedGroup2>().subID);
            found = true;
        }

        Assert.IsTrue(found);
    }

    [TestMethod]
    public void SetSharedComponent_OneOfTwo_OtherPreserved()
    {
        // Use AddSharedComponent to avoid blob ordering assumptions.
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<Tag>(entity);
        _world.EntityManager.AddSharedComponent(entity, new SharedGroup { groupID = 1 });
        _world.EntityManager.AddSharedComponent(entity, new SharedGroup2 { subID = 100 });

        _world.EntityManager.SetSharedComponent(entity, new SharedGroup { groupID = 2 });

        var queryID = QueryBuilder.New().WithAll<Tag>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        foreach (var chunk in query.GetChunkIterator())
        {
            if (chunk.EntityCount == 0) continue;
            Assert.AreEqual(2, chunk.GetSharedComponent<SharedGroup>().groupID, "SharedGroup should be updated.");
            Assert.AreEqual(100, chunk.GetSharedComponent<SharedGroup2>().subID, "SharedGroup2 must be unchanged.");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. Query filtering and entity count
    // ══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Query_CountsEntitiesAcrossAllSharedGroups()
    {
        CreateWithSharedGroup(1);
        CreateWithSharedGroup(1);
        CreateWithSharedGroup(2);

        Assert.AreEqual(3, CountQuery<Tag>(), "Query should count across all chunk groups.");
    }

    [TestMethod]
    public void Query_GroupCountDistributionCorrect()
    {
        CreateWithSharedGroup(10);
        CreateWithSharedGroup(10);
        CreateWithSharedGroup(20);

        var groups = CollectGroupCounts();
        Assert.AreEqual(2, groups[10]);
        Assert.AreEqual(1, groups[20]);
    }

    [TestMethod]
    public void DestroyEntity_InSharedGroup_DoesNotAffectOthersInGroup()
    {
        var e1 = CreateWithSharedGroup(5);
        var e2 = CreateWithSharedGroup(5);
        CreateWithSharedGroup(5);

        _world.EntityManager.DestroyEntity(e1);
        _world.EntityManager.DestroyEntity(e2);

        Assert.AreEqual(1, CountQuery<Tag>(), "Only one entity should remain.");
    }

    [TestMethod]
    public void DestroyAllEntitiesInGroup_OtherGroupsUnaffected()
    {
        var eA = CreateWithSharedGroup(1);
        CreateWithSharedGroup(2);

        _world.EntityManager.DestroyEntity(eA);

        // Group 1 may still have an empty chunk, but it should have 0 entities.
        var groups = CollectGroupCounts();
        if (groups.ContainsKey(1))
        {
            Assert.AreEqual(0, groups[1], "Group 1 should have 0 entities.");
        }

        Assert.IsTrue(groups.ContainsKey(2) && groups[2] == 1, "Group 2 entity should be untouched.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 9. Canonical Ordering
    // ══════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void CanonicalOrdering_SharedComponentSet_IndependentOfInsertionOrder()
    {
        using var sharedSet1 = new SharedComponentSet(64, AllocationHandle.Temp);
        sharedSet1.With(new SharedGroup { groupID = 42 });
        sharedSet1.With(new SharedGroup2 { subID = 99 });

        using var sharedSet2 = new SharedComponentSet(64, AllocationHandle.Temp);
        sharedSet2.With(new SharedGroup2 { subID = 99 });
        sharedSet2.With(new SharedGroup { groupID = 42 });

        Identifier<IComponent>[] ids1 = [ComponentTypeID<Tag>.Value, ComponentTypeID<SharedGroup>.Value, ComponentTypeID<SharedGroup2>.Value];
        var set1 = new ComponentSetView(ids1, sharedSet1);

        Identifier<IComponent>[] ids2 = [ComponentTypeID<Tag>.Value, ComponentTypeID<SharedGroup2>.Value, ComponentTypeID<SharedGroup>.Value];
        var set2 = new ComponentSetView(ids2, sharedSet2);

        Span<Entity> result1 = stackalloc Entity[1];
        _world.EntityManager.CreateEntities(result1, set1);

        Span<Entity> result2 = stackalloc Entity[1];
        _world.EntityManager.CreateEntities(result2, set2);

        var groups = CollectGroupCounts();

        Assert.AreEqual(1, groups.Count, "Expected exactly 1 chunk group due to canonical sorting of SharedComponentSet.");
        Assert.AreEqual(2, groups[42], "Both entities should be grouped under groupID 42.");
    }
}
