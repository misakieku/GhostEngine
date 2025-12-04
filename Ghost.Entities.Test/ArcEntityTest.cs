using Ghost.Test.Core;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Entities.Test;

public partial class ArcEntityTest : ITest
{
    private World _world = null!;

    public void Setup()
    {
        _world = World.Create();
    }

    public void Run()
    {
        var entity1 = _world.EntityManager.CreateEntity(ComponentTypeID<Transform>.value);
        Console.WriteLine(entity1);
        _world.EntityManager.AddComponent<Mesh>(entity1, new Mesh { index = 1 });

        var queryID = new QueryBuilder().WithAll<Transform>().Build(_world);
        ref var query = ref _world.GetEntityQueryReference(queryID);

        foreach (var chunk in query.GetChunkIterator())
        {
            var transforms = chunk.GetComponentData<Transform>();
            var entities = chunk.GetEntities();

            for (var i = 0; i < chunk.Count; i++)
            {
                Console.WriteLine($"Entity {entities[i]} Position: {transforms[i].position}");
                transforms[i].position = new float3(1, 2, 3);
            }
        }

        query.ForEach<Transform>((e, ref t) => 
        {
            Console.WriteLine($"Entity {e} Updated Position: {t.position}");
        });
    }

    public void Cleanup()
    {
        _world.Dispose();
    }
}

public struct Transform : IComponent
{
    public float3 position;
}

public struct Mesh : IComponent
{
    public int index;
}
