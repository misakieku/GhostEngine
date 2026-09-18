using Ghost.Engine.RenderPipeline;

namespace Ghost.UnitTest.Streaming;

[TestClass]
public class GPUViewContextTests
{
    [TestMethod]
    public void ComputeHZBDimensions_Standard1080p_ProducesExactHalfResolution()
    {
        GPUViewContext.ComputeHZBDimensions(
            1920,
            1080,
            out var baseW,
            out var baseH,
            out var hzbMipCount);

        Assert.AreEqual(960u, baseW);
        Assert.AreEqual(540u, baseH);
        Assert.AreEqual(10u, hzbMipCount);
    }

    [TestMethod]
    public void ComputeHZBDimensions_Standard4K_ProducesExactHalfResolution()
    {
        GPUViewContext.ComputeHZBDimensions(
            3840,
            2160,
            out var baseW,
            out var baseH,
            out var hzbMipCount);

        Assert.AreEqual(1920u, baseW);
        Assert.AreEqual(1080u, baseH);
        Assert.AreEqual(11u, hzbMipCount);
    }

    [TestMethod]
    public void ComputeHZBDimensions_Ultrawide21x9_ProducesExactHalfResolution()
    {
        GPUViewContext.ComputeHZBDimensions(
            3440,
            1440,
            out var baseW,
            out var baseH,
            out var hzbMipCount);

        Assert.AreEqual(1720u, baseW);
        Assert.AreEqual(720u, baseH);
        Assert.AreEqual(11u, hzbMipCount);
    }

    [TestMethod]
    public void ComputeHZBDimensions_OddDimensions_UsesCeilingHalfResolution()
    {
        GPUViewContext.ComputeHZBDimensions(
            1739,
            981,
            out var baseW,
            out var baseH,
            out var hzbMipCount);

        Assert.AreEqual(870u, baseW);
        Assert.AreEqual(491u, baseH);
        Assert.AreEqual(10u, hzbMipCount);
    }
}
