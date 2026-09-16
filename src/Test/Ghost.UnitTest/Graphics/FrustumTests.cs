using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public class FrustumTests
{
    private static bool SphereIntersectFrustum(float3 center, float radius, in Frustum frustum)
    {
        for (var i = 0; i < 6; i++)
        {
            var plane = frustum.planes[i];
            var distance = math.dot(plane.xyz, center) + plane.w;
            if (distance < -radius)
            {
                return false;
            }
        }
        return true;
    }

    [TestMethod]
    public void TestFrustumPlanes_NearAndFarPlanes_AtOrigin()
    {
        var viewPos = new float3(0, 0, 0);
        var viewDir = new float3(0, 0, 1);
        var nearClip = 1.0f;
        var farClip = 100.0f;

        var frustum = Frustum.Create(float4x4.identity, viewPos, viewDir, nearClip, farClip);

        ref readonly var nearPlane = ref frustum.planes[4];
        ref readonly var farPlane = ref frustum.planes[5];

#pragma warning disable MSTEST0037
        // Object in middle of frustum
        var centerPt = new float3(0, 0, 50.0f);
        var nearDist = math.dot(nearPlane.xyz, centerPt) + nearPlane.w;
        var farDist = math.dot(farPlane.xyz, centerPt) + farPlane.w;
        Assert.IsTrue(nearDist > 0.0f, $"Near distance was {nearDist}, expected > 0");
        Assert.IsTrue(farDist > 0.0f, $"Far distance was {farDist}, expected > 0");

        // Object behind near plane (at z = 0.5, near = 1.0)
        var behindNear = new float3(0, 0, 0.5f);
        var behindDist = math.dot(nearPlane.xyz, behindNear) + nearPlane.w;
        Assert.IsTrue(behindDist < 0.0f, $"Near distance for behind point was {behindDist}, expected < 0");

        // Object beyond far plane (at z = 105, far = 100)
        var pastFar = new float3(0, 0, 105.0f);
        var pastDist = math.dot(farPlane.xyz, pastFar) + farPlane.w;
        Assert.IsTrue(pastDist < 0.0f, $"Far distance for past point was {pastDist}, expected < 0");
    }

    [TestMethod]
    public void TestFrustumPlanes_NearAndFarPlanes_WhenCameraMovesAway()
    {
        // Camera placed far from origin, looking down positive Z
        var viewPos = new float3(50, 100, -200);
        var viewDir = new float3(0, 0, 1);
        var nearClip = 0.5f;
        var farClip = 500.0f;

        var frustum = Frustum.Create(float4x4.identity, viewPos, viewDir, nearClip, farClip);

        ref readonly var nearPlane = ref frustum.planes[4];
        ref readonly var farPlane = ref frustum.planes[5];

        // An object at (50, 100, 0) is 200 units in front of camera
        var objectPos = new float3(50, 100, 0);
        var nearDist = math.dot(nearPlane.xyz, objectPos) + nearPlane.w;
        var farDist = math.dot(farPlane.xyz, objectPos) + farPlane.w;

        // Exact expected: nearDist = 200 - 0.5 = 199.5; farDist = 500 - 200 = 300
        Assert.AreEqual(199.5f, nearDist, 1e-4f);
        Assert.AreEqual(300.0f, farDist, 1e-4f);

        // Bounding sphere of radius 1.0 at objectPos should intersect the frustum
        Assert.IsTrue(math.dot(nearPlane.xyz, objectPos) + nearPlane.w >= -1.0f);
        Assert.IsTrue(math.dot(farPlane.xyz, objectPos) + farPlane.w >= -1.0f);
#pragma warning restore MSTEST0037
    }

    [TestMethod]
    public void TestFrustumPlanes_RotatedCamera()
    {
        var viewPos = new float3(10, 20, 30);
        var viewDir = math.normalize(new float3(1, 1, 1));
        var nearClip = 2.0f;
        var farClip = 200.0f;

        var frustum = Frustum.Create(float4x4.identity, viewPos, viewDir, nearClip, farClip);

        ref readonly var nearPlane = ref frustum.planes[4];
        ref readonly var farPlane = ref frustum.planes[5];

        // Point along view direction at distance 50 from viewPos
        var testPt = viewPos + viewDir * 50.0f;
        var nearDist = math.dot(nearPlane.xyz, testPt) + nearPlane.w;
        var farDist = math.dot(farPlane.xyz, testPt) + farPlane.w;

        Assert.AreEqual(48.0f, nearDist, 1e-3f);
        Assert.AreEqual(150.0f, farDist, 1e-3f);

        // Point behind near plane
        var tooClose = viewPos + viewDir * 1.0f;
        var tooCloseDist = math.dot(nearPlane.xyz, tooClose) + nearPlane.w;
        Assert.AreEqual(-1.0f, tooCloseDist, 1e-3f);

        // Point beyond far plane
        var tooFar = viewPos + viewDir * 250.0f;
        var tooFarDist = math.dot(farPlane.xyz, tooFar) + farPlane.w;
        Assert.AreEqual(-50.0f, tooFarDist, 1e-3f);
    }
}
