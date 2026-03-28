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
}
