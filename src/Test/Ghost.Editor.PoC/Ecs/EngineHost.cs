using Ghost.Core;
using Ghost.Editor.PoC.Nodes;
using Ghost.Editor.PoC.Services;
using Ghost.Editor.PoC.Sync;
using Ghost.Engine.Components;
using Ghost.Entities;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.Mathematics;
using System.Diagnostics;

namespace Ghost.Editor.PoC.Ecs;

public sealed class EngineHost : IDisposable
{
    private readonly JobScheduler _scheduler;
    private readonly World _world;
    private readonly PoCSystemGroup _systemGroup;
    private readonly EditorSyncBridge _bridge;
    private readonly EditorSubsystem _subsystem;

    private readonly List<EntityNode> _entities = new();
    private EntityNode? _selectedEntity;

    private readonly Thread _engineThread;
    private readonly CancellationTokenSource _cts = new();
    private readonly ManualResetEventSlim _stepSignal = new(false);

    private volatile bool _isPaused = false;
    private long _frameCount = 0;
    private bool _disposed = false;

    public EditorSyncBridge Bridge => _bridge;
    public EditorSubsystem Subsystem => _subsystem;
    public IReadOnlyList<EntityNode> Entities => _entities;
    public EntityNode? SelectedEntity => _selectedEntity;
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

        EditorSelection.SelectionChanged += OnSelectionChanged;

        SetupHardcodedEntities();

        // Select the first entity by default
        SelectEntity(_entities[0]);

        // Initial snapshot
        _subsystem.PostTick(_world);

        _engineThread = new Thread(EngineLoop)
        {
            IsBackground = true,
            Name = "EngineSimulationThread"
        };
        _engineThread.Start();
    }

    private void SetupHardcodedEntities()
    {
        // 1. Player
        var player = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(player, LocalTransform.FromPosition(new float3(0f, 0f, 0f)));
        var playerNode = new EntityNode(Guid.NewGuid(), "Player", player, _bridge);
        playerNode.AddComponent(typeof(LocalTransform));
        _entities.Add(playerNode);

        // 2. Main Camera
        var camera = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(camera, LocalTransform.FromPosition(new float3(0f, 2f, -10f)));
        var cameraNode = new EntityNode(Guid.NewGuid(), "Main Camera", camera, _bridge);
        cameraNode.AddComponent(typeof(LocalTransform));
        _entities.Add(cameraNode);

        // 3. Directional Light
        var light = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(light, LocalTransform.FromPosition(new float3(0f, 10f, 0f)));
        var lightNode = new EntityNode(Guid.NewGuid(), "Directional Light", light, _bridge);
        lightNode.AddComponent(typeof(LocalTransform));
        _entities.Add(lightNode);

        // 4. Counter Worker
        var counter = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(counter, LocalTransform.FromPosition(new float3(2f, 0f, 0f)));
        _world.EntityManager.AddComponent(counter, new CounterComponent
        {
            Value = 1,
            Step = 1,
            AutoIncrement = true
        });
        var counterNode = new EntityNode(Guid.NewGuid(), "Counter Worker", counter, _bridge);
        counterNode.AddComponent(typeof(LocalTransform));
        counterNode.AddComponent(typeof(CounterComponent));
        _entities.Add(counterNode);

        // 5. Spinning Cube
        var cube = _world.EntityManager.CreateEntity();
        _world.EntityManager.AddComponent(cube, LocalTransform.FromPosition(new float3(-3f, 1f, 0f)));
        _world.EntityManager.AddComponent(cube, new SpinningTag());
        var cubeNode = new EntityNode(Guid.NewGuid(), "Spinning Cube", cube, _bridge);
        cubeNode.AddComponent(typeof(LocalTransform));
        cubeNode.AddComponent(typeof(SpinningTag));
        _entities.Add(cubeNode);
    }

    public void SelectEntity(EntityNode? node)
    {
        EditorSelection.Select(node);
    }

    private void OnSelectionChanged(object? target)
    {
        if (target is EntityNode node)
        {
            _selectedEntity = node;
            var types = node.Components.Select(c => c.ComponentType);
            _subsystem.SetMonitored(node.EcsEntity, types);
        }
        else if (target == null)
        {
            _selectedEntity = null;
            _subsystem.SetMonitored(new Entity(Entity.INVALID_ID, 0), Array.Empty<Type>());
        }
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
        EditorSelection.SelectionChanged -= OnSelectionChanged;
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
