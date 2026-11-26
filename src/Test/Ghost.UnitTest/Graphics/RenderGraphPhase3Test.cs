using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.Graphics;

public partial class RenderGraphTest
{
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
        Assert.AreEqual(1, GetDispatchCallCount(), "Contained execution must still record Compute work on Graphics.");
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

        var execution = CompileAndExecute(new ViewportState(1920, 1080, 1920, 1080), RGFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(execution.Dump);
        Assert.IsTrue(GetRecordedBarriers().Any(barrier => barrier.Force));
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
    public void TestPhase3_ViewportGrowthRecompilesAndCachesAtNewResolution()
    {
        SetupTestRenderPipeline();
        var firstExecution = CompileAndExecute(new ViewportState(960, 540, 960, 540), RGFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(firstExecution.Dump);
        Assert.IsFalse(firstExecution.Dump.IsCacheHit);

        _renderGraph.BeginRecord();
        SetupTestRenderPipeline();
        var grownExecution = CompileAndExecute(new ViewportState(1920, 1080, 1920, 1080), RGFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(grownExecution.Dump);
        Assert.IsFalse(grownExecution.Dump.IsCacheHit, "Growing beyond cached viewport must recompile to recompute 2D aliasing intervals.");
        Assert.IsGreaterThan(firstExecution.Dump.TotalHeapSize, grownExecution.Dump.TotalHeapSize);

        _renderGraph.BeginRecord();
        SetupTestRenderPipeline();
        var secondAtNewResolution = CompileAndExecute(new ViewportState(1920, 1080, 1920, 1080), RGFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(secondAtNewResolution.Dump);
        Assert.IsTrue(secondAtNewResolution.Dump.IsCacheHit);
        Assert.IsTrue(grownExecution.Dump.CommandStream.SequenceEqual(secondAtNewResolution.Dump.CommandStream));
    }

    [TestMethod]
    public void TestPhase3_SubRangeAliasing_BigFitsTwoConcurrentSmallResourcesAndFlushesOnce()
    {
        const ulong size64Mb = 64UL * 1024 * 1024;
        const ulong size32Mb = 32UL * 1024 * 1024;

        Identifier<RGBuffer> big;
        Identifier<RGBuffer> chain;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("BigWriter"))
        {
            builder.AllowPassCulling(false);
            big = builder.CreateBuffer(new BufferDesc { Size = size64Mb }, "Big64MB");
            chain = builder.CreateBuffer(new BufferDesc { Size = 256 }, "BigChain");
            builder.UseBuffer(big, AccessFlags.WriteAll);
            builder.UseBuffer(chain, AccessFlags.WriteAll);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGBuffer> smallA;
        Identifier<RGBuffer> smallB;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("SmallConcurrentPass"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(chain, AccessFlags.Read);
            smallA = builder.CreateBuffer(new BufferDesc { Size = size32Mb }, "SmallA32MB");
            smallB = builder.CreateBuffer(new BufferDesc { Size = size32Mb }, "SmallB32MB");
            builder.UseBuffer(smallA, AccessFlags.WriteAll);
            builder.UseBuffer(smallB, AccessFlags.WriteAll);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        var dump = CompilePlannerDump();
        var bigRes = dump.Resources.Single(r => r.LogicalResourceId == big.Value);
        var smallARes = dump.Resources.Single(r => r.LogicalResourceId == smallA.Value);
        var smallBRes = dump.Resources.Single(r => r.LogicalResourceId == smallB.Value);

        Assert.AreEqual(size64Mb + 65536UL, dump.TotalHeapSize);
        Assert.AreEqual(0UL, bigRes.HeapOffset);
        Assert.AreEqual(0UL, smallARes.HeapOffset);
        Assert.AreEqual(size32Mb, smallBRes.HeapOffset);
        Assert.Contains(smallA.Value, bigRes.AliasedWithResources);
        Assert.Contains(smallB.Value, bigRes.AliasedWithResources);
        Assert.DoesNotContain(smallB.Value, smallARes.AliasedWithResources);
        Assert.HasCount(2, dump.MemoryBlocks.Where(b => !b.IsFree).ToList());
        Assert.AreEqual(size64Mb, dump.MemoryBlocks[0].Size);
        Assert.IsFalse(dump.MemoryBlocks[0].IsFree);
        Assert.Contains(big.Value, dump.MemoryBlocks[0].AliasedLogicalResources);
        Assert.Contains(smallA.Value, dump.MemoryBlocks[0].AliasedLogicalResources);
        Assert.Contains(smallB.Value, dump.MemoryBlocks[0].AliasedLogicalResources);

        var bigFlushes = dump.CommandStream
            .Where(line => line.Contains("AliasingFlush:", StringComparison.Ordinal) && line.Contains($"[Buffer #{big.Value}]", StringComparison.Ordinal))
            .ToList();
        Assert.HasCount(1, bigFlushes, "Big64MB must only be flushed once when two concurrent 32MB resources reuse its range.");
    }

    [TestMethod]
    public void TestPhase3_SubRangeAliasing_TwoSmallPredecessorsFlushBeforeBigSuccessor()
    {
        const ulong size64Mb = 64UL * 1024 * 1024;
        const ulong size32Mb = 32UL * 1024 * 1024;

        Identifier<RGBuffer> smallA;
        Identifier<RGBuffer> smallB;
        Identifier<RGBuffer> chain;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("SmallWritersA"))
        {
            builder.AllowPassCulling(false);
            smallA = builder.CreateBuffer(new BufferDesc { Size = size32Mb }, "SubPredA");
            smallB = builder.CreateBuffer(new BufferDesc { Size = size32Mb }, "SubPredB");
            chain = builder.CreateBuffer(new BufferDesc { Size = 256 }, "SubPredChain");
            builder.UseBuffer(smallA, AccessFlags.WriteAll);
            builder.UseBuffer(smallB, AccessFlags.WriteAll);
            builder.UseBuffer(chain, AccessFlags.WriteAll);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("SmallReaders"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(smallA, AccessFlags.Read);
            builder.UseBuffer(smallB, AccessFlags.Read);
            builder.UseBuffer(chain, AccessFlags.ReadWrite);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGBuffer> big;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("BigSuccessorPass"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(chain, AccessFlags.Read);
            big = builder.CreateBuffer(new BufferDesc { Size = size64Mb }, "SubSuccBig");
            builder.UseBuffer(big, AccessFlags.WriteAll);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        var dump = CompilePlannerDump();
        var resA = dump.Resources.Single(r => r.LogicalResourceId == smallA.Value);
        var resB = dump.Resources.Single(r => r.LogicalResourceId == smallB.Value);
        var resBig = dump.Resources.Single(r => r.LogicalResourceId == big.Value);

        Assert.AreEqual(size64Mb + 65536UL, dump.TotalHeapSize);
        Assert.AreEqual(0UL, resBig.HeapOffset);
        Assert.AreEqual(0UL, resA.HeapOffset);
        Assert.AreEqual(size32Mb, resB.HeapOffset);
        Assert.Contains(smallA.Value, resBig.AliasedWithResources);
        Assert.Contains(smallB.Value, resBig.AliasedWithResources);

        var bigAliasingLines = dump.CommandStream
            .Where(line => line.Contains("Aliasing:", StringComparison.Ordinal) && line.Contains($"[Buffer #{big.Value}]", StringComparison.Ordinal))
            .ToList();
        Assert.HasCount(1, bigAliasingLines);
    }

    [TestMethod]
    public void TestPhase3_SubRangeAliasing_MixedWriteAndReadPredecessorsFlushWriterBeforeDiscard()
    {
        const ulong size64Mb = 64UL * 1024 * 1024;
        const ulong size32Mb = 32UL * 1024 * 1024;

        Identifier<RGBuffer> writeOnly32;
        Identifier<RGBuffer> readOnly32;
        Identifier<RGBuffer> chain;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("PredWriteOnly"))
        {
            builder.AllowPassCulling(false);
            writeOnly32 = builder.CreateBuffer(new BufferDesc { Size = size32Mb }, "WriteOnly32");
            readOnly32 = builder.CreateBuffer(new BufferDesc { Size = size32Mb }, "ReadOnly32");
            chain = builder.CreateBuffer(new BufferDesc { Size = 256 }, "Chain0");
            builder.UseBuffer(writeOnly32, AccessFlags.WriteAll);
            builder.UseBuffer(readOnly32, AccessFlags.WriteAll);
            builder.UseBuffer(chain, AccessFlags.WriteAll);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("PredReadOnly"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(readOnly32, AccessFlags.Read);
            builder.UseBuffer(chain, AccessFlags.ReadWrite);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGBuffer> big64;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("SuccBig64"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(chain, AccessFlags.Read);
            big64 = builder.CreateBuffer(new BufferDesc { Size = size64Mb }, "SuccBig64");
            builder.UseBuffer(big64, AccessFlags.WriteAll);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        var dump = CompilePlannerDump();
        var writeRes = dump.Resources.Single(r => r.LogicalResourceId == writeOnly32.Value);
        var readRes = dump.Resources.Single(r => r.LogicalResourceId == readOnly32.Value);
        var bigRes = dump.Resources.Single(r => r.LogicalResourceId == big64.Value);

        Assert.AreNotEqual(writeRes.HeapOffset, readRes.HeapOffset);
        Assert.Contains(writeOnly32.Value, bigRes.AliasedWithResources);
        Assert.Contains(readOnly32.Value, bigRes.AliasedWithResources);
        Assert.IsTrue(dump.CommandStream.Any(line =>
            line.Contains("AliasingFlush:", StringComparison.Ordinal)
            && line.Contains($"[Buffer #{writeOnly32.Value}]", StringComparison.Ordinal)),
            "WriteOnly32 must be flushed before SuccBig64 even though ReadOnly32 had a later last-use index.");
    }

    [TestMethod]
    public void TestPhase3_SubRangeAliasing_CrossQueueAndSameQueuePredecessorsBothSynchronizeBeforeBig()
    {
        const ulong size64Mb = 64UL * 1024 * 1024;
        const ulong size32Mb = 32UL * 1024 * 1024;

        Identifier<RGBuffer> gfxSmall;
        using (var builder = _renderGraph.AddRasterRenderPass<AsyncPlannerPassData>("GfxProducerAndSmall"))
        {
            builder.AllowPassCulling(false);
            gfxSmall = builder.CreateBuffer(new BufferDesc { Size = size32Mb }, "GfxSmall32");
            builder.UseRandomAccessBuffer(gfxSmall);
            var rt = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), "GfxSmallTarget");
            builder.SetColorAttachment(rt, 0, AccessFlags.WriteAll);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = gfxSmall, target = rt });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGBuffer> computeSmall;
        Identifier<RGBuffer> computeSignal;
        using (var builder = _renderGraph.AddComputeRenderPass<AsyncPlannerPassData>("AsyncComputeSmall"))
        {
            builder.AllowPassCulling(false);
            builder.EnableAsyncCompute(true);
            computeSmall = builder.CreateBuffer(new BufferDesc { Size = size32Mb }, "ComputeSmall32");
            computeSignal = builder.CreateBuffer(new BufferDesc { Size = 256 }, "ComputeSignal");
            builder.UseBuffer(computeSmall, AccessFlags.WriteAll);
            builder.UseBuffer(computeSignal, AccessFlags.WriteAll);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = computeSmall, secondBuffer = computeSignal });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        }

        // Independent raster work gives AsyncComputeSmall an overlap window on Compute.
        AddPlannerRasterPass("MixedAliasIndependent");

        Identifier<RGBuffer> big;
        using (var builder = _renderGraph.AddRasterRenderPass<AsyncPlannerPassData>("MixedAliasJoinBig"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(computeSignal, AccessFlags.Read);
            big = builder.CreateBuffer(new BufferDesc { Size = size64Mb }, "MixedJoinBig64");
            builder.UseRandomAccessBuffer(big);
            var rt = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), "MixedJoinTarget");
            builder.SetColorAttachment(rt, 0, AccessFlags.WriteAll);
            builder.SetPassData(new AsyncPlannerPassData { firstBuffer = computeSignal, secondBuffer = big, target = rt });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
        }

        var dump = CompilePlannerDump();
        var gfxRes = dump.Resources.Single(r => r.LogicalResourceId == gfxSmall.Value);
        var computeRes = dump.Resources.Single(r => r.LogicalResourceId == computeSmall.Value);
        var bigRes = dump.Resources.Single(r => r.LogicalResourceId == big.Value);

        Assert.AreEqual(CommandQueueType.Compute, dump.Passes.Single(p => p.Name == "AsyncComputeSmall").EffectiveQueue);
        Assert.AreNotEqual(gfxRes.HeapOffset, computeRes.HeapOffset);
        Assert.Contains(gfxSmall.Value, bigRes.AliasedWithResources);
        Assert.Contains(computeSmall.Value, bigRes.AliasedWithResources);
        Assert.IsTrue(dump.CommandStream.Any(line =>
            line.Contains("QueueRelease:", StringComparison.Ordinal)
            && line.Contains($"[Buffer #{computeSmall.Value}]", StringComparison.Ordinal)));
        Assert.IsTrue(dump.CommandStream.Any(line =>
            line.Contains("QueueAcquire:", StringComparison.Ordinal)
            && line.Contains($"[Buffer #{big.Value}]", StringComparison.Ordinal)));
    }
}
