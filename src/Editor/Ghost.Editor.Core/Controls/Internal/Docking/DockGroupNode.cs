using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

/// <summary>
/// A docking node that contains multiple children and arranges them in a specific orientation.
/// </summary>
public partial class DockGroupNode : DockNode
{
    /// <summary>
    /// Gets or sets the layout orientation of the children.
    /// </summary>
    [ObservableProperty]
    public partial Orientation Orientation { get; set; }

    private readonly ObservableCollection<DockNode> _children = new();

    /// <summary>
    /// Gets the collection of child nodes.
    /// </summary>
    public ReadOnlyObservableCollection<DockNode> Children { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockGroupNode"/> class.
    /// </summary>
    public DockGroupNode()
    {
        Children = new ReadOnlyObservableCollection<DockNode>(_children);
        Orientation = Orientation.Horizontal;
    }

    /// <summary>
    /// Adds a child node to this group, enforcing tree invariants.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if node is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if adding the node would create a cycle or if adding self.</exception>
    public void AddChild(DockNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node == this)
        {
            throw new InvalidOperationException("Cannot add a node to itself.");
        }

        if (_children.Contains(node))
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
        _children.Add(node);
    }

    /// <summary>
    /// Removes a child node from this group.
    /// </summary>
    /// <param name="node">The node to remove.</param>
    public void RemoveChild(DockNode node)
    {
        if (_children.Remove(node))
        {
            node.Parent = null;
        }
    }
}
