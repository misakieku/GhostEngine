using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Panels.Viewport;

public sealed class ViewportPanel : Component
{
    public override Element Render()
    {
        var (gizmoMode, setGizmoMode) = UseState("Translate");
        var (coordSpace, setCoordSpace) = UseState("World");
        var (shadingMode, setShadingMode) = UseState("Lit");

        return Border(
            Grid(
                columns: [GridSize.Star()],
                rows: [GridSize.Star()],

                Border(
                    VStack(8,
                        TextBlock("\uE7C5").FontSize(32).Foreground(GhostColors.TextTertiary).HAlign(HorizontalAlignment.Center),
                        TextBlock("D3D12 Viewport").FontSize(14).SemiBold().Foreground(GhostColors.TextSecondary).HAlign(HorizontalAlignment.Center),
                        TextBlock("Ready for SwapChainPanel Presentation").FontSize(11).Foreground(GhostColors.TextTertiary).HAlign(HorizontalAlignment.Center)
                    ).VAlign(VerticalAlignment.Center)
                ).Background("#0B0C0E").Grid(row: 0, column: 0),

                Border(
                    HStack(4,
                        ToolBtn("\uE7C4", "Select", gizmoMode, setGizmoMode),
                        ToolBtn("\uE7C0", "Translate", gizmoMode, setGizmoMode),
                        ToolBtn("\uE7AD", "Rotate", gizmoMode, setGizmoMode),
                        ToolBtn("\uE740", "Scale", gizmoMode, setGizmoMode),
                        Border(Empty()).Width(1).Height(16).Background(GhostColors.SubtleBorder).Margin(horizontal: 4, vertical: 0),
                        Button(coordSpace, () => setCoordSpace(coordSpace == "World" ? "Local" : "World")).Height(22).FontSize(10)
                    ).Padding(4)
                )
                .Background("#E018191E")
                .CornerRadius(4)
                .WithBorder(GhostColors.SubtleBorder, 1)
                .HAlign(HorizontalAlignment.Left)
                .VAlign(VerticalAlignment.Top)
                .Margin(8)
                .Grid(row: 0, column: 0),

                Border(
                    HStack(4,
                        Button(shadingMode, () => setShadingMode(shadingMode switch
                        {
                            "Lit" => "Unlit",
                            "Unlit" => "Wireframe",
                            _ => "Lit"
                        })).Height(22).FontSize(10)
                    ).Padding(4)
                )
                .Background("#E018191E")
                .CornerRadius(4)
                .WithBorder(GhostColors.SubtleBorder, 1)
                .HAlign(HorizontalAlignment.Left)
                .VAlign(VerticalAlignment.Bottom)
                .Margin(8)
                .Grid(row: 0, column: 0)
            )
        ).Background(GhostColors.RootBackground);
    }

    private static Element ToolBtn(string icon, string mode, string currentMode, Action<string> setMode)
    {
        var isActive = mode == currentMode;
        return Button(icon, () => setMode(mode))
            .Height(22).Width(26).FontSize(11)
            .Foreground(isActive ? GhostColors.AccentPrimary : GhostColors.TextSecondary);
    }
}
