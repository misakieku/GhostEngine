#if GHOST_UNITTEST

using Ghost.Core;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.D3D12;
using Ghost.Graphics.FrameScheduling;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;

namespace Ghost.UnitTest.Graphics;

[TestClass]
[DoNotParallelize]
public class RenderEnginePhase7D3D12Test
{
    private struct ProducerPassData
    {
        public Identifier<RGTexture> targetA;
    }

    private struct AsyncComputePassData
    {
        public Identifier<RGTexture> targetA;
        public Identifier<RGTexture> targetB;
    }

    private struct IndependentGraphicsPassData
    {
        public Identifier<RGTexture> targetC;
    }

    private struct GraphicsJoinPassData
    {
        public Identifier<RGTexture> targetA;
        public Identifier<RGTexture> targetB;
        public Identifier<RGTexture> targetC;
        public Identifier<RGTexture> backBuffer;
    }

    private static void BuildRepresentativePipeline(RenderGraph rg, Handle<GPUTexture> backBufferHandle)
    {
        var backBuffer = rg.ImportTexture(backBufferHandle);

        // 1. Graphics Producer (Raster)
        Identifier<RGTexture> targetA;
        using (var builder = rg.AddRasterRenderPass<ProducerPassData>("Graphics_Producer"))
        {
            targetA = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), "Transient_TargetA");
            builder.SetColorAttachment(targetA, 0, AccessFlags.WriteAll);
            builder.SetPassData(new ProducerPassData { targetA = targetA });
            builder.SetRenderFunc<ProducerPassData>(static (ref readonly ProducerPassData data, IRasterRenderContext ctx) => { });
        }

        // 2. Async Compute Pass (Compute)
        Identifier<RGTexture> targetB;
        using (var builder = rg.AddComputeRenderPass<AsyncComputePassData>("Async_Compute_Work"))
        {
            builder.EnableAsyncCompute(true);
            targetB = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm, usage: TextureUsage.UnorderedAccess | TextureUsage.ShaderResource), "Transient_TargetB");
            builder.UseTexture(targetA, AccessFlags.Read);
            builder.UseTexture(targetB, AccessFlags.Write);
            builder.SetPassData(new AsyncComputePassData { targetA = targetA, targetB = targetB });
            builder.SetRenderFunc<AsyncComputePassData>(static (ref readonly AsyncComputePassData data, IComputeRenderContext ctx) => { });
        }

        // 3. Independent Graphics Pass (Raster, Overlap Window)
        Identifier<RGTexture> targetC;
        using (var builder = rg.AddRasterRenderPass<IndependentGraphicsPassData>("Independent_Graphics_Work"))
        {
            targetC = builder.CreateTexture(RGTextureDesc.RelativeDepth(1.0f, usage: TextureUsage.DepthStencil | TextureUsage.ShaderResource), "Transient_TargetC");
            builder.SetDepthAttachment(targetC, AccessFlags.WriteAll);
            builder.SetPassData(new IndependentGraphicsPassData { targetC = targetC });
            builder.SetRenderFunc<IndependentGraphicsPassData>(static (ref readonly IndependentGraphicsPassData data, IRasterRenderContext ctx) => { });
        }

        // 4. Graphics Join / Consumer Pass (Raster)
        using (var builder = rg.AddRasterRenderPass<GraphicsJoinPassData>("Graphics_Join_Consumer"))
        {
            builder.UseTexture(targetA, AccessFlags.Read);
            builder.UseTexture(targetB, AccessFlags.Read);
            builder.UseTexture(targetC, AccessFlags.Read);
            builder.SetColorAttachment(backBuffer, 0, AccessFlags.WriteAll);
            builder.SetPassData(new GraphicsJoinPassData { targetA = targetA, targetB = targetB, targetC = targetC, backBuffer = backBuffer });
            builder.SetRenderFunc<GraphicsJoinPassData>(static (ref readonly GraphicsJoinPassData data, IRasterRenderContext ctx) => { });
        }
    }

    [TestMethod]
    public void TestPhase7_RealD3D12_RepresentativePipelineMultiFrame()
    {
        using var graphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc { FrameBufferCount = 2 });
        using var graphicsAllocator = graphicsEngine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = graphicsEngine.CreateCommandAllocator(CommandBufferType.Compute);
        using var scheduler = new FrameScheduler(graphicsEngine);
        using var resourceManager = new ResourceManager(graphicsEngine.Device, graphicsEngine.ResourceAllocator, graphicsEngine.ResourceDatabase);
        using var shaderLibrary = new ShaderLibrary(null, graphicsEngine.PipelineLibrary, string.Empty);
        using var renderGraph = new RenderGraph(graphicsEngine.ResourceDatabase, graphicsEngine.ResourceAllocator, graphicsEngine.PipelineLibrary, resourceManager, shaderLibrary);

        var executionContext = new RenderGraphExecutionContext(
            graphicsEngine,
            scheduler,
            graphicsAllocator,
            computeAllocator);

        var viewState = new ViewState(1920, 1080, 1920, 1080);

        var targetDesc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = 1920,
            Height = 1080,
            MipLevels = 1,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.RenderTarget,
        };
        var backBufferHandle = graphicsEngine.ResourceAllocator.CreateTexture(in targetDesc);
        Assert.IsTrue(backBufferHandle.IsValid, "BackBuffer texture must be valid.");

        try
        {
            const int totalFrames = 5;
            for (var frame = 0; frame < totalFrames; frame++)
            {
                graphicsAllocator.Reset();
                computeAllocator.Reset();
                resourceManager.BeginFrame((ulong)frame);
                graphicsEngine.BeginFrame((ulong)frame);

                renderGraph.Reset();
                BuildRepresentativePipeline(renderGraph, backBufferHandle);

                var result = renderGraph.CompileAndExecute(executionContext, viewState);
                Assert.IsTrue(result.IsSuccess, $"Frame {frame} execution must succeed. Error: {result.Error}");

                var execution = result.Value;
                Assert.IsTrue(execution.GraphicsSubmission.IsValid, $"Frame {frame} GraphicsSubmission must be valid.");
                Assert.IsTrue(execution.ComputeSubmission.IsValid, $"Frame {frame} ComputeSubmission must be valid.");

                var completion = scheduler.Flush();
                scheduler.WaitForFrame(completion);

                resourceManager.EndFrame(completion.FrameNumber);
                graphicsEngine.EndFrame(completion.FrameNumber);
            }

            scheduler.WaitIdle();
        }
        finally
        {
            graphicsEngine.ResourceDatabase.ReleaseResourceImmediately(backBufferHandle.AsResource());
        }
    }

    [TestMethod]
    public void TestPhase7_RealD3D12_ForceGraphicsFallback()
    {
        using var graphicsEngine = D3D12GraphicsEngineFactory.Create(new GraphicsEngineDesc { FrameBufferCount = 2 });
        using var graphicsAllocator = graphicsEngine.CreateCommandAllocator(CommandBufferType.Graphics);
        using var computeAllocator = graphicsEngine.CreateCommandAllocator(CommandBufferType.Compute);
        using var scheduler = new FrameScheduler(graphicsEngine);
        using var resourceManager = new ResourceManager(graphicsEngine.Device, graphicsEngine.ResourceAllocator, graphicsEngine.ResourceDatabase);
        using var shaderLibrary = new ShaderLibrary(null, graphicsEngine.PipelineLibrary, string.Empty);
        using var renderGraph = new RenderGraph(graphicsEngine.ResourceDatabase, graphicsEngine.ResourceAllocator, graphicsEngine.PipelineLibrary, resourceManager, shaderLibrary);

        var executionContext = new RenderGraphExecutionContext(
            graphicsEngine,
            scheduler,
            graphicsAllocator,
            computeAllocator);

        var viewState = new ViewState(1280, 720, 1280, 720);

        var targetDesc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = 1280,
            Height = 720,
            MipLevels = 1,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.RenderTarget,
        };
        var backBufferHandle = graphicsEngine.ResourceAllocator.CreateTexture(in targetDesc);

        try
        {
            graphicsAllocator.Reset();
            computeAllocator.Reset();
            resourceManager.BeginFrame(0);
            graphicsEngine.BeginFrame(0);

            renderGraph.Reset();
            BuildRepresentativePipeline(renderGraph, backBufferHandle);

            var result = renderGraph.CompileAndExecute(executionContext, viewState, RGExecutionFlags.ForceGraphics);
            Assert.IsTrue(result.IsSuccess, $"ForceGraphics execution must succeed. Error: {result.Error}");

            var execution = result.Value;
            Assert.IsTrue(execution.GraphicsSubmission.IsValid, "GraphicsSubmission must be valid under ForceGraphics.");
            Assert.IsFalse(execution.ComputeSubmission.IsValid, "ComputeSubmission must be invalid under ForceGraphics.");

            var completion = scheduler.Flush();
            scheduler.WaitForFrame(completion);
            scheduler.WaitIdle();
        }
        finally
        {
            graphicsEngine.ResourceDatabase.ReleaseResourceImmediately(backBufferHandle.AsResource());
        }
    }
}

#endif
