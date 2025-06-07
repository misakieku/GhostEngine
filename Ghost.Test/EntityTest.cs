using Ghost.Entities;
using Ghost.Entities.Components;
using Ghost.Entities.Systems;
using Ghost.Test.TestFramework;
using System.Numerics;

namespace Ghost.Test;

public partial class EntityTest : ITest
{
    public void Run()
    {
        var world = World.Create();

        var entity1 = world.EntityManager.CreateEntity();
        var entity2 = world.EntityManager.CreateEntity();
        var entity3 = world.EntityManager.CreateEntity();

        world.EntityManager.AddComponent(entity1, new Transform { position = new Vector3(1, 2, 3) });
        world.EntityManager.AddComponent(entity1, new Mesh { index = 42 });
        world.EntityManager.AddScript<UIManager>(entity1);
        world.EntityManager.AddScript<EventManager>(entity1);

        world.EntityManager.AddComponent(entity2, new Transform { position = new Vector3(4, 5, 6) });
        world.EntityManager.AddComponent(entity2, new Mesh { index = 43 });
        world.EntityManager.AddScript<UserScript>(entity2);

        world.EntityManager.AddComponent(entity3, new Transform { position = new Vector3(7, 8, 9) });
        world.EntityManager.AddScript<EventManager>(entity3);

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
        world.EntityManager.AddComponent(entity4, new Transform { position = new Vector3(10, 11, 12) });
        world.EntityManager.AddComponent(entity4, new Mesh { index = 44 });
        world.EntityManager.AddScript<UserScript>(entity4);

        world.SystemStorage.AddSystem<TestSystem2>();
        world.SystemStorage.AddSystem<TestSystem>();
        world.SystemStorage.CreateSystems();
        world.SystemStorage.UpdateSystems();

        world.Dispose();
    }
}

public class TestSystem : ISystem
{
    public void OnCreate(in SystemState state)
    {
    }

    public void OnUpdate(in SystemState state)
    {
        foreach (var (entity, transform) in state.World.Query<Transform>())
        {
            Console.WriteLine($"Entity {entity.ID}: Transform Position = {transform.ValueRO.position}");
        }
    }

    public void OnDestroy(in SystemState state)
    {
    }
}

[DependsOn(typeof(TestSystem))]
public class TestSystem2 : ISystem
{
    public void OnCreate(in SystemState state)
    {
    }

    public void OnUpdate(in SystemState state)
    {
        foreach (var (entity, mesh) in state.World.Query<Mesh>())
        {
            Console.WriteLine($"Entity {entity.ID}: Mesh Index = {mesh.ValueRO.index}");
        }
    }

    public void OnDestroy(in SystemState state)
    {
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