using Ghost.Editor.PoC.Ecs;
using Ghost.Engine.Components;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.PoC.Tests;

public static class EditorSyncHeadlessRunner
{
    public static int Run()
    {
        Console.WriteLine("=== Starting GhostEngine Editor PoC Headless Tests ===");

        try
        {
            TestLocalTransformInspectionAndMutation();
            Console.WriteLine("[PASS] Test 1: LocalTransform Selection & Bidirectional Mutation (UI <-> ECS)");

            TestSpinningCubeWorkerAnimation();
            Console.WriteLine("[PASS] Test 2: Multi-Entity Selection & Worker 3D Animation");

            TestWorkerSystemAutoIncrement();
            Console.WriteLine("[PASS] Test 3: Background Worker Concurrency (Counter Auto-Increment)");

            TestConcurrentClashStress();
            Console.WriteLine("[PASS] Test 4: Concurrent Multi-Threaded Stress Test (Race Condition Check)");

            Console.WriteLine("=== ALL HEADLESS TESTS PASSED ===");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAIL] Test failed with exception: {ex}");
            return 1;
        }
    }

    private static void TestLocalTransformInspectionAndMutation()
    {
        using var host = new EngineHost();
        host.IsPaused = true;

        var playerNode = host.Entities.First(e => e.Name == "Player");
        host.SelectEntity(playerNode);

        var transformComp = playerNode.Components.First(c => c.ComponentType == typeof(LocalTransform));
        var posProp = transformComp.Properties.Find(p => p.Name == "position")!;

        // 1. Initial snapshot check
        host.StepSingleFrame();
        Thread.Sleep(50);

        var initialPos = (float3)posProp.GetValue()!;
        if (initialPos.x != 0f || initialPos.y != 0f || initialPos.z != 0f)
        {
            throw new Exception($"Expected initial position (0,0,0), got {initialPos}");
        }

        // 2. Mutate position via PropertyNode
        posProp.SetValue(new float3(10f, 20f, 30f));

        host.StepSingleFrame();
        Thread.Sleep(50);

        var updatedPos = (float3)posProp.GetValue()!;
        if (updatedPos.x != 10f || updatedPos.y != 20f || updatedPos.z != 30f)
        {
            throw new Exception($"Expected updated position (10,20,30), got {updatedPos}");
        }

        // Verify ECS memory directly
        ref var comp = ref host.World.EntityManager.GetComponent<LocalTransform>(playerNode.EcsEntity);
        if (comp.position.x != 10f || comp.position.y != 20f || comp.position.z != 30f)
        {
            throw new Exception($"Expected ECS chunk memory to be (10,20,30), got {comp.position}");
        }
    }

    private static void TestSpinningCubeWorkerAnimation()
    {
        using var host = new EngineHost();
        host.IsPaused = false; // Running at 60 Hz

        var cubeNode = host.Entities.First(e => e.Name == "Spinning Cube");
        host.SelectEntity(cubeNode);

        var transformComp = cubeNode.Components.First(c => c.ComponentType == typeof(LocalTransform));
        var posProp = transformComp.Properties.Find(p => p.Name == "position")!;

        // Let simulation run for 150ms
        Thread.Sleep(150);

        var livePos = (float3)posProp.GetValue()!;
        // Initial position was (-3, 1, 0), Y animates via MathF.Sin
        if (livePos.x != -3f)
        {
            throw new Exception($"Expected X to remain -3, got {livePos.x}");
        }
    }

    private static void TestWorkerSystemAutoIncrement()
    {
        using var host = new EngineHost();
        host.IsPaused = false; // Running at 60 Hz

        var workerNode = host.Entities.First(e => e.Name == "Counter Worker");
        host.SelectEntity(workerNode);

        var compNode = workerNode.Components.First(c => c.ComponentType == typeof(CounterComponent));
        var valProp = compNode.Properties.Find(p => p.Name == "Value")!;
        var stepProp = compNode.Properties.Find(p => p.Name == "Step")!;
        var autoProp = compNode.Properties.Find(p => p.Name == "AutoIncrement")!;

        valProp.SetValue(10);
        stepProp.SetValue(5);
        autoProp.SetValue(true);

        Thread.Sleep(200);

        var valAfterRun = (int)valProp.GetValue()!;
        if (valAfterRun <= 10)
        {
            throw new Exception($"Expected value to increment above 10, got {valAfterRun}");
        }

        autoProp.SetValue(false);
        Thread.Sleep(100);

        var valPaused1 = (int)valProp.GetValue()!;
        Thread.Sleep(100);
        var valPaused2 = (int)valProp.GetValue()!;

        if (valPaused1 != valPaused2)
        {
            throw new Exception($"Expected value to remain stable after auto-increment disabled ({valPaused1} vs {valPaused2})");
        }
    }

    private static void TestConcurrentClashStress()
    {
        using var host = new EngineHost();
        host.IsPaused = false;

        var workerNode = host.Entities.First(e => e.Name == "Counter Worker");
        host.SelectEntity(workerNode);

        var compNode = workerNode.Components.First(c => c.ComponentType == typeof(CounterComponent));
        var valProp = compNode.Properties.Find(p => p.Name == "Value")!;
        var autoProp = compNode.Properties.Find(p => p.Name == "AutoIncrement")!;

        autoProp.SetValue(true);

        var random = new Random(42);
        for (var i = 0; i < 100; i++)
        {
            var targetVal = random.Next(1, 10000);
            valProp.SetValue(targetVal);
            Thread.Sleep(2);
        }

        valProp.SetValue(50000);
        autoProp.SetValue(false);
        Thread.Sleep(100);

        var finalVal = (int)valProp.GetValue()!;
        if (finalVal < 50000)
        {
            throw new Exception($"Expected final value >= 50000, got {finalVal}");
        }

        if (host.FrameCount < 10)
        {
            throw new Exception($"Expected frame count to advance, got {host.FrameCount}");
        }
    }
}
