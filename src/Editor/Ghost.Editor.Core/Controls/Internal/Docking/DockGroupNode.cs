using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

public partial class DockGroupNode : DockNode
{
    [ObservableProperty]
    public partial Orientation Orientation { get; set; }

    public DockGroupNode()
    {
        Orientation = Orientation.Horizontal;
    }

    public ObservableCollection<DockNode> Children { get; } = new();

    public void AddChild(DockNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node == this)
        {
            throw new InvalidOperationException("Cannot add a node to itself.");
        }

        if (Children.Contains(node))
        {
            return;
        }

        // Check for cycles
        var current = this.Parent;
        while (current != null)
        {
            if (current == node)
            {
                throw new InvalidOperationException("Cannot add an ancestor as a child (cycle detected).");
            }
            current = current.Parent;
        }

        if (node.Parent != null && node.Parent != this)
        {
            node.Parent.RemoveChild(node);
        }

        node.Parent = this;
        Children.Add(node);
    }

    public void RemoveChild(DockNode node)
    {
        if (Children.Remove(node))
        {
            node.Parent = null;
        }
    }
}
