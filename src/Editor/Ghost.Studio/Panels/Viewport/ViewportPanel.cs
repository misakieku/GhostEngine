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
                        TextBlock("\uE7C5").FontFamily("Segoe Fluent Icons").FontSize(32).Foreground(GhostColors.TextTertiary).HAlign(HorizontalAlignment.Center),
                        TextBlock("D3D12 Viewport").FontSize(14).SemiBold().Foreground(GhostColors.TextSecondary).HAlign(HorizontalAlignment.Center),
                        TextBlock("Ready for SwapChainPanel Presentation").FontSize(12).Foreground(GhostColors.TextTertiary).HAlign(HorizontalAlignment.Center)
                    ).VAlign(VerticalAlignment.Center)
                ).Background(GhostColors.RootBackground).Grid(row: 0, column: 0),

                Border(
                    HStack(4,
                        ToolBtn("\uE7C4", "Select", gizmoMode, setGizmoMode),
                        ToolBtn("\uE7C0", "Translate", gizmoMode, setGizmoMode),
                        ToolBtn("\uE7AD", "Rotate", gizmoMode, setGizmoMode),
                        ToolBtn("\uE740", "Scale", gizmoMode, setGizmoMode),
                        Border(Empty()).Width(1).Height(16).Background(GhostColors.SubtleBorder).Margin(horizontal: 4, vertical: 0),
                        Button(coordSpace, () => setCoordSpace(coordSpace == "World" ? "Local" : "World"))
                    ).Padding(4)
                )
                .Background(GhostColors.OverlayBackground)
                .CornerRadius(4)
                .WithBorder(GhostColors.OverlayBorder, 1)
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
                        }))
                    ).Padding(4)
                )
                .Background(GhostColors.OverlayBackground)
                .CornerRadius(4)
                .WithBorder(GhostColors.OverlayBorder, 1)
                .HAlign(HorizontalAlignment.Left)
                .VAlign(VerticalAlignment.Bottom)
                .Margin(8)
                .Grid(row: 0, column: 0)
            )
        ).Background(GhostColors.PanelBackground);
    }

    private static Element ToolBtn(string icon, string mode, string currentMode, Action<string> setMode)
    {
        var isActive = mode == currentMode;
        var btn = Button(Icon(FontIcon(icon, fontSize: 12)), () => setMode(mode)).AutomationName(mode);
        return isActive ? btn.AccentButton() : btn.SubtleButton();
    }
}
