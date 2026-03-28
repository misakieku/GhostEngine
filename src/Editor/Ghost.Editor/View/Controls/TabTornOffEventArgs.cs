using Ghost.Editor.Core.Controls.Internal.Docking;

namespace Ghost.Editor.View.Controls;

/// <summary>
/// Event arguments for the TabTornOff event.
/// </summary>
public sealed class TabTornOffEventArgs : EventArgs
{
    /// <summary>
    /// Gets the content of the tab being torn off.
    /// </summary>
    public object TabContent { get; }

    /// <summary>
    /// Gets the source node the tab is being torn off from.
    /// </summary>
    public DockPanelNode SourceNode { get; }

    public TabTornOffEventArgs(object tabContent, DockPanelNode sourceNode)
    {
        TabContent = tabContent;
        SourceNode = sourceNode;
    }
}
