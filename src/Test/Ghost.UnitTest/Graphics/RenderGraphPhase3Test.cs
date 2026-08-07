using Ghost.Core;
using Ghost.Core.Utilities;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Ghost.Graphics.Services;
using Ghost.UnitTest.MockingEnvironment;
using Misaki.HighPerformance.LowLevel.Buffer;
using Misaki.HighPerformance.LowLevel.Collections;

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

        var execution = CompileAndExecute(new ViewState(1920, 1080, 1920, 1080), RGExecutionFlags.GenerateDump).GetValueOrThrow();
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
    public void TestPhase3_ViewportGrowthPreservesAliasGroupsAndCommandBytes()
    {
        SetupTestRenderPipeline();
        var firstExecution = CompileAndExecute(new ViewState(960, 540, 960, 540), RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(firstExecution.Dump);

        _renderGraph.Reset();
        SetupTestRenderPipeline();
        var grownExecution = CompileAndExecute(new ViewState(1920, 1080, 1920, 1080), RGExecutionFlags.GenerateDump).GetValueOrThrow();
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
}
