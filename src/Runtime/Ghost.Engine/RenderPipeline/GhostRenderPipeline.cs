using Ghost.Core;
using Ghost.Core.Graphics;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

namespace Ghost.Engine.RenderPipeline;

internal partial class GhostRenderPipeline : IRenderPipeline
{
    private struct TestPassData
    {
        public Identifier<RGTexture> depth;
    }

    private readonly RenderEngine _renderEngine;

    private readonly RenderGraph _renderGraph;
    private readonly GPUScene _gpuScene;

    public GPUScene GPUScene => _gpuScene;

    public GhostRenderPipeline(RenderEngine renderEngine)
    {
        _renderEngine = renderEngine;

        _renderGraph = new RenderGraph(
            renderEngine.GraphicsEngine.ResourceDatabase,
            renderEngine.GraphicsEngine.ResourceAllocator,
            renderEngine.GraphicsEngine.PipelineLibrary,
            renderEngine.ResourceManager,
            renderEngine.ShaderLibrary);
        _gpuScene = new GPUScene(renderEngine.GraphicsEngine.ResourceAllocator, renderEngine.GraphicsEngine.ResourceDatabase, 102_400u); // 102.4k objects should be enough for now
    }

    public void RecordPrelude(RenderContext ctx, int frameIndex, IRenderPayload payload)
    {
        var ghostPayload = (GhostRenderPayload)payload;

        foreach (ref readonly var request in ghostPayload.RenderRequests)
        {
            try
            {
                using var viewData = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);
                RenderPipelineUtility.GetVPMatrices(in request, viewData.ScreenSize, out var view, out var projection);

                UpdateGPUScene(ctx, ghostPayload);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }

    public RGExecution ExecuteGraph(RenderContext ctx, int frameIndex, IRenderPayload payload,
        in RenderGraphExecutionContext executionContext)
    {
        var ghostPayload = (GhostRenderPayload)payload;
        _renderGraph.Reset();

        if (ghostPayload.RenderRequests.Length == 0)
        {
            return default;
        }

        ref readonly var request = ref ghostPayload.RenderRequests[0];
        using var viewData = new RenderViewData(_renderEngine.SwapChainManager, ctx.ResourceDatabase, in request);
        var viewState = new ViewState(viewData.ScreenSize.x, viewData.ScreenSize.y, viewData.ScreenSize.x, viewData.ScreenSize.y);

        BuildRepresentativePipeline(
            _renderGraph,
            viewData.ColorTexture,
            (uint)frameIndex,
            viewData.ScreenSize.x,
            viewData.ScreenSize.y);

        var result = _renderGraph.CompileAndExecute(executionContext, viewState);
        if (result.IsFailure)
        {
            Logger.Error($"Render graph execution failed: {result.Error}");
            return default;
        }

        return result.Value;
    }

    private static void BuildRepresentativePipeline(
        RenderGraph rg,
        Handle<GPUTexture> backBufferHandle,
        uint frameIndex,
        uint width,
        uint height)
    {
        var backBuffer = rg.ImportTexture(
            backBufferHandle,
            initialState: new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None),
            finalState: new ResourceBarrierData(BarrierLayout.Present, BarrierAccess.NoAccess, BarrierSync.None));

        using (var builder = rg.AddRasterRenderPass<TestPassData>("MeshletTestPass"))
        {
            var depth = builder.CreateTexture(RGTextureDesc.RelativeDepth(1.0f));

            builder.SetColorAttachment(backBuffer, 0, AccessFlags.WriteAll);
            builder.SetDepthAttachment(depth, AccessFlags.WriteAll);

            builder.SetRenderFunc<TestPassData>(static (ref readonly passData, renderCtx) =>
            {
            });
        }
    }

    public void Dispose()
    {
        _renderGraph.Dispose();
        _gpuScene.Dispose();
    }
}
