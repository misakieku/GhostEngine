using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

/// <summary>
/// A docking node that contains a collection of items (tabs) and manages selection.
/// </summary>
public partial class DockPanelNode : DockNode
{
    /// <summary>
    /// Gets the collection of items (tabs) in this panel.
    /// </summary>
    public ObservableCollection<object> Items { get; } = new();

    /// <summary>
    /// Gets or sets the index of the currently selected item.
    /// </summary>
    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    [ObservableProperty]
    public partial object? SelectedItem { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockPanelNode"/> class.
    /// </summary>
    public DockPanelNode()
    {
        SelectedIndex = -1;
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    private void OnItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // Reconcile selection on every collection change
        if (Items.Count == 0)
        {
            SelectedIndex = -1;
            SelectedItem = null;
            return;
        }

        if (SelectedItem != null)
        {
            int index = Items.IndexOf(SelectedItem);
            if (index != -1)
            {
                // Item still exists, update index if it changed
                if (SelectedIndex != index)
                {
                    SelectedIndex = index;
                }
                return;
            }
        }

        // SelectedItem is null or no longer in collection
        if (SelectedIndex >= 0 && SelectedIndex < Items.Count)
        {
            // Keep current index if valid, update item
            SelectedItem = Items[SelectedIndex];
        }
        else
        {
            // Fallback to first item or -1
            SelectedIndex = Items.Count > 0 ? 0 : -1;
        }
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (value >= 0 && value < Items.Count)
        {
            object newItem = Items[value];
            if (SelectedItem != newItem)
            {
                SelectedItem = newItem;
            }
        }
        else if (value == -1)
        {
            if (SelectedItem != null)
            {
                SelectedItem = null;
            }
        }
        else
        {
            // Clamp or reset if out of bounds
            SelectedIndex = Items.Count > 0 ? 0 : -1;
        }
    }

    partial void OnSelectedItemChanged(object? value)
    {
        if (value == null)
        {
            if (SelectedIndex != -1)
            {
                SelectedIndex = -1;
            }
        }
        else
        {
            int index = Items.IndexOf(value);
            if (index != -1)
            {
                if (SelectedIndex != index)
                {
                    SelectedIndex = index;
                }
            }
            else
            {
                // Item not in collection - reject selection
                SelectedItem = null;
                SelectedIndex = -1;
            }
        }
    }
}
