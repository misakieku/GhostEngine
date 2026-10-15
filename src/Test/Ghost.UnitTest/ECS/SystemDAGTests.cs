using Ghost.Core;
using Ghost.Engine.Systems;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;

namespace Ghost.UnitTest.ECS;

[TestClass]
[DoNotParallelize]
public class SystemDAGTests
{
    private struct TestData : IComponentData
    {
        public int Value;
    }

    private static volatile int s_stage;
    private static volatile bool s_parallelRanA;
    private static volatile bool s_parallelRanB;
    private static volatile int s_chunkSum;

    private JobScheduler _scheduler = null!;
    private World _world = null!;

    [TestInitialize]
    public void Setup()
    {
        s_stage = 0;
        s_parallelRanA = false;
        s_parallelRanB = false;
        s_chunkSum = 0;

        var desc = new JobSchedulerDesc
        {
            ThreadCount = 4,
            DependencyChainCapacity = 128,
            ThreadPriority = ThreadPriority.Normal,
        };
        _scheduler = new JobScheduler(in desc);
        _world = World.Create(_scheduler, 128);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _world.Dispose();
        _scheduler.Dispose();
    }

    private class Step1System : SystemBase
    {
        protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
        {
            s_stage = 1;
        }
    }

    [UpdateAfter<Step1System>]
    private class Step2System : SystemBase
    {
        protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
        {
            if (s_stage == 1)
            {
                s_stage = 2;
            }
        }
    }

    [MainThread]
    [UpdateAfter<Step2System>]
    private class Step3MainThreadSystem : SystemBase
    {
        protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
        {
            if (s_stage == 2)
            {
                s_stage = 3;
            }
        }
    }

    private class TestSequentialGroup : SystemGroup
    {
        public TestSequentialGroup()
        {
            AddSystem<Step3MainThreadSystem>();
            AddSystem<Step1System>();
            AddSystem<Step2System>();
            SortSystems();
        }
    }

    [TestMethod]
    public void TestSystemGroup_TopologicalDAGExecution()
    {
        var group = new TestSequentialGroup();
        var api = new SystemAPI
        {
            Time = default,
            World = _world,
        };

        group.Initialize(in api);
        group.Update(in api);
        group.Cleanup(in api);

        Assert.AreEqual(3, s_stage);
    }

    private class ParallelSystemA : SystemBase
    {
        protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
        {
            Thread.Sleep(10);
            s_parallelRanA = true;
        }
    }

    private class ParallelSystemB : SystemBase
    {
        protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
        {
            Thread.Sleep(10);
            s_parallelRanB = true;
        }
    }

    private class TestParallelGroup : SystemGroup
    {
        public TestParallelGroup()
        {
            AddSystem<ParallelSystemA>();
            AddSystem<ParallelSystemB>();
            SortSystems();
        }
    }

    [TestMethod]
    public void TestSystemGroup_ParallelExecution()
    {
        var group = new TestParallelGroup();
        var api = new SystemAPI
        {
            Time = default,
            World = _world,
        };

        group.Initialize(in api);
        group.Update(in api);
        group.Cleanup(in api);

        Assert.IsTrue(s_parallelRanA);
        Assert.IsTrue(s_parallelRanB);
    }

    private struct AddChunkJob : IJobChunk
    {
        public void Execute(ChunkView chunk, ref readonly JobExecutionContext ctx)
        {
            var data = chunk.GetComponentData<TestData>();
            for (var i = 0; i < chunk.EntityCount; i++)
            {
                Interlocked.Add(ref s_chunkSum, data[i].Value);
            }
        }
    }

    private class ParentChunkSystem : SystemBase
    {
        private Identifier<EntityQuery> _query;

        protected override void OnInitialize(scoped in SystemAPI systemAPI)
        {
            _query = QueryBuilder.New().WithAll<TestData>().Build(systemAPI.World);
        }

        protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
        {
            ref var query = ref systemAPI.World.ComponentManager.GetEntityQueryReference(_query);
            var job = new AddChunkJob();
            // Schedule attached to SelfJobHandle without calling Wait()
            query.ScheduleChunkParallel(job, 1, parentJob: systemAPI.SelfJobHandle, forceSchedule: true);
        }
    }

    [MainThread]
    [UpdateAfter<ParentChunkSystem>]
    private class VerifyChunkSystem : SystemBase
    {
        public int ObservedSum;

        protected override void OnUpdate(scoped in SystemUpdateAPI systemAPI)
        {
            ObservedSum = s_chunkSum;
        }
    }

    private class SystemGroupWrapper : SystemGroup
    {
        public VerifyChunkSystem Verifier = new VerifyChunkSystem();

        public SystemGroupWrapper()
        {
            AddSystem<ParentChunkSystem>();
            AddSystem(Verifier);
            SortSystems();
        }
    }

    [TestMethod]
    public void TestSystemGroup_AttachedChildJobs()
    {
        using var scope = AllocationManager.CreateStackScope();
        using var set = new ComponentSet(scope.AllocationHandle, ComponentTypeID<TestData>.Value);

        var entities = new Entity[10];
        _world.EntityManager.CreateEntities(entities, set);
        for (var i = 0; i < entities.Length; i++)
        {
            _world.EntityManager.SetComponent(entities[i], new TestData { Value = 5 });
        }

        var group = new SystemGroupWrapper();
        var api = new SystemAPI
        {
            Time = default,
            World = _world,
        };

        group.Initialize(in api);
        group.Update(in api);
        group.Cleanup(in api);

        // 10 entities * 5 = 50. Verifier must observe 50 because it ran after ParentChunkSystem completed its attached chunks!
        Assert.AreEqual(50, s_chunkSum);
        Assert.AreEqual(50, group.Verifier.ObservedSum);
    }

    [TestMethod]
    public void TestRenderSystemGroup_SortsWithoutCycles()
    {
        var group = new RenderSystemGroup();
        Assert.IsNotNull(group);
    }

    [TestMethod]
    public void TestSystemGroup_StressLoop()
    {
        for (var loop = 0; loop < 1000; loop++)
        {
            var group = new TestParallelGroup();
            var api = new SystemAPI
            {
                Time = default,
                World = _world,
            };

            group.Initialize(in api);
            group.Update(in api);
            group.Cleanup(in api);
        }
    }
}
