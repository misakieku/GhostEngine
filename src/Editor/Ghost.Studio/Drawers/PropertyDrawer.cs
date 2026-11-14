using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public abstract class PropertyDrawer
{
    protected readonly record struct AxisNumberSpec(string Axis, ThemeRef Color, double Value, Action<double> OnValueChanged);

    public abstract Element Draw(PropertyNode property, DrawerContext context);

    protected static Element DrawPropertyLabel(PropertyNode property)
    {
        return TextBlock(property.DisplayName)
            .Foreground(GhostColors.TextSecondary)
            .FontSize(12)
            .TextTrimming(TextTrimming.CharacterEllipsis)
            .MaxLines(1)
            .VAlign(VerticalAlignment.Center)
            .Padding(left: GhostLayout.PROPERTY_LEFT_PADDING, right: GhostLayout.CONTROL_GAP)
            .ToolTip(property.DisplayName);
    }

    protected static Element DrawLabeledRow(PropertyNode property, Element editor)
    {
        return (FlexRow(
            DrawPropertyLabel(property).Flex(basis: GhostLayout.PROPERTY_LABEL_WIDTH, shrink: 0, minWidth: 0),
            editor.Flex(grow: 1, basis: 0, minWidth: 0)
        ) with
        {
            AlignItems = FlexAlign.Center
        }).MinHeight(GhostLayout.ROW_MIN_HEIGHT);
    }

    protected static Element DrawAxisNumberRow(PropertyNode property, params AxisNumberSpec[] axes)
    {
        var axisGroups = new Element[axes.Length];
        for (var i = 0; i < axes.Length; i++)
        {
            var spec = axes[i];
            axisGroups[i] = (FlexRow(
                TextBlock(spec.Axis).Foreground(spec.Color).FontSize(11).SemiBold().Flex(shrink: 0),
                NumberBox(spec.Value, spec.OnValueChanged)
                    .SpinButtons(NumberBoxSpinButtonPlacementMode.Hidden)
                    .AutomationName($"{property.DisplayName} {spec.Axis}")
                    .Flex(grow: 1, basis: 0, minWidth: 0)
            ) with
            {
                AlignItems = FlexAlign.Center,
                ColumnGap = 4
            }).Flex(grow: 1, basis: 0, minWidth: 0);
        }

        var axesRow = FlexRow(axisGroups) with
        {
            AlignItems = FlexAlign.Center,
            ColumnGap = GhostLayout.CONTROL_GAP
        };

        return DrawLabeledRow(property, axesRow);
    }
}
