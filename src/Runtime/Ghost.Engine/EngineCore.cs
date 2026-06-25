using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Streaming;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using System.Diagnostics;

namespace Ghost.Engine;

/// <summary>
/// Indicates that the method should be called during the engine's initialization phase. Methods marked with this attribute will be invoked right after the engine is created and before the main loop starts.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RuntimeInitializeAttribute : Attribute;

/// <summary>
/// Indicates that the method should be called during the engine's shutdown phase. Methods marked with this attribute will be invoked right before the engine is disposed and the application exits.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RuntimeShutdownAttribute : Attribute;

public sealed partial class EngineCore : IDisposable
{
    private readonly IContentProvider _contentProvider;

    private readonly JobScheduler _jobScheduler;
    private readonly ResourceStreamingProcessor _streamingProcessor;
    private readonly RenderEngine _renderEngine;
    private readonly AssetManager _assetManager;

    private readonly Stopwatch _stopwatch;
    private float _lastFrameTime;
    private int _frameIndex;

    public JobScheduler JobScheduler => _jobScheduler;
    public RenderEngine RenderEngine => _renderEngine;
    public AssetManager AssetManager => _assetManager;

    public int FrameIndex => _frameIndex;

    public EngineCore(IContentProvider contentProvider, IShaderCompilationBridge? shaderCompilationBridge = null)
    {
        _contentProvider = contentProvider;

        var desc = new JobSchedulerDesc
        {
            ThreadCount = Environment.ProcessorCount - 2, // We -2 here, one for main thread, one for render thread
            ThreadPriority = ThreadPriority.Normal,
            DependencyChainCapacity = 8192,
        };

        _jobScheduler = new JobScheduler(in desc);
        _streamingProcessor = new ResourceStreamingProcessor();

        var renderingDesc = new RenderSystemDesc
        {
            FrameBufferCount = 2,
            GraphicsAPI = GraphicsAPI.Direct3D12,
            InitialRenderPipelineSettings = new GhostRenderPipelineSettings(),
            ResourceStreamingProcessor = _streamingProcessor,
            ShaderCacheDirectory = "ShaderCache",
            ShaderCompilationBridge = shaderCompilationBridge,
        };

        _renderEngine = new RenderEngine(renderingDesc);
        _assetManager = new AssetManager(_renderEngine.GraphicsEngine.ResourceDatabase, _renderEngine.ResourceManager, _contentProvider, _streamingProcessor, _jobScheduler);

        _stopwatch = new Stopwatch();
    }

    public void Start()
    {
        _renderEngine.Start();
        _stopwatch.Start();

        foreach (var world in World.EnumerateAllWorlds())
        {
            world.SystemManager.InitializeAll();
        }
    }

    public void Tick()
    {
        var currentTime = (float)_stopwatch.Elapsed.TotalSeconds;
        var deltaTime = currentTime - _lastFrameTime;
        _lastFrameTime = currentTime;

        var time = new TimeData
        {
            FrameIndex = _frameIndex,
            DeltaTime = deltaTime,
            ElapsedTime = currentTime,
        };

        foreach (var world in World.EnumerateAllWorlds())
        {
            world.SystemManager.UpdateAll(time);
        }

        RenderEngine.SignalCPUReady(_frameIndex++);
        RenderEngine.WaitForGPUReady(_frameIndex);
    }

    public void Stop()
    {
        _stopwatch.Stop();
        _renderEngine.Stop();

        foreach (var world in World.EnumerateAllWorlds())
        {
            world.SystemManager.CleanupAll();
        }
    }

    public void Dispose()
    {
        _assetManager.Dispose();
        _renderEngine.Dispose();
        _jobScheduler.Dispose();
    }
}