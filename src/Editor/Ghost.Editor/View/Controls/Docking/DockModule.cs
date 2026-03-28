using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

public abstract class DockModule : Control
{
    public DockContainer? Owner { get; internal set; }
    // Note: DockingLayout will be implemented in a later task
    // public DockingLayout? Root { get; internal set; }
    
    public void Detach()
    {
        // Owner?.Children.Remove(this);
        Owner = null;
    }
}
