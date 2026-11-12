using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Panels.Profiler;

public sealed class ProfilerPanel : Component
{
    public override Element Render()
    {
        var snapshot = EditorSyncBridge.Instance.LatestSnapshot;
        var fps = snapshot?.Fps ?? 60.0f;
        var frameTime = snapshot?.FrameTimeMs ?? 16.6f;
        var entityCount = snapshot?.EntityCount ?? 0;

        return Border(
            VStack(8,
                TextBlock("Telemetry & Performance").FontSize(11).SemiBold().Foreground(GhostColors.TextSecondary),
                HStack(12,
                    StatCard("FPS", $"{fps:F0}", GhostColors.StatusSuccess),
                    StatCard("Frame Time", $"{frameTime:F1} ms", GhostColors.AccentPrimary),
                    StatCard("Entities", $"{entityCount}", GhostColors.TextPrimary),
                    StatCard("Draw Calls", "24", GhostColors.TextSecondary),
                    StatCard("GPU VRAM", "512 MB", GhostColors.TextSecondary)
                )
            ).Padding(8)
        ).Background(GhostColors.PanelBackground);
    }

    private static Element StatCard(string title, string value, string color)
    {
        return Border(
            VStack(2,
                TextBlock(title).FontSize(9).Foreground(GhostColors.TextTertiary),
                TextBlock(value).FontSize(13).SemiBold().Foreground(color)
            ).Padding(horizontal: 8, vertical: 4)
        )
        .Background(GhostColors.CardBackground)
        .WithBorder(GhostColors.SubtleBorder, 1)
        .CornerRadius(4);
    }
}
