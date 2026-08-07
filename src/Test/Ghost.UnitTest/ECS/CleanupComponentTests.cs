using Ghost.Core;
using Ghost.Entities;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class CleanupComponentTests
{
    private struct CompA : IComponentData
    {
        public int value;
    }

    private struct CleanupTag : ICleanupComponent
    {
    }

    private struct CleanupData : ICleanupComponent
    {
        public int value;
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

    private int CountQuery<T>() where T : unmanaged, IComponent
    {
        var queryID = QueryBuilder.New().WithAll<T>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        return query.CalculateEntityCount();
    }

    [TestMethod]
    public void DestroyEntity_WithCleanupComponent_MigratesToCleanupArchetype()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new CompA { value = 42 });
        _world.EntityManager.AddComponent<CleanupTag>(entity);

        var error = _world.EntityManager.DestroyEntity(entity);
        Assert.AreEqual(Error.None, error);

        // Entity survives in the cleanup archetype with only cleanup components.
        Assert.IsTrue(_world.EntityManager.Exists(entity), "Cleanup entity should survive the first destroy.");
        Assert.IsFalse(_world.EntityManager.HasComponent<CompA>(entity), "Non-cleanup components must be dropped.");
        Assert.IsTrue(_world.EntityManager.HasComponent<CleanupTag>(entity), "Cleanup components must survive.");
        Assert.AreEqual(0, CountQuery<CompA>(), "Entity must no longer be visible to CompA queries.");
        Assert.AreEqual(1, CountQuery<CleanupTag>(), "Entity must be visible to CleanupTag queries.");
    }

    [TestMethod]
    public void DestroyEntity_SecondCall_RemovesCleanupEntity()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new CompA { value = 7 }); // keeps the entity OUT of the cleanup archetype
        _world.EntityManager.AddComponent<CleanupTag>(entity);

        // First destroy migrates the entity into the cleanup archetype (it survives).
        Assert.AreEqual(Error.None, _world.EntityManager.DestroyEntity(entity));
        Assert.IsTrue(_world.EntityManager.Exists(entity));

        // A second destroy actually removes the entity.
        Assert.AreEqual(Error.None, _world.EntityManager.DestroyEntity(entity));
        Assert.IsFalse(_world.EntityManager.Exists(entity), "Second destroy should fully remove the cleanup entity.");
        Assert.AreEqual(0, CountQuery<CleanupTag>());
    }

    [TestMethod]
    public void DestroyEntity_EntityAlreadyInCleanupArchetype_IsRemovedImmediately()
    {
        // Entity whose only component is a cleanup component lives in the cleanup archetype
        // already — a single destroy must remove it (regression: it used to duplicate forever).
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<CleanupTag>(entity);

        Assert.AreEqual(Error.None, _world.EntityManager.DestroyEntity(entity));
        Assert.IsFalse(_world.EntityManager.Exists(entity));
        Assert.AreEqual(0, CountQuery<CleanupTag>());
    }

    [TestMethod]
    public void DestroyEntities_MixedCleanupAndNormal_SameChunk()
    {
        var cleanup = new List<Entity>();
        var normal = new List<Entity>();

        for (var i = 0; i < 32; i++)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponent(e, new CompA { value = i });
            _world.EntityManager.AddComponent<CleanupTag>(e);
            cleanup.Add(e);
        }

        for (var i = 0; i < 32; i++)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponent(e, new CompA { value = i });
            normal.Add(e);
        }

        var all = cleanup.Concat(normal).ToArray();
        _world.EntityManager.DestroyEntities(all);

        // Normal entities are gone; cleanup entities survive with only cleanup components.
        foreach (var e in normal)
        {
            Assert.IsFalse(_world.EntityManager.Exists(e));
        }

        for (var i = 0; i < cleanup.Count; i++)
        {
            Assert.IsTrue(_world.EntityManager.Exists(cleanup[i]));
            Assert.IsFalse(_world.EntityManager.HasComponent<CompA>(cleanup[i]));
            Assert.IsTrue(_world.EntityManager.HasComponent<CleanupTag>(cleanup[i]));
        }

        Assert.AreEqual(0, CountQuery<CompA>());
        Assert.AreEqual(32, CountQuery<CleanupTag>());
    }

    [TestMethod]
    public void DestroyEntities_MultipleCleanupEntities_PreservesCleanupData()
    {
        var entities = new Entity[10];
        for (var i = 0; i < entities.Length; i++)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponent(e, new CompA { value = i * 100 });
            _world.EntityManager.AddComponent(e, new CleanupData { value = i });
            entities[i] = e;
        }

        _world.EntityManager.DestroyEntities(entities);

        for (var i = 0; i < entities.Length; i++)
        {
            Assert.IsTrue(_world.EntityManager.Exists(entities[i]));
            Assert.AreEqual(i, _world.EntityManager.GetComponent<CleanupData>(entities[i]).value, "Cleanup data must be preserved.");
            Assert.IsFalse(_world.EntityManager.HasComponent<CompA>(entities[i]));
        }
    }

    [TestMethod]
    public void DestroyEntity_WithCleanupAndShared_DropsSharedData()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<CompA>(entity);
        _world.EntityManager.AddSharedComponent(entity, new SharedGroup { groupID = 5 });
        _world.EntityManager.AddComponent<CleanupTag>(entity);

        Assert.AreEqual(Error.None, _world.EntityManager.DestroyEntity(entity));

        Assert.IsTrue(_world.EntityManager.Exists(entity));
        Assert.IsFalse(_world.EntityManager.HasComponent<SharedGroup>(entity), "Shared data must be dropped on cleanup migration.");
        Assert.IsTrue(_world.EntityManager.HasComponent<CleanupTag>(entity));

        // Shared group bookkeeping must stay consistent: a new entity can reuse the same value.
        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddSharedComponent(e2, new SharedGroup { groupID = 5 });
        Assert.IsTrue(_world.EntityManager.HasComponent<SharedGroup>(e2));
        Assert.AreEqual(5, _world.EntityManager.GetSharedComponent<SharedGroup>(e2).groupID);
    }

    [TestMethod]
    public void DestroyEntities_CleanupThenDestroyAgain_RemovesThem()
    {
        var entities = new Entity[8];
        for (var i = 0; i < entities.Length; i++)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponent(e, new CompA { value = i * 10 }); // keeps entities OUT of the cleanup archetype
            _world.EntityManager.AddComponent(e, new CleanupData { value = i });
            entities[i] = e;
        }

        // First batch migrates the entities into the cleanup archetype.
        _world.EntityManager.DestroyEntities(entities);

        for (var i = 0; i < entities.Length; i++)
        {
            Assert.IsTrue(_world.EntityManager.Exists(entities[i]));
            Assert.AreEqual(i, _world.EntityManager.GetComponent<CleanupData>(entities[i]).value, "Cleanup data must survive the migration.");
        }

        // Cleanup systems "finish" and destroy them for real.
        _world.EntityManager.DestroyEntities(entities);

        foreach (var e in entities)
        {
            Assert.IsFalse(_world.EntityManager.Exists(e));
        }

        Assert.AreEqual(0, CountQuery<CleanupData>());
        Assert.AreEqual(0, _world.EntityManager.EntityCount);
    }
}
