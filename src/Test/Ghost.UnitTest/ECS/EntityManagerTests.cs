using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class EntityManagerTests
{
    private struct CompA : IComponentData { public int value; }
    private struct CompB : IComponentData { public int value; }

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

    [TestMethod]
    public void TestEntityManager_CreateEntity()
    {
        var entity = _world.EntityManager.CreateEntity();
        Assert.IsTrue(_world.EntityManager.Exists(entity));
    }

    [TestMethod]
    public void TestEntityManager_CreateEntities()
    {
        var entities = new Entity[3];
        _world.EntityManager.CreateEntities(entities);

        foreach (var e in entities)
        {
            Assert.IsTrue(_world.EntityManager.Exists(e));
        }
        Assert.AreEqual(3, _world.EntityManager.EntityCount);
    }

    [TestMethod]
    public void TestEntityManager_AddComponent()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new CompA { value = 42 });

        Assert.IsTrue(_world.EntityManager.HasComponent<CompA>(entity));
        Assert.AreEqual(42, _world.EntityManager.GetComponent<CompA>(entity).value);
    }

    [TestMethod]
    public void TestEntityManager_RemoveComponent()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new CompA { value = 42 });
        Assert.IsTrue(_world.EntityManager.HasComponent<CompA>(entity));

        _world.EntityManager.RemoveComponent<CompA>(entity);
        Assert.IsFalse(_world.EntityManager.HasComponent<CompA>(entity));
    }

    [TestMethod]
    public void TestEntityManager_SetComponent()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new CompA { value = 42 });

        _world.EntityManager.SetComponent(entity, new CompA { value = 84 });
        Assert.AreEqual(84, _world.EntityManager.GetComponent<CompA>(entity).value);
    }

    [TestMethod]
    public void TestEntityManager_DestroyEntity()
    {
        var entity = _world.EntityManager.CreateEntity();
        Assert.IsTrue(_world.EntityManager.Exists(entity));

        _world.EntityManager.DestroyEntity(entity);
        Assert.IsFalse(_world.EntityManager.Exists(entity));
    }

    [TestMethod]
    public void TestEntityManager_MultipleOperations()
    {
        var entity = _world.EntityManager.CreateEntity();

        _world.EntityManager.AddComponent(entity, new CompA { value = 10 });
        _world.EntityManager.AddComponent(entity, new CompB { value = 20 });

        Assert.IsTrue(_world.EntityManager.HasComponent<CompA>(entity));
        Assert.IsTrue(_world.EntityManager.HasComponent<CompB>(entity));

        _world.EntityManager.RemoveComponent<CompA>(entity);

        Assert.IsFalse(_world.EntityManager.HasComponent<CompA>(entity));
        Assert.IsTrue(_world.EntityManager.HasComponent<CompB>(entity));
        Assert.AreEqual(20, _world.EntityManager.GetComponent<CompB>(entity).value);
    }

    [TestMethod]
    public void TestEntityManager_Singleton()
    {
        _world.EntityManager.CreateSingleton(new CompA { value = 99 });

        Assert.AreEqual(99, _world.EntityManager.GetSingleton<CompA>().value);

        _world.EntityManager.GetSingleton<CompA>().value = 100;
        Assert.AreEqual(100, _world.EntityManager.GetSingleton<CompA>().value);
    }

    [TestMethod]
    public void TestEntityManager_MigrateEntity()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new CompA { value = 10 });
        _world.EntityManager.AddComponent(entity, new CompB { value = 20 });

        using var newSet = new ComponentSet(AllocationHandle.Temp, ComponentTypeID<CompB>.Value);
        _world.EntityManager.MigrateEntity(entity, newSet);

        Assert.IsFalse(_world.EntityManager.HasComponent<CompA>(entity));
        Assert.IsTrue(_world.EntityManager.HasComponent<CompB>(entity));
        Assert.AreEqual(20, _world.EntityManager.GetComponent<CompB>(entity).value);
    }
}
