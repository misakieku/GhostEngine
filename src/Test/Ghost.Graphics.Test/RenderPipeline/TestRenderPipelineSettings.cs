using Ghost.Graphics.Core;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Test.RenderPipeline;

internal sealed class TestRenderPayload : IRenderPayload
{
    public UnsafeList<RenderRequest> renderRequests;

    public TestRenderPayload()
    {
        renderRequests = new UnsafeList<RenderRequest>(2, Allocator.Persistent);
    }

    public void Reset()
    {
        for (int i = 0; i < renderRequests.Count; i++)
        {
            renderRequests[i].Dispose();
        }

        renderRequests.Clear();
    }

    public void Dispose()
    {
        renderRequests.Dispose();
    }
}

internal sealed class TestRenderPipelineSettings : IRenderPipelineSettings
{
    public IRenderPipeline CreatePipeline(RenderSystem renderSystem)
    {
        return new TestRenderPipeline(renderSystem);
    }

    public IRenderPayload CreatePayload(RenderSystem renderSystem)
    {
        return new TestRenderPayload();
    }
}
