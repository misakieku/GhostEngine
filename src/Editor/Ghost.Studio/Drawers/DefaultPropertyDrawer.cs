using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class DefaultPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var valStr = property.Value?.ToString() ?? "null";
        var valueText = TextBlock(valStr)
            .Foreground(GhostColors.TextPrimary)
            .FontSize(12)
            .TextTrimming(TextTrimming.CharacterEllipsis)
            .MaxLines(1)
            .VAlign(VerticalAlignment.Center)
            .ToolTip(valStr);

        return DrawLabeledRow(property, valueText);
    }
}
