using Ghost.Core;
using Ghost.Editor.Core.Controls.Internal.Docking;
using Ghost.Editor.View.Controls;
using WinUIEx;

namespace Ghost.Editor.View.Windows;

internal sealed partial class DockWindow : WindowEx
{
    public DockWindow(object initialTabContent)
    {
        InitializeComponent();
        
        // Setup initial single panel layout
        var rootGroup = new DockGroupNode();
        var panel = new DockPanelNode();
        panel.Items.Add(initialTabContent);
        rootGroup.AddChild(panel);
        
        PART_DockLayout.Root = rootGroup;
        PART_DockLayout.TabTornOff += OnTabTornOff;

        ((System.Collections.Specialized.INotifyCollectionChanged)rootGroup.Children).CollectionChanged += (s, e) =>
        {
            if (rootGroup.Children.Count == 0)
            {
                this.Close();
            }
        };
    }

    private void OnTabTornOff(object? sender, TabTornOffEventArgs e)
    {
        App.CreateAndShowDockWindow(e.TabContent);
    }
}
