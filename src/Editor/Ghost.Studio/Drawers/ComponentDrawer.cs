using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public abstract class ComponentDrawer
{
    public abstract Element Draw(ComponentNode component, DrawerContext context);

    protected static Element BuildComponentCard(ComponentNode component, string title, string iconGlyph, Element body, DrawerContext context, Element? headerExtra = null)
    {
        var removeBtn = Button(Icon(FontIcon("\uE74D", fontSize: 12)), () => context.History.Execute(new RemoveComponentCommand(component.EntityNode, component)))
            .SubtleButton()
            .AutomationName($"Remove {title}")
            .ToolTip("Remove Component");

        Element headerActions = headerExtra != null
            ? HStack(GhostLayout.CONTROL_GAP, headerExtra, removeBtn)
            : removeBtn;

        var headerSection = Border(
            Grid(
                columns: [GridSize.Auto, GridSize.Star(), GridSize.Auto],
                rows: [GridSize.Auto],
                TextBlock(iconGlyph).FontFamily("Segoe Fluent Icons").FontSize(12).Foreground(GhostColors.AccentPrimary).VAlign(VerticalAlignment.Center).Margin(right: GhostLayout.CONTROL_GAP).Grid(row: 0, column: 0),
                TextBlock(title).FontSize(12).SemiBold().Foreground(GhostColors.TextPrimary).TextTrimming(TextTrimming.CharacterEllipsis).MaxLines(1).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 1),
                headerActions.VAlign(VerticalAlignment.Center).Margin(left: GhostLayout.CONTROL_GAP).Grid(row: 0, column: 2)
            )
        )
        .Background(GhostColors.CardHeaderBackground)
        .Padding(horizontal: GhostLayout.CARD_HEADER_PADDING_H, vertical: GhostLayout.CARD_HEADER_PADDING_V);

        return Border(
            VStack(0,
                headerSection,
                Border(Empty()).Height(1).Background(GhostColors.SubtleBorder),
                Border(body).Padding(GhostLayout.CARD_PADDING)
            )
        )
        .Background(GhostColors.CardBackground)
        .WithBorder(GhostColors.CardBorder, 1)
        .CornerRadius(GhostLayout.CORNER_RADIUS);
    }

    protected static Element DrawPropertyRows(IReadOnlyList<PropertyNode> properties, DrawerContext context)
    {
        var elements = new Element[properties.Count];
        for (var i = 0; i < properties.Count; i++)
        {
            elements[i] = context.DrawProperty(properties[i]);
        }

        return VStack(GhostLayout.ROW_GAP, elements);
    }

    protected static Element DrawSection(string header, IReadOnlyList<PropertyNode> properties, DrawerContext context)
    {
        return VStack(GhostLayout.ROW_GAP,
            TextBlock(header).FontSize(11).SemiBold().Foreground(GhostColors.TextTertiary).Margin(top: GhostLayout.SECTION_HEADER_TOP),
            DrawPropertyRows(properties, context)
        );
    }
}
