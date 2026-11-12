using Ghost.Core;
using Ghost.Editor.PoC.Nodes;
using Ghost.Editor.PoC.Sync;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Diagnostics;

namespace Ghost.Editor.PoC.Ecs;

public sealed class EngineHost : IDisposable
{
    private readonly JobScheduler _scheduler;
    private readonly World _world;
    private readonly PoCSystemGroup _systemGroup;
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSubsystem _subsystem;

    private readonly Entity _counterEntity;
    private readonly EntityNode _counterEntityNode;

    private readonly Thread _engineThread;
    private readonly CancellationTokenSource _cts = new();
    private readonly ManualResetEventSlim _stepSignal = new(false);

    private volatile bool _isPaused = false;
    private long _frameCount = 0;
    private bool _disposed = false;

    public EditorSyncBridge Bridge => _bridge;
    public EditorSubsystem Subsystem => _subsystem;
    public EntityNode CounterEntityNode => _counterEntityNode;
    public World World => _world;
    public long FrameCount => Interlocked.Read(ref _frameCount);

    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            _isPaused = value;
            if (!value)
            {
                _stepSignal.Set();
            }
        }
    }

    public EngineHost()
    {
        AllocationManager.Initialize();

        var desc = new JobSchedulerDesc
        {
            ThreadCount = 4,
            DependencyChainCapacity = 128,
            ThreadPriority = ThreadPriority.Normal
        };
        _scheduler = new JobScheduler(in desc);
        _world = World.Create(_scheduler, 128);

        _systemGroup = new PoCSystemGroup();
        _bridge = new EditorSyncBridge();
        _subsystem = new EditorSubsystem(_bridge);

        _counterEntity = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(_counterEntity, new CounterComponent
        {
            Value = 1,
            Step = 1,
            AutoIncrement = false
        });

        _subsystem.RegisterMonitored(_counterEntity, typeof(CounterComponent));

        _counterEntityNode = new EntityNode(Guid.NewGuid(), "CounterEntity", _counterEntity, _bridge);
        _counterEntityNode.AddComponent(typeof(CounterComponent));

        // Initial snapshot so UI has data immediately before first tick
        _subsystem.PostTick(_world);

        _engineThread = new Thread(EngineLoop)
        {
            IsBackground = true,
            Name = "EngineSimulationThread"
        };
        _engineThread.Start();
    }

    public void StepSingleFrame()
    {
        _stepSignal.Set();
    }

    private void EngineLoop()
    {
        var sw = Stopwatch.StartNew();
        var lastTime = sw.Elapsed.TotalSeconds;

        var sysApi = new SystemAPI { World = _world };
        _systemGroup.Initialize(in sysApi);

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var isPaused = _isPaused;
                if (isPaused)
                {
                    if (!_stepSignal.Wait(100, _cts.Token))
                    {
                        continue;
                    }
                    _stepSignal.Reset();
                }

                var currentTime = sw.Elapsed.TotalSeconds;
                var deltaTime = (float)(currentTime - lastTime);
                lastTime = currentTime;

                _subsystem.PreTick(_world);

                var updateApi = new SystemUpdateAPI
                {
                    World = _world,
                    Time = new TimeData
                    {
                        FrameIndex = (int)Interlocked.Increment(ref _frameCount),
                        DeltaTime = deltaTime,
                        ElapsedTime = (float)currentTime
                    }
                };
                _systemGroup.Update(in updateApi);

                _world.PlaybackEntityCommandBuffers();

                _subsystem.PostTick(_world);

                AllocationManager.ResetTempAllocator();

                if (!isPaused)
                {
                    Thread.Sleep(16);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _systemGroup.Cleanup(in sysApi);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        _stepSignal.Set();
        _engineThread.Join(2000);

        _stepSignal.Dispose();
        _cts.Dispose();

        _world.Dispose();
        _scheduler.Dispose();
        AllocationManager.Dispose();
    }
}
