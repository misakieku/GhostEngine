using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;

namespace Ghost.UnitTest.Graphics;

public partial class RenderGraphTest
{
    [TestMethod]
    public void TestAsyncPlanner_MultipleComputeRegionsScheduledInSingleGraph()
    {
        // Region 1: Producer -> Compute1 -> Independent1 -> Join1
        var producer1 = AddPlannerGraphicsProducer("Region1Producer");
        var compute1 = AddPlannerAsyncCompute("Region1Compute", producer1);
        AddPlannerRasterPass("Region1Independent");
        AddPlannerRasterPass("Region1Join", compute1);

        // Region 2: Producer -> Compute2 -> Independent2 -> Join2
        var producer2 = AddPlannerGraphicsProducer("Region2Producer");
        var compute2 = AddPlannerAsyncCompute("Region2Compute", producer2);
        AddPlannerRasterPass("Region2Independent");
        AddPlannerRasterPass("Region2Join", compute2);

        var dump = CompilePlannerDump();
        Assert.HasCount(8, dump.Passes);

        var c1 = dump.Passes.Single(pass => pass.Name == "Region1Compute");
        var c2 = dump.Passes.Single(pass => pass.Name == "Region2Compute");

        Assert.AreEqual(CommandQueueType.Compute, c1.EffectiveQueue);
        Assert.AreEqual(RGQueueDecision.AsyncComputeSelected, c1.QueueDecision);
        Assert.AreEqual(CommandQueueType.Compute, c2.EffectiveQueue);
        Assert.AreEqual(RGQueueDecision.AsyncComputeSelected, c2.QueueDecision);

        var markers = dump.CommandStream.Where(line => line.Contains("CommandBufferSyncPoint")).ToList();
        Assert.HasCount(6, markers, "Two full fork-join compute regions must emit 6 sync markers (3 per region).");

        // Region 1 markers
        Assert.Contains("NextType: Compute", markers[0]);
        Assert.Contains("DependsOn: [0]", markers[0]);
        Assert.Contains("NextType: Graphics", markers[1]);
        Assert.Contains("DependsOn: [none]", markers[1]);
        Assert.Contains("NextType: Graphics", markers[2]);
        Assert.Contains("DependsOn: [1]", markers[2]);

        // Region 2 markers
        Assert.Contains("NextType: Compute", markers[3]);
        Assert.Contains("DependsOn: [3]", markers[3]);
        Assert.Contains("NextType: Graphics", markers[4]);
        Assert.Contains("DependsOn: [none]", markers[4]);
        Assert.Contains("NextType: Graphics", markers[5]);
        Assert.Contains("DependsOn: [4]", markers[5]);

        Assert.AreEqual(2, GetDispatchCallCount(), "Both compute passes must dispatch.");
    }

    [TestMethod]
    public void TestAsyncPlanner_PassZeroComputeWithFewerThanFourPassesSucceeds()
    {
        // 3-pass graph: Pass 0 is Compute, Pass 1 is Overlap Raster, Pass 2 is Join Raster.
        Identifier<RGBuffer> computeOutput;
        using (var builder = _renderGraph.AddComputeRenderPass<AsyncPlannerPassData>("Pass0Compute"))
        {
            builder.AllowPassCulling(false);
            builder.EnableAsyncCompute(true);
            computeOutput = builder.CreateBuffer(new BufferDesc { Size = 1024 }, "Pass0Compute_Output");
            builder.UseBuffer(computeOutput, AccessFlags.Write);
            builder.SetPassData(new AsyncPlannerPassData { secondBuffer = computeOutput });
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => ctx.DispatchCompute(1, 1, 1));
        }

        AddPlannerRasterPass("Pass1IndependentRaster");
        AddPlannerRasterPass("Pass2JoinRaster", computeOutput);

        var dump = CompilePlannerDump();
        Assert.HasCount(3, dump.Passes, "Total compiled passes is 3, which is < 4.");

        var compute = dump.Passes.Single(pass => pass.Name == "Pass0Compute");
        var independent = dump.Passes.Single(pass => pass.Name == "Pass1IndependentRaster");
        var join = dump.Passes.Single(pass => pass.Name == "Pass2JoinRaster");

        Assert.AreEqual(CommandQueueType.Compute, compute.EffectiveQueue);
        Assert.AreEqual(RGQueueDecision.AsyncComputeSelected, compute.QueueDecision);
        Assert.AreEqual(CommandQueueType.Graphics, independent.EffectiveQueue);
        Assert.AreEqual(CommandQueueType.Graphics, join.EffectiveQueue);

        var markers = dump.CommandStream.Where(line => line.Contains("CommandBufferSyncPoint")).ToList();
        Assert.HasCount(3, markers);
        Assert.Contains("NextType: Compute", markers[0]);
        Assert.Contains("DependsOn: [none]", markers[0]);
        Assert.Contains("NextType: Graphics", markers[1]);
        Assert.Contains("DependsOn: [none]", markers[1]);
        Assert.Contains("NextType: Graphics", markers[2]);
        Assert.Contains("DependsOn: [1]", markers[2]);

        Assert.AreEqual(1, GetDispatchCallCount(), "Pass 0 compute must execute.");
    }

    [TestMethod]
    public void TestAsyncPlanner_UnsafePassWithAllowOverlapPermitsAsyncCompute()
    {
        var producerOutput = AddPlannerGraphicsProducer("Producer");
        var computeOutput = AddPlannerAsyncCompute("AsyncComputeWithUnsafeOverlap", producerOutput);

        using (var builder = _renderGraph.AddUnsafeRenderPass<AsyncPlannerPassData>("UnsafeOverlapPermitted"))
        {
            builder.AllowPassCulling(false);
            builder.AllowAsyncComputeOverlap(true);
            builder.SetPassData(new AsyncPlannerPassData());
            builder.SetRenderFunc<AsyncPlannerPassData>(static (ref readonly data, ctx) => { });
        }

        AddPlannerRasterPass("JoinConsumer", computeOutput);

        var dump = CompilePlannerDump();
        var compute = dump.Passes.Single(pass => pass.Name == "AsyncComputeWithUnsafeOverlap");
        var unsafePass = dump.Passes.Single(pass => pass.Name == "UnsafeOverlapPermitted");

        Assert.AreEqual(CommandQueueType.Compute, compute.EffectiveQueue);
        Assert.AreEqual(RGQueueDecision.AsyncComputeSelected, compute.QueueDecision);
        Assert.AreEqual(CommandQueueType.Graphics, unsafePass.EffectiveQueue);
        Assert.IsTrue(unsafePass.AllowAsyncComputeOverlap);

        var markers = dump.CommandStream.Where(line => line.Contains("CommandBufferSyncPoint")).ToList();
        Assert.HasCount(3, markers);
        Assert.Contains("NextType: Compute", markers[0]);
        Assert.Contains("NextType: Graphics", markers[1]);
        Assert.Contains("NextType: Graphics", markers[2]);
    }

    [TestMethod]
    public void TestAsyncPlanner_MultipleRegionsCacheHitPreservesStructure()
    {
        var viewState = new ViewState(1920, 1080, 1920, 1080);

        void SetupTwoRegions()
        {
            var p1 = AddPlannerGraphicsProducer("CacheP1");
            var c1 = AddPlannerAsyncCompute("CacheC1", p1);
            AddPlannerRasterPass("CacheR1");
            AddPlannerRasterPass("CacheJ1", c1);

            var p2 = AddPlannerGraphicsProducer("CacheP2");
            var c2 = AddPlannerAsyncCompute("CacheC2", p2);
            AddPlannerRasterPass("CacheR2");
            AddPlannerRasterPass("CacheJ2", c2);
        }

        SetupTwoRegions();
        var exec1 = CompileAndExecute(viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec1.Dump);
        Assert.IsFalse(exec1.Dump.IsCacheHit);

        _renderGraph.Reset();

        SetupTwoRegions();
        var exec2 = CompileAndExecute(viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec2.Dump);
        Assert.IsTrue(exec2.Dump.IsCacheHit);
        Assert.AreEqual(exec1.Dump.GraphHash, exec2.Dump.GraphHash);
        Assert.IsTrue(exec1.Dump.CommandStream.SequenceEqual(exec2.Dump.CommandStream));
    }

    [TestMethod]
    public void TestNonAttachmentResource_BufferDescSizeChangeInvalidatesCache()
    {
        var viewState = new ViewState(1920, 1080, 1920, 1080);

        void SetupBufferPass(ulong size)
        {
            var buffer = _renderGraph.CreateBuffer(new BufferDesc
            {
                Size = size,
                Stride = 4,
                Usage = BufferUsage.UnorderedAccess | BufferUsage.ShaderResource,
                HeapType = HeapType.Default
            }, "TransientBuffer");

            using var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("ComputeWithBuffer");
            builder.AllowPassCulling(false);
            builder.UseBuffer(buffer, AccessFlags.Write);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        // Frame 1: buffer size = 256
        SetupBufferPass(256);
        var exec1 = CompileAndExecute(viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec1.Dump);
        Assert.IsFalse(exec1.Dump.IsCacheHit);
        var res1 = exec1.Dump.Resources.Single(r => r.Name == "TransientBuffer");
        Assert.AreEqual(256UL, res1.SizeInBytes);

        _renderGraph.Reset();

        // Frame 2: same pass name, same topology, but buffer size = 4096
        SetupBufferPass(4096);
        var exec2 = CompileAndExecute(viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec2.Dump);
        Assert.AreNotEqual(exec1.Dump.GraphHash, exec2.Dump.GraphHash, "Changing buffer Size must invalidate the graph hash.");
        Assert.IsFalse(exec2.Dump.IsCacheHit, "Buffer size mutation must not result in a stale cache hit.");
        var res2 = exec2.Dump.Resources.Single(r => r.Name == "TransientBuffer");
        Assert.AreEqual(4096UL, res2.SizeInBytes);
    }

    [TestMethod]
    public void TestNonAttachmentResource_UavTextureFormatChangeInvalidatesCache()
    {
        var viewState = new ViewState(1920, 1080, 1920, 1080);

        void SetupUavTexturePass(TextureFormat format)
        {
            var texture = _renderGraph.CreateTexture(new RGTextureDesc
            {
                sizeMode = RGTextureSizeMode.Absolute,
                width = 512,
                height = 512,
                format = format,
                usage = TextureUsage.UnorderedAccess | TextureUsage.ShaderResource,
                dimension = TextureDimension.Texture2D,
                mipLevels = 1,
                slice = 1
            }, "TransientUavTexture");

            using var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("ComputeWithUavTexture");
            builder.AllowPassCulling(false);
            builder.UseTexture(texture, AccessFlags.Write);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        // Frame 1: UAV format = R8G8B8A8_UNorm
        SetupUavTexturePass(TextureFormat.R8G8B8A8_UNorm);
        var exec1 = CompileAndExecute(viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec1.Dump);
        Assert.IsFalse(exec1.Dump.IsCacheHit);

        _renderGraph.Reset();

        // Frame 2: same pass name and topology, but UAV format = R32G32B32A32_Float
        SetupUavTexturePass(TextureFormat.R32G32B32A32_Float);
        var exec2 = CompileAndExecute(viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec2.Dump);
        Assert.AreNotEqual(exec1.Dump.GraphHash, exec2.Dump.GraphHash, "Changing UAV texture format must invalidate the graph hash.");
        Assert.IsFalse(exec2.Dump.IsCacheHit, "UAV texture format mutation must not result in a stale cache hit.");
    }

    [TestMethod]
    public void TestNonAttachmentResource_IdenticalDescriptorsProduceCacheHit()
    {
        var viewState = new ViewState(1920, 1080, 1920, 1080);

        void SetupPass()
        {
            var buffer = _renderGraph.CreateBuffer(new BufferDesc
            {
                Size = 1024,
                Stride = 4,
                Usage = BufferUsage.UnorderedAccess,
                HeapType = HeapType.Default
            }, "StableBuffer");

            var texture = _renderGraph.CreateTexture(new RGTextureDesc
            {
                sizeMode = RGTextureSizeMode.Absolute,
                width = 256,
                height = 256,
                format = TextureFormat.R8G8B8A8_UNorm,
                usage = TextureUsage.UnorderedAccess,
                dimension = TextureDimension.Texture2D,
                mipLevels = 1,
                slice = 1
            }, "StableUavTexture");

            using var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("StableCompute");
            builder.AllowPassCulling(false);
            builder.UseBuffer(buffer, AccessFlags.Write);
            builder.UseTexture(texture, AccessFlags.Write);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        SetupPass();
        var exec1 = CompileAndExecute(viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec1.Dump);
        Assert.IsFalse(exec1.Dump.IsCacheHit);

        _renderGraph.Reset();

        SetupPass();
        var exec2 = CompileAndExecute(viewState, RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec2.Dump);
        Assert.IsTrue(exec2.Dump.IsCacheHit, "Identical non-attachment descriptors across frames must hit compilation cache.");
        Assert.AreEqual(exec1.Dump.GraphHash, exec2.Dump.GraphHash);
    }

    [TestMethod]
    public void TestRelativeResource_ViewportShrinkReusesBackingResourceAndHitsCache()
    {
        void SetupPipeline()
        {
            using var builder = _renderGraph.AddRasterRenderPass<CullingPassData>("RasterPass");
            builder.AllowPassCulling(false);
            var target = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8B8A8_UNorm), "RelativeTarget");
            builder.SetColorAttachment(target, 0, AccessFlags.WriteAll);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        // Frame 1: 4K (3840x2160)
        SetupPipeline();
        var exec4K = CompileAndExecute(new ViewState(3840, 2160, 3840, 2160), RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec4K.Dump);
        Assert.IsFalse(exec4K.Dump.IsCacheHit);
        var res4K = exec4K.Dump.Resources.Single(r => r.Name == "RelativeTarget");
        var backing4K = res4K.BackingResource;

        _renderGraph.Reset();

        // Frame 2: Resize down to 2K (1920x1080)
        SetupPipeline();
        var exec2K = CompileAndExecute(new ViewState(1920, 1080, 1920, 1080), RGExecutionFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec2K.Dump);
        Assert.AreEqual(exec4K.Dump.GraphHash, exec2K.Dump.GraphHash, "Relative texture graph hash must remain identical across viewport resize.");
        Assert.IsTrue(exec2K.Dump.IsCacheHit, "Downsizing viewport must be a cache hit.");
        var res2K = exec2K.Dump.Resources.Single(r => r.Name == "RelativeTarget");
        Assert.AreEqual(backing4K, res2K.BackingResource, "Downsizing must reuse the 4K backing resource without reallocating.");
    }
}
