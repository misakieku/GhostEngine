using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

/// <summary>
/// Base class for all dockable modules in the docking system.
/// </summary>
public abstract class DockModule : Control
{
    /// <summary>
    /// Gets the container that owns this module.
    /// </summary>
    public DockContainer? Owner { get; internal set; }
    
    private DockingLayout? _root;

    /// <summary>
    /// Gets or sets the root docking layout this module belongs to.
    /// </summary>
    public virtual DockingLayout? Root
    {
        get => _root;
        internal set
        {
            if (_root != value)
            {
                _root = value;
                OnRootChanged();
            }
        }
    }

    protected virtual void OnRootChanged() { }
    
    /// <summary>
    /// Detaches this module from its current owner.
    /// </summary>
    public void Detach()
    {
        Owner?.RemoveChild(this);
    }
}
