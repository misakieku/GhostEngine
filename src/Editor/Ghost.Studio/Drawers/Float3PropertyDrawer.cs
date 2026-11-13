using Ghost.Core;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using Misaki.HighPerformance.Mathematics;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class Float3PropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var current = property.Value is float3 f ? f : default;

        return Grid(
            columns: [GridSize.Px(GhostLayout.PROPERTY_LABEL_WIDTH), GridSize.Auto, GridSize.Star(), GridSize.Auto, GridSize.Star(), GridSize.Auto, GridSize.Star()],
            rows: [GridSize.Auto],

            DrawPropertyLabel(property).Grid(row: 0, column: 0),

            TextBlock("X").Foreground(GhostColors.StatusError).FontSize(11).SemiBold().VAlign(VerticalAlignment.Center).Margin(right: 4).Grid(row: 0, column: 1),
            NumberBox(current.x, v => context.ModifyProperty(property, current, new float3((float)v, current.y, current.z)))
                .SpinButtons(Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden)
                .AutomationName($"{property.DisplayName} X")
                .Margin(right: GhostLayout.CONTROL_GAP).Grid(row: 0, column: 2),

            TextBlock("Y").Foreground(GhostColors.StatusSuccess).FontSize(11).SemiBold().VAlign(VerticalAlignment.Center).Margin(right: 4).Grid(row: 0, column: 3),
            NumberBox(current.y, v => context.ModifyProperty(property, current, new float3(current.x, (float)v, current.z)))
                .SpinButtons(Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden)
                .AutomationName($"{property.DisplayName} Y")
                .Margin(right: GhostLayout.CONTROL_GAP).Grid(row: 0, column: 4),

            TextBlock("Z").Foreground(GhostColors.AccentPrimary).FontSize(11).SemiBold().VAlign(VerticalAlignment.Center).Margin(right: 4).Grid(row: 0, column: 5),
            NumberBox(current.z, v => context.ModifyProperty(property, current, new float3(current.x, current.y, (float)v)))
                .SpinButtons(Microsoft.UI.Xaml.Controls.NumberBoxSpinButtonPlacementMode.Hidden)
                .AutomationName($"{property.DisplayName} Z")
                .Grid(row: 0, column: 6)
        ).MinHeight(GhostLayout.ROW_MIN_HEIGHT);
    }
}
