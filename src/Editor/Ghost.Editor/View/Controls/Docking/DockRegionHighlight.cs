using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

/// <summary>
/// Represents a visual highlight for a docking region.
/// </summary>
public class DockRegionHighlight : Control
{
    public DockRegionHighlight()
    {
        DefaultStyleKey = typeof(DockRegionHighlight);
        IsHitTestVisible = false;
    }
}
