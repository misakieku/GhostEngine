using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Panels.Console;

public sealed class ConsolePanel : Component
{
    private static readonly string[] s_sampleLogs =
    [
        "[Info] GhostEngine Studio initialized successfully.",
        "[Info] D3D12 Render Hardware Interface adapter detected: NVIDIA GeForce RTX.",
        "[Info] Asset database indexed 48 assets in 12ms.",
        "[Warning] Entity #4 has missing Material binding, using fallback magenta material.",
        "[Info] ECS World ticking at 60 Hz."
    ];

    public override Element Render()
    {
        var logRows = s_sampleLogs.Select(msg =>
        {
            var isWarn = msg.Contains("[Warning]");
            var isError = msg.Contains("[Error]");
            var color = isError ? GhostColors.StatusError : (isWarn ? GhostColors.StatusWarning : GhostColors.TextSecondary);
            var icon = isError ? "\uEA39" : (isWarn ? "\uE7BA" : "\uE946");

            return HStack(6,
                TextBlock(icon).FontFamily("Segoe Fluent Icons").FontSize(12).Foreground(color).VAlign(VerticalAlignment.Center),
                TextBlock(msg).FontSize(12).Foreground(GhostColors.TextPrimary).VAlign(VerticalAlignment.Center)
            ).Padding(horizontal: 6, vertical: 2);
        }).ToArray();

        return Border(
            VStack(4,
                HStack(8,
                    Button("Clear", () => {}),
                    TextBlock("5 Messages").FontSize(11).Foreground(GhostColors.TextTertiary).VAlign(VerticalAlignment.Center)
                ).Padding(horizontal: 8, vertical: 4),

                Border(Empty()).Height(1).Background(GhostColors.SubtleBorder),

                ScrollView(
                    VStack(2, logRows)
                ).Padding(4)
            )
        ).Background(GhostColors.PanelBackground);
    }
}
