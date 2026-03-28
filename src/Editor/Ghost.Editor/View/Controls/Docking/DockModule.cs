using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

public abstract class DockModule : Control
{
    public DockContainer? Owner { get; internal set; }
    
    /// <summary>
    /// Gets or sets the root docking layout this module belongs to.
    /// </summary>
    public DockingLayout? Root { get; internal set; }
    
    public void Detach()
    {
        Owner?.RemoveChild(this);
    }
}
