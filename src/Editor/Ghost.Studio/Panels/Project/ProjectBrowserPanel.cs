using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
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
                    TextBlock("\uED25").FontSize(11).Foreground(isSelected ? GhostColors.AccentPrimary : GhostColors.TextSecondary),
                    TextBlock(folder).FontSize(11).Foreground(isSelected ? "#FFFFFF" : GhostColors.TextPrimary)
                ).Padding(horizontal: 8, vertical: 4)
            )
            .Background(isSelected ? GhostColors.CardSelected : "Transparent")
            .CornerRadius(3)
            .OnPointerPressed((_, _) => setSelectedFolder(folder));
        }).ToArray();

        var assetCards = s_sampleAssets.Select(asset =>
            Border(
                VStack(4,
                    Border(
                        TextBlock("\uE7C3").FontSize(24).Foreground(GhostColors.AccentPrimary).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center)
                    )
                    .Background("#1A1C22")
                    .CornerRadius(3)
                    .Height(60),

                    TextBlock(asset).FontSize(10).SemiBold().Foreground(GhostColors.TextPrimary).HAlign(HorizontalAlignment.Center)
                ).Padding(6)
            )
            .Background(GhostColors.CardBackground)
            .WithBorder(GhostColors.SubtleBorder, 1)
            .CornerRadius(4)
            .Width(90)
            .Height(95)
        ).ToArray();

        return Border(
            Grid(
                columns: [GridSize.Px(140), GridSize.Px(1), GridSize.Star()],
                rows: [GridSize.Star()],

                Border(
                    VStack(2,
                        TextBlock("Assets").FontSize(11).SemiBold().Foreground(GhostColors.TextSecondary).Margin(bottom: 4),
                        VStack(2, folderButtons)
                    ).Padding(6)
                ).Background(GhostColors.PanelBackground).Grid(row: 0, column: 0),

                Border(Empty()).Background(GhostColors.SubtleBorder).Grid(row: 0, column: 1),

                Border(
                    VStack(6,
                        HStack(6,
                            TextBlock($"Assets > {selectedFolder}").FontSize(11).SemiBold().Foreground(GhostColors.TextSecondary)
                        ).Padding(horizontal: 8, vertical: 4),

                        ScrollView(
                            HStack(8, assetCards).Padding(8)
                        )
                    )
                ).Background(GhostColors.PanelBackground).Grid(row: 0, column: 2)
            )
        ).Background(GhostColors.PanelBackground);
    }
}
