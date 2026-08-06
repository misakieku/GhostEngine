using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

namespace Ghost.UnitTest.Graphics;

[TestClass]
[DoNotParallelize]
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

    private struct AsyncPlannerPassData
    {
        public Identifier<RGBuffer> firstBuffer;
        public Identifier<RGBuffer> secondBuffer;
        public Identifier<RGTexture> target;
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
            builder.EnableAsyncCompute(false);

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
                backBuffer = builder.UseRenderTargetTexture(backBuffer, AccessFlags.WriteAll)
            });

            builder.SetRenderFunc<FinalBlitPassData>(static (ref readonly data, ctx) =>
            {
                Assert.IsTrue(ctx.GetActualTexture(data.backBuffer).IsValid);
            });
        }
    }

    private Identifier<RGBuffer> AddPlannerGraphicsProducer(string name)
    {
        using var builder = _renderGraph.AddRasterRenderPass<AsyncPlannerPassData>(name);
        builder.AllowPassCulling(false);
        var output = builder.CreateBuffer(new BufferDesc { Size = 1024 }, name + "_Output");
        builder.UseRandomAccessBuffer(output);
        var target = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), name + "_Target");
        builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
        builder.SetPassData(new AsyncPlannerPassData { firstBuffer = output, target = target });
        builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
        return output;
    }

    private Identifier<RGBuffer> AddPlannerAsyncCompute(
        string name,
        Identifier<RGBuffer> firstInput,
        Identifier<RGBuffer>? secondInput = null,
        bool reverseDeclarations = false)
    {
        using var builder = _renderGraph.AddComputeRenderPass<AsyncPlannerPassData>(name);
        builder.AllowPassCulling(false);
        builder.EnableAsyncCompute(true);

        if (reverseDeclarations && secondInput.HasValue)
        {
            builder.UseBuffer(secondInput.Value, AccessFlags.Read);
            builder.UseBuffer(firstInput, AccessFlags.Read);
        }
        else
        {
            builder.UseBuffer(firstInput, AccessFlags.Read);
            if (secondInput.HasValue)
            {
                builder.UseBuffer(secondInput.Value, AccessFlags.Read);
            }
        }

        var output = builder.CreateBuffer(new BufferDesc { Size = 1024 }, name + "_Output");
        builder.UseBuffer(output, AccessFlags.Write);
        builder.SetPassData(new AsyncPlannerPassData
        {
            firstBuffer = firstInput,
            secondBuffer = output
        });
        builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        return output;
    }

    private void AddPlannerRasterPass(
        string name,
        Identifier<RGBuffer>? firstInput = null,
        Identifier<RGBuffer>? secondInput = null)
    {
        using var builder = _renderGraph.AddRasterRenderPass<AsyncPlannerPassData>(name);
        builder.AllowPassCulling(false);
        if (firstInput.HasValue)
        {
            builder.UseBuffer(firstInput.Value, AccessFlags.Read);
        }
        if (secondInput.HasValue)
        {
            builder.UseBuffer(secondInput.Value, AccessFlags.Read);
        }

        var target = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), name + "_Target");
        builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
        builder.SetPassData(new AsyncPlannerPassData { target = target });
        builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
    }

    private void AddPlannerUnsafePass(string name)
    {
        using var builder = _renderGraph.AddUnsafeRenderPass<AsyncPlannerPassData>(name);
        builder.AllowPassCulling(false);
        builder.SetPassData(new AsyncPlannerPassData());
        builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
    }

    private RenderGraphDump CompilePlannerDump()
    {
        _commandBuffer.Begin(null!);
        var execution = _renderGraph.CompileAndExecute(
            _commandBuffer,
            new ViewState(1920, 1080, 1920, 1080),
            RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execution.Dump);
        return execution.Dump;
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

        Assert.AreNotEqual(0UL, execution.Dump.GraphHash, "The dump must expose the compiled graph hash.");
        Assert.HasCount(4, execution.Dump.Passes);
        Assert.HasCount(3, execution.Dump.Passes.Where(p => !p.IsCulled));
        Assert.HasCount(1, execution.Dump.Passes.Where(p => p.NativePassIndex != -1));

        for (var i = 0; i < execution.Dump.Resources.Count; i++)
        {
            Assert.AreEqual(i, execution.Dump.Resources[i].LogicalResourceId, "Resource dump IDs must match logical resource identifiers.");
        }

        var cullingPass = execution.Dump.Passes.First(p => p.Name == "Culling");
        Assert.IsFalse(cullingPass.AsyncRequested);
        Assert.AreEqual(CommandQueueType.Graphics, cullingPass.EffectiveQueue);
        Assert.IsTrue(execution.Dump.CommandStream.Any(line => line.Contains("AsyncRequested: False, EffectiveQueue: Graphics")));
        Assert.IsTrue(execution.Dump.CommandStream.Any(line => line.Contains("[Texture #") || line.Contains("[Buffer #")), "Barrier disassembly must include resource type and logical ID.");

        // Verify the unused pass was culled and therefore has no effective execution queue.
        var unusedPass = execution.Dump.Passes.First(p => p.Name == "Unused");
        Assert.IsTrue(unusedPass.IsCulled, "Pass 'Unused' should be culled.");
        Assert.IsNull(unusedPass.EffectiveQueue);
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

        // Hashes, command bytes, native-pass assignment, and transient placement must be identical.
        Assert.AreNotEqual(0UL, execFrame1.Dump.GraphHash, "The compiled graph hash must be present in diagnostics.");
        Assert.AreEqual(execFrame1.Dump.GraphHash, execFrame2.Dump.GraphHash, "Graph hashes across identical frames should match.");
        Assert.IsTrue(execFrame1.Dump.CommandStream.SequenceEqual(execFrame2.Dump.CommandStream), "Cache hits must replay the exact compiled command stream.");
        Assert.IsTrue(
            execFrame1.Dump.Passes.Select(pass => pass.NativePassIndex).SequenceEqual(execFrame2.Dump.Passes.Select(pass => pass.NativePassIndex)),
            "Cache hits must preserve native-pass assignments.");
        Assert.AreEqual(execFrame1.Dump.TotalHeapSize, execFrame2.Dump.TotalHeapSize, "Cache hits must preserve total transient heap size.");
        Assert.HasCount(execFrame1.Dump.Resources.Count, execFrame2.Dump.Resources);
        for (var i = 0; i < execFrame1.Dump.Resources.Count; i++)
        {
            var freshResource = execFrame1.Dump.Resources[i];
            var cachedResource = execFrame2.Dump.Resources[i];
            Assert.AreEqual(freshResource.LogicalResourceId, cachedResource.LogicalResourceId);
            Assert.AreEqual(freshResource.Type, cachedResource.Type);
            Assert.AreEqual(freshResource.HeapOffset, cachedResource.HeapOffset);
            Assert.AreEqual(freshResource.SizeInBytes, cachedResource.SizeInBytes);
            Assert.IsTrue(
                freshResource.AliasedWithResources.SequenceEqual(cachedResource.AliasedWithResources),
                $"Cache hits must preserve aliases for resource #{freshResource.LogicalResourceId}.");
        }

        var executionWithoutDump = _renderGraph.CompileAndExecute(_commandBuffer, viewState).GetValueOrThrow();
        Assert.IsNull(executionWithoutDump.Dump, "A previous diagnostic request must not leak into later executions.");
    }

    [TestMethod]
    public void TestCompatibleRasterPassesMergeAndCacheIdentically()
    {
        var targetDesc = new TextureDesc
        {
            Width = 1920,
            Height = 1080,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.RenderTarget
        };
        var target = _resourceAllocator.CreateTexture(in targetDesc);
        var viewState = new ViewState(1920, 1080, 1920, 1080);

        void SetupMergedPasses()
        {
            var importedTarget = _renderGraph.ImportTexture(target);
            for (var passIndex = 0; passIndex < 2; passIndex++)
            {
                using var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>($"MergedPass{passIndex}");
                builder.AllowPassCulling(false);
                builder.SetColorAttachment(importedTarget, 0, AccessFlags.Write);
                builder.SetPassData(new VBufferPassData());
                builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
            }
        }

        SetupMergedPasses();
        var freshExecution = _renderGraph.CompileAndExecute(
            _commandBuffer,
            viewState,
            RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(freshExecution.Dump);
        var freshPasses = freshExecution.Dump.Passes.Where(pass => pass.Name.StartsWith("MergedPass", StringComparison.Ordinal)).ToList();
        Assert.HasCount(2, freshPasses);
        Assert.AreNotEqual(-1, freshPasses[0].NativePassIndex);
        Assert.AreEqual(freshPasses[0].NativePassIndex, freshPasses[1].NativePassIndex, "Compatible raster passes must merge into one native pass.");
        Assert.HasCount(1, freshExecution.Dump.CommandStream.Where(line => line.Contains("BeginNativePass", StringComparison.Ordinal)));

        _renderGraph.Reset();

        SetupMergedPasses();
        var cachedExecution = _renderGraph.CompileAndExecute(
            _commandBuffer,
            viewState,
            RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(cachedExecution.Dump);
        Assert.IsTrue(cachedExecution.Dump.IsCacheHit);
        var cachedPasses = cachedExecution.Dump.Passes.Where(pass => pass.Name.StartsWith("MergedPass", StringComparison.Ordinal)).ToList();
        Assert.IsTrue(freshPasses.Select(pass => pass.NativePassIndex).SequenceEqual(cachedPasses.Select(pass => pass.NativePassIndex)));
        Assert.IsTrue(freshExecution.Dump.CommandStream.SequenceEqual(cachedExecution.Dump.CommandStream));
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
                backBuffer = builder.UseRenderTargetTexture(backBuffer, AccessFlags.WriteAll)
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

        var aliasingLines = exec.Dump.CommandStream
            .Where(line => line.Contains("Aliasing:", StringComparison.Ordinal) && line.Contains($"[Texture #{texB.Value}]", StringComparison.Ordinal))
            .ToList();
        Assert.HasCount(1, aliasingLines, "An aliased texture first use must serialize one final transition.");
        Assert.Contains($"TextureA [Texture #{texA.Value}] -> TextureB [Texture #{texB.Value}]", aliasingLines[0]);
        Assert.Contains("Layout: RenderTarget", aliasingLines[0]);
        Assert.Contains("Access: RenderTarget", aliasingLines[0]);
        Assert.Contains("Sync: RenderTarget", aliasingLines[0]);
        Assert.Contains("FirstUsage", aliasingLines[0]);
        Assert.Contains("Discard", aliasingLines[0]);

        var textureAliasingBarriers = _commandBuffer.RecordedBarriers
            .Where(barrier => barrier.IsAliasing && _resourceDatabase.GetResourceName(barrier.Resource) == "TextureB")
            .ToList();
        Assert.HasCount(1, textureAliasingBarriers, "The texture alias must execute as one aliasing barrier.");
        var textureAliasingBarrier = textureAliasingBarriers[0];
        Assert.AreEqual(BarrierLayout.RenderTarget, textureAliasingBarrier.LayoutAfter);
        Assert.AreEqual(BarrierAccess.RenderTarget, textureAliasingBarrier.AccessAfter);
        Assert.AreEqual(BarrierSync.RenderTarget, textureAliasingBarrier.SyncAfter);
        Assert.IsTrue(textureAliasingBarrier.Discard);
    }

    [TestMethod]
    public void TestAliasedBufferFirstUseEmitsSingleFinalTransition()
    {
        (Identifier<RGBuffer> Before, Identifier<RGBuffer> After) AddAliasingBufferPasses()
        {
            Identifier<RGBuffer> before;
            using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("BufferAliasPassA"))
            {
                builder.AllowPassCulling(false);
                before = builder.CreateBuffer(new BufferDesc { Size = 65536 }, "BufferAliasA");
                builder.UseBuffer(before, AccessFlags.WriteAll);
                builder.SetPassData(new CullingPassData());
                builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
            }

            Identifier<RGBuffer> intermediate;
            using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("BufferAliasPassB"))
            {
                builder.AllowPassCulling(false);
                builder.UseBuffer(before, AccessFlags.Read);
                intermediate = builder.CreateBuffer(new BufferDesc { Size = 131072 }, "BufferAliasIntermediate");
                builder.UseBuffer(intermediate, AccessFlags.WriteAll);
                builder.SetPassData(new CullingPassData());
                builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
            }

            Identifier<RGBuffer> after;
            using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("BufferAliasPassC"))
            {
                builder.AllowPassCulling(false);
                builder.UseBuffer(intermediate, AccessFlags.Read);
                after = builder.CreateBuffer(new BufferDesc { Size = 65536 }, "BufferAliasB");
                builder.UseBuffer(after, AccessFlags.WriteAll);
                builder.SetPassData(new CullingPassData());
                builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
            }

            using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("BufferAliasPassD"))
            {
                builder.AllowPassCulling(false);
                builder.UseBuffer(after, AccessFlags.Read);
                builder.SetPassData(new CullingPassData());
                builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
            }

            return (before, after);
        }

        void AssertAliasingTransition(RGExecution execution, Identifier<RGBuffer> before, Identifier<RGBuffer> after)
        {
            Assert.IsNotNull(execution.Dump);
            var beforeResource = execution.Dump.Resources.Single(resource => resource.LogicalResourceId == before.Value);
            var afterResource = execution.Dump.Resources.Single(resource => resource.LogicalResourceId == after.Value);
            Assert.AreEqual(beforeResource.HeapOffset, afterResource.HeapOffset, "The buffer lifetimes must share one heap offset for this test.");

            var aliasingLines = execution.Dump.CommandStream
                .Where(line => line.Contains("Aliasing:", StringComparison.Ordinal) && line.Contains($"[Buffer #{after.Value}]", StringComparison.Ordinal))
                .ToList();
            Assert.HasCount(1, aliasingLines, "An aliased buffer first use must serialize one final transition.");
            Assert.Contains($"BufferAliasA [Buffer #{before.Value}] -> BufferAliasB [Buffer #{after.Value}]", aliasingLines[0]);
            Assert.Contains("Access: UnorderedAccess", aliasingLines[0]);
            Assert.Contains("Sync: ComputeShading", aliasingLines[0]);
            Assert.Contains("FirstUsage", aliasingLines[0]);
            Assert.Contains("Discard", aliasingLines[0]);

            var aliasingBarriers = _commandBuffer.RecordedBarriers
                .Where(barrier => barrier.IsAliasing && _resourceDatabase.GetResourceName(barrier.Resource) == "BufferAliasB")
                .ToList();
            Assert.HasCount(1, aliasingBarriers, "The buffer alias must execute as one aliasing barrier.");
            Assert.AreEqual(BarrierAccess.UnorderedAccess, aliasingBarriers[0].AccessAfter);
            Assert.AreEqual(BarrierSync.ComputeShading, aliasingBarriers[0].SyncAfter);

            var firstUseTransitions = _commandBuffer.RecordedBarriers
                .Where(barrier => _resourceDatabase.GetResourceName(barrier.Resource) == "BufferAliasB" && barrier.AccessAfter == BarrierAccess.UnorderedAccess)
                .ToList();
            Assert.HasCount(1, firstUseTransitions, "The aliased buffer first use must not emit a second ordinary UAV transition.");
            Assert.IsTrue(firstUseTransitions[0].IsAliasing);
        }

        var (before, after) = AddAliasingBufferPasses();
        var execution = _renderGraph.CompileAndExecute(
            _commandBuffer,
            new ViewState(1920, 1080, 1920, 1080),
            RGExecutionFlags.GenerateDump).GetValueOrThrow();
        AssertAliasingTransition(execution, before, after);

        _renderGraph.Reset();
        _commandBuffer.RecordedBarriers.Clear();

        var (cachedBefore, cachedAfter) = AddAliasingBufferPasses();
        var cachedExecution = _renderGraph.CompileAndExecute(
            _commandBuffer,
            new ViewState(1920, 1080, 1920, 1080),
            RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(cachedExecution.Dump);
        Assert.IsTrue(cachedExecution.Dump.IsCacheHit, "The second equivalent graph must replay the cached barrier stream.");
        AssertAliasingTransition(cachedExecution, cachedBefore, cachedAfter);
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
                backBuffer = builder.UseRenderTargetTexture(backBuffer, AccessFlags.WriteAll)
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

#if GHOST_UNITTEST
    [TestMethod]
    public void TestRenderGraphBarrierGeneration()
    {
        var backBufferDesc = new TextureDesc();
        var backBuffer = _renderGraph.ImportTexture(_resourceAllocator.CreateTexture(in backBufferDesc));

        Identifier<RGTexture> renderTexture;

        // Pass 1: Render to renderTexture
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("RenderPass"))
        {
            renderTexture = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "RenderTexture");
            builder.SetColorAttachment(renderTexture, 0, AccessFlags.WriteAll);

            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        // Pass 2: Read renderTexture in Unsafe/Blit pass
        using (var builder = _renderGraph.AddUnsafeRenderPass<FinalBlitPassData>("BlitPass"))
        {
            builder.SetPassData(new FinalBlitPassData
            {
                source = builder.UseTexture(renderTexture, AccessFlags.Read),
                backBuffer = builder.UseRenderTargetTexture(backBuffer, AccessFlags.WriteAll)
            });
            builder.SetRenderFunc<FinalBlitPassData>(static (ref readonly data, ctx) => { });
        }

        _commandBuffer.Begin(null!);

        var exec = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState
        {
            actualWidth = 1920,
            actualHeight = 1080,
            viewportWidth = 1920,
            viewportHeight = 1080
        }).GetValueOrThrow();

        Assert.IsNotEmpty(_commandBuffer.RecordedBarriers, "Barriers should be issued during graph execution.");

        // Check that at least one barrier transitioned a texture to RenderTarget and ShaderResource
        var hasRenderTargetBarrier = _commandBuffer.RecordedBarriers.Any(b => b.LayoutAfter == BarrierLayout.RenderTarget);
        var hasShaderResourceBarrier = _commandBuffer.RecordedBarriers.Any(b => b.LayoutAfter == BarrierLayout.ShaderResource);

        Assert.IsTrue(hasRenderTargetBarrier, "Must issue a RenderTarget barrier for color attachment.");
        Assert.IsTrue(hasShaderResourceBarrier, "Must issue a ShaderResource barrier when reading texture.");
    }

    [TestMethod]
    public void TestComputeReadWriteEmitsSingleBarrier()
    {
        Identifier<RGBuffer> readWriteBuffer;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("ReadWriteCompute"))
        {
            builder.AllowPassCulling(false);
            readWriteBuffer = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "ReadWriteBuffer");
            builder.UseBuffer(readWriteBuffer, AccessFlags.ReadWrite);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        var execution = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState(1920, 1080, 1920, 1080), RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execution.Dump);

        var resourceLabel = $"[Buffer #{readWriteBuffer.Value}]";
        var barriers = execution.Dump.CommandStream.Where(line => line.Contains(resourceLabel)).ToList();
        Assert.HasCount(1, barriers, "A pass must emit one transition per whole resource.");
        Assert.Contains("Access: UnorderedAccess", barriers[0]);
    }

    [TestMethod]
    public void TestRepeatedResourceReadDeclarationIsUnique()
    {
        var backingBuffer = _resourceAllocator.CreateBuffer(new BufferDesc { Size = 1024 });
        PassRenderFunc<CullingPassData, IComputeRenderContext> renderFunc = static (ref readonly data, ctx) => { };

        Identifier<RGBuffer> SetupReadPass(bool repeatDeclaration)
        {
            var importedBuffer = _renderGraph.ImportBuffer(backingBuffer);
            using var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("RepeatedRead");
            builder.AllowPassCulling(false);
            builder.UseBuffer(importedBuffer, AccessFlags.Read);
            if (repeatDeclaration)
            {
                builder.UseBuffer(importedBuffer, AccessFlags.Read);
            }
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc(renderFunc);
            return importedBuffer;
        }

        var repeatedBuffer = SetupReadPass(repeatDeclaration: true);
        var repeatedExecution = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState(1920, 1080, 1920, 1080), RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(repeatedExecution.Dump);

        var repeatedPass = repeatedExecution.Dump.Passes.Single(pass => pass.Name == "RepeatedRead");
        Assert.HasCount(1, repeatedPass.ResourceReads.Where(id => id == repeatedBuffer.Value), "Repeated reads must produce one dependency entry.");
        var repeatedResource = repeatedExecution.Dump.Resources.Single(resource => resource.LogicalResourceId == repeatedBuffer.Value);
        Assert.HasCount(1, repeatedResource.ConsumerPasses, "Repeated reads must register the pass as one consumer.");
        Assert.IsTrue(repeatedResource.ConsumerPasses.Contains(repeatedPass.Index));

        _renderGraph.Reset();

        SetupReadPass(repeatDeclaration: false);
        var singleExecution = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState(1920, 1080, 1920, 1080), RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(singleExecution.Dump);
        Assert.AreEqual(repeatedExecution.Dump.GraphHash, singleExecution.Dump.GraphHash, "Duplicate declarations must not alter the structural graph hash.");
        Assert.IsTrue(singleExecution.Dump.IsCacheHit, "Canonical declarations must reuse the compilation cached for the equivalent graph.");
    }

    [TestMethod]
    public void TestRepeatedRandomAccessDeclarationIsUnique()
    {
        var randomAccessBuffer = _renderGraph.ImportBuffer(_resourceAllocator.CreateBuffer(new BufferDesc { Size = 1024 }));
        Identifier<RGTexture> target;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("RepeatedRandomAccess"))
        {
            builder.AllowPassCulling(false);
            builder.UseRandomAccessBuffer(randomAccessBuffer);
            builder.UseRandomAccessBuffer(randomAccessBuffer);
            target = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "RandomAccessTarget");
            builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("ConsumeRandomAccessTarget"))
        {
            builder.AllowPassCulling(false);
            builder.UseTexture(target, AccessFlags.Read);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        var execution = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState(1920, 1080, 1920, 1080), RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execution.Dump);

        var pass = execution.Dump.Passes.Single(item => item.Name == "RepeatedRandomAccess");
        Assert.HasCount(1, pass.ResourceReads.Where(id => id == randomAccessBuffer.Value), "Random access must retain one read dependency.");
        Assert.HasCount(1, pass.ResourceWrites.Where(id => id == randomAccessBuffer.Value), "Random access must retain one write dependency.");

        var resource = execution.Dump.Resources.Single(item => item.LogicalResourceId == randomAccessBuffer.Value);
        Assert.HasCount(1, resource.ConsumerPasses, "Repeated random access must register one consumer.");
        Assert.HasCount(1, resource.ProducerPass, "Repeated random access must register one producer.");

        var resourceLabel = $"[Buffer #{randomAccessBuffer.Value}]";
        Assert.HasCount(1, execution.Dump.CommandStream.Where(line => line.Contains(resourceLabel)), "Repeated random access must emit one explicit UAV declaration in Step 3.");
    }

    [TestMethod]
    public void TestCanonicalDeclarationsPreserveRawWarAndWawDependencies()
    {
        var sharedBuffer = _renderGraph.ImportBuffer(_resourceAllocator.CreateBuffer(new BufferDesc { Size = 1024 }));
        PassRenderFunc<CullingPassData, IComputeRenderContext> renderFunc = static (ref readonly data, ctx) => { };

        void AddPass(string name, AccessFlags accessFlags)
        {
            using var builder = _renderGraph.AddComputeRenderPass<CullingPassData>(name);
            builder.AllowPassCulling(false);
            builder.UseBuffer(sharedBuffer, accessFlags);
            builder.UseBuffer(sharedBuffer, accessFlags);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc(renderFunc);
        }

        AddPass("WriteA", AccessFlags.Write);
        AddPass("ReadB", AccessFlags.Read);
        AddPass("WriteC", AccessFlags.Write);
        AddPass("WriteD", AccessFlags.Write);

        var execution = _renderGraph.CompileAndExecute(_commandBuffer, new ViewState(1920, 1080, 1920, 1080), RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execution.Dump);

        var executionOrder = execution.Dump.CommandStream.Where(line => line.Contains("ExecutePass")).ToList();
        Assert.HasCount(4, executionOrder);
        Assert.Contains("'WriteA'", executionOrder[0]);
        Assert.Contains("'ReadB'", executionOrder[1]);
        Assert.Contains("'WriteC'", executionOrder[2]);
        Assert.Contains("'WriteD'", executionOrder[3]);

        var resource = execution.Dump.Resources.Single(item => item.LogicalResourceId == sharedBuffer.Value);
        Assert.HasCount(3, resource.ProducerPass, "Writers must be registered once per pass.");
        Assert.HasCount(1, resource.ConsumerPasses, "Readers must be registered once per pass.");

        foreach (var pass in execution.Dump.Passes)
        {
            Assert.IsFalse(pass.IsCulled);
            Assert.HasCount(pass.ResourceReads.Distinct().Count(), pass.ResourceReads, $"Pass '{pass.Name}' contains duplicate read declarations.");
            Assert.HasCount(pass.ResourceWrites.Distinct().Count(), pass.ResourceWrites, $"Pass '{pass.Name}' contains duplicate write declarations.");
        }
    }

    [TestMethod]
    public void TestResourceDeclarationOrderDoesNotAffectHashOrCache()
    {
        var firstBufferHandle = _resourceAllocator.CreateBuffer(new BufferDesc { Size = 1024 });
        var secondBufferHandle = _resourceAllocator.CreateBuffer(new BufferDesc { Size = 2048 });
        var viewState = new ViewState(1920, 1080, 1920, 1080);

        void SetupPipeline(bool reverseDeclarationOrder)
        {
            var firstBuffer = _renderGraph.ImportBuffer(firstBufferHandle);
            var secondBuffer = _renderGraph.ImportBuffer(secondBufferHandle);

            using var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("CanonicalResourceOrder");
            builder.AllowPassCulling(false);
            if (reverseDeclarationOrder)
            {
                builder.UseBuffer(secondBuffer, AccessFlags.Read);
                builder.UseBuffer(firstBuffer, AccessFlags.Read);
            }
            else
            {
                builder.UseBuffer(firstBuffer, AccessFlags.Read);
                builder.UseBuffer(secondBuffer, AccessFlags.Read);
            }
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        SetupPipeline(false);
        var firstExecution = _renderGraph.CompileAndExecute(_commandBuffer, viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(firstExecution.Dump);
        Assert.IsFalse(firstExecution.Dump.IsCacheHit);

        _renderGraph.Reset();

        SetupPipeline(true);
        var secondExecution = _renderGraph.CompileAndExecute(_commandBuffer, viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(secondExecution.Dump);
        Assert.IsTrue(secondExecution.Dump.IsCacheHit);
        Assert.AreEqual(firstExecution.Dump.GraphHash, secondExecution.Dump.GraphHash);
        Assert.IsTrue(firstExecution.Dump.CommandStream.SequenceEqual(secondExecution.Dump.CommandStream));
    }

#if GHOST_UNITTEST
    [TestMethod]
    public void TestMockQueueRejectsRecordingCommandBufferSubmission()
    {
        MockingCommandQueue.GlobalRecordedOps.Clear();
        var commandBuffer = new MockingCommandBuffer(_resourceDatabase, CommandBufferType.Graphics);
        var queue = new MockingCommandQueue(CommandQueueType.Graphics, validateCommandBufferState: true);
        commandBuffer.Begin(null!);

        Assert.ThrowsExactly<InvalidOperationException>(() => queue.Submit(commandBuffer));
        Assert.HasCount(1, MockingCommandQueue.GlobalRecordedOps);
        Assert.IsTrue(MockingCommandQueue.GlobalRecordedOps[0].CommandBufferWasRecording);
    }

    [TestMethod]
    public void TestAsyncComputeEligibilityExecutesOnGraphicsQueue()
    {
        MockingCommandQueue.GlobalRecordedOps.Clear();

        Identifier<RGBuffer> computeOutputBuffer;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("AsyncComputeCulling"))
        {
            builder.AllowPassCulling(false);
            builder.EnableAsyncCompute(true);

            computeOutputBuffer = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "CullingBuffer");
            builder.UseBuffer(computeOutputBuffer, AccessFlags.Write);

            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        }

        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("RasterPass"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(computeOutputBuffer, AccessFlags.Read);
            var vbuffer = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "VBuffer");
            builder.SetColorAttachment(vbuffer, 0, AccessFlags.WriteAll);

            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        _commandBuffer.Begin(null!);

        var execResult = _renderGraph.CompileAndExecute(
            _commandBuffer,
            new ViewState(1920, 1080, 1920, 1080),
            RGExecutionFlags.GenerateDump);

        Assert.IsTrue(execResult.IsSuccess, "Async-eligible compute should execute successfully on the graphics queue.");
        var exec = execResult.Value;
        Assert.IsNotNull(exec.Dump, "Execution dump should not be null.");

        var asyncPass = exec.Dump.Passes.First(p => p.Name == "AsyncComputeCulling");
        Assert.IsTrue(asyncPass.AsyncCompute, "AsyncComputeCulling should retain the compatibility flag.");
        Assert.IsTrue(asyncPass.AsyncRequested, "AsyncComputeCulling should retain async scheduling intent.");
        Assert.AreEqual(CommandQueueType.Graphics, asyncPass.EffectiveQueue, "Async-eligible compute must execute on Graphics during containment.");
        Assert.AreEqual(1, _commandBuffer.DispatchCallCount, "The compute callback must dispatch through the graphics command buffer.");
        Assert.IsEmpty(MockingCommandQueue.GlobalRecordedOps, "The contained render graph must not submit, signal, or wait on queues.");
        Assert.IsTrue(exec.Dump.CommandStream.Any(line => line.Contains("AsyncComputeCulling") && line.Contains("AsyncRequested: True, EffectiveQueue: Graphics")));
        var hasQueueOperation = exec.Dump.CommandStream.Any(line => line.Contains("SignalFence") || line.Contains("SubmitQueue") || line.Contains("GPUWait"));
        Assert.IsFalse(hasQueueOperation, "The command stream must not contain graph-owned queue synchronization.");
    }
#endif

    [TestMethod]
    public void TestAsyncComputeEligibilityAffectsGraphHash()
    {
        PassRenderFunc<CullingPassData, IComputeRenderContext> computeRenderFunc = static (ref readonly data, ctx) => { };
        PassRenderFunc<VBufferPassData, IRasterRenderContext> rasterRenderFunc = static (ref readonly data, ctx) => { };
        var viewState = new ViewState(1920, 1080, 1920, 1080);

        void SetupComputePass(bool asyncRequested)
        {
            Identifier<RGBuffer> output;
            using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("HashComputePass"))
            {
                builder.AllowPassCulling(false);
                builder.EnableAsyncCompute(asyncRequested);
                output = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "HashOutput");
                builder.UseBuffer(output, AccessFlags.Write);
                builder.SetPassData(new CullingPassData());
                builder.SetRenderFunc(computeRenderFunc);
            }

            using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("HashRasterPass"))
            {
                builder.AllowPassCulling(false);
                builder.UseBuffer(output, AccessFlags.Read);
                var target = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "HashTarget");
                builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
                builder.SetPassData(new VBufferPassData());
                builder.SetRenderFunc(rasterRenderFunc);
            }
        }

        SetupComputePass(asyncRequested: true);
        var asyncExecution = _renderGraph.CompileAndExecute(_commandBuffer, viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(asyncExecution.Dump);

        _renderGraph.Reset();

        SetupComputePass(asyncRequested: false);
        var graphicsExecution = _renderGraph.CompileAndExecute(_commandBuffer, viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(graphicsExecution.Dump);

        Assert.AreNotEqual(asyncExecution.Dump.GraphHash, graphicsExecution.Dump.GraphHash, "Async scheduling intent must remain part of the graph hash.");
        var asyncPass = asyncExecution.Dump.Passes.Single(pass => pass.Name == "HashComputePass");
        var graphicsPass = graphicsExecution.Dump.Passes.Single(pass => pass.Name == "HashComputePass");
        Assert.IsTrue(asyncPass.AsyncRequested);
        Assert.IsFalse(graphicsPass.AsyncRequested);
        Assert.AreEqual(CommandQueueType.Graphics, asyncPass.EffectiveQueue);
        Assert.AreEqual(CommandQueueType.Graphics, graphicsPass.EffectiveQueue);
    }
#endif

    // ------------------------------------------------------------------------------------------
    // Phase 2: Dependency-window sync-point planning
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void TestAsyncPlanner_EligibleForkJoinEmitsStructuralOverlapWindow()
    {
        var producerOutput = AddPlannerGraphicsProducer("PlannerProducer");
        var computeOutput = AddPlannerAsyncCompute("PlannerCompute", producerOutput);
        AddPlannerRasterPass("PlannerIndependent");
        AddPlannerRasterPass("PlannerConsumer", computeOutput);

        var dump = CompilePlannerDump();
        var markers = dump.CommandStream.Where(line => line.Contains("CommandBufferSyncPoint")).ToList();
        Assert.HasCount(3, markers);
        Assert.Contains("SourceType: Graphics, NextType: Compute", markers[0]);
        Assert.Contains("DependsOn: [0]", markers[0]);
        Assert.Contains("SourceType: Compute, NextType: Graphics", markers[1]);
        Assert.Contains("DependsOn: [none]", markers[1]);
        Assert.Contains("SourceType: Graphics, NextType: Graphics", markers[2]);
        Assert.Contains("DependsOn: [1]", markers[2]);

        var producer = dump.Passes.Single(pass => pass.Name == "PlannerProducer");
        var compute = dump.Passes.Single(pass => pass.Name == "PlannerCompute");
        var independent = dump.Passes.Single(pass => pass.Name == "PlannerIndependent");
        var consumer = dump.Passes.Single(pass => pass.Name == "PlannerConsumer");

        Assert.AreEqual(CommandQueueType.Graphics, producer.EffectiveQueue);
        Assert.AreEqual(CommandQueueType.Compute, compute.EffectiveQueue);
        Assert.AreEqual(CommandQueueType.Graphics, independent.EffectiveQueue);
        Assert.AreEqual(CommandQueueType.Graphics, consumer.EffectiveQueue);
        Assert.AreEqual(RGQueueDecision.AsyncComputeSelected, compute.QueueDecision);
        Assert.IsTrue(compute.SyncBoundaryBefore.HasValue);
        Assert.IsTrue(compute.SyncBoundaryAfter.HasValue);
        Assert.IsTrue(consumer.SyncBoundaryBefore.HasValue);
        Assert.AreEqual(CommandQueueType.Graphics, compute.SyncBoundaryBefore.Value.SourceType);
        Assert.AreEqual(CommandQueueType.Compute, compute.SyncBoundaryBefore.Value.DestinationType);
        Assert.AreEqual(CommandQueueType.Compute, consumer.SyncBoundaryBefore.Value.ProducerTypes[0]);
        Assert.AreEqual(1, _commandBuffer.DispatchCallCount, "Phase 2 must still record Compute callbacks into the Graphics command buffer.");
    }

    [TestMethod]
    public void TestAsyncPlanner_SerialComputeWithoutIndependentGraphicsIsDemoted()
    {
        var producerOutput = AddPlannerGraphicsProducer("SerialProducer");
        var computeOutput = AddPlannerAsyncCompute("SerialCompute", producerOutput);
        AddPlannerRasterPass("SerialConsumer", computeOutput);

        var dump = CompilePlannerDump();
        Assert.IsFalse(dump.CommandStream.Any(line => line.Contains("CommandBufferSyncPoint")));

        var compute = dump.Passes.Single(pass => pass.Name == "SerialCompute");
        Assert.AreEqual(CommandQueueType.Graphics, compute.EffectiveQueue);
        Assert.AreEqual(RGQueueDecision.NoLegalOverlapWindow, compute.QueueDecision);
    }

    [TestMethod]
    public void TestAsyncPlanner_CompatibleComputePassesShareOneRegion()
    {
        var producerOutput = AddPlannerGraphicsProducer("GroupedProducer");
        var firstOutput = AddPlannerAsyncCompute("GroupedComputeA", producerOutput);
        var secondOutput = AddPlannerAsyncCompute("GroupedComputeB", producerOutput);
        AddPlannerRasterPass("GroupedIndependent");
        AddPlannerRasterPass("GroupedConsumer", firstOutput, secondOutput);

        var dump = CompilePlannerDump();
        Assert.HasCount(3, dump.CommandStream.Where(line => line.Contains("CommandBufferSyncPoint")));
        Assert.AreEqual(CommandQueueType.Compute, dump.Passes.Single(pass => pass.Name == "GroupedComputeA").EffectiveQueue);
        Assert.AreEqual(CommandQueueType.Compute, dump.Passes.Single(pass => pass.Name == "GroupedComputeB").EffectiveQueue);
    }

    [TestMethod]
    public void TestAsyncPlanner_ConflictingCandidatesUseOriginalIndexTieBreaker()
    {
        var producerOutput = AddPlannerGraphicsProducer("ConflictProducer");
        var firstOutput = AddPlannerAsyncCompute("ConflictComputeA", producerOutput);
        var secondOutput = AddPlannerAsyncCompute("ConflictComputeB", producerOutput);
        AddPlannerRasterPass("ConflictIndependent");
        AddPlannerRasterPass("ConflictConsumerA", firstOutput);
        AddPlannerRasterPass("ConflictConsumerB", secondOutput);

        var dump = CompilePlannerDump();
        var first = dump.Passes.Single(pass => pass.Name == "ConflictComputeA");
        var second = dump.Passes.Single(pass => pass.Name == "ConflictComputeB");
        Assert.AreEqual(CommandQueueType.Compute, first.EffectiveQueue);
        Assert.AreEqual(RGQueueDecision.AsyncComputeSelected, first.QueueDecision);
        Assert.AreEqual(CommandQueueType.Graphics, second.EffectiveQueue);
        Assert.AreEqual(RGQueueDecision.NoLegalOverlapWindow, second.QueueDecision);
    }

    [TestMethod]
    public void TestAsyncPlanner_UnsafeOverlapAndImportedSideEffectsAreDemoted()
    {
        var producerOutput = AddPlannerGraphicsProducer("UnsafeProducer");
        var computeOutput = AddPlannerAsyncCompute("UnsafeCompute", producerOutput);
        AddPlannerUnsafePass("UnsafeOverlap");
        AddPlannerRasterPass("UnsafeIndependent");
        AddPlannerRasterPass("UnsafeConsumer", computeOutput);

        var unsafeDump = CompilePlannerDump();
        Assert.AreEqual(CommandQueueType.Graphics, unsafeDump.Passes.Single(pass => pass.Name == "UnsafeCompute").EffectiveQueue);
        Assert.AreEqual(CommandQueueType.Graphics, unsafeDump.Passes.Single(pass => pass.Name == "UnsafeOverlap").EffectiveQueue);
        Assert.IsFalse(unsafeDump.CommandStream.Any(line => line.Contains("CommandBufferSyncPoint")));

        _renderGraph.Reset();
        var importedOutput = _renderGraph.ImportBuffer(_resourceAllocator.CreateBuffer(new BufferDesc { Size = 1024 }));
        producerOutput = AddPlannerGraphicsProducer("SideEffectProducer");
        using (var builder = _renderGraph.AddComputeRenderPass<AsyncPlannerPassData>("SideEffectCompute"))
        {
            builder.AllowPassCulling(false);
            builder.EnableAsyncCompute(true);
            builder.UseBuffer(producerOutput, AccessFlags.Read);
            builder.UseBuffer(importedOutput, AccessFlags.Write);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = producerOutput, secondBuffer = importedOutput });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        }
        AddPlannerRasterPass("SideEffectIndependent");
        AddPlannerRasterPass("SideEffectConsumer", importedOutput);

        var sideEffectDump = CompilePlannerDump();
        var sideEffectCompute = sideEffectDump.Passes.Single(pass => pass.Name == "SideEffectCompute");
        Assert.AreEqual(CommandQueueType.Graphics, sideEffectCompute.EffectiveQueue);
        Assert.AreEqual(RGQueueDecision.NoLegalOverlapWindow, sideEffectCompute.QueueDecision);
        Assert.IsFalse(sideEffectDump.CommandStream.Any(line => line.Contains("CommandBufferSyncPoint")));
    }

    [TestMethod]
    public void TestAsyncPlanner_DeclarationOrderAndCachePreserveMarkers()
    {
        var secondInputHandle = _resourceAllocator.CreateBuffer(new BufferDesc { Size = 1024 });

        void SetupPipeline(bool reverseDeclarations)
        {
            var secondInput = _renderGraph.ImportBuffer(secondInputHandle);
            var producerOutput = AddPlannerGraphicsProducer("CacheProducer");
            var computeOutput = AddPlannerAsyncCompute("CacheCompute", producerOutput, secondInput, reverseDeclarations);
            AddPlannerRasterPass("CacheIndependent");
            AddPlannerRasterPass("CacheConsumer", computeOutput);
        }

        SetupPipeline(reverseDeclarations: false);
        var freshDump = CompilePlannerDump();
        Assert.IsFalse(freshDump.IsCacheHit);

        _renderGraph.Reset();
        SetupPipeline(reverseDeclarations: true);
        var cachedDump = CompilePlannerDump();
        Assert.IsTrue(cachedDump.IsCacheHit);
        Assert.AreEqual(freshDump.GraphHash, cachedDump.GraphHash);
        Assert.IsTrue(freshDump.CommandStream.SequenceEqual(cachedDump.CommandStream));
        Assert.HasCount(3, cachedDump.CommandStream.Where(line => line.Contains("CommandBufferSyncPoint")));
    }

    // ------------------------------------------------------------------------------------------
    // Phase 1: Structural sync-point command-stream encoding
    // ------------------------------------------------------------------------------------------

    [TestMethod]
    public void TestPhase3_CrossQueueHandoffsAreSerializedAndContained()
    {
        var producer = AddPlannerGraphicsProducer("HandoffProducer");
        var computeOutput = AddPlannerAsyncCompute("HandoffCompute", producer);
        AddPlannerRasterPass("HandoffIndependent");
        AddPlannerRasterPass("HandoffJoin", computeOutput);

        var dump = CompilePlannerDump();
        var releases = dump.CommandStream.Where(line => line.Contains("QueueRelease", StringComparison.Ordinal)).ToList();
        var acquires = dump.CommandStream.Where(line => line.Contains("QueueAcquire", StringComparison.Ordinal)).ToList();

        Assert.HasCount(2, releases);
        Assert.HasCount(2, acquires);
        Assert.IsTrue(releases.Any(line => line.Contains("Graphics -> Compute", StringComparison.Ordinal)));
        Assert.IsTrue(releases.Any(line => line.Contains("Compute -> Graphics", StringComparison.Ordinal)));
        Assert.IsTrue(releases.All(line => line.Contains("Access: NoAccess", StringComparison.Ordinal)));
        Assert.IsTrue(releases.All(line => line.Contains("Sync: None", StringComparison.Ordinal)));
        Assert.AreEqual(1, _commandBuffer.DispatchCallCount, "Contained execution must still record Compute work on Graphics.");
    }

    [TestMethod]
    public void TestPhase3_CrossQueueRawWarAndWawEachProduceOneHandoff()
    {
        Identifier<RGBuffer> rawResource;
        Identifier<RGTexture> warResource;
        Identifier<RGBuffer> wawResource;
        using (var builder = _renderGraph.AddRasterRenderPass<AsyncPlannerPassData>("HazardProducer"))
        {
            builder.AllowPassCulling(false);
            rawResource = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "RawHazard");
            builder.UseRandomAccessBuffer(rawResource);
            warResource = builder.CreateTexture(
                RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm),
                "WarHazard");
            builder.UseTexture(warResource, AccessFlags.Read);
            wawResource = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "WawHazard");
            builder.UseRandomAccessBuffer(wawResource);
            var target = builder.CreateTexture(
                RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm),
                "HazardProducerTarget");
            builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = rawResource, secondBuffer = wawResource, target = target });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGBuffer> computeOutput;
        using (var builder = _renderGraph.AddComputeRenderPass<AsyncPlannerPassData>("HazardCompute"))
        {
            builder.AllowPassCulling(false);
            builder.EnableAsyncCompute(true);
            builder.UseBuffer(rawResource, AccessFlags.Read);
            builder.UseTexture(warResource, AccessFlags.Write);
            builder.UseBuffer(wawResource, AccessFlags.Write);
            computeOutput = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "HazardComputeOutput");
            builder.UseBuffer(computeOutput, AccessFlags.Write);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = rawResource, secondBuffer = computeOutput });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        }

        AddPlannerRasterPass("HazardIndependent");
        AddPlannerRasterPass("HazardJoin", computeOutput);
        var dump = CompilePlannerDump();
        var releases = dump.CommandStream
            .Where(line => line.Contains("QueueRelease", StringComparison.Ordinal)
                && line.Contains("Graphics -> Compute", StringComparison.Ordinal))
            .ToList();
        var acquires = dump.CommandStream
            .Where(line => line.Contains("QueueAcquire", StringComparison.Ordinal)
                && line.Contains("Graphics -> Compute", StringComparison.Ordinal))
            .ToList();

        Assert.HasCount(3, releases);
        Assert.HasCount(3, acquires);
        Assert.IsTrue(releases.Any(line => line.Contains("RawHazard", StringComparison.Ordinal)));
        Assert.IsTrue(releases.Any(line => line.Contains("WarHazard", StringComparison.Ordinal)));
        Assert.IsTrue(releases.Any(line => line.Contains("WawHazard", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void TestPhase3_ParallelReadersPreserveCrossQueueWarHandoff()
    {
        var sharedResource = AddPlannerGraphicsProducer("WarFanoutProducer");
        var computeOutput = AddPlannerAsyncCompute("WarFanoutCompute", sharedResource);
        AddPlannerRasterPass("WarFanoutGraphicsReader", sharedResource);

        using (var builder = _renderGraph.AddRasterRenderPass<AsyncPlannerPassData>("WarFanoutJoin"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(computeOutput, AccessFlags.Read);
            builder.UseRandomAccessBuffer(sharedResource);
            var target = builder.CreateTexture(
                RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm),
                "WarFanoutJoinTarget");
            builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = computeOutput, secondBuffer = sharedResource, target = target });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
        }

        var dump = CompilePlannerDump();
        var warReleases = dump.CommandStream
            .Where(line => line.Contains("QueueRelease", StringComparison.Ordinal)
                && line.Contains("Compute -> Graphics", StringComparison.Ordinal)
                && line.Contains("WarFanoutProducer_Output", StringComparison.Ordinal))
            .ToList();

        Assert.AreEqual(CommandQueueType.Compute, dump.Passes.Single(pass => pass.Name == "WarFanoutCompute").EffectiveQueue);
        Assert.HasCount(1, warReleases);
    }

    [TestMethod]
    public void TestPhase3_ComputeUavWawForcesSameStateBarrier()
    {
        Identifier<RGBuffer> buffer;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("UavWriteA"))
        {
            builder.AllowPassCulling(false);
            buffer = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "ForcedUavBuffer");
            builder.UseBuffer(buffer, AccessFlags.ReadWrite);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        }

        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("UavWriteB"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(buffer, AccessFlags.ReadWrite);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        }

        _commandBuffer.Begin(null!);
        var execution = _renderGraph.CompileAndExecute(
            _commandBuffer,
            new ViewState(1920, 1080, 1920, 1080),
            RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execution.Dump);
        Assert.IsTrue(_commandBuffer.RecordedBarriers.Any(barrier => barrier.Force));
        Assert.IsTrue(execution.Dump.CommandStream.Any(
            line => line.Contains("ForcedUavBuffer", StringComparison.Ordinal)
                && line.Contains("Force", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void TestPhase3_IncomparableGraphicsAndComputeResourcesDoNotAlias()
    {
        var producer = AddPlannerGraphicsProducer("AliasForkProducer");
        Identifier<RGBuffer> computeTransient;
        Identifier<RGBuffer> computeOutput;
        using (var builder = _renderGraph.AddComputeRenderPass<AsyncPlannerPassData>("AliasForkCompute"))
        {
            builder.AllowPassCulling(false);
            builder.EnableAsyncCompute(true);
            builder.UseBuffer(producer, AccessFlags.Read);
            computeTransient = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "ComputeTransient");
            builder.UseBuffer(computeTransient, AccessFlags.ReadWrite);
            computeOutput = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "AliasForkOutput");
            builder.UseBuffer(computeOutput, AccessFlags.Write);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = producer, secondBuffer = computeOutput });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        }

        Identifier<RGBuffer> graphicsTransient;
        using (var builder = _renderGraph.AddRasterRenderPass<AsyncPlannerPassData>("AliasForkGraphics"))
        {
            builder.AllowPassCulling(false);
            graphicsTransient = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "GraphicsTransient");
            var target = builder.CreateTexture(
                RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm),
                "AliasForkGraphicsTarget");
            builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
            builder.SetPassData(new AsyncPlannerPassData { target = target });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
        }

        AddPlannerRasterPass("AliasForkJoin", computeOutput);
        var dump = CompilePlannerDump();
        var computeResource = dump.Resources.Single(resource => resource.LogicalResourceId == computeTransient.Value);
        var graphicsResource = dump.Resources.Single(resource => resource.LogicalResourceId == graphicsTransient.Value);

        Assert.AreNotEqual(computeResource.HeapOffset, graphicsResource.HeapOffset);
        Assert.DoesNotContain(graphicsTransient.Value, computeResource.AliasedWithResources);
        Assert.DoesNotContain(computeTransient.Value, graphicsResource.AliasedWithResources);
    }

    [TestMethod]
    public void TestPhase3_TransitivelyOrderedCrossQueueResourcesMayAlias()
    {
        Identifier<RGBuffer> firstResource;
        Identifier<RGBuffer> dependencyResource;
        using (var builder = _renderGraph.AddRasterRenderPass<AsyncPlannerPassData>("AliasOrderedProducer"))
        {
            builder.AllowPassCulling(false);
            firstResource = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "OrderedBefore");
            dependencyResource = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "OrderingDependency");
            var target = builder.CreateTexture(
                RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm),
                "AliasOrderedProducerTarget");
            builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = dependencyResource, target = target });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGBuffer> secondResource;
        using (var builder = _renderGraph.AddComputeRenderPass<AsyncPlannerPassData>("AliasOrderedCompute"))
        {
            builder.AllowPassCulling(false);
            builder.EnableAsyncCompute(true);
            builder.UseBuffer(dependencyResource, AccessFlags.Read);
            secondResource = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "OrderedAfter");
            builder.UseBuffer(secondResource, AccessFlags.Write);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = dependencyResource, secondBuffer = secondResource });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        }

        AddPlannerRasterPass("AliasOrderedIndependent");
        AddPlannerRasterPass("AliasOrderedJoin", secondResource);
        var dump = CompilePlannerDump();
        var before = dump.Resources.Single(resource => resource.LogicalResourceId == firstResource.Value);
        var after = dump.Resources.Single(resource => resource.LogicalResourceId == secondResource.Value);

        Assert.AreEqual(before.HeapOffset, after.HeapOffset);
        Assert.Contains(secondResource.Value, before.AliasedWithResources);
        Assert.Contains(firstResource.Value, after.AliasedWithResources);
    }

    [TestMethod]
    public void TestPhase3_SyncBoundarySplitsCompatibleNativePasses()
    {
        var producer = AddPlannerGraphicsProducer("NativeSplitProducer");
        var computeOutput = AddPlannerAsyncCompute("NativeSplitCompute", producer);
        var targetDesc = new TextureDesc
        {
            Width = 1920,
            Height = 1080,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.RenderTarget
        };
        var target = _renderGraph.ImportTexture(_resourceAllocator.CreateTexture(in targetDesc));

        void AddTargetPass(string name, Identifier<RGBuffer>? input)
        {
            using var builder = _renderGraph.AddRasterRenderPass<AsyncPlannerPassData>(name);
            builder.AllowPassCulling(false);
            if (input.HasValue)
            {
                builder.UseBuffer(input.Value, AccessFlags.Read);
            }
            builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
            builder.SetPassData(new AsyncPlannerPassData { target = target });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
        }

        AddTargetPass("NativeSplitIndependent", null);
        AddTargetPass("NativeSplitJoin", computeOutput);
        var dump = CompilePlannerDump();
        var independent = dump.Passes.Single(pass => pass.Name == "NativeSplitIndependent");
        var join = dump.Passes.Single(pass => pass.Name == "NativeSplitJoin");

        Assert.AreEqual(CommandQueueType.Compute, dump.Passes.Single(pass => pass.Name == "NativeSplitCompute").EffectiveQueue);
        Assert.AreNotEqual(independent.NativePassIndex, join.NativePassIndex);
        Assert.HasCount(3, dump.CommandStream.Where(line => line.Contains("CommandBufferSyncPoint", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void TestPhase3_ViewportGrowthPreservesAliasGroupsAndCommandBytes()
    {
        SetupTestRenderPipeline();
        var firstExecution = _renderGraph.CompileAndExecute(
            _commandBuffer,
            new ViewState(960, 540, 960, 540),
            RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(firstExecution.Dump);

        _renderGraph.Reset();
        SetupTestRenderPipeline();
        var grownExecution = _renderGraph.CompileAndExecute(
            _commandBuffer,
            new ViewState(1920, 1080, 1920, 1080),
            RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(grownExecution.Dump);
        Assert.IsTrue(grownExecution.Dump.IsCacheHit);
        Assert.IsTrue(firstExecution.Dump.CommandStream.SequenceEqual(grownExecution.Dump.CommandStream));

        foreach (var firstResource in firstExecution.Dump.Resources)
        {
            var grownResource = grownExecution.Dump.Resources.Single(
                resource => resource.LogicalResourceId == firstResource.LogicalResourceId);
            Assert.IsTrue(firstResource.AliasedWithResources.SequenceEqual(grownResource.AliasedWithResources));
            Assert.AreEqual(firstResource.ScheduledFirstUseIndex, grownResource.ScheduledFirstUseIndex);
            Assert.AreEqual(firstResource.ScheduledLastUseIndex, grownResource.ScheduledLastUseIndex);
        }
    }

    [TestMethod]
    public void TestSyncMarkerRoundTrip_SingleDependency()
    {
        using var scope = AllocationManager.CreateStackScope();
        var writer = new BufferWriter(256, scope.AllocationHandle);

        // Write a sync marker: command buffer 1 (Compute) depends on command buffer 0 (Graphics).
        var producerIds = new int[] { 0 };
        RGCommandStream.WriteSyncMarker(ref writer, CommandQueueType.Compute, producerIds, nextCommandBufferId: 1);

        var reader = new SpanReader(writer.AsSpan());
        var opcode = reader.Read<RGExecutionOpType>();
        Assert.AreEqual(RGExecutionOpType.CommandBufferSyncPoint, opcode);

        var marker = RGCommandStream.ReadSyncMarker(ref reader);
        Assert.AreEqual(CommandQueueType.Compute, marker.NextCommandBufferType);
        Assert.AreEqual(1, marker.ProducerCommandBufferIds.Length);
        Assert.AreEqual(0, marker.ProducerCommandBufferIds[0]);
        Assert.AreEqual(0, reader.RemainingBytes, "No trailing bytes expected after a single sync marker.");
        writer.Dispose();
    }

    [TestMethod]
    public void TestSyncMarkerRoundTrip_NoDependencies()
    {
        using var scope = AllocationManager.CreateStackScope();
        var writer = new BufferWriter(256, scope.AllocationHandle);

        RGCommandStream.WriteSyncMarker(ref writer, CommandQueueType.Graphics, ReadOnlySpan<int>.Empty, nextCommandBufferId: 1);

        var reader = new SpanReader(writer.AsSpan());
        Assert.AreEqual(RGExecutionOpType.CommandBufferSyncPoint, reader.Read<RGExecutionOpType>());

        var marker = RGCommandStream.ReadSyncMarker(ref reader);
        Assert.AreEqual(CommandQueueType.Graphics, marker.NextCommandBufferType);
        Assert.AreEqual(0, marker.ProducerCommandBufferIds.Length);
        Assert.AreEqual(0, reader.RemainingBytes);
        writer.Dispose();
    }

    [TestMethod]
    public void TestSyncMarkerRoundTrip_MultipleDependencies()
    {
        using var scope = AllocationManager.CreateStackScope();
        var writer = new BufferWriter(256, scope.AllocationHandle);

        // Fork/join: command buffer 3 (Graphics consumer) depends on Compute 1 and Graphics 2.
        var producerIds = new int[] { 1, 2 };
        RGCommandStream.WriteSyncMarker(ref writer, CommandQueueType.Graphics, producerIds, nextCommandBufferId: 3);

        var reader = new SpanReader(writer.AsSpan());
        Assert.AreEqual(RGExecutionOpType.CommandBufferSyncPoint, reader.Read<RGExecutionOpType>());

        var marker = RGCommandStream.ReadSyncMarker(ref reader);
        Assert.AreEqual(CommandQueueType.Graphics, marker.NextCommandBufferType);
        Assert.AreEqual(2, marker.ProducerCommandBufferIds.Length);
        Assert.AreEqual(1, marker.ProducerCommandBufferIds[0]);
        Assert.AreEqual(2, marker.ProducerCommandBufferIds[1]);
        Assert.AreEqual(0, reader.RemainingBytes);
        writer.Dispose();
    }

    [TestMethod]
    public void TestSyncMarkerRoundTrip_MultipleMarkersInStream()
    {
        using var scope = AllocationManager.CreateStackScope();
        var writer = new BufferWriter(512, scope.AllocationHandle);

        // Simulate the fork/join command stream described in the plan:
        // ID 0 (Graphics) -> marker [Compute, deps=[0]]   starts ID 1
        // ID 1 (Compute)  -> marker [Graphics, deps=[]]   starts ID 2
        // ID 2 (Graphics) -> marker [Graphics, deps=[1]]  starts ID 3
        RGCommandStream.WriteSyncMarker(ref writer, CommandQueueType.Compute, new int[] { 0 }, nextCommandBufferId: 1);
        RGCommandStream.WriteSyncMarker(ref writer, CommandQueueType.Graphics, ReadOnlySpan<int>.Empty, nextCommandBufferId: 2);
        RGCommandStream.WriteSyncMarker(ref writer, CommandQueueType.Graphics, new int[] { 1 }, nextCommandBufferId: 3);

        var reader = new SpanReader(writer.AsSpan());

        Assert.AreEqual(RGExecutionOpType.CommandBufferSyncPoint, reader.Read<RGExecutionOpType>());
        var m0 = RGCommandStream.ReadSyncMarker(ref reader);
        Assert.AreEqual(CommandQueueType.Compute, m0.NextCommandBufferType);
        Assert.AreEqual(1, m0.ProducerCommandBufferIds.Length);
        Assert.AreEqual(0, m0.ProducerCommandBufferIds[0]);

        Assert.AreEqual(RGExecutionOpType.CommandBufferSyncPoint, reader.Read<RGExecutionOpType>());
        var m1 = RGCommandStream.ReadSyncMarker(ref reader);
        Assert.AreEqual(CommandQueueType.Graphics, m1.NextCommandBufferType);
        Assert.AreEqual(0, m1.ProducerCommandBufferIds.Length);

        Assert.AreEqual(RGExecutionOpType.CommandBufferSyncPoint, reader.Read<RGExecutionOpType>());
        var m2 = RGCommandStream.ReadSyncMarker(ref reader);
        Assert.AreEqual(CommandQueueType.Graphics, m2.NextCommandBufferType);
        Assert.AreEqual(1, m2.ProducerCommandBufferIds.Length);
        Assert.AreEqual(1, m2.ProducerCommandBufferIds[0]);

        Assert.AreEqual(0, reader.RemainingBytes);
        writer.Dispose();
    }

    [TestMethod]
    public void TestSyncMarkerValidation_RejectsProducerIdEqualToNext()
    {
        // Producer ID must be strictly less than the ID of the next command buffer.
        Assert.ThrowsExactly<ArgumentException>(
            () => RGCommandStream.ValidateProducerIds(new int[] { 1 }, nextCommandBufferId: 1),
            "A producer ID equal to the next command buffer ID must be rejected.");
    }

    [TestMethod]
    public void TestSyncMarkerValidation_RejectsProducerIdGreaterThanNext()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => RGCommandStream.ValidateProducerIds(new int[] { 5 }, nextCommandBufferId: 3),
            "A producer ID greater than the next command buffer ID must be rejected.");
    }

    [TestMethod]
    public void TestSyncMarkerValidation_RejectsNegativeProducerId()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => RGCommandStream.ValidateProducerIds(new int[] { -1 }, nextCommandBufferId: 1),
            "A negative producer ID must be rejected.");
    }

    [TestMethod]
    public void TestSyncMarkerValidation_RejectsDuplicateProducerIds()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => RGCommandStream.ValidateProducerIds(new int[] { 0, 1, 0 }, nextCommandBufferId: 3),
            "Duplicate producer IDs within one marker must be rejected.");
    }

    [TestMethod]
    public void TestSyncMarkerValidation_AcceptsValidProducerIds()
    {
        // Should not throw for a valid set of strictly earlier, unique IDs.
        RGCommandStream.ValidateProducerIds(new int[] { 0, 1, 2 }, nextCommandBufferId: 3);
        RGCommandStream.ValidateProducerIds(ReadOnlySpan<int>.Empty, nextCommandBufferId: 0);
        RGCommandStream.ValidateProducerIds(new int[] { 0 }, nextCommandBufferId: 1);
    }

}
