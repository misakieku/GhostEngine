using Ghost.Entities;
using Ghost.Graphics;
using Misaki.HighPerformance.Jobs;

namespace Ghost.Engine;

public interface IEngineContext : IDisposable
{
    IJobScheduler JobScheduler { get; }
    IRenderSystem RenderSystem { get; }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal class EngineEntryAttribute : Attribute
{
}

[EngineEntry]
internal sealed partial class EngineCore : IEngineContext
{
    private readonly JobScheduler _jobScheduler;
    private readonly RenderSystem _renderSystem;

    public IJobScheduler JobScheduler => _jobScheduler;
    public IRenderSystem RenderSystem => _renderSystem;

    public EngineCore()
    {
        _jobScheduler = new JobScheduler(Environment.ProcessorCount - 2); // We -2 here, one for main thread, one for render thread

        // TODO: Remove the windows dependency from RenderSystem.
        var renderingConfig = new RenderSystemDesc
        {
            FrameBufferCount = 2,
            GraphicsAPI = GraphicsAPI.Direct3D12,
        };

        _renderSystem = new RenderSystem(renderingConfig);

        ComponentRegistry.GetOrRegisterComponentID<ManagedEntityRef>();
    }

    public void Init()
    {
    }

    public void Dispose()
    {
        _jobScheduler.Dispose();
    }
}
