using Ghost.Editor.Core.Menus;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Menus;

/// <summary>
/// Converts headless <see cref="MenuNode"/> trees from <see cref="EditorMenuRegistry"/> into Reactor <see cref="MenuFlyoutItemBase"/> elements.
/// </summary>
public static class EditorMenuBuilder
{
    public static Element BuildFlyout(string location, MenuContext context)
    {
        var items = BuildItems(location, context);
        return MenuItems(items);
    }

    public static MenuFlyoutItemBase[] BuildItems(string location, MenuContext context)
    {
        var nodes = EditorMenuRegistry.Default.BuildMenu(location, context);
        return ConvertNodes(nodes);
    }

    private static MenuFlyoutItemBase[] ConvertNodes(IReadOnlyList<MenuNode> nodes)
    {
        var result = new List<MenuFlyoutItemBase>(nodes.Count);
        for (var i = 0; i < nodes.Count; i++)
        {
            switch (nodes[i])
            {
                case MenuSeparatorNode:
                    result.Add(MenuSeparator());
                    break;

                case MenuSubMenuNode sub:
                    var subItem = MenuSubItem(sub.Label, ConvertNodes(sub.Children));
                    if (!string.IsNullOrEmpty(sub.Icon))
                    {
                        subItem = subItem with { IconElement = FontIcon(sub.Icon, fontSize: 12) };
                    }

                    result.Add(subItem);
                    break;

                case MenuActionNode action:
                    var label = string.IsNullOrEmpty(action.Shortcut) ? action.Label : $"{action.Label} ({action.Shortcut})";
                    var item = MenuItem(label, action.IsEnabled ? action.Execute : null);
                    if (!string.IsNullOrEmpty(action.Icon))
                    {
                        item = item with { IconElement = FontIcon(action.Icon, fontSize: 12) };
                    }

                    result.Add(item);
                    break;
            }
        }

        return result.ToArray();
    }
}
