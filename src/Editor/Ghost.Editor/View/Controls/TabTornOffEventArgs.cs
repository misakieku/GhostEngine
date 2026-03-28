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

    public TabTornOffEventArgs(object tabContent)
    {
        TabContent = tabContent;
    }
}
