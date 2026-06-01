using Ghost.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Controls;

public sealed partial class MenuContextBar : MenuBar
{
    private bool _isPopulated;

    public string ContextMenuTag
    {
        get; set;
    } = string.Empty;

    public MenuContextBar()
    {
        Loaded += MenuContextBar_Loaded;
    }

    private void MenuContextBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isPopulated)
        {
            return;
        }

        PopulateMenu();
        _isPopulated = true;
    }

    private void PopulateMenu()
    {
        var rootNodes = MenuUtility.BuildTree(ContextMenuTag);

        foreach (var node in rootNodes)
        {
            if (node.Children.Count == 0)
            {
                Logger.Warning($"Menu item '{node.Name}' cannot be placed at the root of a MenuContextBar because it lacks a parent group.");
                continue;
            }

            var menuBarItem = new MenuBarItem
            {
                Title = node.Name
            };

            MenuUtility.BuildNodes(node.Children, menuBarItem.Items);
            Items.Add(menuBarItem);
        }
    }
}
