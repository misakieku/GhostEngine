using Ghost.Editor.Core.Commands;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Panels.TitleBar;

public sealed class EditorTitleBar : Component
{
    public override Element Render()
    {
        var history = CommandHistory.Instance;
        var (revision, setRevision) = UseState(0);
        var (isPlaying, setIsPlaying) = UseState(false);

        UseEffect(() =>
        {
            Action onHistoryChanged = () => setRevision(revision + 1);
            history.HistoryChanged += onHistoryChanged;
            return () => history.HistoryChanged -= onHistoryChanged;
        }, Array.Empty<object>());

        var undoText = history.CanUndo ? $"Undo ({history.NextUndoName})" : "Undo";
        var redoText = history.CanRedo ? $"Redo ({history.NextRedoName})" : "Redo";

        return Border(
            Grid(
                columns: [GridSize.Auto, GridSize.Star(), GridSize.Auto],
                rows: [GridSize.Star()],

                HStack(10,
                    TextBlock("\uE7C3").FontSize(14).Foreground(GhostColors.AccentPrimary).VAlign(VerticalAlignment.Center),
                    TextBlock("GhostEngine Studio").FontSize(12).SemiBold().Foreground(GhostColors.TextPrimary).VAlign(VerticalAlignment.Center),

                    Border(Empty()).Width(1).Height(14).Background(GhostColors.SubtleBorder).VAlign(VerticalAlignment.Center),

                    HStack(4,
                        MenuBtn("File"),
                        MenuBtn("Edit"),
                        MenuBtn("Entity"),
                        MenuBtn("Window"),
                        MenuBtn("Help")
                    ),

                    Border(Empty()).Width(1).Height(14).Background(GhostColors.SubtleBorder).VAlign(VerticalAlignment.Center),

                    Button(undoText, () => history.Undo()).IsEnabled(history.CanUndo).Height(22).FontSize(10),
                    Button(redoText, () => history.Redo()).IsEnabled(history.CanRedo).Height(22).FontSize(10)
                ).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 0),

                HStack(4,
                    Button(isPlaying ? "\uE769" : "\uE768", () => setIsPlaying(!isPlaying))
                        .Height(24).Width(32).FontSize(11)
                        .Foreground(isPlaying ? GhostColors.StatusWarning : GhostColors.StatusSuccess),
                    Button("\uE71A", () => setIsPlaying(false))
                        .Height(24).Width(32).FontSize(11)
                        .Foreground(GhostColors.TextSecondary)
                ).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 1),

                HStack(8,
                    Border(
                        TextBlock("FPS 60").FontSize(10).Foreground(GhostColors.StatusSuccess).VAlign(VerticalAlignment.Center)
                    ).Background(GhostColors.CardBackground).Padding(horizontal: 6, vertical: 2).CornerRadius(3)
                ).HAlign(HorizontalAlignment.Right).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 2)
            ).Padding(horizontal: 10, vertical: 4)
        )
        .Height(36)
        .Background(GhostColors.RootBackground)
        .WithBorder(GhostColors.SubtleBorder, 1);
    }

    private static Element MenuBtn(string text) =>
        Button(text, () => {}).Height(22).FontSize(11).Foreground(GhostColors.TextSecondary);
}
