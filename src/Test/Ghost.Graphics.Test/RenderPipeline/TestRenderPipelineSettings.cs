using Ghost.Graphics.Core;
using Ghost.Graphics.RenderPipeline;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.Graphics.Test.RenderPipeline;

internal sealed class TestRenderPayload : IRenderPayload
{
    public class FrameData
    {
        public UnsafeList<RenderRequest> renderRequests;
    }

    private readonly RenderSystem _renderSystem;
    private readonly FrameData[] _frameData;

    public RenderSystem RenderSystem => _renderSystem;
    public ReadOnlySpan<FrameData> FrameRequestData => _frameData;

    public TestRenderPayload(RenderSystem renderSystem)
    {
        _renderSystem = renderSystem;
        _frameData = new FrameData[renderSystem.MaxFrameLatency];

        for (int i = 0; i < _frameData.Length; i++)
        {
            _frameData[i].renderRequests = new UnsafeList<RenderRequest>(2, Allocator.Persistent);
        }
    }

    public void AddRenderRequest(RenderRequest request)
    {
        var index = (int)(_renderSystem.CPUFenceValue % (uint)_frameData.Length);
        _frameData[index].renderRequests.Add(request);
    }

    public void Dispose()
    {
        for (int i = 0; i < _frameData.Length; i++)
        {
            _frameData[i].renderRequests.Dispose();
        }
    }
}

internal sealed class TestRenderPipelineSettings : IRenderPipelineSettings
{
    public void CreatePipeline(RenderSystem renderSystem, out IRenderPipeline renderPipeline, out IRenderPayload renderPayload)
    {
        renderPipeline = new TestRenderPipeline(renderSystem);
        renderPayload = new TestRenderPayload(renderSystem);
    }
}
