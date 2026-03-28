using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ghost.UnitTest;

[TestClass]
public class DockingModelTest
{
    [TestMethod]
    public void TestAddChild_SetsParent()
    {
        var group = new DockGroupNode();
        var child = new DockPanelNode();

        group.AddChild(child);

        Assert.AreEqual(group, child.Parent);
        CollectionAssert.Contains(group.Children, child);
    }

    [TestMethod]
    public void TestAddChild_RemovesFromOldParent()
    {
        var group1 = new DockGroupNode();
        var group2 = new DockGroupNode();
        var child = new DockPanelNode();

        group1.AddChild(child);
        group2.AddChild(child);

        Assert.AreEqual(group2, child.Parent);
        CollectionAssert.DoesNotContain(group1.Children, child);
        CollectionAssert.Contains(group2.Children, child);
    }

    [TestMethod]
    public void TestAddChild_PreventsCycle()
    {
        var group1 = new DockGroupNode();
        var group2 = new DockGroupNode();

        group1.AddChild(group2);
        bool thrown = false;
        try
        {
            group2.AddChild(group1);
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }
        Assert.IsTrue(thrown, "Should have thrown InvalidOperationException due to cycle.");
    }

    [TestMethod]
    public void TestRemoveChild_ClearsParent()
    {
        var group = new DockGroupNode();
        var child = new DockPanelNode();

        group.AddChild(child);
        group.RemoveChild(child);

        Assert.IsNull(child.Parent);
        CollectionAssert.DoesNotContain(group.Children, child);
    }

    [TestMethod]
    public void TestPanel_SelectionSync_IndexToItem()
    {
        var panel = new DockPanelNode();
        var item1 = new object();
        var item2 = new object();

        panel.Items.Add(item1);
        panel.Items.Add(item2);

        panel.SelectedIndex = 1;
        Assert.AreEqual(item2, panel.SelectedItem);

        panel.SelectedIndex = 0;
        Assert.AreEqual(item1, panel.SelectedItem);

        panel.SelectedIndex = -1;
        Assert.IsNull(panel.SelectedItem);
    }

    [TestMethod]
    public void TestPanel_SelectionSync_ItemToIndex()
    {
        var panel = new DockPanelNode();
        var item1 = new object();
        var item2 = new object();

        panel.Items.Add(item1);
        panel.Items.Add(item2);

        panel.SelectedItem = item2;
        Assert.AreEqual(1, panel.SelectedIndex);

        panel.SelectedItem = item1;
        Assert.AreEqual(0, panel.SelectedIndex);

        panel.SelectedItem = null;
        Assert.AreEqual(-1, panel.SelectedIndex);
    }

    [TestMethod]
    public void TestPanel_CollectionChanged_UpdatesSelection()
    {
        var panel = new DockPanelNode();
        var item1 = new object();
        
        panel.Items.Add(item1);
        Assert.AreEqual(0, panel.SelectedIndex);
        Assert.AreEqual(item1, panel.SelectedItem);

        panel.Items.Remove(item1);
        Assert.AreEqual(-1, panel.SelectedIndex);
        Assert.IsNull(panel.SelectedItem);
    }

    [TestMethod]
    public void TestPanel_RemoveMiddleItem_MaintainsSelection()
    {
        var panel = new DockPanelNode();
        var item1 = new object();
        var item2 = new object();
        var item3 = new object();

        panel.Items.Add(item1);
        panel.Items.Add(item2);
        panel.Items.Add(item3);

        panel.SelectedItem = item2;
        Assert.AreEqual(1, panel.SelectedIndex);

        // Remove item1 (before selection)
        panel.Items.Remove(item1);
        Assert.AreEqual(item2, panel.SelectedItem);
        Assert.AreEqual(0, panel.SelectedIndex);
    }

    [TestMethod]
    public void TestPanel_RemoveSelectedItem_UpdatesSelection()
    {
        var panel = new DockPanelNode();
        var item1 = new object();
        var item2 = new object();

        panel.Items.Add(item1);
        panel.Items.Add(item2);

        panel.SelectedItem = item1;
        panel.Items.Remove(item1);

        // Should fallback to next available item at same index
        Assert.AreEqual(item2, panel.SelectedItem);
        Assert.AreEqual(0, panel.SelectedIndex);
    }

    [TestMethod]
    public void TestInsertChild_Reorder()
    {
        var group = new DockGroupNode();
        var child1 = new DockPanelNode();
        var child2 = new DockPanelNode();
        var child3 = new DockPanelNode();

        group.AddChild(child1);
        group.AddChild(child2);
        group.AddChild(child3);

        // Move child1 to end
        group.InsertChild(3, child1);
        Assert.AreEqual(child2, group.Children[0]);
        Assert.AreEqual(child3, group.Children[1]);
        Assert.AreEqual(child1, group.Children[2]);

        // Move child3 to start
        group.InsertChild(0, child3);
        Assert.AreEqual(child3, group.Children[0]);
        Assert.AreEqual(child2, group.Children[1]);
        Assert.AreEqual(child1, group.Children[2]);
    }

    [TestMethod]
    public void TestInsertChild_SameIndex_NoOp()
    {
        var group = new DockGroupNode();
        var child1 = new DockPanelNode();
        var child2 = new DockPanelNode();

        group.AddChild(child1);
        group.AddChild(child2);

        group.InsertChild(0, child1);
        Assert.AreEqual(child1, group.Children[0]);
        Assert.AreEqual(child2, group.Children[1]);
    }
}
