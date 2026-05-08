using Ghost.TestCore;

namespace Ghost.Entities.Test;

internal class SystemTest : ITest
{
    private World _world = null!;

    public void Setup()
    {
        _world = World.Create();
    }

    public void Run()
    {
        var group = _world.SystemManager.GetSystem<DefaultSystemGroup>();
        group.AddSystem<TestSystemB>();
        group.AddSystem<TestSystemA>();

        group.SortSystems();

        _world.SystemManager.InitializeAll(new TimeData());
    }

    public void Cleanup()
    {
        _world.Dispose();
    }
}

internal class TestSystemA : SystemBase
{
    protected override void OnInitialize(ref readonly SystemAPI systemAPI)
    {
        Console.WriteLine("TestSystemA Initialized");
    }
}

[UpdateAfter<TestSystemA>]
internal class TestSystemB : SystemBase
{
    protected override void OnInitialize(ref readonly SystemAPI systemAPI)
    {
        Console.WriteLine("TestSystemB Initialized");
    }
}
