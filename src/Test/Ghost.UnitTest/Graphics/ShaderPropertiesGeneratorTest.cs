using Ghost.Engine.ShaderProperties;
using System.Runtime.InteropServices;

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
        Assert.AreEqual(12, size); // 3 fields = 12 bytes

        var instance = new HiddenBlitShaderProperties();
        Assert.AreEqual(0u, instance.mainTex);
        Assert.AreEqual(0u, instance.sampler_mainTex);
        Assert.AreEqual(0u, instance.linearToSRGB);
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
        Assert.AreEqual(76, size); // 2 uint4 fields + 11 uint fields = 76 bytes

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

    [TestMethod]
    public unsafe void TestInternalTileLightCullingShaderProperties_LayoutAndConstants()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Internal/TileLightCulling", InternalTileLightCullingShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(InternalTileLightCullingShaderProperties);
        Assert.AreEqual(24, size); // 6 uint fields = 24 bytes

        Assert.AreEqual(0, (int)Marshal.OffsetOf<InternalTileLightCullingShaderProperties>(nameof(InternalTileLightCullingShaderProperties.depthTextureIndex)));
        Assert.AreEqual(4, (int)Marshal.OffsetOf<InternalTileLightCullingShaderProperties>(nameof(InternalTileLightCullingShaderProperties.tileLightListUav)));
        Assert.AreEqual(8, (int)Marshal.OffsetOf<InternalTileLightCullingShaderProperties>(nameof(InternalTileLightCullingShaderProperties.renderWidth)));
        Assert.AreEqual(12, (int)Marshal.OffsetOf<InternalTileLightCullingShaderProperties>(nameof(InternalTileLightCullingShaderProperties.renderHeight)));
        Assert.AreEqual(16, (int)Marshal.OffsetOf<InternalTileLightCullingShaderProperties>(nameof(InternalTileLightCullingShaderProperties.tilesX)));
        Assert.AreEqual(20, (int)Marshal.OffsetOf<InternalTileLightCullingShaderProperties>(nameof(InternalTileLightCullingShaderProperties.tilesY)));
    }

    [TestMethod]
    public unsafe void TestInternalConvolveSkyIrradianceSHShaderProperties_LayoutAndOffset()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Internal/ConvolveSkyIrradianceSH", InternalConvolveSkyIrradianceSHShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(InternalConvolveSkyIrradianceSHShaderProperties);
        Assert.AreEqual(48, size);

        Assert.AreEqual(0, (int)Marshal.OffsetOf<InternalConvolveSkyIrradianceSHShaderProperties>(nameof(InternalConvolveSkyIrradianceSHShaderProperties.skyboxTintAndIntensity)));
        Assert.AreEqual(16, (int)Marshal.OffsetOf<InternalConvolveSkyIrradianceSHShaderProperties>(nameof(InternalConvolveSkyIrradianceSHShaderProperties.skyboxRotationSinCos)));
        Assert.AreEqual(24, (int)Marshal.OffsetOf<InternalConvolveSkyIrradianceSHShaderProperties>(nameof(InternalConvolveSkyIrradianceSHShaderProperties.skyboxTexture)));
        Assert.AreEqual(28, (int)Marshal.OffsetOf<InternalConvolveSkyIrradianceSHShaderProperties>(nameof(InternalConvolveSkyIrradianceSHShaderProperties.skyboxSampler)));
        Assert.AreEqual(32, (int)Marshal.OffsetOf<InternalConvolveSkyIrradianceSHShaderProperties>(nameof(InternalConvolveSkyIrradianceSHShaderProperties.persistentViewStateUav)));
        Assert.AreEqual(36, (int)Marshal.OffsetOf<InternalConvolveSkyIrradianceSHShaderProperties>(nameof(InternalConvolveSkyIrradianceSHShaderProperties.viewId)));
        Assert.AreEqual(40, (int)Marshal.OffsetOf<InternalConvolveSkyIrradianceSHShaderProperties>(nameof(InternalConvolveSkyIrradianceSHShaderProperties.skyboxMipCount)));
        Assert.AreEqual(44, (int)Marshal.OffsetOf<InternalConvolveSkyIrradianceSHShaderProperties>(nameof(InternalConvolveSkyIrradianceSHShaderProperties.skyboxWidth)));
    }

    [TestMethod]
    public unsafe void TestInternalBuildHZBShaderProperties_LayoutAndOffset()
    {
#pragma warning disable MSTEST0032 // Assertion condition is always true
        Assert.AreEqual("Internal/BuildHZB", InternalBuildHZBShaderProperties.SHADER_NAME);
#pragma warning restore MSTEST0032 // Assertion condition is always true
        var size = sizeof(InternalBuildHZBShaderProperties);
        Assert.AreEqual(80, size);

        Assert.AreEqual(0, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.srcBuffer)));
        Assert.AreEqual(4, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.srcUavMip)));
        Assert.AreEqual(8, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.dstUavMip0)));
        Assert.AreEqual(12, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.dstUavMip1)));
        Assert.AreEqual(16, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.dstUavMip2)));
        Assert.AreEqual(20, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.dstUavMip3)));
        Assert.AreEqual(24, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.minDstCount)));
        Assert.AreEqual(28, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.isFirstBatch)));
        Assert.AreEqual(32, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.srcLimit)));
        Assert.AreEqual(40, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties._padding)));
        Assert.AreEqual(48, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.dstSize01)));
        Assert.AreEqual(64, (int)Marshal.OffsetOf<InternalBuildHZBShaderProperties>(nameof(InternalBuildHZBShaderProperties.dstSize23)));
    }
}
