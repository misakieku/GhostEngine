using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Views.Controls.Docking;

/// <summary>
/// Represents a visual highlight for a docking region.
/// </summary>
public partial class DockRegionHighlight : Control
{
    public DockRegionHighlight()
    {
        DefaultStyleKey = typeof(DockRegionHighlight);
        IsHitTestVisible = false;
    }
}
