using CommunityToolkit.Mvvm.ComponentModel;

namespace Ghost.Editor.Core.Controls.Internal.Docking;

/// <summary>
/// Base class for all nodes in the docking layout tree.
/// </summary>
public abstract partial class DockNode : ObservableObject
{
    /// <summary>
    /// Gets the parent group of this node.
    /// </summary>
    [ObservableProperty]
    public partial DockGroupNode? Parent { get; internal set; }
}
