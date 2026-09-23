using Ghost.MicroTest.Core;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.CompilerServices;

namespace Ghost.MicroTest;

internal unsafe class BarycentricTest : ITest
{
    public void Setup() { }
    public void Cleanup() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float4x4 TRS(float3 position, quaternion rotation, float3 scale)
    {
        var R = new float3x3(rotation);
        return new float4x4(
            R[0][0] * scale.x, R[1][0] * scale.y, R[2][0] * scale.z, position.x,
            R[0][1] * scale.x, R[1][1] * scale.y, R[2][1] * scale.z, position.y,
            R[0][2] * scale.x, R[1][2] * scale.y, R[2][2] * scale.z, position.z,
            0f, 0f, 0f, 1f
        );
    }

    public void Run()
    {
        var screenSize = new float2(1920, 1080);

        // Triangle vertices in local space
        var v0Pos = new float3(-0.5f, -0.5f, 0.0f);
        var v1Pos = new float3( 0.5f, -0.5f, 0.0f);
        var v2Pos = new float3( 0.0f,  0.5f, 0.0f);

        // A fixed 3D surface point with known barycentrics (0.5, 0.3, 0.2)
        var trueBary = new float3(0.5f, 0.3f, 0.2f);
        var fixedLocalPos = trueBary.x * v0Pos + trueBary.y * v1Pos + trueBary.z * v2Pos;

        // Test from Camera 1: Straight on
        TestView("Cam 1 (Straight)", new float3(0, 0, -30), quaternion.identity, fixedLocalPos, v0Pos, v1Pos, v2Pos, screenSize);

        // Test from Camera 2: Viewed from side / angle (grazing angle)
        var rotY45 = quaternion.EulerXYZ(new float3(0, math.radians(45.0f), 0));
        var camPos2 = math.mul(rotY45, new float3(0, 0, -30));
        TestView("Cam 2 (45 deg angle)", camPos2, rotY45, fixedLocalPos, v0Pos, v1Pos, v2Pos, screenSize);

        // Test from Camera 3: Viewed from steep angle (75 deg grazing)
        var rotY75 = quaternion.EulerXYZ(new float3(0, math.radians(75.0f), 0));
        var camPos3 = math.mul(rotY75, new float3(0, 0, -30));
        TestView("Cam 3 (75 deg angle)", camPos3, rotY75, fixedLocalPos, v0Pos, v1Pos, v2Pos, screenSize);
    }

    private static void TestView(string name, float3 camPos, quaternion camRot, float3 localPoint, float3 v0, float3 v1, float3 v2, float2 screenSize)
    {
        var viewMatrix = math.inverse(TRS(camPos, camRot, new float3(1, 1, 1)));

        float vfov = 2.0f * math.atan(24.0f / (2.0f * 20.0f));
        float aspectScreen = screenSize.x / screenSize.y;
        float m_11 = 1.0f / math.tan(vfov * 0.5f);
        float m_00 = m_11 / aspectScreen;
        float nearClip = 0.1f;
        float farClip = 1000.0f;
        float m_22 = nearClip / (nearClip - farClip);
        float m_23 = (farClip * nearClip) / (farClip - nearClip);

        var projMatrix = new float4x4(
            m_00, 0, 0, 0,
            0, m_11, 0, 0,
            0, 0, m_22, m_23,
            0, 0, 1, 0
        );

        var vpMatrix = math.mul(projMatrix, viewMatrix);
        var localToWorld = TRS(float3.zero, quaternion.identity, new float3(5, 5, 5));
        var wvp = math.mul(vpMatrix, localToWorld);

        var p0 = math.mul(wvp, new float4(v0, 1.0f));
        var p1 = math.mul(wvp, new float4(v1, 1.0f));
        var p2 = math.mul(wvp, new float4(v2, 1.0f));
        var pPt = math.mul(wvp, new float4(localPoint, 1.0f));

        var invW = new float3(1.0f / p0.w, 1.0f / p1.w, 1.0f / p2.w);

        var s0 = new float2((p0.x * invW.x * 0.5f + 0.5f) * screenSize.x, (1.0f - (p0.y * invW.y * 0.5f + 0.5f)) * screenSize.y);
        var s1 = new float2((p1.x * invW.x * 0.5f + 0.5f) * screenSize.x, (1.0f - (p1.y * invW.y * 0.5f + 0.5f)) * screenSize.y);
        var s2 = new float2((p2.x * invW.x * 0.5f + 0.5f) * screenSize.x, (1.0f - (p2.y * invW.y * 0.5f + 0.5f)) * screenSize.y);

        // Project fixed 3D point to screen
        var ptInvW = 1.0f / pPt.w;
        var pScreen = new float2((pPt.x * ptInvW * 0.5f + 0.5f) * screenSize.x, (1.0f - (pPt.y * ptInvW * 0.5f + 0.5f)) * screenSize.y);

        // Evaluate using GhostEngine's formula
        var e0 = s1 - s2;
        var e1 = s2 - s0;
        var det = e0.y * (s0.x - s2.x) - e0.x * (s0.y - s2.y);
        var invDet = (Math.Abs(det) > 1e-12f) ? (1.0f / det) : 0.0f;

        var lambda0 = (e0.y * (pScreen.x - s2.x) - e0.x * (pScreen.y - s2.y)) * invDet;
        var lambda1 = (e1.y * (pScreen.x - s0.x) - e1.x * (pScreen.y - s0.y)) * invDet;
        var lambda2 = 1.0f - lambda0 - lambda1;

        var b0 = lambda0 * invW.x;
        var b1 = lambda1 * invW.y;
        var b2 = lambda2 * invW.z;
        var W = b0 + b1 + b2;
        var invWSum = (Math.Abs(W) > 1e-12f) ? (1.0f / W) : 0.0f;
        var baryHLSL = new float3(b0, b1, b2) * invWSum;

        // Evaluate using Nyx/UE formula
        var invViewportSize = new float2(1.0f / screenSize.x, 1.0f / screenSize.y);
        var pixelClip = pScreen * invViewportSize * new float2(2.0f, -2.0f) + new float2(-1.0f, 1.0f);
        var ndc0 = p0.xy * invW.x;
        var ndc1 = p1.xy * invW.y;
        var ndc2 = p2.xy * invW.z;

        float Edge(float2 a, float2 b, float2 pt) => (pt.x - a.x) * (b.y - a.y) - (pt.y - a.y) * (b.x - a.x);
        var edge = new float3(Edge(ndc1, ndc2, pixelClip), Edge(ndc2, ndc0, pixelClip), Edge(ndc0, ndc1, pixelClip));
        var numer = edge * invW;
        var denom = numer.x + numer.y + numer.z;
        var baryNyx = numer / denom;

        Console.WriteLine($"[{name}]");
        Console.WriteLine($"  Expected 3D Bary: (0.500, 0.300, 0.200)");
        Console.WriteLine($"  invW: ({invW.x}, {invW.y}, {invW.z})");
        Console.WriteLine($"  s0: ({s0.x}, {s0.y}), s1: ({s1.x}, {s1.y}), s2: ({s2.x}, {s2.y})");
        Console.WriteLine($"  pScreen: ({pScreen.x}, {pScreen.y})");
        Console.WriteLine($"  det: {det}");
        Console.WriteLine($"  2D Screen Lambda: ({lambda0}, {lambda1}, {lambda2})");
        Console.WriteLine($"  b0, b1, b2: ({b0}, {b1}, {b2})");
        Console.WriteLine($"  GhostEngine Bary: ({baryHLSL.x:F3}, {baryHLSL.y:F3}, {baryHLSL.z:F3})");
        Console.WriteLine($"  edge: ({edge.x}, {edge.y}, {edge.z})");
        Console.WriteLine($"  numer: ({numer.x}, {numer.y}, {numer.z}), denom: {denom}");
        Console.WriteLine($"  Nyx/UE Bary:      ({baryNyx.x:F3}, {baryNyx.y:F3}, {baryNyx.z:F3})");
    }
}
