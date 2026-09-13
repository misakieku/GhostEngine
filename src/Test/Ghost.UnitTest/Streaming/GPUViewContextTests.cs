using Ghost.Engine.RenderPipeline;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.UnitTest.Streaming;

[TestClass]
public class GPUViewContextTests
{
    [TestMethod]
    public void Settings_DefaultHzbMaxMegapixels_IsOneMegapixel()
    {
        var settings = new GhostRenderPipelineSettings();
        Assert.AreEqual(1.0f, settings.HzbMaxMegapixels);
    }

    [TestMethod]
    public void ComputeHZBMipOffsets_Standard1080p_UnderBudget_IsNotClamped()
    {
        GPUViewContext.ComputeHZBMipOffsets(
            1920,
            1080,
            1.0f,
            out var baseW,
            out var baseH,
            out var hzbMipCount,
            out var atlasWidth,
            out var atlasHeight,
            out var o0,
            out var o1,
            out var o2,
            out var o3);

        Assert.AreEqual(960u, baseW);
        Assert.AreEqual(540u, baseH);
        Assert.IsLessThanOrEqualTo(1_000_000.0, (double)baseW * baseH);

        // Verify packed[0] stores base width in low 16 bits and base height in high 16 bits
        Assert.AreEqual(960u, o0.x & 0xFFFFu);
        Assert.AreEqual(540u, o0.x >> 16);
    }

    [TestMethod]
    public void ComputeHZBMipOffsets_Standard4K_ExceedsBudget_IsClamped()
    {
        GPUViewContext.ComputeHZBMipOffsets(
            3840,
            2160,
            1.0f,
            out var baseW,
            out var baseH,
            out var hzbMipCount,
            out var atlasWidth,
            out var atlasHeight,
            out var o0,
            out var o1,
            out var o2,
            out var o3);

        Assert.IsLessThan(1920u, baseW);
        Assert.IsLessThan(1080u, baseH);
        Assert.IsLessThanOrEqualTo(1_000_000.0, (double)baseW * baseH);

        // Aspect ratio (16:9 ~ 1.777) should be preserved
        var expectedAspect = 3840.0 / 2160.0;
        var actualAspect = (double)baseW / baseH;
        Assert.AreEqual(expectedAspect, actualAspect, 0.05);

        Assert.AreEqual(baseW, o0.x & 0xFFFFu);
        Assert.AreEqual(baseH, o0.x >> 16);
    }

    [TestMethod]
    public void ComputeHZBMipOffsets_Ultrawide21x9_IsClamped_PreservesAspect()
    {
        // 3440x1440 ultrawide -> half res 1720x720 = 1.238 MP (> 1.0 MP)
        GPUViewContext.ComputeHZBMipOffsets(
            3440,
            1440,
            1.0f,
            out var baseW,
            out var baseH,
            out var hzbMipCount,
            out var atlasWidth,
            out var atlasHeight,
            out var o0,
            out var o1,
            out var o2,
            out var o3);

        Assert.IsLessThan(1720u, baseW);
        Assert.IsLessThan(720u, baseH);
        Assert.IsLessThanOrEqualTo(1_000_000.0, (double)baseW * baseH);

        var expectedAspect = 3440.0 / 1440.0;
        var actualAspect = (double)baseW / baseH;
        Assert.AreEqual(expectedAspect, actualAspect, 0.05);
    }

    [TestMethod]
    public void ComputeHZBMipOffsets_BudgetZero_DisablesClamping()
    {
        GPUViewContext.ComputeHZBMipOffsets(
            3840,
            2160,
            0.0f,
            out var baseW,
            out var baseH,
            out var hzbMipCount,
            out var atlasWidth,
            out var atlasHeight,
            out var o0,
            out var o1,
            out var o2,
            out var o3);

        Assert.AreEqual(1920u, baseW);
        Assert.AreEqual(1080u, baseH);
    }
}
