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
    public void ComputeHZBDimensions_Standard1080p_UnderBudget_IsNotClamped()
    {
        GPUViewContext.ComputeHZBDimensions(
            1920,
            1080,
            1.0f,
            out var baseW,
            out var baseH,
            out var hzbMipCount);

        Assert.AreEqual(960u, baseW);
        Assert.AreEqual(540u, baseH);
        Assert.IsLessThanOrEqualTo(1_000_000.0, (double)baseW * baseH);
        Assert.AreEqual(10u, hzbMipCount);
    }

    [TestMethod]
    public void ComputeHZBDimensions_Standard4K_ExceedsBudget_IsClamped()
    {
        GPUViewContext.ComputeHZBDimensions(
            3840,
            2160,
            1.0f,
            out var baseW,
            out var baseH,
            out var hzbMipCount);

        Assert.IsLessThan(1920u, baseW);
        Assert.IsLessThan(1080u, baseH);
        Assert.IsLessThanOrEqualTo(1_000_000.0, (double)baseW * baseH);

        // Aspect ratio (16:9 ~ 1.777) should be preserved
        var expectedAspect = 3840.0 / 2160.0;
        var actualAspect = (double)baseW / baseH;
        Assert.AreEqual(expectedAspect, actualAspect, 0.05);
    }

    [TestMethod]
    public void ComputeHZBDimensions_Ultrawide21x9_IsClamped_PreservesAspect()
    {
        // 3440x1440 ultrawide -> half res 1720x720 = 1.238 MP (> 1.0 MP)
        GPUViewContext.ComputeHZBDimensions(
            3440,
            1440,
            1.0f,
            out var baseW,
            out var baseH,
            out var hzbMipCount);

        Assert.IsLessThan(1720u, baseW);
        Assert.IsLessThan(720u, baseH);
        Assert.IsLessThanOrEqualTo(1_000_000.0, (double)baseW * baseH);

        var expectedAspect = 3440.0 / 1440.0;
        var actualAspect = (double)baseW / baseH;
        Assert.AreEqual(expectedAspect, actualAspect, 0.05);
    }

    [TestMethod]
    public void ComputeHZBDimensions_BudgetZero_DisablesClamping()
    {
        GPUViewContext.ComputeHZBDimensions(
            3840,
            2160,
            0.0f,
            out var baseW,
            out var baseH,
            out var hzbMipCount);

        Assert.AreEqual(1920u, baseW);
        Assert.AreEqual(1080u, baseH);
    }
}
