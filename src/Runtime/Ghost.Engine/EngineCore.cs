using Ghost.Core.Graphics;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Jobs;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine;

public interface IRuntimeInitializeCallback
{
    void Initialize();
    void Shutdown();
}

public sealed partial class EngineCore : IDisposable
{
    private readonly IContentProvider _contentProvider;

    private readonly JobScheduler _jobScheduler;
    private readonly ResourceStreamingProcessor _streamingProcessor;
    private readonly RenderSystem _renderSystem;
    private readonly AssetManager _assetManager;

    internal JobScheduler JobScheduler => _jobScheduler;
    internal RenderSystem RenderSystem => _renderSystem;
    internal AssetManager AssetManager => _assetManager;

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

        _renderSystem = new RenderSystem(renderingDesc);
        _assetManager = new AssetManager(_renderSystem.GraphicsEngine.ResourceDatabase, _renderSystem.ResourceManager, _contentProvider, _streamingProcessor, _jobScheduler);
    }

    public void Dispose()
    {
        _assetManager.Dispose();
        _renderSystem.Dispose();
        _jobScheduler.Dispose();
    }
}

[GenerateShaderProperty("TestShader")]
public partial struct TestShaderProperty
{
    public Texture2DHandle texture;
    public uint someValue;
    public float3 otherValue;
}