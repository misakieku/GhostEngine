using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class EntityCommandBufferTests
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
    public void TestECB_CreateEntity()
    {
        using var ecb = new EntityCommandBuffer(1024, AllocationHandle.Persistent);
        ecb.CreateEntity(3);
        ecb.Playback(_world.EntityManager);

        var queryID = QueryBuilder.New().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(3, query.CalculateEntityCount());
    }

    [TestMethod]
    public void TestECB_AddComponent()
    {
        var entity = _world.EntityManager.CreateEntity();

        using var ecb = new EntityCommandBuffer(1024, AllocationHandle.Persistent);
        ecb.AddComponent(entity, new CompA { value = 100 });
        ecb.Playback(_world.EntityManager);

        Assert.IsTrue(_world.EntityManager.HasComponent<CompA>(entity));
        Assert.AreEqual(100, _world.EntityManager.GetComponent<CompA>(entity).value);
    }

    [TestMethod]
    public void TestECB_RemoveComponent()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new CompA { value = 100 });

        using var ecb = new EntityCommandBuffer(1024, AllocationHandle.Persistent);
        ecb.RemoveComponent<CompA>(entity);
        ecb.Playback(_world.EntityManager);

        Assert.IsFalse(_world.EntityManager.HasComponent<CompA>(entity));
    }

    [TestMethod]
    public void TestECB_SetComponent()
    {
        var entity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity, new CompA { value = 100 });

        using var ecb = new EntityCommandBuffer(1024, AllocationHandle.Persistent);
        ecb.SetComponent(entity, new CompA { value = 200 });
        ecb.Playback(_world.EntityManager);

        Assert.AreEqual(200, _world.EntityManager.GetComponent<CompA>(entity).value);
    }

    [TestMethod]
    public void TestECB_DestroyEntity()
    {
        var entity = _world.EntityManager.CreateEntity();

        using var ecb = new EntityCommandBuffer(1024, AllocationHandle.Persistent);
        ecb.DestroyEntity(entity);
        ecb.Playback(_world.EntityManager);

        Assert.IsFalse(_world.EntityManager.Exists(entity));
    }

    [TestMethod]
    public void TestECB_MultipleOperations()
    {
        var entity = _world.EntityManager.CreateEntity();

        using var ecb = new EntityCommandBuffer(1024, AllocationHandle.Persistent);
        ecb.AddComponent(entity, new CompA { value = 10 });
        ecb.AddComponent(entity, new CompB { value = 20 });
        ecb.RemoveComponent<CompA>(entity);

        ecb.Playback(_world.EntityManager);

        Assert.IsFalse(_world.EntityManager.HasComponent<CompA>(entity));
        Assert.IsTrue(_world.EntityManager.HasComponent<CompB>(entity));
        Assert.AreEqual(20, _world.EntityManager.GetComponent<CompB>(entity).value);
    }

    private struct SharedComp : ISharedComponent
    {
        public int value;
    }

    [TestMethod]
    public void TestECB_SharedComponentOperations()
    {
        var entity = _world.EntityManager.CreateEntity();

        using var ecb = new EntityCommandBuffer(1024, AllocationHandle.Persistent);
        ecb.AddSharedComponent(entity, new SharedComp { value = 10 });
        ecb.SetSharedComponent(entity, new SharedComp { value = 20 });
        ecb.Playback(_world.EntityManager);

        Assert.IsTrue(_world.EntityManager.HasComponent<SharedComp>(entity));

        var queryID = QueryBuilder.New().WithAll<SharedComp>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        var found = false;
        foreach (var chunk in query.GetChunkIterator())
        {
            Assert.AreEqual(20, chunk.GetSharedComponent<SharedComp>().value);
            found = true;
        }
        Assert.IsTrue(found);

        ecb.Reset();
        ecb.RemoveSharedComponent<SharedComp>(entity);
        ecb.Playback(_world.EntityManager);

        Assert.IsFalse(_world.EntityManager.HasComponent<SharedComp>(entity));
    }
}
