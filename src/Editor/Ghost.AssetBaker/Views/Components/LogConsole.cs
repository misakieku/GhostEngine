using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Reactor.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.AssetBaker.Views.Components;

public record LogConsoleProps(
    IReadOnlyList<string> Logs
);

public class LogConsole : Component<LogConsoleProps>
{
    public override Element Render()
    {
        var logs = Props.Logs;
        var textBoxRef = UseMemo(() => TypedElementRef.Create<TextBox>(), Array.Empty<object>());

        // Join logs into a single string
        var stringBuilder = new StringBuilder();
        foreach (var log in logs)
        {
            stringBuilder.AppendLine(log);
        }
        var text = stringBuilder.ToString();

        // Auto-scroll to the end whenever logs change
        UseEffect(() =>
        {
            if (textBoxRef.Current is { } tb)
            {
                // Move selection/focus caret to the end to trigger auto-scroll
                tb.SelectionStart = tb.Text.Length;
                tb.SelectionLength = 0;
            }
        }, logs.Count);

        return FlexColumn(
            // Header for Console
            (Border(
                FlexRow(
                    Icon(FontIcon("\uE756")) // Command prompt icon
                        .VAlign(VerticalAlignment.Center),
                    BodyStrong("Baking Log Console")
                        .VAlign(VerticalAlignment.Center)
                ) with { ColumnGap = 8 }
            ) with
            {
                BorderThickness = 1,
                ThemeBindings = new Dictionary<string, ThemeRef> { { "BorderBrush", Theme.CardStroke } }
            })
            .Padding(horizontal: 16, vertical: 8)
            .Background(Theme.ControlFillSecondary)
            .CornerRadius(6, 6, 0, 0)
            .Flex(shrink: 0),

            // Monospaced dark text area
            TextBox(text)
                .IsReadOnly(true)
                .AcceptsReturn(true)
                .TextWrapping(TextWrapping.Wrap)
                .FontFamily("Consolas")
                .FontSize(12)
                .Padding(12)
                .AutomationName("Baking Log Console TextBox")
                .Resources(r => r
                    .Set("TextControlBackground", Theme.Ref("ConsoleBackgroundBrush"))
                    .Set("TextControlBackgroundPointerOver", Theme.Ref("ConsoleBackgroundBrush"))
                    .Set("TextControlBackgroundFocused", Theme.Ref("ConsoleBackgroundBrush"))
                    .Set("TextControlBackgroundDisabled", Theme.Ref("ConsoleBackgroundBrush"))
                    .Set("TextControlForeground", Theme.Ref("ConsoleForegroundBrush"))
                    .Set("TextControlForegroundPointerOver", Theme.Ref("ConsoleForegroundBrush"))
                    .Set("TextControlForegroundFocused", Theme.Ref("ConsoleForegroundBrush"))
                    .Set("TextControlForegroundDisabled", Theme.Ref("ConsoleForegroundBrush"))
                    .Set("TextControlBorderBrush", Theme.CardStroke)
                    .Set("TextControlBorderBrushPointerOver", Theme.CardStroke)
                    .Set("TextControlBorderBrushFocused", Theme.CardStroke)
                )
                .Ref(textBoxRef)
                .CornerRadius(0, 0, 6, 6)
                .Flex(grow: 1, basis: 0)
        )
        .Flex(grow: 1, basis: 0);
    }
}

