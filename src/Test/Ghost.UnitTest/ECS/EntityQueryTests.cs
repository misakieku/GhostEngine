using Ghost.Entities;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class EntityQueryTests
{
    private struct CompA : IComponentData { public int value; }
    private struct CompB : IComponentData { public int value; }
    private struct CompC : IComponentData { public int value; }
    private struct Tag : IComponentData { }
    private struct EnableableComp : IEnableableComponent { public int value; }

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
    public void Query_WithAll_FiltersCorrectly()
    {
        var e1 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e1, new CompA { value = 1 });
        _world.EntityManager.AddComponent(e1, new CompB { value = 2 });

        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e2, new CompA { value = 3 });

        var queryID = QueryBuilder.New()
            .WithAll<CompA, CompB>()
            .Build(_world);

        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(1, query.CalculateEntityCount());

        var found = false;
        foreach (var item in query.GetEntityComponentIterator<CompA, CompB>())
        {
            Assert.AreEqual(e1, item.entity);
            Assert.AreEqual(1, item.component0.value);
            Assert.AreEqual(2, item.component1.value);
            found = true;
        }
        Assert.IsTrue(found);
    }

    [TestMethod]
    public void Query_WithAny_FiltersCorrectly()
    {
        var e1 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e1, new CompA());

        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e2, new CompB());

        var e3 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e3, new CompC());

        var queryID = QueryBuilder.New()
            .WithAny<CompA, CompB>()
            .Build(_world);

        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(2, query.CalculateEntityCount());
    }

    [TestMethod]
    public void Query_WithNone_FiltersCorrectly()
    {
        var e1 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e1, new CompA());
        _world.EntityManager.AddComponent(e1, new CompB());

        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e2, new CompA());

        var queryID = QueryBuilder.New()
            .WithAll<CompA>()
            .WithNone<CompB>()
            .Build(_world);

        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(1, query.CalculateEntityCount());
    }

    [TestMethod]
    public void Query_WithEnableable_FiltersOutDisabled()
    {
        var e1 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e1, new EnableableComp { value = 10 });

        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e2, new EnableableComp { value = 20 });
        _world.EntityManager.SetEnabled<EnableableComp>(e2, false);

        var queryID = QueryBuilder.New()
            .WithAll<EnableableComp>()
            .Build(_world);

        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(1, query.CalculateEntityCount(), "Disabled component should be filtered out by default in WithAll.");

        var found = false;
        foreach (var item in query.GetEntityComponentIterator<EnableableComp>())
        {
            Assert.AreEqual(e1, item.entity);
            Assert.AreEqual(10, item.component0.value);
            found = true;
        }
        Assert.IsTrue(found);
    }

    [TestMethod]
    public void Query_WithDisabled_FindsOnlyDisabled()
    {
        var e1 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e1, new EnableableComp { value = 10 });

        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e2, new EnableableComp { value = 20 });
        _world.EntityManager.SetEnabled<EnableableComp>(e2, false);

        var queryID = QueryBuilder.New()
            .WithDisabled<EnableableComp>()
            .Build(_world);

        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(1, query.CalculateEntityCount());

        var found = false;
        foreach (var item in query.GetEntityComponentIterator<EnableableComp>())
        {
            Assert.AreEqual(e2, item.entity);
            Assert.AreEqual(20, item.component0.value);
            found = true;
        }
        Assert.IsTrue(found);
    }

    [TestMethod]
    public void Query_WithPresent_IgnoresEnablement()
    {
        var e1 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e1, new EnableableComp { value = 10 });

        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(e2, new EnableableComp { value = 20 });
        _world.EntityManager.SetEnabled<EnableableComp>(e2, false);

        var queryID = QueryBuilder.New()
            .WithPresent<EnableableComp>()
            .Build(_world);

        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(2, query.CalculateEntityCount(), "WithPresent should ignore enablement state.");
    }

    [TestMethod]
    public void Query_Build_ReturnsCachedID()
    {
        var queryID1 = QueryBuilder.New().WithAll<CompA, CompB>().Build(_world);
        var queryID2 = QueryBuilder.New().WithAll<CompA, CompB>().Build(_world);

        Assert.AreEqual(queryID1, queryID2, "Query with same mask should return same ID.");
    }

    [TestMethod]
    public void Query_CalculateEntityCount_IsCorrectAcrossMultipleArchetypes()
    {
        // Archetype A+B
        for (var i = 0; i < 5; i++)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponent<CompA>(e);
            _world.EntityManager.AddComponent<CompB>(e);
        }

        // Archetype A+C
        for (var i = 0; i < 3; i++)
        {
            var e = _world.EntityManager.CreateEntity();
            _world.EntityManager.AddComponent<CompA>(e);
            _world.EntityManager.AddComponent<CompC>(e);
        }

        var queryID = QueryBuilder.New().WithAll<CompA>().Build(_world);
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        Assert.AreEqual(8, query.CalculateEntityCount());
    }

    [TestMethod]
    public void TestQuery_WithMixedEnableable()
    {
        var e1 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<Tag>(e1);
        _world.EntityManager.AddComponent<EnableableComp>(e1);
        _world.EntityManager.SetEnabled<EnableableComp>(e1, true);

        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<Tag>(e2);
        _world.EntityManager.AddComponent<EnableableComp>(e2);
        _world.EntityManager.SetEnabled<EnableableComp>(e2, false);

        var queryID = QueryBuilder.New()
            .WithAll<Tag>()
            .WithAll<EnableableComp>()
            .Build(_world);

        ref var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(1, query.CalculateEntityCount());
    }

    [TestMethod]
    public void TestQuery_Enableable_WithPresent()
    {
        var e1 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<EnableableComp>(e1);
        _world.EntityManager.SetEnabled<EnableableComp>(e1, true);

        var e2 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent<EnableableComp>(e2);
        _world.EntityManager.SetEnabled<EnableableComp>(e2, false);

        var queryID = QueryBuilder.New()
            .WithPresent<EnableableComp>()
            .Build(_world);

        ref var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);
        Assert.AreEqual(2, query.CalculateEntityCount(), "WithPresent should match both enabled and disabled components.");
    }
}
