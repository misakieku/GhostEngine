using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.UI.Xaml.Controls;
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
        Assert.IsTrue(group.Children.Contains(child));
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
        Assert.IsFalse(group1.Children.Contains(child));
        Assert.IsTrue(group2.Children.Contains(child));
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
        Assert.IsTrue(thrown);
    }

    [TestMethod]
    public void TestRemoveChild_ClearsParent()
    {
        var group = new DockGroupNode();
        var child = new DockPanelNode();

        group.AddChild(child);
        group.RemoveChild(child);

        Assert.IsNull(child.Parent);
        Assert.IsFalse(group.Children.Contains(child));
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
}
