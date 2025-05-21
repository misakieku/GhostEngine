using Ghost.Entities;
using System.Numerics;

var world = new World();

var entity1 = world.CreateEntity();
var entity2 = world.CreateEntity();
var entity3 = world.CreateEntity();

world.AddComponent(entity1, new Transform { position = new Vector3(1, 2, 3) });
world.AddComponent(entity1, new Mesh { index = 42 });
world.AddComponent(entity2, new Transform { position = new Vector3(4, 5, 6) });
world.AddComponent(entity2, new Mesh { index = 43 });
world.AddComponent(entity3, new Transform { position = new Vector3(7, 8, 9) });

world.Query<Transform>((Entity entity, ref Transform transform) =>
{
    transform.position += new Vector3(1, 1, 1);
});

world.Query<Mesh>((Entity entity, ref Mesh mesh) =>
{
    mesh.index += 1;
});

world.RemoveEntity(entity2);
var entity4 = world.CreateEntity();
world.AddComponent(entity4, new Transform { position = new Vector3(10, 11, 12) });
world.AddComponent(entity4, new Mesh { index = 44 });

world.Query<Transform, Mesh>((Entity entity, ref Transform transform, ref Mesh mesh) =>
{
    Console.WriteLine($"Entity {entity.ID}: Transform Position = {transform.position}, Mesh Index = {mesh.index}");
});

world.Dispose();

public struct Transform : IComponent
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

public struct Mesh : IComponent
{
    public uint index;
}