using Ghost.Editor.Core.Controls.Internal.Docking;
using WinUIEx;

namespace Ghost.Editor.View.Windows;

public sealed partial class DockWindow : WindowEx
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
        
        // Optional: Titlebar setup etc.
    }
}
