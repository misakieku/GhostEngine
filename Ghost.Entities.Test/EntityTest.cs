using Ghost.Entities.Components;
using Ghost.Entities.Query;
using Ghost.Entities.Systems;
using Ghost.Test.Core;
using System.Numerics;

namespace Ghost.Entities.Test;

public partial class EntityTest : ITest
{
    private World _world = null!;

    public void Setup()
    {
        _world = World.Create();
    }

    public void Run()
    {
        var entity1 = _world.EntityManager.CreateEntity();
        var entity2 = _world.EntityManager.CreateEntity();
        var entity3 = _world.EntityManager.CreateEntity();

        _world.EntityManager.AddComponent(entity1, new Transform { position = new Vector3(1, 2, 3) });
        _world.EntityManager.AddComponent(entity1, new Mesh { index = 42 });
        _world.EntityManager.AddScript<UIManager>(entity1);
        _world.EntityManager.AddScript<EventManager>(entity1);

        _world.EntityManager.AddComponent(entity2, new Transform { position = new Vector3(4, 5, 6) });
        _world.EntityManager.AddComponent(entity2, new Mesh { index = 43 });
        _world.EntityManager.AddScript<UserScript>(entity2);

        _world.EntityManager.AddComponent(entity3, new Transform { position = new Vector3(7, 8, 9) });
        _world.EntityManager.AddScript<EventManager>(entity3);

        foreach (var (_, transform) in _world.Query<Transform>())
        {
            transform.ValueRW.position += new Vector3(1, 1, 1);
        }

        var filter = new QueryBuilder()
            .WithAll<Mesh>()
            .Build();

        foreach (var (_, mesh) in _world.QueryFilter<Mesh>(in filter))
        {
            mesh.ValueRW.index += 1;
        }

        _world.EntityManager.RemoveEntity(ref entity2);

        var entity4 = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(entity4, new Transform { position = new Vector3(10, 11, 12) });
        _world.EntityManager.AddComponent(entity4, new Mesh { index = 45 });
        _world.EntityManager.AddScript<UserScript>(entity4);

        _world.SystemStorage.AddSystem<TestSystem2>();
        _world.SystemStorage.AddSystem<TestSystem>();

        _world.SystemStorage.CreateSystems();
        _world.SystemStorage.UpdateSystems();
    }

    public void Cleanup()
    {
        _world.Dispose();
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
            Console.WriteLine($"Entity {entity}: Transform Position = {transform.ValueRO.position}");
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
            Console.WriteLine($"Entity {entity}: Mesh Index = {mesh.ValueRO.index}");
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

    public override void OnEnable()
    {
        Console.WriteLine("UserScript enabled for entity: " + Owner);
        EntityManager.GetComponent<Transform>(Owner).ValueRW.position += new Vector3(10, 10, 10);
    }

    override public void OnDisable()
    {
        Console.WriteLine("UserScript disabled for entity: " + Owner);
    }

    public override void Initialize()
    {
        Console.WriteLine("UserScript initialized for entity: " + Owner);
    }

    public override void Start()
    {
        Console.WriteLine("UserScript started for entity: " + Owner);
    }

    public override void Update()
    {
        Console.WriteLine("UserScript updating for entity: " + Owner);
    }

    public override void OnDestroy()
    {
        Console.WriteLine("UserScript destroyed for entity: " + Owner);
    }
}

public class UIManager : ScriptComponent
{
    public override void Start()
    {
        Console.WriteLine("UIManager started for entity: " + Owner);
    }

    public override void Update()
    {
        Console.WriteLine("UIManager updating for entity: " + Owner);
    }

    public override void OnDestroy()
    {
        Console.WriteLine("UIManager destroyed for entity: " + Owner);
    }
}

public class EventManager : ScriptComponent
{
    public override void Start()
    {
        Console.WriteLine("EventManager started for entity: " + Owner);
    }

    public override void Update()
    {
        Console.WriteLine("EventManager updating for entity: " + Owner);
    }

    public override void OnDestroy()
    {
        Console.WriteLine("EventManager destroyed for entity: " + Owner);
    }
}