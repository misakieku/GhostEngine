using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Controls.Docking;

public class DockRegionHighlight : Control
{
    public DockRegionHighlight()
    {
        DefaultStyleKey = typeof(DockRegionHighlight);
        IsHitTestVisible = false;
    }
}
