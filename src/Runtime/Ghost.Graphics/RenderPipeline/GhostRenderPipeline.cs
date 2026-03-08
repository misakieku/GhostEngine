using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using System.Runtime.CompilerServices;

namespace Ghost.Graphics.RenderPipeline;

public sealed class GhostRenderPipelineSettings : IRenderPipelineSettings
{
    public static IRenderPipeline CreatePipeline(IRenderSystem renderSystem)
    {
        return new GhostRenderPipeline(renderSystem);
    }
}

public unsafe partial class GhostRenderPipeline : IRenderPipeline
{
    private readonly RenderGraph _renderGraph;

    private bool _disposed;

    ~GhostRenderPipeline()
    {
        Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    internal GhostRenderPipeline(IRenderSystem renderSystem)
    {
        _renderGraph = new RenderGraph(renderSystem.ResourceManager,
                renderSystem.GraphicsEngine.ResourceAllocator,
                renderSystem.GraphicsEngine.ResourceDatabase,
                renderSystem.GraphicsEngine.PipelineLibrary,
                renderSystem.GraphicsEngine.ShaderCompiler);
    }

    public void Render(RenderContext ctx, ReadOnlySpan<RenderRequest> requests)
    {
        for (int i = 0; i < requests.Length; i++)
        {
            ref readonly var request = ref requests[i];

            if (request.renderFunc != null)
            {
                request.renderFunc(in ctx, in request);
            }

            // TODO: Set up the rendering pipeline using render graph based on the request data
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
