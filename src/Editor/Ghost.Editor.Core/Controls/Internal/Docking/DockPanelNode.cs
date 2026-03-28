using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

public partial class DockPanelNode : DockNode
{
    public ObservableCollection<object> Items { get; } = new();

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    [ObservableProperty]
    public partial object? SelectedItem { get; set; }

    public DockPanelNode()
    {
        SelectedIndex = -1;
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    private void OnItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (Items.Count == 0)
        {
            SelectedIndex = -1;
            SelectedItem = null;
        }
        else if (SelectedIndex >= Items.Count)
        {
            SelectedIndex = Items.Count - 1;
        }
        else if (SelectedIndex == -1 && Items.Count > 0)
        {
            SelectedIndex = 0;
        }
    }

    partial void OnSelectedIndexChanged(int value)
    {
        if (value >= 0 && value < Items.Count)
        {
            SelectedItem = Items[value];
        }
        else if (value == -1)
        {
            SelectedItem = null;
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
            SelectedIndex = -1;
        }
        else
        {
            int index = Items.IndexOf(value);
            if (index != -1)
            {
                SelectedIndex = index;
            }
        }
    }
}
