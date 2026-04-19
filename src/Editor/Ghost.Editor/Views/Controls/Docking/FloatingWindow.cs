using Microsoft.UI.Xaml;

namespace Ghost.Editor.Views.Controls.Docking;

/// <summary>
/// A floating window that contains a docking layout.
/// </summary>
public class FloatingWindow : Window
{
    private readonly DockingLayout _layout;

    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingWindow"/> class with the specified document.
    /// </summary>
    /// <param name="document">The document to display in the floating window.</param>
    public FloatingWindow(DockDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        _layout = new DockingLayout();
        var group = new DockGroup();
        group.AddChild(document);
        
        _layout.RootModule = group;
        _layout.LayoutEmpty += (s, e) => Close();

        Content = _layout;
        
        // Basic window setup
        AppWindow.Resize(new global::Windows.Graphics.SizeInt32(800, 600));

        // When the user manually closes the window, ensure we clean up the documents inside
        this.Closed += FloatingWindow_Closed;
    }

    private void FloatingWindow_Closed(object sender, WindowEventArgs args)
    {
        // Force cleanup of the visual tree so we don't leak anything from this window
        _layout.RootModule = null;
        Content = null;
    }
}
