using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.UnitTest;

[TestClass]
public class DockLayoutTest
{
    private const double THRESHOLD = 0.25;

    [TestMethod]
    public void TestCalculateDockPosition_Center()
    {
        // 100x100, threshold 0.25 -> Center is [25, 75]
        var pos = DockMath.CalculateDockPosition(100, 100, 50, 50, THRESHOLD);
        Assert.AreEqual(DockPosition.Center, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Left()
    {
        var pos = DockMath.CalculateDockPosition(100, 100, 10, 50, THRESHOLD);
        Assert.AreEqual(DockPosition.Left, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Right()
    {
        var pos = DockMath.CalculateDockPosition(100, 100, 90, 50, THRESHOLD);
        Assert.AreEqual(DockPosition.Right, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Top()
    {
        var pos = DockMath.CalculateDockPosition(100, 100, 50, 10, THRESHOLD);
        Assert.AreEqual(DockPosition.Top, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Bottom()
    {
        var pos = DockMath.CalculateDockPosition(100, 100, 50, 90, THRESHOLD);
        Assert.AreEqual(DockPosition.Bottom, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_CornerPrecedence_LeftTop()
    {
        // (10, 10) is in both Left and Top zones.
        // Current implementation: Left/Right win over Top/Bottom.
        var pos = DockMath.CalculateDockPosition(100, 100, 10, 10, THRESHOLD);
        Assert.AreEqual(DockPosition.Left, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_CornerPrecedence_RightBottom()
    {
        var pos = DockMath.CalculateDockPosition(100, 100, 90, 90, THRESHOLD);
        Assert.AreEqual(DockPosition.Right, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Boundary_Left()
    {
        // x = 25 is exactly on the threshold. Current logic: x < 25 is Left, so 25 is Center.
        var pos = DockMath.CalculateDockPosition(100, 100, 25, 50, THRESHOLD);
        Assert.AreEqual(DockPosition.Center, pos);

        pos = DockMath.CalculateDockPosition(100, 100, 24.9, 50, THRESHOLD);
        Assert.AreEqual(DockPosition.Left, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Boundary_Right()
    {
        // x = 75 is exactly on the threshold (100 * (1 - 0.25)). Current logic: x > 75 is Right, so 75 is Center.
        var pos = DockMath.CalculateDockPosition(100, 100, 75, 50, THRESHOLD);
        Assert.AreEqual(DockPosition.Center, pos);

        pos = DockMath.CalculateDockPosition(100, 100, 75.1, 50, THRESHOLD);
        Assert.AreEqual(DockPosition.Right, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_InvalidSize()
    {
        var pos = DockMath.CalculateDockPosition(0, 100, 50, 50, THRESHOLD);
        Assert.AreEqual(DockPosition.None, pos);

        pos = DockMath.CalculateDockPosition(100, -10, 50, 50, THRESHOLD);
        Assert.AreEqual(DockPosition.None, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_ThresholdClamping()
    {
        // Threshold > 0.5 should be clamped to 0.5
        var pos = DockMath.CalculateDockPosition(100, 100, 40, 50, 0.8);
        Assert.AreEqual(DockPosition.Left, pos);

        // Threshold < 0 should be clamped to 0
        pos = DockMath.CalculateDockPosition(100, 100, 0.1, 50, -0.1);
        Assert.AreEqual(DockPosition.Center, pos);
    }
}
