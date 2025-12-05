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
        _world.EntityManager.AddComponent(entity1, new Mesh { index = 1 });

        var queryID = new QueryBuilder().WithAll<Transform>().Build(_world);
        ref var query = ref _world.GetEntityQueryReference(queryID);

        query.ForEach<Transform>((ref t) => 
        {
            t.position = new float3(1, 2, 3);
        });

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
