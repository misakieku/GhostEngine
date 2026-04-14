using Ghost.Engine.RenderPipeline;
using Ghost.Graphics;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Engine;

public sealed partial class EngineCore : IDisposable
{
    private readonly JobScheduler _jobScheduler;
    private readonly RenderSystem _renderSystem;

    public JobScheduler JobScheduler => _jobScheduler;
    public RenderSystem RenderSystem => _renderSystem;

    public EngineCore()
    {
        _jobScheduler = new JobScheduler(Environment.ProcessorCount - 2); // We -2 here, one for main thread, one for render thread

        var renderingConfig = new RenderSystemDesc
        {
            FrameBufferCount = 2,
            GraphicsAPI = GraphicsAPI.Direct3D12,
            InitialRenderPipelineSettings = new GhostRenderPipelineSettings(),
            ShaderCacheDirectory = "ShaderCache",
        };

        _renderSystem = new RenderSystem(renderingConfig);
    }

    public void Dispose()
    {
        _renderSystem.Dispose();
        _jobScheduler.Dispose();
    }
}