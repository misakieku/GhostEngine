using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public class RenderGraphTest
{
    private struct CullingPassData
    {
        public Identifier<RGBuffer> scene;
        public Identifier<RGBuffer> visibale;
    }

    private struct VBufferPassData
    {
        public Identifier<RGTexture> vbuffer;
        public Identifier<RGBuffer> scene;
        public Identifier<RGBuffer> visibale;
    }

    private struct UnusedPassData
    {
        public Identifier<RGBuffer> unusedBuffer;
    }

    private struct FinalBlitPassData
    {
        public Identifier<RGTexture> source;
        public Identifier<RGTexture> backBuffer;
    }

    private MockingRenderDevice _renderDevice = null!;
    private MockingResourceDatabase _resourceDatabase = null!;
    private MockingResourceAllocator _resourceAllocator = null!;
    private MockingPipelineLibrary _pipelineLibrary = null!;
    private MockingCommandBuffer _commandBuffer = null!;
    private ResourceManager _resourceManager = null!;
    private ShaderLibrary _shaderLibrary = null!;

    private RenderGraph _renderGraph = null!;

    [TestInitialize]
    public void Setup()
    {
        _renderDevice = new MockingRenderDevice();
        _resourceDatabase = new MockingResourceDatabase();
        _resourceAllocator = new MockingResourceAllocator(_resourceDatabase);
        _pipelineLibrary = new MockingPipelineLibrary();
        _commandBuffer = new MockingCommandBuffer(_resourceDatabase, CommandBufferType.Graphics);
        _resourceManager = new ResourceManager(_renderDevice, _resourceAllocator, _resourceDatabase);
        _shaderLibrary = new ShaderLibrary(null, null, string.Empty);

        _renderGraph = new RenderGraph(_resourceDatabase, _resourceAllocator, _pipelineLibrary, _resourceManager, _shaderLibrary);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _renderGraph.Dispose();
        _shaderLibrary.Dispose();
        _resourceManager.Dispose();
        _commandBuffer.Dispose();
        _pipelineLibrary.Dispose();
        _resourceAllocator.Dispose();
        _resourceDatabase.Dispose();
        _renderDevice.Dispose();
    }

    private void SetupTestRenderPipeline()
    {
        var sceneDataDesc = new BufferDesc();
        var backBufferDesc = new TextureDesc();

        var sceneBuffer = _renderGraph.ImportBuffer(_resourceAllocator.CreateBuffer(in sceneDataDesc));
        var backBuffer = _renderGraph.ImportTexture(_resourceAllocator.CreateTexture(in backBufferDesc));

        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("Culling"))
        {
            builder.EnableAsyncCompute(true);

            var cullingData = new CullingPassData
            {
                scene = builder.UseBuffer(sceneBuffer, AccessFlags.Read),
                visibale = builder.CreateBuffer(new BufferDesc
                {
                    Size = 100000
                })
            };

            builder.SetPassData(cullingData, true);
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) =>
            {
                Assert.IsTrue(ctx.GetActualBuffer(data.visibale).IsValid);
            });
        }

        Identifier<RGTexture> vbuffer;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("Vbuffer"))
        {
            var cullingData = _renderGraph.Blackboard.Get<CullingPassData>();
            var vbufferData = new VBufferPassData
            {
                scene = builder.UseBuffer(cullingData.scene, AccessFlags.Read),
                visibale = builder.UseBuffer(cullingData.visibale, AccessFlags.Read),
                vbuffer = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm))
            };

            vbuffer = vbufferData.vbuffer;

            builder.SetColorAttachment(vbufferData.vbuffer, 0, AccessFlags.WriteAll);
            builder.SetPassData(vbufferData);
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) =>
            {
                Assert.IsTrue(ctx.GetActualTexture(data.vbuffer).IsValid);
            });
        }

        using (var builder = _renderGraph.AddUnsafeRenderPass<UnusedPassData>("Unused"))
        {
            builder.SetPassData(new UnusedPassData
            {
                unusedBuffer = builder.CreateBuffer(new BufferDesc
                {
                    Size = 100000
                })
            });

            builder.SetRenderFunc<UnusedPassData>(static (ref readonly data, ctx) =>
            {
                Assert.Fail("This pass should be culled.");
            });
        }

        using (var builder = _renderGraph.AddUnsafeRenderPass<FinalBlitPassData>("FinalBlit"))
        {
            builder.SetPassData(new FinalBlitPassData
            {
                source = builder.UseTexture(vbuffer, AccessFlags.Read),
                backBuffer = builder.UseTexture(backBuffer, AccessFlags.WriteAll)
            });

            builder.SetRenderFunc<FinalBlitPassData>(static (ref readonly data, ctx) =>
            {
                Assert.IsTrue(ctx.GetActualTexture(data.backBuffer).IsValid);
            });
        }
    }

    [TestMethod]
    public void TestRenderGraphCompileAndExecute()
    {
        SetupTestRenderPipeline();

        _renderGraph.CompileAndExecute(_commandBuffer, new ViewState
        {
            actualWidth = 1920,
            actualHeight = 1080,
            viewportWidth = 1920,
            viewportHeight = 1080
        });
    }
}
