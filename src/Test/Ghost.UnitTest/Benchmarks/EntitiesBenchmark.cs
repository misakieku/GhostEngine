//#define TEST_QUERY

using BenchmarkDotNet.Attributes;
using Ghost.Core;
using Ghost.Engine.Components;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Utilities;
using System.Runtime.CompilerServices;

namespace Ghost.UnitTest.Benchmarks;

public class EntitiesBenchmark
{
    private struct VelocityComponent : IComponentData
    {
        public float3 delta;
    }

    private class ScriptComponent
    {
        public GameObject GameObject
        {
            get; internal set;
        } = null!;

        public virtual void Update()
        {
        }
    }

    private class VelocityScriptComponent : ScriptComponent
    {
        private float3 _delta = new float3(1, 2, 3);

        public override void Update()
        {
            GameObject.transform.c3.xyz += _delta;
        }
    }

    private class GameObject
    {
        private Dictionary<nint, ScriptComponent> _components = new Dictionary<nint, ScriptComponent>(1);
        public float4x4 transform;

        public IReadOnlyCollection<ScriptComponent> Components => _components.Values;

        public void AddComponent<T>()
            where T : ScriptComponent, new()
        {
            var comp = new T
            {
                GameObject = this
            };

            AddComponent(comp);
        }

        public void AddComponent<T>(T comp)
            where T : ScriptComponent
        {
            _components[typeof(T).TypeHandle.Value] = comp;
        }

        public T GetComponent<T>()
            where T : ScriptComponent
        {
            return (T)_components[typeof(T).TypeHandle.Value];
        }
    }

    private const int COUNT = 100_000;

    private JobScheduler _jobScheduler = null!;
    private World _world = null!;

    private GameObject[] _gameObjects = null!;
    private Entity[] _entities = null!;
#if TEST_QUERY
    private Identifier<EntityQuery> _query;
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CreateGameObjects_Core(int start, int length, Span<GameObject> dst)
    {
        for (var i = start; i < start + length; i++)
        {
            var gameObject = new GameObject
            {
                transform = float4x4.identity
            };

            gameObject.AddComponent<VelocityScriptComponent>();
            dst[i] = gameObject;
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        AllocationManager.Initialize();

        _gameObjects = new GameObject[COUNT];
        _entities = new Entity[COUNT];

#if TEST_QUERY
        // Split to simulate object creation in different phases. Real game scenarioa will be even worse.
        CreateGameObjects_Core(0, COUNT / 2, _gameObjects);
#endif

        _jobScheduler = new JobScheduler(new JobSchedulerDesc { DependencyChainCapacity = 64, ThreadCount = 1 });
        _world = World.Create(_jobScheduler, COUNT);

#if TEST_QUERY
        _world.EntityManager.CreateEntities(_entities, new VelocityComponent { delta = new float3(1, 2, 3) }, new LocalToWorld { matrix = float4x4.identity });

        CreateGameObjects_Core(COUNT / 2, COUNT/ 2, _gameObjects);

        _query = QueryBuilder.New()
            .WithAllRW<LocalToWorld>()
            .WithAll<VelocityComponent>()
            .Build(_world);
#endif
    }

    [GlobalCleanup]
    public void CleanUp()
    {
#if TEST_QUERY
        _world.EntityManager.DestroyEntities(_entities);
#endif
        _world.Dispose();
        _jobScheduler.Dispose();

        AllocationManager.Dispose();
    }

#if TEST_QUERY

    [Benchmark]
    public void GameObjectQuery()
    {
        foreach (var obj in _gameObjects)
        {
            foreach (var comp in obj.Components)
            {
                comp.Update();
            }
        }
    }

    [Benchmark]
    public void EntitiesQuery()
    {
        ref readonly var query = ref _world.ComponentManager.GetEntityQueryReference(_query);
        foreach (var chunk in query.GetChunkIterator())
        {
            var ltws = chunk.GetComponentDataRW<LocalToWorld>();
            var vels = chunk.GetComponentData<VelocityComponent>();

            for (var i = 0; i < chunk.EntityCount; i++)
            {
                ltws.GetElementUnsafe(i).matrix.c3.xyz += vels.GetElementUnsafe(i).delta;
            }
        }
    }
#else

    [Benchmark]
    public void CreateGameObjects()
    {
        CreateGameObjects_Core(0, COUNT, _gameObjects);
    }

    [Benchmark]
    public void CreateEntities()
    {
        _world.EntityManager.CreateEntities<VelocityComponent, LocalToWorld>(_entities);

        // We must destory thoes entities right here because iteration cleanup in benchmark dotnet is slow and cause gc (add noise to the test method).
        // It’s unfair to ecs but we must do it.
        _world.EntityManager.DestroyEntities(_entities);
    }
#endif
}
