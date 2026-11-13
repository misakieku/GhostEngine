using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Studio.DesignSystem;
using Ghost.Studio.Menus;
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
        var selection = EditorSelection.Instance;
        var (_, setRevision) = UseState(0);
        var (isPlaying, setIsPlaying) = UseState(false);

        UseEffect(() =>
        {
            var rev = 0;
            Action onChanged = () => setRevision(++rev);
            history.HistoryChanged += onChanged;
            selection.SelectionChanged += onChanged;
            return () =>
            {
                history.HistoryChanged -= onChanged;
                selection.SelectionChanged -= onChanged;
            };
        }, Array.Empty<object>());

        var undoText = history.CanUndo ? $"Undo ({history.NextUndoName})" : "Undo";
        var redoText = history.CanRedo ? $"Redo ({history.NextRedoName})" : "Redo";

        return Border(
            Grid(
                columns: new GridSize[] { GridSize.Auto, GridSize.Star(), GridSize.Auto },
                rows: new GridSize[] { GridSize.Auto },

                HStack(8,
                    TextBlock("\uE7C3").FontFamily("Segoe Fluent Icons").FontSize(14).Foreground(GhostColors.AccentPrimary).VAlign(VerticalAlignment.Center),
                    TextBlock("GhostEngine Studio").FontSize(12).SemiBold().Foreground(GhostColors.TextPrimary).VAlign(VerticalAlignment.Center),

                    Border(Empty()).Width(1).Height(16).Background(GhostColors.SubtleBorder).VAlign(VerticalAlignment.Center),

                    HStack(2,
                        MenuBtn("File"),
                        MenuBtn("Edit"),
                        Button("Entity", () => {}).SubtleButton().WithFlyout(EditorMenuBuilder.BuildFlyout("Entity", new MenuContext())),
                        MenuBtn("Window"),
                        MenuBtn("Help")
                    ),

                    Border(Empty()).Width(1).Height(16).Background(GhostColors.SubtleBorder).VAlign(VerticalAlignment.Center),

                    Button(undoText, () => history.Undo()).IsEnabled(history.CanUndo).SubtleButton(),
                    Button(redoText, () => history.Redo()).IsEnabled(history.CanRedo).SubtleButton()
                ).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 0),

                HStack(4,
                    Button(Icon(FontIcon(isPlaying ? "\uE769" : "\uE768", fontSize: 12)), () => setIsPlaying(!isPlaying))
                        .Foreground(isPlaying ? GhostColors.StatusWarning : GhostColors.StatusSuccess)
                        .AutomationName(isPlaying ? "Pause" : "Play"),
                    Button(Icon(FontIcon("\uE71A", fontSize: 12)), () => setIsPlaying(false))
                        .Foreground(GhostColors.TextSecondary)
                        .AutomationName("Stop")
                ).HAlign(HorizontalAlignment.Center).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 1),

                HStack(8,
                    Border(
                        TextBlock("FPS 60").FontSize(11).SemiBold().Foreground(GhostColors.StatusSuccess).VAlign(VerticalAlignment.Center)
                    ).Background(GhostColors.CardBackground).WithBorder(GhostColors.CardBorder, 1).Padding(horizontal: 8, vertical: 3).CornerRadius(4)
                ).HAlign(HorizontalAlignment.Right).VAlign(VerticalAlignment.Center).Grid(row: 0, column: 2)
            ).Padding(horizontal: 10, vertical: 4)
        )
        .Background(GhostColors.PanelBackground)
        .WithBorder(GhostColors.SubtleBorder, 1);
    }

    private static Element MenuBtn(string text) =>
        Button(text, () => {}).SubtleButton();
}
