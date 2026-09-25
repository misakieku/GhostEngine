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
        Assert.AreEqual(68, size); // 2 uint4 fields + 9 uint fields = 68 bytes

        var offset0 = (int)Marshal.OffsetOf<InternalTileMaterialClassificationShaderProperties>(nameof(InternalTileMaterialClassificationShaderProperties.deferredVariantMask0));
        Assert.AreEqual(0, offset0);

        var offset1 = (int)Marshal.OffsetOf<InternalTileMaterialClassificationShaderProperties>(nameof(InternalTileMaterialClassificationShaderProperties.deferredVariantMask1));
        Assert.AreEqual(16, offset1);

        var offsetVis = (int)Marshal.OffsetOf<InternalTileMaterialClassificationShaderProperties>(nameof(InternalTileMaterialClassificationShaderProperties.visBufferIndex));
        Assert.AreEqual(32, offsetVis);
    }

    [TestMethod]
    public unsafe void TestInternalPrepareDeferredTexturingIndirectArgsShaderProperties_LayoutAndConstants()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Internal/PrepareDeferredTexturingIndirectArgs", InternalPrepareDeferredTexturingIndirectArgsShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(InternalPrepareDeferredTexturingIndirectArgsShaderProperties);
        Assert.AreEqual(20, size); // 5 uint fields = 20 bytes
    }

    [TestMethod]
    public unsafe void TestInternalScatterVariantTilesShaderProperties_LayoutAndConstants()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Internal/ScatterVariantTiles", InternalScatterVariantTilesShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(InternalScatterVariantTilesShaderProperties);
        Assert.AreEqual(20, size); // 5 uint fields = 20 bytes
    }
}
