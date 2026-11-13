using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public abstract class PropertyDrawer
{
    public abstract Element Draw(PropertyNode property, DrawerContext context);

    protected static Element DrawPropertyLabel(PropertyNode property)
    {
        return TextBlock(property.DisplayName)
            .Foreground(GhostColors.TextSecondary)
            .FontSize(12)
            .TextTrimming(TextTrimming.CharacterEllipsis)
            .MaxLines(1)
            .VAlign(VerticalAlignment.Center)
            .Padding(right: GhostLayout.CONTROL_GAP)
            .ToolTip(property.DisplayName);
    }

    protected static Element DrawLabeledRow(PropertyNode property, Element editor)
    {
        return Grid(
            columns: [GridSize.Px(GhostLayout.PROPERTY_LABEL_WIDTH), GridSize.Star()],
            rows: [GridSize.Auto],
            DrawPropertyLabel(property).Grid(row: 0, column: 0),
            editor.VAlign(VerticalAlignment.Center).Grid(row: 0, column: 1)
        ).MinHeight(GhostLayout.ROW_MIN_HEIGHT);
    }
}
