using Ghost.Test.Core;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;
using System.Diagnostics;

namespace Ghost.Entities.Test;

internal struct TestChunkQueryJob : IJobChunk
{
    public readonly void Execute(ChunkView view, int threadIndex)
    {
        var transforms = view.GetComponentDataRW<Transform>();
        for (var i = 0; i < view.Count; i++)
        {
            transforms[i].position += new float3(10, 10, 10);
        }
    }
}

internal struct TestEntityQueryJob : IJobEntity<Transform>
{
    public readonly void Execute(Entity entity, ref Transform transform, int index)
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
        var entity1 = _world.EntityManager.CreateEntity(ComponentTypeID<Transform>.value, ComponentTypeID<ManagedEntityRef>.value);
        _world.EntityManager.AddComponent(entity1, new Mesh { index = 1 });
        _world.EntityManager.SetComponent(entity1, new ManagedEntityRef
        {
            entity = _world.EntityManager.CreateManagedEntity()
        });

        var entity2 = _world.EntityManager.CreateEntity(ComponentTypeID<Transform>.value);
        _world.EntityManager.SetComponent(entity2, new Transform { position = new float3(1, 2, 3) });

        var queryID = new QueryBuilder().WithAllRW<Transform>().WithAbsent<Mesh>().Build(_world);
        ref var query = ref _world.GetEntityQueryReference(queryID);

        _world.AdvanceVersion();

        var testJob = new TestEntityQueryJob();
        var handle = query.ScheduleEntityParallel<TestEntityQueryJob, Transform>(testJob, 64, JobHandle.Invalid);
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
            var entities = chunk.GetEntities();

            if (chunk.HasChanged<Transform>(0))
            {
                var bits = chunk.GetEnableBits<Transform>();

                var it = bits.GetIterator();
                while (it.Next(out var index) && index < chunk.Count)
                {
                    Console.WriteLine($"Entity {entities[index]} Updated Position: {transforms[index].position}");
                }
            }
        }

        Debug.Assert(_world.EntityManager.DestroyEntity(entity1) == Core.ErrorStatus.None);
        Debug.Assert(_world.EntityManager.DestroyEntity(entity2) == Core.ErrorStatus.None);
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
