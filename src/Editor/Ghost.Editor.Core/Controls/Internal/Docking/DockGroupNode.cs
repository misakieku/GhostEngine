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
        InsertChild(_children.Count, node);
    }

    /// <summary>
    /// Inserts a child node at the specified index, enforcing tree invariants.
    /// </summary>
    /// <param name="index">The zero-based index at which node should be inserted.</param>
    /// <param name="node">The node to insert.</param>
    /// <remarks>
    /// If the node is already a child of this group, it will be moved to the specified index.
    /// The index represents the desired final position in the collection.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if node is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if index is less than 0 or greater than Children.Count.</exception>
    /// <exception cref="InvalidOperationException">Thrown if adding the node would create a cycle or if adding self.</exception>
    public void InsertChild(int index, DockNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (index < 0 || index > _children.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (node == this)
        {
            throw new InvalidOperationException("Cannot add a node to itself.");
        }

        if (_children.Contains(node))
        {
            int oldIndex = _children.IndexOf(node);
            if (oldIndex == index) return;

            _children.Move(oldIndex, index);
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
        _children.Insert(index, node);
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
