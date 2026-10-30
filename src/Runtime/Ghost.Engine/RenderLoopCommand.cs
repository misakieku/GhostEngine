using Ghost.Engine.Streaming;
using Ghost.Graphics;
using Ghost.Graphics.RHI;

namespace Ghost.Engine;

internal class StreamingCommand : IRenderLoopCommand
{
    private readonly ResourceStreamingProcessor _streamingProcessor;

    public StreamingCommand(ResourceStreamingProcessor streamingProcessor)
    {
        _streamingProcessor = streamingProcessor;
    }

    public void Execute(scoped in RenderContext context, ICommandBuffer commandBuffer)
    {
        _streamingProcessor.ProcessPendingShaderCommits(context);
        _streamingProcessor.ProcessPendingUploads(context, commandBuffer);
    }

    public void Dispose()
    {
    }
}

internal class MeshletStreamingPreCommand : IRenderLoopCommand
{
    private readonly MeshletStreamingManager _meshletStreamingManager;
    public MeshletStreamingPreCommand(MeshletStreamingManager meshletStreamingManager)
    {
        _meshletStreamingManager = meshletStreamingManager;
    }

    public void Execute(scoped in RenderContext context, ICommandBuffer commandBuffer)
    {
        _meshletStreamingManager.Update(context, commandBuffer);
    }

    public void Dispose()
    {
    }
}

internal class MeshletStreamingPostCommand : IRenderLoopCommand
{
    private readonly MeshletStreamingManager _meshletStreamingManager;
    public MeshletStreamingPostCommand(MeshletStreamingManager meshletStreamingManager)
    {
        _meshletStreamingManager = meshletStreamingManager;
    }

    public void Execute(scoped in RenderContext context, ICommandBuffer commandBuffer)
    {
        _meshletStreamingManager.RecordReadbackCopy(commandBuffer, context.FrameIndex);
    }

    public void Dispose()
    {
    }
}