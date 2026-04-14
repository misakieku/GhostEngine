using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline : IRenderPipeline
{
    private struct AddInstanceData
    {
        public float4x4 localToWorld;
        public uint instanceID;
        public uint meshBuffer;
        public uint materialPalette;
        public uint renderingLayerMask;
        public uint shadowCastingMode;
    }

    private struct RemoveInstanceData
    {
        public uint instanceID;
        public uint swapWithInstanceID;
    }

    private readonly RenderSystem _renderSystem;

    private readonly RenderGraph _renderGraph;
    private readonly GPUScene _gpuScene;

    public GPUScene GPUScene => _gpuScene;

    public GhostRenderPipeline(RenderSystem renderSystem)
    {
        _renderSystem = renderSystem;

        _renderGraph = new RenderGraph(renderSystem);
        _gpuScene = new GPUScene(renderSystem.GraphicsEngine.ResourceAllocator, renderSystem.GraphicsEngine.ResourceDatabase, 102_400u); // 102.4k objects should be enough for now
    }

    public void Render(RenderContext ctx, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        foreach (ref readonly var request in ghostPayload.RenderRequests)
        {
            try
            {
                using var viewData = new RenderViewData(_renderSystem.SwapChainManager, ctx.ResourceDatabase, in request);
                RenderPipelineUtility.GetVPMatrices(in request, viewData.ScreenSize, out var view, out var projection);

                UpdateGPUScene(ctx, ghostPayload);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }

    public void Dispose()
    {
        _renderGraph.Dispose();
        _gpuScene.Dispose();
    }
}
