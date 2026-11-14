using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Workspace;

/// <summary>
/// Input properties for <see cref="StudioTabPane"/>.
/// </summary>
public sealed record StudioTabPaneProps(IReadOnlyList<StudioTabItem> Tabs, string ActiveTabKey, Action<string> OnSelectTab);

/// <summary>
/// Lightweight tabbed panel host with studio-consistent tab strip chrome.
/// </summary>
public sealed class StudioTabPane : Component<StudioTabPaneProps>
{
    public override Element Render()
    {
        var (hoveredKey, setHoveredKey) = UseState<string?>(null);
        var tabs = Props.Tabs;
        var activeTab = FindActiveTab(tabs, Props.ActiveTabKey);

        var tabButtons = new Element[tabs.Count];
        for (var i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            var isActive = tab.Key == activeTab.Key;
            var isHovered = tab.Key == hoveredKey;
            var key = tab.Key;

            var label = TextBlock(tab.Title).FontSize(12).Foreground(isActive ? GhostColors.TextPrimary : GhostColors.TextSecondary).VAlign(VerticalAlignment.Center);
            if (isActive)
            {
                label = label.SemiBold();
            }

            tabButtons[i] = Border(
                HStack(6,
                    TextBlock(tab.IconGlyph).FontFamily("Segoe Fluent Icons").FontSize(12).Foreground(isActive ? GhostColors.AccentPrimary : GhostColors.TextSecondary).VAlign(VerticalAlignment.Center),
                    label
                )
            )
            .Background(isActive ? GhostColors.PanelBackground : (isHovered ? GhostColors.SubtleSelected : GhostColors.TransparentHitTarget))
            .WithBorder(isActive ? GhostColors.CardBorder : GhostColors.TransparentHitTarget)
            .BorderThickness(left: 1, top: 1, right: 1, bottom: 0)
            .CornerRadius(GhostLayout.CORNER_RADIUS, GhostLayout.CORNER_RADIUS, 0, 0)
            .Padding(horizontal: 10, vertical: 5)
            .WithKey($"tab_{key}")
            .AutomationName(tab.Title)
            .OnPointerEntered((_, _) => setHoveredKey(key))
            .OnPointerExited((_, _) => setHoveredKey(null))
            .OnPointerPressed((_, _) => Props.OnSelectTab(key));
        }

        var tabStrip = Border(
            FlexRow(tabButtons) with
            {
                AlignItems = FlexAlign.FlexEnd,
                ColumnGap = 2,
                FlexPadding = new Thickness(6, 4, 6, 0)
            }
        )
        .Background(GhostColors.CardHeaderBackground)
        .Flex(shrink: 0);

        return Border(
            FlexColumn(
                tabStrip,
                Border(Empty()).Height(1).Background(GhostColors.SubtleBorder).Flex(shrink: 0),
                Border(activeTab.ContentFactory().WithKey(activeTab.Key)).Background(GhostColors.PanelBackground).Flex(grow: 1, basis: 0, minHeight: 0, minWidth: 0)
            )
        )
        .Background(GhostColors.PanelBackground)
        .WithBorder(GhostColors.SubtleBorder, 1)
        .CornerRadius(GhostLayout.CORNER_RADIUS);
    }

    private static StudioTabItem FindActiveTab(IReadOnlyList<StudioTabItem> tabs, string activeKey)
    {
        for (var i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].Key == activeKey)
            {
                return tabs[i];
            }
        }

        return tabs[0];
    }
}
