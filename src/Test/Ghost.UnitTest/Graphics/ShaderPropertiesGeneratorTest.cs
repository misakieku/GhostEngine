using System.Runtime.InteropServices;
using Ghost.Engine.ShaderProperties;

namespace Ghost.UnitTest.Graphics;

[TestClass]
public class ShaderPropertiesGeneratorTest
{
    [TestMethod]
    public unsafe void TestHiddenBlitShaderProperties_LayoutAndConstants()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Hidden/Blit", HiddenBlitShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(HiddenBlitShaderProperties);
        Assert.AreEqual(8, size); // 2 uint fields = 8 bytes

        var instance = new HiddenBlitShaderProperties();
        Assert.AreEqual(0u, instance.mainTex);
        Assert.AreEqual(0u, instance.sampler_mainTex);
    }

    [TestMethod]
    public unsafe void TestInternalUpdateGPUSceneShaderProperties_LayoutAndConstants()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Internal/UpdateGPUScene", InternalUpdateGPUSceneShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(InternalUpdateGPUSceneShaderProperties);
        Assert.AreEqual(20, size); // 5 uint fields = 20 bytes

        var instance = new InternalUpdateGPUSceneShaderProperties();
        Assert.AreEqual(0u, instance.gpuSceneBuffer);
    }

    [TestMethod]
    public unsafe void TestHiddenDebugClassificationShaderProperties_LayoutAndConstants()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Hidden/DebugClassification", HiddenDebugClassificationShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(HiddenDebugClassificationShaderProperties);
        Assert.AreEqual(24, size); // 6 uint fields = 24 bytes

        var instance = new HiddenDebugClassificationShaderProperties();
        Assert.AreEqual(0u, instance.variantTileListIndex);
    }

    [TestMethod]
    public unsafe void TestInternalTileMaterialClassificationShaderProperties_LayoutAndOffset()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Internal/TileMaterialClassification", InternalTileMaterialClassificationShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(InternalTileMaterialClassificationShaderProperties);
        Assert.AreEqual(44, size); // 9 uint fields + 1 uint2 field = 44 bytes

        var offset = (int)Marshal.OffsetOf<InternalTileMaterialClassificationShaderProperties>(nameof(InternalTileMaterialClassificationShaderProperties.deferredVariantMask));
        Assert.AreEqual(36, offset); // Exactly at byte 36, matching HLSL ByteAddressBuffer packing!
    }
}
