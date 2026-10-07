using Ghost.Engine.RenderPipeline;
using Ghost.Graphics.RHI;
using Misaki.HighPerformance.Mathematics;
using System.Runtime.InteropServices;

namespace Ghost.UnitTest.Rendering;

[TestClass]
public class LightGatherTests
{
    [TestMethod]
    public void FrameData_HasCorrectSizeAnd16ByteAlignment()
    {
        var size = Marshal.SizeOf<FrameData>();
        Assert.AreEqual(48, size, "FrameData must be exactly 48 bytes (12 uints).");
        Assert.AreEqual(0, size % 16, "FrameData must be 16-byte aligned for constant buffer compatibility.");
    }

    [TestMethod]
    public void GhostRenderPayload_DirectionalLightCollection()
    {
        // Allocate a payload (with null pipeline since we only test light storage)
        var payload = new GhostRenderPayload(null!);

        Assert.AreEqual(0u, payload.DirectionalLightCount);
        Assert.IsFalse(payload.HasDirectionalLight);
        Assert.AreEqual(-1, payload.PrimaryDirectionalLightIndex);

        var light0 = new GPUDirectionalLight
        {
            directionWS = new float3(0.0f, -1.0f, 0.0f),
            castShadows = 1u,
            color = new float3(1.0f, 0.9f, 0.8f) * 2.0f,
            shadowBiasMultiplier = 1.0f
        };

        var light1 = new GPUDirectionalLight
        {
            directionWS = new float3(0.5f, -0.5f, 0.0f),
            castShadows = 0u,
            color = new float3(0.2f, 0.3f, 0.5f) * 0.5f,
            shadowBiasMultiplier = 1.0f
        };

        var idx0 = payload.AddDirectionalLight(in light0);
        var idx1 = payload.AddDirectionalLight(in light1);

        Assert.AreEqual(0u, idx0);
        Assert.AreEqual(1u, idx1);
        Assert.AreEqual(2u, payload.DirectionalLightCount);
        Assert.IsTrue(payload.HasDirectionalLight);

        payload.SetPrimaryDirectionalLightIndex((int)idx0);
        Assert.AreEqual(0, payload.PrimaryDirectionalLightIndex);
        Assert.AreEqual(light0.color, payload.CurrentSunLight.color);

        // Verify read view
        var view = payload.DirectionalLights;
        Assert.AreEqual(2, view.Length);
        Assert.AreEqual(light0.directionWS, view[0].directionWS);
        Assert.AreEqual(light1.directionWS, view[1].directionWS);

        // Reset
        payload.Reset();
        Assert.AreEqual(0u, payload.DirectionalLightCount);
        Assert.IsFalse(payload.HasDirectionalLight);
        Assert.AreEqual(-1, payload.PrimaryDirectionalLightIndex);

        payload.Dispose();
    }
}
