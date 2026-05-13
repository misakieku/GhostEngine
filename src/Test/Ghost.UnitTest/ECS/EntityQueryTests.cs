using Ghost.Engine.Components;
using Ghost.Entities;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class EntityQueryTests
{
    private struct Enableable : IEnableableComponent
    {
    }

    private World _world = null!;

    [TestInitialize]
    public void Setup()
    {
        AllocationManager.Initialize();
        _world = World.Create(null, 64);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _world.Dispose();
        AllocationManager.Dispose();
    }

    private void CreateDefaultEntities(int count)
    {
        var set = new ComponentSetView([ComponentTypeID<MeshInstance>.Value, ComponentTypeID<LocalToWorld>.Value]);
        _world.EntityManager.CreateEntities(count, set);
    }

    [TestMethod]
    public void ComponentSetCreation()
    {
        using var set1 = new ComponentSet(AllocationHandle.Persistent, ComponentTypeID<MeshInstance>.Value, ComponentTypeID<LocalToWorld>.Value);
        Assert.AreEqual(2, set1.Components.Length);

        using var set2 = set1.With(AllocationHandle.Persistent, ComponentTypeID<Camera>.Value);
        Assert.AreEqual(3, set2.Components.Length);
        Assert.IsTrue(set2.Components.Contains(ComponentTypeID<Camera>.Value));

        using var set3 = set2.Without(AllocationHandle.Persistent, ComponentTypeID<MeshInstance>.Value);
        Assert.AreEqual(2, set3.Components.Length);
        Assert.IsFalse(set3.Components.Contains(ComponentTypeID<MeshInstance>.Value));
    }

    [TestMethod]
    public void SimpleQuery_EntityCountShouldEqual()
    {
        CreateDefaultEntities(100);

        var queryID = QueryBuilder.New()
            .WithAll<LocalToWorld, MeshInstance>()
            .Build(_world);

        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(queryID);

        var i = 0;
        foreach (var (entity, ltw, mesh) in query.GetEntityComponentIterator<LocalToWorld, MeshInstance>())
        {
            i++;
        }

        Assert.AreEqual(100, i);
    }
}
