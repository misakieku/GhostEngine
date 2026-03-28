using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

public partial class DockGroupNode : DockNode
{
    [ObservableProperty]
    private Orientation _orientation = Orientation.Horizontal;

    public ObservableCollection<DockNode> Children { get; } = new();

    public void AddChild(DockNode node)
    {
        node.Parent = this;
        Children.Add(node);
    }

    public void RemoveChild(DockNode node)
    {
        if (Children.Remove(node))
        {
            node.Parent = null;
        }
    }
}
