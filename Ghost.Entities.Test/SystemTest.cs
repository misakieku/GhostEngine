using Ghost.Test.Core;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Entities.Test;

internal class SystemTest : ITest
{
    private JobScheduler _jobScheduler = null!;
    private World _world = null!;

    public void Setup()
    {
        _jobScheduler = new JobScheduler(4);
        _world = World.Create(_jobScheduler);
    }

    public void Run()              
    {
        var group = _world.SystemManager.GetSystem<DefaultSystemGroup>();
        group.AddSystem<TestSystemB>();
        group.AddSystem<TestSystemA>();

        group.SortSystems();

        var api = new SystemAPI();
        _world.SystemManager.InitializeAll(in api);
    }

    public void Cleanup()
    {
        _world.Dispose();
        _jobScheduler.Dispose();
        JobScheduler.ReleaseTempAllocator();
    }
}

internal class TestSystemA : SystemBase
{
    protected override void OnInitialize(ref readonly SystemAPI systemAPI)
    {
        Console.WriteLine("TestSystemA Initialized");
    }
}

[UpdateAfter(typeof(TestSystemA))]
internal class TestSystemB : SystemBase
{
    protected override void OnInitialize(ref readonly SystemAPI systemAPI)
    {
        Console.WriteLine("TestSystemB Initialized");
    }
}
