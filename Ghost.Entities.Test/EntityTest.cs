using Ghost.Test.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Entities.Test;

internal struct TestEntityQueryJob : IJobChunk
{
    public void Execute(ChunkView view, int threadIndex)
    {
        var transforms = view.GetComponentData<Transform>();
        for (var i = 0; i < view.Count; i++)
        {
            transforms[i].position += new float3(10, 10, 10);
        }
    }
}

public partial class EntityTest : ITest
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
        var entity1 = _world.EntityManager.CreateEntity(ComponentTypeID<Transform>.value);
        _world.EntityManager.AddComponent(entity1, new Mesh { index = 1 });

        var entity2 = _world.EntityManager.CreateEntity(ComponentTypeID<Transform>.value);
        _world.EntityManager.SetComponent(entity2, new Transform { position = new float3(1, 2, 3) });

        var queryID = new QueryBuilder().WithAll<Transform>().Build(_world);
        ref var query = ref _world.GetEntityQueryReference(queryID);

        var testJob = new TestEntityQueryJob();
        var handle = query.ScheduleChunkParallel(testJob, 64, JobHandle.Invalid);
        _jobScheduler.WaitComplete(handle);

        query.ForEach<Transform>((e, ref t) =>
        {
            Console.WriteLine($"Entity {e} Has Position: {t.position}");
        });

        foreach (var (entity, transform) in query.GetEntityComponentIterator<Transform>())
        {
            Console.WriteLine($"Entity {entity} Updated Position: {transform.Get().position}");
        }

        foreach (var chunk in query.GetChunkIterator())
        {
            var transforms = chunk.GetComponentData<Transform>();
            var entities = chunk.GetEntities();
            var bits = chunk.GetEnableBits<Transform>();

            var it = bits.GetIterator();
            while (it.Next(out var index) && index < chunk.Count)
            {
                Console.WriteLine($"Entity {entities[index]} Updated Position: {transforms[index].position}");
            }
        }
    }

    public void Cleanup()
    {
        _world.Dispose();
        _jobScheduler.Dispose();
        JobScheduler.ReleaseTempAllocator();
    }
}

public struct Transform : IEnableableComponent
{
    public float3 position;
}

public struct Mesh : IComponent
{
    public int index;
}
