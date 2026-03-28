using Ghost.Editor.View.Controls;
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
        var pos = DockLayout.CalculateDockPosition(100, 100, 50, 50, THRESHOLD);
        Assert.AreEqual(DockLayout.DockPosition.Center, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Left()
    {
        var pos = DockLayout.CalculateDockPosition(100, 100, 10, 50, THRESHOLD);
        Assert.AreEqual(DockLayout.DockPosition.Left, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Right()
    {
        var pos = DockLayout.CalculateDockPosition(100, 100, 90, 50, THRESHOLD);
        Assert.AreEqual(DockLayout.DockPosition.Right, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Top()
    {
        var pos = DockLayout.CalculateDockPosition(100, 100, 50, 10, THRESHOLD);
        Assert.AreEqual(DockLayout.DockPosition.Top, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_Bottom()
    {
        var pos = DockLayout.CalculateDockPosition(100, 100, 50, 90, THRESHOLD);
        Assert.AreEqual(DockLayout.DockPosition.Bottom, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_CornerPrecedence_LeftTop()
    {
        // (10, 10) is in both Left and Top zones.
        // Current implementation: Left/Right win over Top/Bottom.
        var pos = DockLayout.CalculateDockPosition(100, 100, 10, 10, THRESHOLD);
        Assert.AreEqual(DockLayout.DockPosition.Left, pos);
    }

    [TestMethod]
    public void TestCalculateDockPosition_CornerPrecedence_RightBottom()
    {
        var pos = DockLayout.CalculateDockPosition(100, 100, 90, 90, THRESHOLD);
        Assert.AreEqual(DockLayout.DockPosition.Right, pos);
    }
}
