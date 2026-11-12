using Ghost.Editor.PoC.Ecs;

namespace Ghost.Editor.PoC.Tests;

public static class EditorSyncHeadlessRunner
{
    public static int Run()
    {
        Console.WriteLine("=== Starting GhostEngine Editor PoC Headless Tests ===");

        try
        {
            TestBasicBidirectionalSync();
            Console.WriteLine("[PASS] Test 1: Basic Bidirectional Sync (UI -> ECS -> UI)");

            TestWorkerSystemAutoIncrement();
            Console.WriteLine("[PASS] Test 2: Background Worker Concurrency (ECS Worker -> UI)");

            TestConcurrentClashStress();
            Console.WriteLine("[PASS] Test 3: Concurrent Mutation Stress Test (Race Condition Check)");

            Console.WriteLine("=== ALL HEADLESS TESTS PASSED ===");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FAIL] Test failed with exception: {ex}");
            return 1;
        }
    }

    private static void TestBasicBidirectionalSync()
    {
        using var host = new EngineHost();
        host.IsPaused = true;

        var entityNode = host.CounterEntityNode;
        var compNode = entityNode.Components[0];
        var valProp = compNode.Properties.Find(p => p.Name == "Value")!;

        // 1. Initial snapshot check
        var initialVal = valProp.GetValue();
        if ((int)initialVal! != 1)
        {
            throw new Exception($"Expected initial value 1, got {initialVal}");
        }

        // 2. UI -> ECS mutation
        valProp.SetValue(42);

        // Step one frame to execute PreTick -> Worker DAG -> PostTick
        host.StepSingleFrame();
        Thread.Sleep(50);

        var updatedVal = valProp.GetValue();
        if ((int)updatedVal! != 42)
        {
            throw new Exception($"Expected updated value 42, got {updatedVal}");
        }

        // Verify ECS memory directly
        ref var comp = ref host.World.EntityManager.GetComponent<CounterComponent>(entityNode.EcsEntity);
        if (comp.Value != 42)
        {
            throw new Exception($"Expected ECS chunk memory to be 42, got {comp.Value}");
        }
    }

    private static void TestWorkerSystemAutoIncrement()
    {
        using var host = new EngineHost();
        host.IsPaused = false; // Running at 60 Hz

        var entityNode = host.CounterEntityNode;
        var compNode = entityNode.Components[0];
        var valProp = compNode.Properties.Find(p => p.Name == "Value")!;
        var stepProp = compNode.Properties.Find(p => p.Name == "Step")!;
        var autoProp = compNode.Properties.Find(p => p.Name == "AutoIncrement")!;

        valProp.SetValue(10);
        stepProp.SetValue(5);
        autoProp.SetValue(true);

        // Let background engine thread and worker DAG run for 200ms
        Thread.Sleep(200);

        var valAfterRun = (int)valProp.GetValue()!;
        if (valAfterRun <= 10)
        {
            throw new Exception($"Expected value to increment above 10, got {valAfterRun}");
        }

        // Turn off auto-increment
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
        host.IsPaused = false; // Continuous play

        var entityNode = host.CounterEntityNode;
        var compNode = entityNode.Components[0];
        var valProp = compNode.Properties.Find(p => p.Name == "Value")!;
        var autoProp = compNode.Properties.Find(p => p.Name == "AutoIncrement")!;

        autoProp.SetValue(true);

        // Simulated UI thread spamming mutations while worker threads are executing
        var random = new Random(42);
        for (var i = 0; i < 100; i++)
        {
            var targetVal = random.Next(1, 10000);
            valProp.SetValue(targetVal);
            Thread.Sleep(3);
        }

        // Final settle
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
