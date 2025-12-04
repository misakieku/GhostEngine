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
        var entity1 = _world.EntityManager.CreateEntity(ComponentTypeID<TransformData>.value);
        var mesh = new MeshData { index = 1 };
        _world.EntityManager.AddComponent<MeshData>(entity1, ref mesh);

        var queryID = new QueryBuilder().WithAll<TransformData>().Build(_world);
        ref var query = ref _world.GetEntityQueryReference(queryID);

        foreach (var item in query)
        {
            var transforms = item.GetComponentData<TransformData>();
            Console.WriteLine($"Item Count: {item.Count}");
            for (var i = 0; i < item.Count; i++)
            {
                Console.WriteLine($"Entity Position: {transforms[i].position}");
                transforms[i].position = new float3(1, 2, 3);
                Console.WriteLine($"Updated Entity Position: {transforms[i].position}");
            }
        }
    }

    public void Cleanup()
    {
        _world.Dispose();
    }
}

public struct TransformData : IComponent
{
    public float3 position;
}

public struct MeshData : IComponent
{
    public int index;
}
