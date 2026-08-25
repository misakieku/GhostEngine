using Ghost.Engine.Streaming;
using Ghost.Entities;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Diagnostics;

namespace Ghost.Engine;

/// <summary>
/// Indicates that the method should be called during the engine's configuration phase. Methods marked with this attribute will be invoked before the engine is created, allowing for any necessary configuration or setup to be performed.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RuntimeConfigurationAttribute : Attribute;

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

public struct EngineDesc
{
    public struct Render
    {
        public required IGraphicsEngine GraphicsEngine
        {
            get; set;
        }

        public required uint FrameBufferCount
        {
            get; set;
        }

        public required IRenderPipelineSettings RenderPipelineSettings
        {
            get; set;
        }

        public required string ShaderCacheDirectory
        {
            get; set;
        }

        public IShaderCompilationBridge? ShaderCompilationBridge
        {
            get; set;
        }
    }

    public required AllocationManagerDesc AllocationManagerDesc
    {
        get; set;
    }

    public required WindowDesc WindowDesc
    {
        get; set;
    }

    public required JobSchedulerDesc JobSchedulerDesc
    {
        get; set;
    }

    public required Func<Render> RenderDescFactory
    {
        get; set;
    }

    public required Func<IContentProvider> ContentProviderFactory
    {
        get; set;
    }
}

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

    public EngineCore(JobSchedulerDesc jobSchedulerDesc, EngineDesc.Render renderDesc, IContentProvider contentProvider)
    {
        _contentProvider = contentProvider;

        _jobScheduler = new JobScheduler(in jobSchedulerDesc);
        _streamingProcessor = new ResourceStreamingProcessor();

        var renderingDesc = new RenderEngineDesc
        {
            GraphicsEngine = renderDesc.GraphicsEngine,
            FrameBufferCount = renderDesc.FrameBufferCount,
            InitialRenderPipelineSettings = renderDesc.RenderPipelineSettings,
            ShaderCacheDirectory = renderDesc.ShaderCacheDirectory,
            ShaderCompilationBridge = renderDesc.ShaderCompilationBridge,
            ResourceStreamingProcessor = _streamingProcessor,
        };

        _renderEngine = new RenderEngine(renderingDesc);
        _assetManager = new AssetManager(_renderEngine.GraphicsEngine.ResourceDatabase, _renderEngine.ResourceManager, _contentProvider, _streamingProcessor, _jobScheduler);

        _stopwatch = new Stopwatch();
    }

    public void Start()
    {
        _renderEngine.Start();
        _stopwatch.Start();

        foreach (var world in World.GetWorldEnumerator())
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

        foreach (var world in World.GetWorldEnumerator())
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

        foreach (var world in World.GetWorldEnumerator())
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
