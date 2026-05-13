using Ghost.Entities;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class WorldTests
{
    [TestMethod]
    public void CreateWorld()
    {
        using var world = World.Create();
        Assert.IsNotNull(world);
    }

    [TestMethod]
    public void AddEntityThenClearWorld()
    {
        using var world =  World.Create();
        Assert.IsNotNull(world);

        world.EntityManager.CreateEntity();
        Assert.AreEqual(1, world.EntityManager.EntityCount);

        world.Clear(default);
        Assert.AreEqual(0, world.EntityManager.EntityCount);
    }
}
