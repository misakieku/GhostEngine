using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.UnitTest;

[TestClass]
public class DockingMutationTest
{
    [TestMethod]
    public void TestCenterDrop_MovesItem()
    {
        var root = new DockGroupNode();
        var panel1 = new DockPanelNode();
        var panel2 = new DockPanelNode();
        var item = new object();
        
        panel1.Items.Add(item);
        root.AddChild(panel1);
        root.AddChild(panel2);

        bool result = DockMutationEngine.TryApplyDropMutation(root, panel2, panel1, item, DockPosition.Center);

        Assert.IsTrue(result);
        Assert.IsEmpty(panel1.Items);
        Assert.HasCount(1, panel2.Items);
        Assert.AreEqual(item, panel2.Items[0]);
    }

    [TestMethod]
    public void TestSplitDrop_CreatesNewStructure()
    {
        var root = new DockGroupNode { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal };
        var panel1 = new DockPanelNode();
        var item = new object();
        
        panel1.Items.Add(item);
        root.AddChild(panel1);

        // Vertical Split Drop (Bottom) on panel1
        bool result = DockMutationEngine.TryApplyDropMutation(root, panel1, panel1, item, DockPosition.Bottom);

        Assert.IsTrue(result);
        Assert.HasCount(1, root.Children);
        var newGroup = root.Children[0] as DockGroupNode;
        Assert.IsNotNull(newGroup);
        Assert.AreEqual(Microsoft.UI.Xaml.Controls.Orientation.Vertical, newGroup.Orientation);
        Assert.HasCount(2, newGroup.Children);
        Assert.AreEqual(panel1, newGroup.Children[0]);
        var newNode = newGroup.Children[1] as DockPanelNode;
        Assert.IsNotNull(newNode);
        Assert.HasCount(1, newNode.Items);
        Assert.AreEqual(item, newNode.Items[0]);
    }

    [TestMethod]
    public void TestCleanup_Cascades()
    {
        var root = new DockGroupNode();
        var group1 = new DockGroupNode();
        var panel1 = new DockPanelNode();
        
        group1.AddChild(panel1);
        root.AddChild(group1);

        // panel1 becomes empty
        panel1.Items.Clear();
        
        DockMutationEngine.CleanupEmptyNodes(panel1);

        Assert.IsEmpty(root.Children);
        Assert.IsNull(group1.Parent);
        Assert.IsNull(panel1.Parent);
    }

    [TestMethod]
    public void TestCleanup_CollapsesRedundantGroup()
    {
        var root = new DockGroupNode();
        var group1 = new DockGroupNode();
        var panel1 = new DockPanelNode();
        var panel2 = new DockPanelNode();
        
        group1.AddChild(panel1);
        group1.AddChild(panel2);
        root.AddChild(group1);

        // panel2 is removed
        group1.RemoveChild(panel2);
        
        DockMutationEngine.CleanupEmptyNodes(group1);

        Assert.HasCount(1, root.Children);
        Assert.AreEqual(panel1, root.Children[0]);
        Assert.AreEqual(root, panel1.Parent);
    }
}
