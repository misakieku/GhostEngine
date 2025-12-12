using Ghost.Test.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Entities.Test;

internal struct TestChunkQueryJob : IJobChunk
{
    public readonly void Execute(ChunkView view, int threadIndex)
    {
        var random = new random((uint)threadIndex + 1u);

        var transforms = view.GetComponentDataRW<Transform>();
        for (var i = 0; i < view.Count; i++)
        {
            transforms[i].position += random.NextFloat3(-1f, 1f);
        }
    }
}

internal struct TestEntityQueryJob : IJobEntity<Transform>
{
    public readonly void Execute(Entity entity, ref Transform transform, int threadIndex)
    {
        transform.position += new float3(5, 5, 5);
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
        var entities = (Span<Entity>)stackalloc Entity[1000];
        _world.EntityManager.CreateEntities(entities, ComponentTypeID<Transform>.value);

        var queryID = new QueryBuilder().WithAllRW<Transform>().Build(_world);
        ref var query = ref _world.GetEntityQueryReference(queryID);

        _world.AdvanceVersion();

        var testJob = new TestChunkQueryJob();
        var handle = query.ScheduleChunkParallel<TestChunkQueryJob>(testJob, 64, JobHandle.Invalid);
        _jobScheduler.WaitComplete(handle);

        // _world.EntityManager.AddScriptComponent<TestScriptComponent>(entity1);
        // _world.EntityManager.RemoveComponent<ManagedEntityRef>(entity1); // This should destory the managed entity and call OnDestroy

        // query.ForEach<Transform>((e, ref t) =>
        // {
        //     Console.WriteLine($"Entity {e} Has Position: {t.position}");
        // });
        //
        // foreach (var (entity, transform) in query.GetEntityComponentIterator<Transform>())
        // {
        //     Console.WriteLine($"Entity {entity} Updated Position: {transform.Get().position}");
        // }

        foreach (var chunk in query.GetChunkIterator())
        {
            var transforms = chunk.GetComponentData<Transform>();
            var chunkEntities = chunk.GetEntities();

            // if (chunk.HasChanged<Transform>(0))
            {
                // var bits = chunk.GetEnableBits<Transform>();

                // var it = bits.GetIterator();
                // while (it.Next(out var index) && index < chunk.Count)
                for (var index = 0; index < chunk.Count; index++)
                {
                    Console.WriteLine($"Entity {chunkEntities[index]} Updated Position: {transforms[index].position}");
                }
            }
        }

        _world.EntityManager.DestroyEntities(entities);
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

public class TestScriptComponent : ScriptComponent
{
    public override void OnCreate()
    {
        Console.WriteLine($"TestScriptComponent OnCreate called for Entity {Entity}");
        ref var transform = ref GetComponent<Transform>();
        transform.position += new float3(0, 1, 0);
    }

    public override void OnDestroy()
    {
        Console.WriteLine($"TestScriptComponent OnDestroy called for Entity {Entity}");
    }
}
