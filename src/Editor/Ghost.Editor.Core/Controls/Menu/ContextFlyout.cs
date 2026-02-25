using Ghost.Editor.Core.Utilities;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;


namespace Ghost.Editor.Core.Controls;

public sealed partial class ContextFlyout : MenuFlyout
{
    private class MenuNode
    {
        public required string Name
        {
            get; init;
        }

        public MethodInfo? Method
        {
            get; set;
        }

        public List<MenuNode> Children
        {
            get;
        } = new();

        public int RawGroup
        {
            get; set;
        } = int.MaxValue;

        // The calculated group used for sorting (min of children for folders)
        public int EffectiveGroup
        {
            get; set;
        }
    }

    private bool _isPopulated;

    public string Tag
    {
        get; set;
    } = string.Empty;

    public ContextFlyout()
    {
        Opening += ContextFlyout_Opening;
    }

    // Recursively sorts nodes and calculates folder groups
    private static void PrepareNodes(List<MenuNode> nodes)
    {
        if (nodes.Count == 0)
        {
            return;
        }

        foreach (var node in nodes)
        {
            if (node.Children.Count > 0)
            {
                // Go deep first
                PrepareNodes(node.Children);

                // A folder's group is determined by its highest priority child (lowest group number).
                // This ensures a "File" folder (containing Group 0 items) sits at the top 
                // alongside other Group 0 leaf items.
                node.EffectiveGroup = node.Children.Min(c => c.EffectiveGroup);
            }
            else
            {
                node.EffectiveGroup = node.RawGroup;
            }
        }

        // Sort by Group, then by Name
        nodes.Sort((a, b) =>
        {
            var groupCompare = a.EffectiveGroup.CompareTo(b.EffectiveGroup);
            return groupCompare != 0
                ? groupCompare
                : string.CompareOrdinal(a.Name, b.Name);
        });
    }

    // Recursively builds the UI elements
    private static void BuildNodes(List<MenuNode> nodes, IList<MenuFlyoutItemBase> targetCollection)
    {
        if (nodes.Count == 0)
        {
            return;
        }

        var currentGroup = nodes[0].EffectiveGroup;

        foreach (var node in nodes)
        {
            if (node.EffectiveGroup != currentGroup)
            {
                targetCollection.Add(new MenuFlyoutSeparator());
                currentGroup = node.EffectiveGroup;
            }

            if (node.Children.Count > 0)
            {
                var subItem = new MenuFlyoutSubItem
                {
                    Text = node.Name
                };

                // Recursively render children into the subitem
                BuildNodes(node.Children, subItem.Items);
                targetCollection.Add(subItem);
            }
            else
            {
                var menuItem = new MenuFlyoutItem
                {
                    Text = node.Name
                };

                var methodToInvoke = node.Method;
                menuItem.Click += (_, _) =>
                {
                    methodToInvoke?.Invoke(null, null);
                };

                targetCollection.Add(menuItem);
            }
        }
    }

    private void PopulateContextMenu()
    {
        var methods = TypeCache.GetMethodsWithAttribute<ContextMenuItemAttribute>();
        if (methods == null)
        {
            return;
        }

        // 1. Build the Tree
        var rootNodes = new List<MenuNode>();

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<ContextMenuItemAttribute>();
            if (attr == null)
            {
                continue;
            }

            // Filter tags
            if (!string.Equals(attr.Tag, Tag, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var nameSpan = attr.Name.AsSpan();
            var pathParts = nameSpan.Split('/');

            var currentLevel = rootNodes;
            MenuNode? currentNode = null;

            foreach (var range in pathParts)
            {
                var part = nameSpan[range.Start..range.End];

                MenuNode? foundNode = null;

                // Try to find existing node in the current level
                foreach (var node in currentLevel)
                {
                    if (part.Equals(node.Name.AsSpan(), StringComparison.Ordinal))
                    {
                        foundNode = node;
                        break;
                    }
                }

                if (foundNode == null)
                {
                    foundNode = new MenuNode { Name = part.ToString() };
                    currentLevel.Add(foundNode);
                }

                currentNode = foundNode;

                // If this is the last part, it's the executable item
                if (range.End.Value == nameSpan.Length)
                {
                    currentNode.Method = method;
                    currentNode.RawGroup = attr.Group;
                }

                currentLevel = currentNode.Children;
            }
        }

        PrepareNodes(rootNodes);
        BuildNodes(rootNodes, Items);
    }

    private async void ContextFlyout_Opening(object? sender, object e)
    {
        if (_isPopulated)
        {
            return;
        }

        PopulateContextMenu();
        _isPopulated = true;
    }
}