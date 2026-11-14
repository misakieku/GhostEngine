using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Menus;
using Ghost.Editor.Core.Selection;
using Ghost.Studio.DesignSystem;
using Ghost.Studio.Menus;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Layout;
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
            FlexRow(
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
                ).Flex(shrink: 0),

                (FlexRow(
                    HStack(4,
                        Button(Icon(FontIcon(isPlaying ? "\uE769" : "\uE768", fontSize: 12)), () => setIsPlaying(!isPlaying))
                            .Foreground(isPlaying ? GhostColors.StatusWarning : GhostColors.StatusSuccess)
                            .AutomationName(isPlaying ? "Pause" : "Play")
                            .CornerRadius(4),
                        Button(Icon(FontIcon("\uE71A", fontSize: 12)), () => setIsPlaying(false))
                            .Foreground(GhostColors.TextSecondary)
                            .AutomationName("Stop")
                            .CornerRadius(4)
                    )
                ) with
                {
                    JustifyContent = FlexJustify.Center,
                    AlignItems = FlexAlign.Center
                }).Flex(grow: 1, basis: 0, minWidth: 0)
            ) with
            {
                AlignItems = FlexAlign.Center,
                ColumnGap = 8,
                FlexPadding = new Thickness(10, 4, 10, 4)
            }
        )
        .Background(GhostColors.PanelBackground)
        .WithBorder(GhostColors.SubtleBorder, 1);
    }

    private static Element MenuBtn(string text) =>
        Button(text, () => {}).SubtleButton();
}
