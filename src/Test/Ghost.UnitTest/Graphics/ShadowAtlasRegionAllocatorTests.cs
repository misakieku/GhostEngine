using Ghost.Engine.RenderPipeline;
using Misaki.HighPerformance.LowLevel.Buffer;
using System.Collections.Concurrent;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public class ShadowAtlasRegionAllocatorTests
{
    [TestMethod]
    public void Allocate_SingleTile_ZeroGutter_ComputesFlushUV()
    {
        using var allocator = new ShadowAtlasRegionAllocator(2048, 2048, 0, AllocationHandle.Persistent);

        var success = allocator.Allocate(512, 512, out var region);

        Assert.IsTrue(success);
        Assert.AreEqual(0u, region.x);
        Assert.AreEqual(0u, region.y);
        Assert.AreEqual(512u, region.width);
        Assert.AreEqual(512u, region.height);

        Assert.AreEqual(0.0f, region.tileOffsetScale.x, 1e-6f);
        Assert.AreEqual(0.0f, region.tileOffsetScale.y, 1e-6f);
        Assert.AreEqual(512.0f / 2048.0f, region.tileOffsetScale.z, 1e-6f);
        Assert.AreEqual(512.0f / 2048.0f, region.tileOffsetScale.w, 1e-6f);
    }

    [TestMethod]
    public void Allocate_SingleTile_SucceedsAndComputesGutterUV()
    {
        using var allocator = new ShadowAtlasRegionAllocator(2048, 2048, 1, AllocationHandle.Persistent);

        var success = allocator.Allocate(512, 512, out var region);

        Assert.IsTrue(success);
        Assert.AreEqual(0u, region.x);
        Assert.AreEqual(0u, region.y);
        Assert.AreEqual(512u, region.width);
        Assert.AreEqual(512u, region.height);

        // UV offset should include 1 texel gutter: 1 / 2048
        Assert.AreEqual(1.0f / 2048.0f, region.tileOffsetScale.x, 1e-6f);
        Assert.AreEqual(1.0f / 2048.0f, region.tileOffsetScale.y, 1e-6f);
        // Inner width is 512 - 2 = 510 texels
        Assert.AreEqual(510.0f / 2048.0f, region.tileOffsetScale.z, 1e-6f);
        Assert.AreEqual(510.0f / 2048.0f, region.tileOffsetScale.w, 1e-6f);
    }

    [TestMethod]
    public void AllocatePointLightBlock_AllocatesSixFacesContiguously()
    {
        using var allocator = new ShadowAtlasRegionAllocator(2048, 2048, 1, AllocationHandle.Persistent);
        Span<ShadowAtlasRegion> faces = stackalloc ShadowAtlasRegion[6];

        var success = allocator.AllocatePointLightBlock(256, faces);

        Assert.IsTrue(success);
        for (var i = 0; i < 6; i++)
        {
            Assert.AreEqual(256u, faces[i].width);
            Assert.AreEqual(256u, faces[i].height);
        }
    }

    [TestMethod]
    public void AllocatePointLightBlock_RollbackWhenInsufficientSpace()
    {
        // 512x256 atlas can hold two 256x256 tiles, not 6
        using var allocator = new ShadowAtlasRegionAllocator(512, 256, 1, AllocationHandle.Persistent);
        Span<ShadowAtlasRegion> faces = stackalloc ShadowAtlasRegion[6];

        var success = allocator.AllocatePointLightBlock(256, faces);

        Assert.IsFalse(success);

        // After rollback, space is preserved and a single 256x256 tile can still be allocated at (0, 0)
        var singleAlloc = allocator.Allocate(256, 256, out var singleRegion);
        Assert.IsTrue(singleAlloc);
        Assert.AreEqual(0u, singleRegion.x);
        Assert.AreEqual(0u, singleRegion.y);
    }

    [TestMethod]
    public void Allocate_ConcurrentMultithreadedAllocations_NoOverlappingTiles()
    {
        // 2048x2048 atlas can hold 16 tiles of 512x512
        using var allocator = new ShadowAtlasRegionAllocator(2048, 2048, 1, AllocationHandle.Persistent);
        var allocatedRegions = new ConcurrentBag<ShadowAtlasRegion>();

        Parallel.For(0, 32, _ =>
        {
            if (allocator.Allocate(512, 512, out var region))
            {
                allocatedRegions.Add(region);
            }
        });

        // Exactly 16 tiles fit in 2048x2048
        Assert.HasCount(16, allocatedRegions);

        var list = allocatedRegions.ToList();
        for (var i = 0; i < list.Count; i++)
        {
            for (var j = i + 1; j < list.Count; j++)
            {
                var r1 = list[i];
                var r2 = list[j];
                var overlapX = (r1.x < r2.x + r2.width) && (r1.x + r1.width > r2.x);
                var overlapY = (r1.y < r2.y + r2.height) && (r1.y + r1.height > r2.y);
                Assert.IsFalse(overlapX && overlapY, $"Regions {i} and {j} overlap!");
            }
        }
    }
}
