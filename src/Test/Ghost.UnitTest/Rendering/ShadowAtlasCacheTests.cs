using Ghost.Core;
using Ghost.Engine;
using Ghost.Engine.RenderPipeline;
using Ghost.Engine.Utilities;
using Ghost.Graphics;
using Ghost.Graphics.Core;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using Misaki.HighPerformance.Mathematics.Geometry;

namespace Ghost.UnitTest.Rendering;

[TestClass]
public class ShadowAtlasCacheTests
{
    private static Frustum CreateSpotWorldFrustum(float3 pos, float3 dir, float range, float fovRadians = 1.0f)
    {
        var up = math.abs(dir.y) > 0.99f ? new float3(0, 0, 1) : new float3(0, 1, 0);
        var zAxis = math.normalize(dir);
        var xAxis = math.normalize(math.cross(up, zAxis));
        var yAxis = math.cross(zAxis, xAxis);

        var view = new float4x4(
            xAxis.x, xAxis.y, xAxis.z, -math.dot(xAxis, pos),
            yAxis.x, yAxis.y, yAxis.z, -math.dot(yAxis, pos),
            zAxis.x, zAxis.y, zAxis.z, -math.dot(zAxis, pos),
            0, 0, 0, 1);

        var near = 0.05f;
        var m11 = 1.0f / math.tan(fovRadians * 0.5f);
        var m22 = near / (near - range);
        var m23 = range * near / (range - near);
        var proj = new float4x4(
            m11, 0, 0, 0,
            0, m11, 0, 0,
            0, 0, m22, m23,
            0, 0, 1, 0);

        return Frustum.Create(math.mul(proj, view), pos, dir, near, range);
    }

    [TestMethod]
    public void BuddyAllocator_AllocateAndFree_CoalescesBackToFullAtlas()
    {
        var allocator = new ShadowAtlasBuddyAllocator(1024);

        // Fill entire 1024 atlas with sixteen 256x256 tiles
        var nodes = new uint[16];
        for (var i = 0; i < 16; i++)
        {
            Assert.IsTrue(allocator.TryAllocate(256, 1, out nodes[i], out _));
        }

        Assert.IsFalse(allocator.TryAllocate(256, 1, out _, out _));

        // Free all 16 tiles: must coalesce cleanly so a full 1024x1024 allocation succeeds
        for (var i = 0; i < 16; i++)
        {
            allocator.Free(nodes[i]);
        }

        Assert.IsTrue(allocator.TryAllocate(1024, 2, out var rootNode, out var rootRegion));
        Assert.AreEqual(0u, rootNode);
        Assert.AreEqual(0u, rootRegion.x);
        Assert.AreEqual(0u, rootRegion.y);
        Assert.AreEqual(1024u, rootRegion.width);
    }

    [TestMethod]
    public void BuddyAllocator_FragmentedCheckerboard_EvictsOldestBlockAndSucceeds()
    {
        var allocator = new ShadowAtlasBuddyAllocator(1024);

        // Allocate 4x 512 blocks, then free 3 children of one 512 block (leaving 1 checkerboard child)
        Assert.IsTrue(allocator.TryAllocate(256, 1, out var keepOld, out _));
        Assert.IsTrue(allocator.TryAllocate(512, 2, out _, out _));
        Assert.IsTrue(allocator.TryAllocate(512, 2, out _, out _));
        Assert.IsTrue(allocator.TryAllocate(512, 2, out _, out _));

        // Frame 3: cannot allocate 512 directly because all 4 quadrants have >= 1 tile
        Assert.IsFalse(allocator.TryAllocate(512, 3, out _, out _));

        // Eviction must pick the block containing keepOld (frame 1 < frame 2)
        var leaves = new List<uint>();
        Assert.IsTrue(allocator.TryFindEvictionBlock(512, 3, out _, leaves));
        Assert.AreEqual(1, leaves.Count);
        Assert.AreEqual(keepOld, leaves[0]);

        allocator.Free(leaves[0]);
        Assert.IsTrue(allocator.TryAllocate(512, 3, out _, out _));
    }

    [TestMethod]
    public void PunctualShadowCache_StationaryAndMovedBounds_InvalidatesCorrectly()
    {
        using var cache = new PunctualShadowCache(1024);

        var pos = new float3(0, 5, 0);
        var dir = new float3(0, -1, 0);
        var frustum = CreateSpotWorldFrustum(pos, dir, 15.0f);

        var query = new ShadowViewCacheQuery
        {
            entityKey = 1001ul,
            faceIndex = 0u,
            positionWS = pos,
            directionWS = dir,
            range = 15.0f,
            fovOrAngle = 1.0f,
            nearPlane = 0.05f,
            shadowSize = 256,
            lodErrorScale = 1.0f,
            worldFrustum = frustum,
        };

        // Frame 1: cache miss -> dirty
        cache.BeginFrame(1);
        Assert.IsTrue(cache.TryAcquireView(in query, out var region1, out var dirty1));
        Assert.IsTrue(dirty1);
        cache.MarkRendered(1001ul, 0u);

        // Frame 2: stationary -> clean cache hit, same atlas region
        cache.BeginFrame(2);
        Assert.IsTrue(cache.TryAcquireView(in query, out var region2, out var dirty2));
        Assert.IsFalse(dirty2);
        Assert.AreEqual(region1.x, region2.x);
        Assert.AreEqual(region1.y, region2.y);

        // Frame 3: object moves OUTSIDE frustum -> still clean
        cache.BeginFrame(3);
        cache.InvalidateBounds(new AABB(new float3(50, 0, 50), new float3(52, 2, 52)));
        Assert.IsTrue(cache.TryAcquireView(in query, out _, out var dirty3));
        Assert.IsFalse(dirty3);

        // Frame 4: instance moves INSIDE frustum -> becomes dirty
        cache.BeginFrame(4);
        var meshHandle = new Handle<Mesh>(1, 1);
        var localBox = new AABB(new float3(-0.5f), new float3(0.5f));
        var insideTransform = float4x4.TRS(new float3(0, 1, 0), quaternion.identity, float3.one);
        cache.OnInstanceUpdated(0u, in insideTransform, meshHandle, in localBox, ShadowCastingMode.On);

        Assert.IsTrue(cache.TryAcquireView(in query, out _, out var dirty4));
        Assert.IsTrue(dirty4);
        cache.MarkRendered(1001ul, 0u);

        // Frame 5: instance moves OUT of frustum -> old bounds inside frustum must still invalidate!
        cache.BeginFrame(5);
        var outsideTransform = float4x4.TRS(new float3(100, 1, 0), quaternion.identity, float3.one);
        cache.OnInstanceUpdated(0u, in outsideTransform, meshHandle, in localBox, ShadowCastingMode.On);

        Assert.IsTrue(cache.TryAcquireView(in query, out _, out var dirty5));
        Assert.IsTrue(dirty5);
        cache.MarkRendered(1001ul, 0u);

        // Frame 6: light moves -> becomes dirty
        cache.BeginFrame(6);
        query.positionWS = new float3(1, 5, 0);
        query.worldFrustum = CreateSpotWorldFrustum(query.positionWS, dir, 15.0f);
        Assert.IsTrue(cache.TryAcquireView(in query, out _, out var dirty6));
        Assert.IsTrue(dirty6);
    }

    [TestMethod]
    public void PunctualShadowCache_PointLight_InvalidatesOnlyAffectedFaces()
    {
        using var cache = new PunctualShadowCache(2048);
        var pos = float3.zero;
        var range = 20.0f;
        var fov = MathF.PI * 0.5f;

        var dirs = new float3[]
        {
            new(1, 0, 0), new(-1, 0, 0),
            new(0, 1, 0), new(0, -1, 0),
            new(0, 0, 1), new(0, 0, -1)
        };

        cache.BeginFrame(1);
        for (uint f = 0; f < 6; f++)
        {
            var q = new ShadowViewCacheQuery
            {
                entityKey = 42ul,
                faceIndex = f,
                positionWS = pos,
                directionWS = dirs[f],
                range = range,
                fovOrAngle = fov,
                nearPlane = 0.05f,
                shadowSize = 256,
                lodErrorScale = 1.0f,
                worldFrustum = CreateSpotWorldFrustum(pos, dirs[f], range, fov),
            };
            Assert.IsTrue(cache.TryAcquireView(in q, out _, out var dirty));
            Assert.IsTrue(dirty);
            cache.MarkRendered(42ul, f);
        }

        // Move an object strictly on the +X side (face 0)
        cache.BeginFrame(2);
        cache.InvalidateBounds(new AABB(new float3(8, -1, -1), new float3(10, 1, 1)));

        for (uint f = 0; f < 6; f++)
        {
            var q = new ShadowViewCacheQuery
            {
                entityKey = 42ul,
                faceIndex = f,
                positionWS = pos,
                directionWS = dirs[f],
                range = range,
                fovOrAngle = fov,
                nearPlane = 0.05f,
                shadowSize = 256,
                lodErrorScale = 1.0f,
                worldFrustum = CreateSpotWorldFrustum(pos, dirs[f], range, fov),
            };
            Assert.IsTrue(cache.TryAcquireView(in q, out _, out var dirty));
            Assert.AreEqual(f == 0u, dirty, $"Expected only +X face (0) to be dirty; face {f} dirty={dirty}.");
        }
    }
}
