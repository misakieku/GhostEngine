using Ghost.Editor.Core.Controls.Internal.Docking;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

/// <summary>
/// Provides methods for mutating the docking layout tree.
/// </summary>
public static class DockMutationEngine
{
    /// <summary>
    /// Applies the tree mutation for a drop operation.
    /// </summary>
    /// <returns>True if the mutation was applied; otherwise, false.</returns>
    public static bool TryApplyDropMutation(DockGroupNode root, DockPanelNode targetNode, DockPanelNode sourceNode, object item, DockPosition position)
    {
        if (position == DockPosition.Center)
        {
            if (!sourceNode.Items.Remove(item)) return false;
            targetNode.Items.Add(item);
            return true;
        }

        // Split scenario
        bool isHorizontalSplit = position == DockPosition.Left || position == DockPosition.Right;
        bool isAfter = position == DockPosition.Right || position == DockPosition.Bottom;

        var parentGroup = targetNode.Parent;
        if (parentGroup == null)
        {
            // Target is root or only child of root.
            if (root.Children.Count == 1 && ReferenceEquals(root.Children[0], targetNode))
            {
                if (!sourceNode.Items.Remove(item)) return false;

                var newGroup = new DockGroupNode
                {
                    Orientation = isHorizontalSplit ? Orientation.Horizontal : Orientation.Vertical
                };

                var newNode = new DockPanelNode();
                newNode.Items.Add(item);

                root.RemoveChild(targetNode);
                root.AddChild(newGroup);

                if (isAfter)
                {
                    newGroup.AddChild(targetNode);
                    newGroup.AddChild(newNode);
                }
                else
                {
                    newGroup.AddChild(newNode);
                    newGroup.AddChild(targetNode);
                }
                return true;
            }
            return false;
        }

        int targetIndex = parentGroup.Children.IndexOf(targetNode);
        if (targetIndex < 0) return false;

        if (!sourceNode.Items.Remove(item)) return false;

        if ((isHorizontalSplit && parentGroup.Orientation == Orientation.Horizontal) ||
            (!isHorizontalSplit && parentGroup.Orientation == Orientation.Vertical))
        {
            var newNode = new DockPanelNode();
            newNode.Items.Add(item);
            parentGroup.InsertChild(isAfter ? targetIndex + 1 : targetIndex, newNode);
        }
        else
        {
            parentGroup.RemoveChild(targetNode);
            
            var newGroup = new DockGroupNode
            {
                Orientation = isHorizontalSplit ? Orientation.Horizontal : Orientation.Vertical
            };
            
            var newNode = new DockPanelNode();
            newNode.Items.Add(item);

            if (isAfter)
            {
                newGroup.AddChild(targetNode);
                newGroup.AddChild(newNode);
            }
            else
            {
                newGroup.AddChild(newNode);
                newGroup.AddChild(targetNode);
            }

            parentGroup.InsertChild(targetIndex, newGroup);
        }

        return true;
    }

    /// <summary>
    /// Cleans up empty panels and redundant groups in the tree.
    /// </summary>
    public static void CleanupEmptyNodes(DockNode node)
    {
        if (node is DockPanelNode panelNode && panelNode.Items.Count > 0) return;
        
        var parentGroup = node.Parent;
        if (parentGroup == null) return;

        // If it's an empty panel, remove it
        if (node is DockPanelNode emptyPanel && emptyPanel.Items.Count == 0)
        {
            parentGroup.RemoveChild(emptyPanel);
            CleanupEmptyNodes(parentGroup);
            return;
        }

        // If it's a group with 0 or 1 children, collapse it
        if (node is DockGroupNode group)
        {
            if (group.Children.Count == 0)
            {
                parentGroup.RemoveChild(group);
                CleanupEmptyNodes(parentGroup);
            }
            else if (group.Children.Count == 1)
            {
                var onlyChild = group.Children[0];
                int index = parentGroup.Children.IndexOf(group);
                if (index >= 0)
                {
                    parentGroup.RemoveChild(group);
                    parentGroup.InsertChild(index, onlyChild);
                    CleanupEmptyNodes(parentGroup);
                }
            }
        }
    }
}
