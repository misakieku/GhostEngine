using Ghost.Editor.Core.Utilities;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;
using System.Runtime.InteropServices;


namespace Ghost.Editor.Core.Controls;

public sealed partial class ContextFlyout : MenuFlyout
{
    private bool _isPopulated;

    public string Tag
    {
        get; set;
    } = string.Empty;

    public ContextFlyout()
    {
        Opening += ContextFlyout_Opening;
    }

    private void PopulateContextMenu()
    {
        var methods = TypeCache.GetMethodsWithAttribute<ContextMenuItemAttribute>();
        if (methods == null)
        {
            return;
        }

        var list = new List<(ContextMenuItemAttribute attr, MethodInfo method)>();
        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes(typeof(ContextMenuItemAttribute), false);
            var attribute = (ContextMenuItemAttribute)attributes[0];

            if (!string.Equals(attribute.Tag, Tag, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            list.Add((attribute, method));
        }

        var span = CollectionsMarshal.AsSpan(list);
        span.Sort((a, b) =>
        {
            var result = a.attr.Group.CompareTo(b.attr.Group);
            if (result == 0)
            {
                result = string.CompareOrdinal(a.attr.Name, b.attr.Name);
            }

            return result;
        });

        // itemContainer may be a main thread only collection (for example, ObservableCollection), we run it on the UI thread
        var i = 0;
        var group = 0;
        while (i < list.Count)
        {
            var (attr, method) = list[i];
            if (attr.Group != group)
            {
                Items.Add(new MenuFlyoutSeparator());
                group = attr.Group;
            }

            // TODO: Group items with / in the name into submenus
            var menuItem = new MenuFlyoutItem
            {
                Text = attr.Name
            };

            menuItem.Click += (_, _) =>
            {
                method.Invoke(null, null);
            };

            Items.Add(menuItem);
            i++;
        }
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