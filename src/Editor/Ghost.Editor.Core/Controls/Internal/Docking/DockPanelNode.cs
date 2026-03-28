using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

public partial class DockPanelNode : DockNode
{
    public ObservableCollection<object> Items { get; } = new();

    [ObservableProperty]
    private int _selectedIndex = -1;

    [ObservableProperty]
    private object? _selectedItem;
}
