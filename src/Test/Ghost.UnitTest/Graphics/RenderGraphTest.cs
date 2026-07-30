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
        _shaderLibrary = new ShaderLibrary(null, _pipelineLibrary, string.Empty);

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

        var execution = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState
        {
            actualWidth = 1920,
            actualHeight = 1080,
            viewportWidth = 1920,
            viewportHeight = 1080
        }).GetValueOrThrow();

        Assert.IsNull(execution.Dump);
    }

    [TestMethod]
    public void TestRenderGraphDumpAndCulling()
    {
        SetupTestRenderPipeline();

        var execution = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState
        {
            actualWidth = 1920,
            actualHeight = 1080,
            viewportWidth = 1920,
            viewportHeight = 1080
        }, RGExecutionFlags.GenerateDump).GetValueOrThrow();

        Assert.IsNotNull(execution.Dump);

        Assert.HasCount(4, execution.Dump.Passes);
        Assert.HasCount(3, execution.Dump.Passes.Where(p => !p.IsCulled));
        Assert.HasCount(1, execution.Dump.Passes.Where(p => p.NativePassIndex != -1));

        // Verify the unused pass was culled
        var unusedPass = execution.Dump.Passes.First(p => p.Name == "Unused");
        Assert.IsTrue(unusedPass.IsCulled, "Pass 'Unused' should be culled.");
    }

    [TestMethod]
    public void TestRenderGraphCacheHit()
    {
        ViewState viewState = new ViewState
        {
            actualWidth = 1920,
            actualHeight = 1080,
            viewportWidth = 1920,
            viewportHeight = 1080
        };

        // Frame 1: Initial compilation (Cache Miss)
        SetupTestRenderPipeline();
        var execFrame1 = _renderGraph.CompileAndExecute(_commandBuffer, viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execFrame1.Dump);
        Assert.IsFalse(execFrame1.Dump.IsCacheHit, "Frame 1 should be a cache miss.");

        _renderGraph.Reset();

        // Frame 2: Same pipeline setup (Cache Hit)
        SetupTestRenderPipeline();
        var execFrame2 = _renderGraph.CompileAndExecute(_commandBuffer, viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execFrame2.Dump);
        Assert.IsTrue(execFrame2.Dump.IsCacheHit, "Frame 2 should be a cache hit.");

        // Hashes should match
        Assert.AreEqual(execFrame1.Dump.GraphHash, execFrame2.Dump.GraphHash, "Graph hashes across identical frames should match.");
    }

    [TestMethod]
    public void TestRenderGraphMemoryAliasing()
    {
        var backBufferDesc = new TextureDesc();
        var backBuffer = _renderGraph.ImportTexture(_resourceAllocator.CreateTexture(in backBufferDesc));

        Identifier<RGTexture> texA;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("PassA"))
        {
            texA = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "TextureA");
            builder.SetColorAttachment(texA, 0, AccessFlags.WriteAll);
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGTexture> texIntermediate;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("PassB"))
        {
            // Read texA, producing texIntermediate. texA lifetime ends in PassB.
            builder.UseTexture(texA, AccessFlags.Read);
            texIntermediate = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "TextureInter");
            builder.SetColorAttachment(texIntermediate, 0, AccessFlags.WriteAll);
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGTexture> texB;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("PassC"))
        {
            // Read texIntermediate, producing texB. PassC is after texA is dead.
            builder.UseTexture(texIntermediate, AccessFlags.Read);
            texB = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "TextureB");
            builder.SetColorAttachment(texB, 0, AccessFlags.WriteAll);
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = _renderGraph.AddUnsafeRenderPass<FinalBlitPassData>("PassD"))
        {
            builder.SetPassData(new FinalBlitPassData
            {
                source = builder.UseTexture(texB, AccessFlags.Read),
                backBuffer = builder.UseTexture(backBuffer, AccessFlags.WriteAll)
            });
            builder.SetRenderFunc<FinalBlitPassData>(static (ref readonly data, ctx) => { });
        }

        var exec = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState
        {
            actualWidth = 1920,
            actualHeight = 1080,
            viewportWidth = 1920,
            viewportHeight = 1080
        }, RGExecutionFlags.GenerateDump).GetValueOrThrow();

        Assert.IsNotNull(exec.Dump);

        var resA = exec.Dump.Resources.First(r => r.Name == "TextureA");
        var resB = exec.Dump.Resources.First(r => r.Name == "TextureB");

        Assert.IsFalse(resA.IsImported);
        Assert.IsFalse(resB.IsImported);

        // Memory aliasing verification: Non-overlapping lifetimes can share the same heap offset!
        Assert.AreEqual(resA.HeapOffset, resB.HeapOffset, "TextureA and TextureB should alias the same heap offset.");
    }

    [TestMethod]
    public void TestRenderGraphResourceExtraction()
    {
        var backBufferDesc = new TextureDesc();
        var backBuffer = _renderGraph.ImportTexture(_resourceAllocator.CreateTexture(in backBufferDesc));

        // Create a slot in the database for the extracted resource
        var dstSlot = _resourceDatabase.CreateEmpty().AsTexture();

        Identifier<RGTexture> transientTex;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("RenderPass"))
        {
            transientTex = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "ExtractedTexture");
            builder.SetColorAttachment(transientTex, 0, AccessFlags.WriteAll);

            // Queue extraction of transientTex into dstSlot
            builder.QueueTextureExtraction(transientTex, dstSlot);

            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = _renderGraph.AddUnsafeRenderPass<FinalBlitPassData>("FinalBlit"))
        {
            builder.SetPassData(new FinalBlitPassData
            {
                source = builder.UseTexture(transientTex, AccessFlags.Read),
                backBuffer = builder.UseTexture(backBuffer, AccessFlags.WriteAll)
            });
            builder.SetRenderFunc<FinalBlitPassData>(static (ref readonly data, ctx) => { });
        }

        var exec = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState
        {
            actualWidth = 1920,
            actualHeight = 1080,
            viewportWidth = 1920,
            viewportHeight = 1080
        }, RGExecutionFlags.GenerateDump).GetValueOrThrow();

        Assert.IsNotNull(exec.Dump);

        var extractedRes = exec.Dump.Resources.First(r => r.Name == "ExtractedTexture");
        Assert.IsTrue(extractedRes.IsExtracted, "Resource should be marked as extracted.");

        // Verify dstSlot now points to a valid GPU resource in the database
        Assert.IsTrue(_resourceDatabase.HasResource(dstSlot.AsResource()), "Destination handle slot should contain a valid extracted resource after execution.");
        Assert.AreEqual(TextureFormat.R8G8_UNorm, _resourceDatabase.GetResourceDescription(dstSlot.AsResource()).Value.TextureDescriptor.Format);
    }
}
