using Microsoft.UI.Xaml;

namespace Ghost.Editor.View.Controls.Docking;

/// <summary>
/// A floating window that contains a docking layout.
/// </summary>
public class FloatingWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingWindow"/> class with the specified document.
    /// </summary>
    /// <param name="document">The document to display in the floating window.</param>
    public FloatingWindow(DockDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

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
