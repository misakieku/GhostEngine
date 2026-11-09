using BenchmarkDotNet.Attributes;
using Ghost.Core;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Ghost.UnitTest.Benchmarks;

[MemoryDiagnoser]
public class EntitiesSystemWithTwoComponents
{
    private struct CompA : IComponentData
    {
        public int value;
    }

    public struct CompB : IComponentData
    {
        public int value;
    }

    private struct Job : IJobChunk
    {
        public readonly void Execute(ChunkView chunk, ref readonly JobExecutionContext ctx)
        {
            var a = chunk.GetComponentDataRW<CompA>();
            var b = chunk.GetComponentData<CompB>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                a[i].value += b[i].value;
            }
        }
    }

    private const int COUNT = 100_000;

    private JobScheduler _jobScheduler = null!;
    private World _world = null!;
    private Identifier<EntityQuery> _query;

    [GlobalSetup]
    public void Setup()
    {
        AllocationManager.Initialize();

        _jobScheduler = new JobScheduler(new JobSchedulerDesc { DependencyChainCapacity = 64, ThreadCount = Environment.ProcessorCount - 1 });
        _world = World.Create(_jobScheduler, COUNT);

        _world.EntityManager.CreateEntities(new CompA { value = 0 }, new CompB { value = 1 }, COUNT);
        _query = QueryBuilder.New()
            .WithAllRW<CompA>()
            .WithAll<CompB>()
            .Build(_world);
    }

    [GlobalCleanup]
    public void CleanUp()
    {
        _world.Dispose();
        _jobScheduler.Dispose();

        TempJobAllocatorManager.Dispose();
        AllocationManager.Dispose();
    }

    [Benchmark]
    public void Query()
    {
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(_query);
        foreach (var chunk in query.GetChunkIterator())
        {
            var a = chunk.GetComponentDataRW<CompA>();
            var b = chunk.GetComponentData<CompB>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                a[i].value += b[i].value;
            }
        }
    }

    [Benchmark]
    public void Query_Simd()
    {
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(_query);
        foreach (var chunk in query.GetChunkIterator())
        {
            var len = chunk.EntityCount - (chunk.EntityCount & 7);

            var a = chunk.GetComponentDataRW<CompA>();
            var b = chunk.GetComponentData<CompB>();

            var va = MemoryMarshal.Cast<CompA, Vector<int>>(a.Slice(0, len));
            var vb = MemoryMarshal.Cast<CompB, Vector<int>>(b.Slice(0, len));

            for (var i = 0; i < va.Length; i++)
            {
                va[i] += vb[i];
            }

            for (var i = len; i < chunk.EntityCount; i++)
            {
                a[i].value += b[i].value;
            }
        }
    }

    [Benchmark]
    public void Query_Job()
    {
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(_query);
        var handle = query.ScheduleChunkParallel(new Job(), 1);
        _world.JobScheduler.Wait(handle, waitThreshold: -1);

        TempJobAllocatorManager.AdvanceFrame();
    }
}
