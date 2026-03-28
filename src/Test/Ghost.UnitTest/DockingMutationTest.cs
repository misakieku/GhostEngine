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

        // Simulate Center Drop: panel1 -> panel2
        panel1.Items.Remove(item);
        panel2.Items.Add(item);

        Assert.AreEqual(0, panel1.Items.Count);
        Assert.AreEqual(1, panel2.Items.Count);
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

        // Simulate Vertical Split Drop (Bottom) on panel1
        // 1. Remove from source
        panel1.Items.Remove(item);
        
        // 2. Different orientation (Vertical), replace panel1 with new group
        root.RemoveChild(panel1);
        var newGroup = new DockGroupNode { Orientation = Microsoft.UI.Xaml.Controls.Orientation.Vertical };
        var newNode = new DockPanelNode();
        newNode.Items.Add(item);
        
        newGroup.AddChild(panel1); // Original
        newGroup.AddChild(newNode); // New split
        root.AddChild(newGroup);

        Assert.AreEqual(1, root.Children.Count);
        Assert.AreEqual(newGroup, root.Children[0]);
        Assert.AreEqual(2, newGroup.Children.Count);
        Assert.AreEqual(panel1, newGroup.Children[0]);
        Assert.AreEqual(newNode, newGroup.Children[1]);
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
        
        // Simulate CleanupEmptyNodes(panel1)
        // 1. panel1 is empty, remove from group1
        group1.RemoveChild(panel1);
        
        // 2. group1 is now empty, remove from root
        if (group1.Children.Count == 0)
        {
            root.RemoveChild(group1);
        }

        Assert.AreEqual(0, root.Children.Count);
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
        
        // group1 now has only 1 child (panel1), should collapse
        if (group1.Children.Count == 1)
        {
            var onlyChild = group1.Children[0];
            var parent = group1.Parent;
            int index = parent.Children.IndexOf(group1);
            
            parent.RemoveChild(group1);
            parent.InsertChild(index, onlyChild);
        }

        Assert.AreEqual(1, root.Children.Count);
        Assert.AreEqual(panel1, root.Children[0]);
        Assert.AreEqual(root, panel1.Parent);
    }
}
