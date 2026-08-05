using Ghost.Core;
using Ghost.Entities;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class WorldTests
{
    private struct CompA : IComponentData { public int value; }
    private struct CompB : IComponentData { public int value; }

    private World _world = null!;
    private EntityManager _entityManager = null!;

    [TestInitialize]
    public void Setup()
    {
        _world = World.Create(entityCapacity: 128);
        _entityManager = _world.EntityManager;
    }

    [TestCleanup]
    public void Cleanup()
    {
        _world.Dispose();
    }

    [TestMethod]
    public void TestWorld_EntityLifecycle()
    {
        var entity = _world.EntityManager.CreateEntity();
        Assert.IsTrue(_world.EntityManager.Exists(entity));

        _world.EntityManager.DestroyEntity(entity);
        Assert.IsFalse(_world.EntityManager.Exists(entity));
    }

    [TestMethod]
    public void TestWorld_GenerationalID()
    {
        var e1 = _world.EntityManager.CreateEntity();
        var index = e1.ID;
        var version = e1.Generation;

        _world.EntityManager.DestroyEntity(e1);

        var e2 = _world.EntityManager.CreateEntity();
        Assert.AreEqual(index, e2.ID, "Should reuse entity index.");
        Assert.AreNotEqual(version, e2.Generation, "Should increment version.");
    }

    [TestMethod]
    public void TestWorld_ComponentPersistence()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new CompA { value = 42 });

        Assert.IsTrue(_world.EntityManager.HasComponent<CompA>(entity));
        Assert.AreEqual(42, _world.EntityManager.GetComponent<CompA>(entity).value);

        // Add another component, migrates archetype
        _world.EntityManager.AddComponent(entity, new CompB { value = 100 });

        Assert.IsTrue(_world.EntityManager.HasComponent<CompA>(entity));
        Assert.IsTrue(_world.EntityManager.HasComponent<CompB>(entity));
        Assert.AreEqual(42, _world.EntityManager.GetComponent<CompA>(entity).value);
    }

    [TestMethod]
    public void TestWorld_Reset_ClearsAll()
    {
        _world.EntityManager.CreateEntity();
        _world.EntityManager.CreateEntity();

        _world.Reset();

        // We can't easily check internal state, but new entities should start from index 0 again
        var entity = _world.EntityManager.CreateEntity();
        Assert.AreEqual(0, entity.ID);
    }

    [TestMethod]
    public void DestroyEntity_ReturnsNotFound_OnDoubleDestroy()
    {
        using var world = World.Create(null, 64);
        var e = world.EntityManager.CreateEntity();

        var err1 = world.EntityManager.DestroyEntity(e);
        Assert.AreEqual(Error.None, err1);

        var err2 = world.EntityManager.DestroyEntity(e);
        Assert.AreEqual(Error.NotFound, err2);
    }

    [TestMethod]
    public void TestWorld_MultipleWorlds_Isolation()
    {
        var worldB = World.Create(null, 64);
        try
        {
            var eA = _world.EntityManager.CreateEntity();
            var eB = worldB.EntityManager.CreateEntity();

            Assert.AreEqual(eA, eB);
            // Entity does not store world reference, so we can't directly check if world a has eb or world b has ea, but we can check existence in each world.
            Assert.IsTrue(_world.EntityManager.Exists(eA));
            Assert.IsTrue(worldB.EntityManager.Exists(eB));
        }
        finally
        {
            worldB.Dispose();
        }
    }

    private struct SharedData : ISharedComponent
    {
        public int value;
    }

    [TestMethod]
    public void TestWorld_Reset_SharedComponents()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddSharedComponent(entity, new SharedData { value = 42 });

        _world.Reset();

        Assert.IsFalse(_world.EntityManager.Exists(entity));
        // Shared components should be cleaned up by Reset (via ComponentManager.Reset)
        // We can't directly check the store, but creating a new entity shouldn't have old shared data.
        var e2 = _world.EntityManager.CreateEntity();
        Assert.IsFalse(_world.EntityManager.HasComponent<SharedData>(e2));
    }
}
