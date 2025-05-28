using Ghost.Entities;
using Ghost.Entities.Helpers;
using System.Numerics;

var t = new Test();
t.Run();

public partial class Test
{
    public void Run()
    {
        var world = World.Create();

        var entity1 = world.EntityManager.CreateEntity();
        var entity2 = world.EntityManager.CreateEntity();
        var entity3 = world.EntityManager.CreateEntity();

        entity1.AddComponent(new Transform { position = new Vector3(1, 2, 3) });
        entity1.AddComponent(new Mesh { index = 42 });
        entity1.AddScript<UIManager>();
        entity1.AddScript<EventManager>();

        entity2.AddComponent(new Transform { position = new Vector3(4, 5, 6) });
        entity2.AddComponent(new Mesh { index = 43 });
        entity2.AddScript<UserScript>();

        entity3.AddComponent(new Transform { position = new Vector3(7, 8, 9) });
        entity3.AddScript<EventManager>();

        foreach (var (_, transform) in world.Query<Transform>())
        {
            transform.ValueRW.position += new Vector3(1, 1, 1);
        }

        foreach (var (_, mesh) in world.Query<Mesh>())
        {
            mesh.ValueRW.index += 1;
        }

        world.EntityManager.RemoveEntity(ref entity2);

        var entity4 = world.EntityManager.CreateEntity();
        entity4.AddComponent(new Transform { position = new Vector3(10, 11, 12) });
        entity4.AddComponent(new Mesh { index = 44 });
        entity4.AddScript<UserScript>();

        world.AddSystem<TestSystem>();
        world._systemStorage.UpdateSystems();

        //world.SystemStorage.RebuildExecutionList();
        //world.ComponentStorage.RebuildExecutionList();

        //Console.WriteLine();
        //Console.WriteLine("Starting scripts...");
        //foreach (var component in world.QueryScript())
        //{
        //    if (!component.Enable)
        //    {
        //        continue;
        //    }
        //    component.Start();
        //}

        //Console.WriteLine();
        //Console.WriteLine("Updating scripts...");
        //foreach (var component in world.QueryScript())
        //{
        //    if (!component.Enable)
        //    {
        //        continue;
        //    }
        //    component.Update();
        //}

        //Console.WriteLine();
        //Console.WriteLine("LateUpdating scripts...");
        //foreach (var component in world.QueryScript())
        //{
        //    if (!component.Enable)
        //    {
        //        continue;
        //    }
        //    component.LateUpdate();
        //}

        world.Dispose();
    }
}

public class TestSystem : SystemBase
{
    public override void OnUpdate()
    {
        foreach (var (entity, transform) in World.Query<Transform>().WithAny<Mesh>())
        {
            Console.WriteLine($"Entity {entity.ID}: Transform Position = {transform.ValueRO.position}");
        }
    }
}

public struct Transform : IComponentData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

public struct Mesh : IComponentData
{
    public uint index;
}

public class UserScript : ScriptComponent
{
    public override int ExecutionOrder => -1;

    public override void Start()
    {
        Console.WriteLine("UserScript started for entity: " + Owner.ID);
    }

    public override void Update()
    {
        Console.WriteLine("UserScript updating for entity: " + Owner.ID);
    }

    public override void OnDestroy()
    {
        Console.WriteLine("UserScript destroyed for entity: " + Owner.ID);
    }
}

public class UIManager : ScriptComponent
{
    public override void Start()
    {
        Console.WriteLine("UIManager started for entity: " + Owner.ID);
    }

    public override void Update()
    {
        Console.WriteLine("UIManager updating for entity: " + Owner.ID);
    }

    public override void OnDestroy()
    {
        Console.WriteLine("UIManager destroyed for entity: " + Owner.ID);
    }
}

public class EventManager : ScriptComponent
{
    public override void Start()
    {
        Console.WriteLine("EventManager started for entity: " + Owner.ID);
    }

    public override void Update()
    {
        Console.WriteLine("EventManager updating for entity: " + Owner.ID);
    }

    public override void OnDestroy()
    {
        Console.WriteLine("EventManager destroyed for entity: " + Owner.ID);
    }
}