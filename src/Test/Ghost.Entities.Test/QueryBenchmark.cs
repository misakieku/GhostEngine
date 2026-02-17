using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Ghost.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace Ghost.Entities.Test;

internal class GameObject
{
    public Vector4 Position { get; set; }
}

internal struct Position : IComponent
{
    public Vector4 value;
}

[HardwareCounters(HardwareCounter.CacheMisses, HardwareCounter.LlcReference, HardwareCounter.InstructionRetired)]
public class QueryBenchmark
{
    private World _world = null!;
    private Identifier<EntityQuery> _queryIdentifier;

    private GameObject[] _gameObjects = null!;

    private float _dt = Random.Shared.NextSingle();

    [GlobalSetup]
    public void Setup()
    {
        _world = World.Create(entityCapacity: 1_000_000);
        _gameObjects = new GameObject[1_000_000];

        using var scope = AllocationManager.CreateStackScope();
        var componentSet = new ComponentSet(scope.AllocationHandle, ComponentTypeID<Position>.Value);
        _world.EntityManager.CreateEntities(1_000_000, componentSet);

        _queryIdentifier = new QueryBuilder().WithAllRW<Position>().Build(_world);

        for (var i = 0; i < 1_000_000; i++)
        {
            _gameObjects[i] = new GameObject { Position = new Vector4(i, i, i, 0) };
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _world.Dispose();
    }

    [Benchmark]
    public void QueryGameObjects()
    {
        for (var i = 0; i < _gameObjects.Length; i++)
        {
            _gameObjects[i].Position += new Vector4(_dt, _dt, _dt, 0);
        }
    }

    [Benchmark(Baseline = true)]
    public void QueryEntities()
    {
        ref var query = ref _world.ComponentManager.GetEntityQueryReference(_queryIdentifier);
        var vecDT = Vector256.Create(_dt);

        foreach (var chunkView in query.GetChunkIterator())
        {
            var positions = chunkView.GetComponentDataRW<Position>();
            ref var address = ref MemoryMarshal.GetReference(positions);

            for (var i = 0; i < positions.Length; i++)
            {
                Unsafe.Add(ref address, i).value += new Vector4(_dt, _dt, _dt, 0);
            }
        }
    }
}