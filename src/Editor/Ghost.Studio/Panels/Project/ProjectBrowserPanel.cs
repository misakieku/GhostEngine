using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Panels.Project;

public sealed class ProjectBrowserPanel : Component
{
    private static readonly string[] s_folders = ["Scenes", "Materials", "Meshes", "Textures", "Prefabs", "Scripts"];
    private static readonly string[] s_sampleAssets = ["MainScene", "TestLevel", "DungeonRoom", "LightingDemo"];

    public override Element Render()
    {
        var (selectedFolder, setSelectedFolder) = UseState("Scenes");

        var folderButtons = s_folders.Select(folder =>
        {
            var isSelected = folder == selectedFolder;
            return Border(
                HStack(6,
                    TextBlock("\uED25").FontFamily("Segoe Fluent Icons").FontSize(12).Foreground(isSelected ? GhostColors.TextOnAccent : GhostColors.TextSecondary).VAlign(VerticalAlignment.Center),
                    TextBlock(folder).FontSize(12).Foreground(isSelected ? GhostColors.TextOnAccent : GhostColors.TextPrimary).VAlign(VerticalAlignment.Center)
                ).Padding(horizontal: 8, vertical: 5)
            )
            .Background(isSelected ? GhostColors.CardSelected : GhostColors.TransparentHitTarget)
            .CornerRadius(4)
            .OnPointerPressed((_, _) => setSelectedFolder(folder));
        }).ToArray();

        var assetCards = s_sampleAssets.Select(asset =>
            Border(
                VStack(4,
                    Border(
                        TextBlock("\uE7C3").FontFamily("Segoe Fluent Icons").FontSize(24).Foreground(GhostColors.AccentPrimary).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center)
                    )
                    .Background(GhostColors.RootBackground)
                    .CornerRadius(3)
                    .Height(60),

                    TextBlock(asset).FontSize(11).SemiBold().Foreground(GhostColors.TextPrimary).HAlign(HorizontalAlignment.Center)
                ).Padding(6)
            )
            .Background(GhostColors.CardBackground)
            .WithBorder(GhostColors.CardBorder, 1)
            .CornerRadius(4)
            .Width(96)
            .MinHeight(95)
        ).ToArray();

        return Border(
            FlexRow(
                Border(
                    VStack(2,
                        TextBlock("Assets").FontSize(11).SemiBold().Foreground(GhostColors.TextSecondary).Margin(bottom: 4),
                        VStack(2, folderButtons)
                    ).Padding(6)
                ).Background(GhostColors.PanelBackground).Flex(basis: 170, shrink: 0),

                Border(Empty()).Width(1).Background(GhostColors.SubtleBorder).Flex(shrink: 0),

                Border(
                    FlexColumn(
                        HStack(6,
                            TextBlock($"Assets > {selectedFolder}").FontSize(11).SemiBold().Foreground(GhostColors.TextSecondary)
                        ).Padding(horizontal: 8, vertical: 4).Flex(shrink: 0),

                        ScrollView(
                            FlexRow(assetCards) with { Wrap = FlexWrap.Wrap, ColumnGap = 8, RowGap = 8, FlexPadding = new Thickness(8) }
                        ).Flex(grow: 1, basis: 0, minHeight: 0, minWidth: 0)
                    )
                ).Background(GhostColors.PanelBackground).Flex(grow: 1, basis: 0, minWidth: 0)
            )
        ).Background(GhostColors.PanelBackground);
    }
}
