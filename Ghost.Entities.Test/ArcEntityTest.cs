using Ghost.Test.Core;
using Ghost.ArcEntities;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Entities.Test;

public partial class ArcEntityTest : ITest
{
    private Ghost.ArcEntities.World _world = null!;

    public void Setup()
    {
        _world = Ghost.ArcEntities.World.Create();
    }

    public void Run()
    {
        var entity1 = _world.EntityManager.CreateEntity(ComponentTypeID<TransformData>.value);
        Console.WriteLine($"{entity1} Has Transform: {_world.EntityManager.HasComponent<TransformData>(entity1)}");
        var mesh = new MeshData { index = 1 };
        _world.EntityManager.AddComponent<MeshData>(entity1, ref mesh);
        Console.WriteLine($"{entity1} Has Mesh: {_world.EntityManager.HasComponent<MeshData>(entity1)}");

        _world.EntityManager.DestoryEntity(entity1);
        Console.WriteLine($"{entity1} Has Transform: {_world.EntityManager.HasComponent<TransformData>(entity1)}");
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
