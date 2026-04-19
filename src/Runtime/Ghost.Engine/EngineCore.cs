using Ghost.Engine.RenderPipeline;
using Ghost.Graphics;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Engine;

public sealed partial class EngineCore : IDisposable
{
    private readonly IContentProvider _contentProvider;

    private readonly JobScheduler _jobScheduler;
    private readonly ResourceStreamingProcessor _streamingProcessor;
    private readonly RenderSystem _renderSystem;
    private readonly AssetManager _assetManager;

    public EngineCore(IContentProvider contentProvider)
    {
        _contentProvider = contentProvider;

        var desc = new JobSchedulerDesc
        {
            ThreadCount = Environment.ProcessorCount - 2, // We -2 here, one for main thread, one for render thread
            ThreadPriority = ThreadPriority.Normal,
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
        };

        _renderSystem = new RenderSystem(renderingDesc);
        _assetManager = new AssetManager(_renderSystem.GraphicsEngine.ResourceDatabase, _contentProvider, _streamingProcessor, _jobScheduler);
    }

    public void Dispose()
    {
        _assetManager.Dispose();
        _renderSystem.Dispose();
        _jobScheduler.Dispose();
    }
}