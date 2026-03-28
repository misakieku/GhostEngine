using Microsoft.UI.Xaml;

namespace Ghost.Editor.View.Controls.Docking;

public class FloatingWindow : Window
{
    public FloatingWindow(DockDocument document)
    {
        var layout = new DockingLayout();
        var group = new DockGroup();
        group.AddChild(document);
        
        var panel = new DockPanel();
        panel.AddChild(group);
        layout.RootPanel = panel;

        Content = layout;
        
        // Basic window setup
        AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
    }
}
