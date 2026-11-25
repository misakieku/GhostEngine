using Ghost.Core;
using Ghost.Graphics.RenderGraphModule;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.UnitTest.Graphics;

public partial class RenderGraphTest
{
    [TestMethod]
    public void TestSameQueueAliasing_ReadOnlyPredecessor_SyncsInSingleBarrier()
    {
        var backBuffer = _renderGraph.ImportTexture(_resourceAllocator.CreateTexture(new TextureDesc
        {
            Width = 1920,
            Height = 1080,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.RenderTarget
        }));

        Identifier<RGTexture> texA;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("ReadAliasPassA"))
        {
            texA = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "ReadAliasTexA");
            builder.SetColorAttachment(texA, 0, AccessFlags.WriteAll);
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGTexture> texInter;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("ReadAliasPassB"))
        {
            builder.UseTexture(texA, AccessFlags.Read);
            texInter = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "ReadAliasInter");
            builder.SetColorAttachment(texInter, 0, AccessFlags.WriteAll);
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGTexture> texB;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("ReadAliasPassC"))
        {
            builder.UseTexture(texInter, AccessFlags.Read);
            texB = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "ReadAliasTexB");
            builder.SetColorAttachment(texB, 0, AccessFlags.WriteAll);
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = _renderGraph.AddUnsafeRenderPass<FinalBlitPassData>("ReadAliasPassD"))
        {
            builder.SetPassData(new FinalBlitPassData
            {
                source = builder.UseTexture(texB, AccessFlags.Read),
                backBuffer = builder.UseRenderTargetTexture(backBuffer, AccessFlags.WriteAll)
            });
            builder.SetRenderFunc<FinalBlitPassData>(static (ref readonly data, ctx) => { });
        }

        var exec = CompileAndExecute(new ViewportState(1920, 1080, 1920, 1080), RGFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec.Dump);

        var resA = exec.Dump.Resources.Single(r => r.Name == "ReadAliasTexA");
        var resB = exec.Dump.Resources.Single(r => r.Name == "ReadAliasTexB");
        Assert.AreEqual(resA.HeapOffset, resB.HeapOffset);

        var texBBarriers = GetRecordedBarriers()
            .Where(b => _resourceDatabase.GetResourceName(b.Resource) == "ReadAliasTexB" && b.AccessBefore == BarrierAccess.NoAccess)
            .ToList();
        Assert.HasCount(1, texBBarriers);
        Assert.AreEqual(BarrierSync.PixelShading | BarrierSync.NonPixelShading, texBBarriers[0].SyncBefore);
        Assert.IsTrue(texBBarriers[0].Discard);
    }

    [TestMethod]
    public void TestSameQueueAliasing_WritePredecessor_EmitsSeparateFlushBarrierBeforeDiscard()
    {
        var backBuffer = _renderGraph.ImportTexture(_resourceAllocator.CreateTexture(new TextureDesc
        {
            Width = 1920,
            Height = 1080,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.RenderTarget
        }));

        Identifier<RGTexture> texA;
        Identifier<RGBuffer> chainBuf0;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("WriteAliasPassA"))
        {
            builder.AllowPassCulling(false);
            texA = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "WriteAliasTexA");
            builder.SetColorAttachment(texA, 0, AccessFlags.WriteAll);
            chainBuf0 = builder.CreateBuffer(new BufferDesc { Size = 256 }, "WriteAliasChain0");
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGBuffer> chainBuf1;
        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("WriteAliasPassB"))
        {
            builder.AllowPassCulling(false);
            builder.UseBuffer(chainBuf0, AccessFlags.Read);
            chainBuf1 = builder.CreateBuffer(new BufferDesc { Size = 256 }, "WriteAliasChain1");
            builder.UseBuffer(chainBuf1, AccessFlags.Write);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
        }

        Identifier<RGTexture> texB;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("WriteAliasPassC"))
        {
            builder.UseBuffer(chainBuf1, AccessFlags.Read);
            texB = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "WriteAliasTexB");
            builder.SetColorAttachment(texB, 0, AccessFlags.WriteAll);
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = _renderGraph.AddUnsafeRenderPass<FinalBlitPassData>("WriteAliasPassD"))
        {
            builder.SetPassData(new FinalBlitPassData
            {
                source = builder.UseTexture(texB, AccessFlags.Read),
                backBuffer = builder.UseRenderTargetTexture(backBuffer, AccessFlags.WriteAll)
            });
            builder.SetRenderFunc<FinalBlitPassData>(static (ref readonly data, ctx) => { });
        }

        var exec = CompileAndExecute(new ViewportState(1920, 1080, 1920, 1080), RGFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec.Dump);

        var resA = exec.Dump.Resources.Single(r => r.Name == "WriteAliasTexA");
        var resB = exec.Dump.Resources.Single(r => r.Name == "WriteAliasTexB");
        Assert.AreEqual(resA.HeapOffset, resB.HeapOffset, "WriteAliasTexA and WriteAliasTexB must share the same heap slot.");
        Assert.IsTrue(exec.Dump.CommandStream.Any(line => line.Contains("AliasingFlush:", StringComparison.Ordinal)));

        var cmd = _graphicsEngine.AcquiredCommandBuffers.Single();
        var batchIndices = new List<int>();
        var batchIdx = 0;
        var remainingInBatch = cmd.RecordedBarrierBatchSizes[0];
        for (var i = 0; i < cmd.RecordedBarriers.Count; i++)
        {
            while (remainingInBatch == 0)
            {
                batchIdx++;
                remainingInBatch = cmd.RecordedBarrierBatchSizes[batchIdx];
            }

            batchIndices.Add(batchIdx);
            remainingInBatch--;
        }

        var flushIdx = cmd.RecordedBarriers.FindIndex(b =>
            _resourceDatabase.GetResourceName(b.Resource) == "WriteAliasTexA"
            && b.LayoutBefore == BarrierLayout.RenderTarget
            && b.LayoutAfter == BarrierLayout.Undefined
            && b.AccessAfter == BarrierAccess.NoAccess
            && b.SyncAfter == BarrierSync.All);
        var activateIdx = cmd.RecordedBarriers.FindIndex(b =>
            _resourceDatabase.GetResourceName(b.Resource) == "WriteAliasTexB"
            && b.AccessBefore == BarrierAccess.NoAccess
            && b.Discard);

        Assert.IsGreaterThanOrEqualTo(0, flushIdx, "Predecessor write-flush barrier must be emitted.");
        Assert.IsGreaterThanOrEqualTo(0, activateIdx, "Successor discard activation barrier must be emitted.");
        Assert.IsLessThan(activateIdx, flushIdx, "Predecessor flush must precede successor activation.");
        Assert.IsLessThan(batchIndices[activateIdx], batchIndices[flushIdx], "Predecessor flush and successor DISCARD must execute in separate Barrier() calls.");
    }

    [TestMethod]
    public void TestMultiWordBitmaskReachability_75Passes()
    {
        var backBuffer = _renderGraph.ImportTexture(_resourceAllocator.CreateTexture(new TextureDesc
        {
            Width = 1920,
            Height = 1080,
            Format = TextureFormat.R8G8B8A8_UNorm,
            Usage = TextureUsage.RenderTarget
        }));

        Identifier<RGTexture> earlyTex;
        Identifier<RGBuffer> chain;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("Pass0"))
        {
            earlyTex = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "Word0Tex");
            builder.SetColorAttachment(earlyTex, 0, AccessFlags.WriteAll);
            chain = builder.CreateBuffer(new BufferDesc { Size = 256 }, "Chain0");
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = _renderGraph.AddComputeRenderPass<CullingPassData>("Pass1_ReadEarlyTex"))
        {
            builder.UseTexture(earlyTex, AccessFlags.Read);
            builder.UseBuffer(chain, AccessFlags.Read);
            var next = builder.CreateBuffer(new BufferDesc { Size = 256 }, "Chain1");
            builder.UseBuffer(next, AccessFlags.Write);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
            chain = next;
        }

        // Add an unused pass that should be culled even with > 64 passes
        using (var builder = _renderGraph.AddComputeRenderPass<UnusedPassData>("CullablePass"))
        {
            var dead = builder.CreateBuffer(new BufferDesc { Size = 256 }, "DeadBuf");
            builder.UseBuffer(dead, AccessFlags.Write);
            builder.SetPassData(new UnusedPassData());
            builder.SetRenderFunc<UnusedPassData>(static (ref readonly data, ctx) => { });
        }

        for (var i = 2; i < 73; i++)
        {
            using var builder = _renderGraph.AddComputeRenderPass<CullingPassData>($"ChainPass{i}");
            builder.UseBuffer(chain, AccessFlags.Read);
            var next = builder.CreateBuffer(new BufferDesc { Size = 256 }, $"Chain{i}");
            builder.UseBuffer(next, AccessFlags.Write);
            builder.SetPassData(new CullingPassData());
            builder.SetRenderFunc<CullingPassData>(static (ref readonly data, ctx) => { });
            chain = next;
        }

        Identifier<RGTexture> lateTex;
        using (var builder = _renderGraph.AddRasterRenderPass<VBufferPassData>("Pass73_LateAliased"))
        {
            builder.UseBuffer(chain, AccessFlags.Read);
            lateTex = builder.CreateTexture(RGTextureDesc.Relative(1.0f, TextureFormat.R8G8_UNorm), "Word1Tex");
            builder.SetColorAttachment(lateTex, 0, AccessFlags.WriteAll);
            builder.SetPassData(new VBufferPassData());
            builder.SetRenderFunc<VBufferPassData>(static (ref readonly data, ctx) => { });
        }

        using (var builder = _renderGraph.AddUnsafeRenderPass<FinalBlitPassData>("Pass74_Present"))
        {
            builder.SetPassData(new FinalBlitPassData
            {
                source = builder.UseTexture(lateTex, AccessFlags.Read),
                backBuffer = builder.UseRenderTargetTexture(backBuffer, AccessFlags.WriteAll)
            });
            builder.SetRenderFunc<FinalBlitPassData>(static (ref readonly data, ctx) => { });
        }

        var exec = CompileAndExecute(new ViewportState(1920, 1080, 1920, 1080), RGFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(exec.Dump);
        Assert.IsTrue(exec.Dump.Passes.Single(p => p.Name == "CullablePass").IsCulled);
        Assert.HasCount(76, exec.Dump.Passes);

        var resEarly = exec.Dump.Resources.Single(r => r.Name == "Word0Tex");
        var resLate = exec.Dump.Resources.Single(r => r.Name == "Word1Tex");
        Assert.AreEqual(resEarly.HeapOffset, resLate.HeapOffset, "Resources across 64-bit word boundaries must alias cleanly.");
        Assert.IsGreaterThan(64, resLate.ScheduledFirstUseIndex);
    }

    [TestMethod]
    public void TestCacheHit_DumpMatchesFreshCompile()
    {
        SetupTestRenderPipeline();
        var freshExec = CompileAndExecute(new ViewportState(1920, 1080, 1920, 1080), RGFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(freshExec.Dump);
        Assert.IsFalse(freshExec.Dump.IsCacheHit);

        _renderGraph.Reset();

        SetupTestRenderPipeline();
        var cachedExec = CompileAndExecute(new ViewportState(1920, 1080, 1920, 1080), RGFlags.GenerateDump).GetValueOrThrow();
        Assert.IsNotNull(cachedExec.Dump);
        Assert.IsTrue(cachedExec.Dump.IsCacheHit);

        Assert.AreEqual(freshExec.Dump.GraphHash, cachedExec.Dump.GraphHash);
        Assert.AreEqual(freshExec.Dump.TotalHeapSize, cachedExec.Dump.TotalHeapSize);
        Assert.AreEqual(freshExec.Dump.SizeWithoutAliasing, cachedExec.Dump.SizeWithoutAliasing);
        Assert.IsTrue(freshExec.Dump.CommandStream.SequenceEqual(cachedExec.Dump.CommandStream));
        Assert.HasCount(freshExec.Dump.Resources.Count, cachedExec.Dump.Resources);

        for (var i = 0; i < freshExec.Dump.Resources.Count; i++)
        {
            var freshRes = freshExec.Dump.Resources[i];
            var cachedRes = cachedExec.Dump.Resources[i];
            Assert.AreEqual(freshRes.LogicalResourceId, cachedRes.LogicalResourceId);
            Assert.AreEqual(freshRes.HeapOffset, cachedRes.HeapOffset);
            Assert.AreEqual(freshRes.SizeInBytes, cachedRes.SizeInBytes);
            Assert.AreEqual(freshRes.ScheduledFirstUseIndex, cachedRes.ScheduledFirstUseIndex);
            Assert.AreEqual(freshRes.ScheduledLastUseIndex, cachedRes.ScheduledLastUseIndex);
            Assert.IsTrue(freshRes.AliasedWithResources.SequenceEqual(cachedRes.AliasedWithResources));
            Assert.IsTrue(freshRes.ProducerPass.SequenceEqual(cachedRes.ProducerPass));
            Assert.IsTrue(freshRes.ConsumerPasses.SequenceEqual(cachedRes.ConsumerPasses));
        }
    }
}
