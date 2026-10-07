using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Utilities;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.UnitTest.Rendering;

[TestClass]
public class ShadowAtlasTests
{
    [TestMethod]
    public void GPUShadowViewData_HasCorrectSizeAndAlignment()
    {
        var size = Marshal.SizeOf<GPUShadowViewData>();
        Assert.AreEqual(208, size, "GPUShadowViewData must be exactly 208 bytes (13 float4 vectors).");
        Assert.AreEqual(0, size % 16, "GPUShadowViewData must be 16-byte aligned.");
    }

    [TestMethod]
    public void ShadowAtlasRegionAllocator_SingleTileAllocation_ComputesGutterCorrectly()
    {
        using var allocator = new ShadowAtlasRegionAllocator(2048, 2048, gutterTexels: 1);

        var success = allocator.Allocate(512, 512, out var region);
        Assert.IsTrue(success);
        Assert.AreEqual(0u, region.x);
        Assert.AreEqual(0u, region.y);
        Assert.AreEqual(512u, region.width);
        Assert.AreEqual(512u, region.height);

        // Inner rect: 510x510 with offset at (1, 1)
        var expectedOffsetX = 1.0f / 2048.0f;
        var expectedOffsetY = 1.0f / 2048.0f;
        var expectedScaleX = 510.0f / 2048.0f;
        var expectedScaleY = 510.0f / 2048.0f;

        Assert.AreEqual(expectedOffsetX, region.tileOffsetScale.x, 1e-6f);
        Assert.AreEqual(expectedOffsetY, region.tileOffsetScale.y, 1e-6f);
        Assert.AreEqual(expectedScaleX, region.tileOffsetScale.z, 1e-6f);
        Assert.AreEqual(expectedScaleY, region.tileOffsetScale.w, 1e-6f);
    }

    [TestMethod]
    public void ShadowAtlasRegionAllocator_MultipleTiles_PacksAcrossShelves()
    {
        using var allocator = new ShadowAtlasRegionAllocator(2048, 2048, gutterTexels: 1);

        // First shelf (y=0, height=512): fits 4 tiles of 512x512
        for (var i = 0; i < 4; i++)
        {
            var ok = allocator.Allocate(512, 512, out var reg);
            Assert.IsTrue(ok);
            Assert.AreEqual((uint)(i * 512), reg.x);
            Assert.AreEqual(0u, reg.y);
        }

        // 5th tile must spill over to second shelf (y=512, height=512)
        var ok5 = allocator.Allocate(512, 512, out var reg5);
        Assert.IsTrue(ok5);
        Assert.AreEqual(0u, reg5.x);
        Assert.AreEqual(512u, reg5.y);
    }

    [TestMethod]
    public void ShadowAtlasRegionAllocator_PointLightBlock_Allocates6ContiguousFaces()
    {
        using var allocator = new ShadowAtlasRegionAllocator(2048, 2048, gutterTexels: 1);

        Span<ShadowAtlasRegion> faces = stackalloc ShadowAtlasRegion[6];
        var success = allocator.AllocatePointLightBlock(256, faces);
        Assert.IsTrue(success);

        for (var f = 0; f < 6; f++)
        {
            Assert.AreEqual(256u, faces[f].width);
            Assert.AreEqual(256u, faces[f].height);
            Assert.IsGreaterThan(0.0f, faces[f].tileOffsetScale.z);
            Assert.IsGreaterThan(0.0f, faces[f].tileOffsetScale.w);
        }

        // All 6 faces should fit on the first shelf of height 256
        for (var f = 0; f < 6; f++)
        {
            Assert.AreEqual(0u, faces[f].y);
            Assert.AreEqual((uint)(f * 256), faces[f].x);
        }
    }

    [TestMethod]
    public void ShadowAtlasRegionAllocator_PointLightBlock_RollsBackOnFailure()
    {
        // Small atlas of 512x256: can fit 2 faces of 256x256, not 6
        using var allocator = new ShadowAtlasRegionAllocator(512, 256, gutterTexels: 1);

        Span<ShadowAtlasRegion> faces = stackalloc ShadowAtlasRegion[6];
        var success = allocator.AllocatePointLightBlock(256, faces);
        Assert.IsFalse(success, "Point light block should fail when not all 6 faces fit.");

        // After rollback, allocator should still be able to allocate 2 individual tiles
        var ok1 = allocator.Allocate(256, 256, out var r1);
        var ok2 = allocator.Allocate(256, 256, out var r2);
        Assert.IsTrue(ok1);
        Assert.IsTrue(ok2);
        Assert.AreEqual(0u, r1.x);
        Assert.AreEqual(256u, r2.x);
    }

    [TestMethod]
    public void ShadowAtlasRegionAllocator_Reset_AllowsReallocation()
    {
        using var allocator = new ShadowAtlasRegionAllocator(1024, 1024, gutterTexels: 1);

        // Fill the atlas with 4 tiles of 512x512
        for (var i = 0; i < 4; i++)
        {
            Assert.IsTrue(allocator.Allocate(512, 512, out _));
        }

        // Next allocation should fail
        Assert.IsFalse(allocator.Allocate(512, 512, out _));

        // Reset
        allocator.Reset();

        // Should be able to allocate again starting from (0, 0)
        var ok = allocator.Allocate(512, 512, out var reg);
        Assert.IsTrue(ok);
        Assert.AreEqual(0u, reg.x);
        Assert.AreEqual(0u, reg.y);
    }

    [TestMethod]
    public void ReversedZ_PerspectiveProjection_MapsCorrectDepths()
    {
        var nearClip = 0.1f;
        var farClip = 100.0f;
        var fov = math.radians(90.0f);
        var aspect = 1.0f;

        var m11 = 1.0f / math.tan(fov * 0.5f);
        var m00 = m11 / aspect;
        var m22 = nearClip / (nearClip - farClip);
        var m23 = (farClip * nearClip) / (farClip - nearClip);

        var proj = new float4x4(
            m00,  0.0f, 0.0f, 0.0f,
            0.0f, m11,  0.0f, 0.0f,
            0.0f, 0.0f, m22,  m23,
            0.0f, 0.0f, 1.0f, 0.0f);

        // Near plane point: (0, 0, nearClip, 1) in view space
        var nearView = new float4(0.0f, 0.0f, nearClip, 1.0f);
        var nearClipSpace = math.mul(proj, nearView);
        var nearNdcZ = nearClipSpace.z / nearClipSpace.w;
        Assert.AreEqual(1.0f, nearNdcZ, 1e-5f, "Reversed-Z near plane must map to 1.0.");

        // Far plane point: (0, 0, farClip, 1) in view space
        var farView = new float4(0.0f, 0.0f, farClip, 1.0f);
        var farClipSpace = math.mul(proj, farView);
        var farNdcZ = farClipSpace.z / farClipSpace.w;
        Assert.AreEqual(0.0f, farNdcZ, 1e-5f, "Reversed-Z far plane must map to 0.0.");
    }

    [TestMethod]
    public void SphereIntersectFrustum_AccuratelyTestsVisibility()
    {
        var eye = float3.zero;
        var forward = new float3(0.0f, 0.0f, 1.0f);
        var up = new float3(0.0f, 1.0f, 0.0f);
        var nearClip = 0.1f;
        var farClip = 100.0f;

        var m11 = 1.0f / math.tan(math.radians(90.0f) * 0.5f);
        var m22 = nearClip / (nearClip - farClip);
        var m23 = (farClip * nearClip) / (farClip - nearClip);
        var proj = new float4x4(
            m11,  0.0f, 0.0f, 0.0f,
            0.0f, m11,  0.0f, 0.0f,
            0.0f, 0.0f, m22,  m23,
            0.0f, 0.0f, 1.0f, 0.0f);

        var frustum = Frustum.Create(proj, eye, forward, nearClip, farClip);

        // Sphere directly in front of camera
        var visible = MathUtility.SphereIntersectFrustum(new float3(0.0f, 0.0f, 10.0f), 2.0f, frustum.planes);
        Assert.IsTrue(visible, "Sphere in front of camera should intersect frustum.");

        // Sphere behind camera
        var behind = MathUtility.SphereIntersectFrustum(new float3(0.0f, 0.0f, -10.0f), 2.0f, frustum.planes);
        Assert.IsFalse(behind, "Sphere behind camera should not intersect frustum.");

        // Sphere far beyond far clip plane
        var tooFar = MathUtility.SphereIntersectFrustum(new float3(0.0f, 0.0f, 150.0f), 2.0f, frustum.planes);
        Assert.IsFalse(tooFar, "Sphere beyond far clip plane should not intersect frustum.");

        // Sphere barely touching near plane from behind
        var touchingNear = MathUtility.SphereIntersectFrustum(new float3(0.0f, 0.0f, -0.5f), 1.0f, frustum.planes);
        Assert.IsTrue(touchingNear, "Sphere whose radius crosses near plane should intersect frustum.");
    }

    [TestMethod]
    public void ShadowAtlasRegionAllocator_WithCustomAllocationHandle_AllocatesAndDisposesCleanly()
    {
        using var stackScope = Misaki.HighPerformance.LowLevel.Buffer.AllocationManager.CreateStackScope();
        using var allocator = new ShadowAtlasRegionAllocator(1024, 1024, gutterTexels: 1, stackScope.AllocationHandle);

        var success = allocator.Allocate(256, 256, out var region);
        Assert.IsTrue(success);
        Assert.AreEqual(256u, region.width);
        Assert.AreEqual(256u, region.height);
        Assert.AreEqual(0u, region.x);
        Assert.AreEqual(0u, region.y);
    }

    [TestMethod]
    public void PunctualLight_ShadowSize_PacksAndUnpacksCorrectly()
    {
        var spot = Ghost.Engine.Components.PunctualLight.CreateSpot(new float3(1, 1, 1), 5.0f, 15.0f, 0.2f, 0.5f, shadowSize: 512);
        Assert.AreEqual(512u, spot.shadowSize);

        var flags = ((uint)spot.type & 0xFu) | ((spot.shadowSize & 0xFFFFu) << 4);
        var unpackedType = flags & 0xFu;
        var unpackedShadowSize = (flags >> 4) & 0xFFFFu;

        Assert.AreEqual((uint)Ghost.Engine.Components.PunctualLightType.Spot, unpackedType);
        Assert.AreEqual(512u, unpackedShadowSize);

        var unshadowedPoint = Ghost.Engine.Components.PunctualLight.CreatePoint(new float3(1, 0, 0), 2.0f, 8.0f, shadowSize: 0);
        Assert.AreEqual(0u, unshadowedPoint.shadowSize);

        var unshadowedFlags = ((uint)unshadowedPoint.type & 0xFu) | ((unshadowedPoint.shadowSize & 0xFFFFu) << 4);
        Assert.AreEqual((uint)Ghost.Engine.Components.PunctualLightType.Point, unshadowedFlags & 0xFu);
        Assert.AreEqual(0u, (unshadowedFlags >> 4) & 0xFFFFu);
    }
}
