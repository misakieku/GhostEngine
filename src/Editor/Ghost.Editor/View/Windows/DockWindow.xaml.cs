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
    }

    private void OnTabTornOff(object? sender, TabTornOffEventArgs e)
    {
        var newWindow = new DockWindow(e.TabContent);
        App.AddSecondaryWindow(newWindow);
        newWindow.Activate();
    }
}
